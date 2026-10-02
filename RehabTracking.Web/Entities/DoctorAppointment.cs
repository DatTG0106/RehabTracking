using System;

namespace RehabTracking.Web.Entities;

/// <summary>
/// Quản lý lịch hẹn khám, tư vấn và tái khám giữa Bệnh nhân và Bác sĩ/KTV Vật lý trị liệu
/// </summary>
public class DoctorAppointment
{
    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    /// <summary>
    /// Thời gian diễn ra cuộc hẹn
    /// </summary>
    public DateTime AppointmentDate { get; set; }

    /// <summary>
    /// Thời lượng dự kiến (phút)
    /// </summary>
    public int DurationMinutes { get; set; } = 30;

    /// <summary>
    /// Hình thức/Phân loại: InitialConsultation (Khám khởi đầu), PeriodicReview (Tái khám định kỳ), OnlineROMCheck (Đánh giá ROM từ xa), InPersonClinic (Khám trực tiếp tại phòng khám)
    /// </summary>
    public string AppointmentType { get; set; } = "PeriodicReview";

    /// <summary>
    /// Trạng thái: Pending (Chờ bác sĩ duyệt), Confirmed (Đã xác nhận), Completed (Đã hoàn thành), Cancelled (Đã hủy)
    /// </summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Lý do khám / triệu chứng do bệnh nhân khai báo
    /// </summary>
    public string PatientReason { get; set; } = string.Empty;

    /// <summary>
    /// Ghi chú chuyên môn hoặc kết luận của Bác sĩ sau buổi khám
    /// </summary>
    public string? DoctorNotes { get; set; }

    /// <summary>
    /// Đường dẫn phòng khám trực tuyến (Google Meet, Zoom, WebRTC Telehealth) nếu khám từ xa
    /// </summary>
    public string? MeetingLink { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual User Patient { get; set; } = null!;

    public virtual User Doctor { get; set; } = null!;
}
