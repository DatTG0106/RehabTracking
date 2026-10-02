using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RehabTracking.Web.Services;

namespace RehabTracking.Web.Features.Healthcare.Notifications;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppNotificationService _notificationService;

    public NotificationsController(AppNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out int id) ? id : 0;
    }

    /// <summary>
    /// Lấy danh sách thông báo của người dùng hiện tại
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications([FromQuery] int limit = 20)
    {
        int userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var notifs = await _notificationService.GetNotificationsAsync(userId, limit);
        return Ok(notifs);
    }

    /// <summary>
    /// Lấy số thông báo chưa đọc
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        int userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        int count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(new { unreadCount = count });
    }

    /// <summary>
    /// Đánh dấu thông báo là đã đọc
    /// </summary>
    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        int userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        bool success = await _notificationService.MarkAsReadAsync(id, userId);
        if (!success) return NotFound();

        return Ok(new { success = true });
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo là đã đọc
    /// </summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        int userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new { success = true });
    }
}
