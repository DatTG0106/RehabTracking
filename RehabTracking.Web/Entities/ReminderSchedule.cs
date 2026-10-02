using System;

namespace RehabTracking.Web.Entities;

/// <summary>
/// Cấu hình lịch nhắc nhở cá nhân hóa (Tập luyện, uống thuốc, đo biên độ khớp, uống nước)
/// </summary>
public class ReminderSchedule
{
    public int ReminderId { get; set; }

    public int PatientId { get; set; }

    public string Title { get; set; } = "Nhắc nhở tập vật lý trị liệu";

    /// <summary>
    /// Loại nhắc: Exercise (Tập luyện), Medication (Uống thuốc), Measurement (Đo ROM/Huyết áp), Hydration (Uống nước)
    /// </summary>
    public string ReminderType { get; set; } = "Exercise";

    /// <summary>
    /// Thời điểm nhắc trong ngày (ví dụ: 08:30:00, 17:00:00)
    /// </summary>
    public TimeSpan TimeOfDay { get; set; } = new TimeSpan(8, 0, 0);

    /// <summary>
    /// Các ngày trong tuần lặp lại (dạng chuỗi phân tách bởi dấu phẩy, ví dụ: Monday,Wednesday,Friday hoặc All)
    /// </summary>
    public string DaysOfWeek { get; set; } = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday";

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Thời điểm gửi thông báo gần nhất để tránh gửi trùng lặp trong cùng 1 khung giờ
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual User Patient { get; set; } = null!;
}
