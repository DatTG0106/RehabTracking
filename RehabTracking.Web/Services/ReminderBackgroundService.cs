using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Services;

/// <summary>
/// Background Job quét lịch nhắc nhở và cảnh báo bỏ tập định kỳ cho bệnh nhân
/// </summary>
public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(
        IServiceProvider serviceProvider, 
        ILogger<ReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderBackgroundService đã khởi động.");

        // Quét định kỳ mỗi 1 phút bằng PeriodicTimer của .NET 8
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessScheduledRemindersAsync(stoppingToken);
                await ProcessInactivityAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra trong vòng lặp ReminderBackgroundService.");
            }
        }

        _logger.LogInformation("ReminderBackgroundService đang dừng lại.");
    }

    private async Task ProcessScheduledRemindersAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RehabTrackingContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<AppNotificationService>();

        var now = DateTime.UtcNow.AddHours(7); // Giờ địa phương Việt Nam (UTC+7)
        var currentTime = now.TimeOfDay;
        var currentDay = now.DayOfWeek.ToString();

        // Tìm các lịch nhắc hoạt động trong ngày hôm nay
        var activeReminders = await db.ReminderSchedules
            .Where(r => r.IsActive)
            .ToListAsync(stoppingToken);

        foreach (var reminder in activeReminders)
        {
            if (stoppingToken.IsCancellationRequested) break;

            // Kiểm tra ngày trong tuần
            bool dayMatches = reminder.DaysOfWeek.Contains("All", StringComparison.OrdinalIgnoreCase) ||
                              reminder.DaysOfWeek.Contains(currentDay, StringComparison.OrdinalIgnoreCase);

            if (!dayMatches) continue;

            // Kiểm tra thời gian khớp (+/- 3 phút)
            var diffMinutes = Math.Abs((currentTime - reminder.TimeOfDay).TotalMinutes);
            if (diffMinutes <= 3)
            {
                // Kiểm tra xem đã trigger trong 50 phút qua chưa
                if (reminder.LastTriggeredAt == null || (now - reminder.LastTriggeredAt.Value).TotalMinutes >= 50)
                {
                    string actionUrl = reminder.ReminderType switch
                    {
                        "Exercise" => "/patient/workout",
                        "Measurement" => "/patient/recovery-diary",
                        _ => "/patient-dashboard"
                    };

                    string msg = reminder.ReminderType switch
                    {
                        "Exercise" => $"Đã đến giờ {reminder.Title.ToLower()}. Hãy bắt đầu với các động tác khởi động nhẹ nhàng nhé!",
                        "Measurement" => "Đã đến lúc đo lường tầm vận động (ROM) và ghi nhận mức đau để bác sĩ theo dõi.",
                        "Medication" => "Nhắc nhở y tế: Đã đến giờ uống thuốc theo đúng chỉ dẫn của bác sĩ.",
                        _ => reminder.Title
                    };

                    await notificationService.CreateNotificationAsync(
                        reminder.PatientId,
                        reminder.Title,
                        msg,
                        "Reminder",
                        actionUrl);

                    reminder.LastTriggeredAt = now;
                    await db.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("Đã kích hoạt nhắc nhở #{ReminderId} cho bệnh nhân {PatientId}", reminder.ReminderId, reminder.PatientId);
                }
            }
        }
    }

    private async Task ProcessInactivityAlertsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RehabTrackingContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<AppNotificationService>();

        var now = DateTime.UtcNow;
        var thresholdDate = now.AddDays(-2); // Đã 48 giờ chưa tập

        // Tìm các bệnh nhân không tập luyện trong 2-3 ngày
        var inactiveProfiles = await db.GamificationProfiles
            .Include(p => p.User)
            .Where(p => p.LastActivityDate != null && p.LastActivityDate < thresholdDate)
            .Take(20)
            .ToListAsync(stoppingToken);

        foreach (var profile in inactiveProfiles)
        {
            if (stoppingToken.IsCancellationRequested) break;

            // Kiểm tra xem đã gửi thông báo không hoạt động trong 24h qua chưa
            bool recentlyNotified = await db.InAppNotifications
                .AnyAsync(n => n.UserId == profile.UserId 
                            && n.Type == "Reminder" 
                            && n.Title.Contains("Đã 2 ngày bạn chưa tập")
                            && n.CreatedAt >= now.AddHours(-24), stoppingToken);

            if (!recentlyNotified)
            {
                await notificationService.CreateNotificationAsync(
                    profile.UserId,
                    "Nhắc nhở y tế: Đã 2 ngày bạn chưa tập luyện",
                    "Vận động phục hồi chức năng đều đặn là điều kiện tiên quyết để lấy lại tầm vận động. Hãy dành 10-15 phút tập các bài nhẹ hôm nay nhé!",
                    "Reminder",
                    "/patient/workout");

                _logger.LogInformation("Đã gửi cảnh báo không hoạt động cho bệnh nhân {UserId}", profile.UserId);
            }
        }
    }
}
