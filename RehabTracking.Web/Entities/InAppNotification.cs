using System;

namespace RehabTracking.Web.Entities;

/// <summary>
/// Thông báo nội bộ trong ứng dụng (Nhắc lịch tập, duyệt hẹn khám, gamification, cảnh báo an toàn)
/// </summary>
public class InAppNotification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Loại thông báo: Reminder (Nhắc nhở), Appointment (Lịch hẹn), Gamification (Điểm/Huy hiệu), ClinicalAlert (Cảnh báo lâm sàng)
    /// </summary>
    public string Type { get; set; } = "Reminder";

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Đường dẫn điều hướng khi click vào thông báo (ví dụ: /patient/workout, /patient/appointments)
    /// </summary>
    public string? ActionUrl { get; set; }

    public virtual User User { get; set; } = null!;
}
