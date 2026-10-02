using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Services;

public class NotificationDto
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Reminder";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ActionUrl { get; set; }
}

/// <summary>
/// Quản lý thông báo in-app cho Bệnh nhân, Bác sĩ và Quản trị viên
/// </summary>
public class AppNotificationService
{
    private readonly RehabTrackingContext _db;

    public AppNotificationService(RehabTrackingContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy danh sách thông báo của người dùng với số lượng giới hạn
    /// </summary>
    public async Task<List<NotificationDto>> GetNotificationsAsync(int userId, int limit = 20)
    {
        return await _db.InAppNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new NotificationDto
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                ActionUrl = n.ActionUrl
            })
            .ToListAsync();
    }

    /// <summary>
    /// Đếm số thông báo chưa đọc của người dùng
    /// </summary>
    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _db.InAppNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    /// <summary>
    /// Tạo thông báo mới và lưu vào cơ sở dữ liệu
    /// </summary>
    public async Task<InAppNotification> CreateNotificationAsync(
        int userId, 
        string title, 
        string message, 
        string type = "Reminder", 
        string? actionUrl = null)
    {
        var notif = new InAppNotification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            ActionUrl = actionUrl
        };

        _db.InAppNotifications.Add(notif);
        await _db.SaveChangesAsync();
        return notif;
    }

    /// <summary>
    /// Đánh dấu một thông báo là đã đọc
    /// </summary>
    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var notif = await _db.InAppNotifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);
        if (notif == null) return false;

        notif.IsRead = true;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo của người dùng là đã đọc
    /// </summary>
    public async Task MarkAllAsReadAsync(int userId)
    {
        var unread = await _db.InAppNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var item in unread)
        {
            item.IsRead = true;
        }

        await _db.SaveChangesAsync();
    }
}
