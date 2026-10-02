using System;
using System.Collections.Generic;

namespace RehabTracking.Web.Entities;

/// <summary>
/// Nhật ký phục hồi & đánh giá tiến độ sau mỗi buổi tập
/// </summary>
public partial class RecoveryLog
{
    public int LogId { get; set; }

    public int PatientId { get; set; }

    public int? ExerciseId { get; set; }

    /// <summary>Ngày giờ thực hiện</summary>
    public DateTime LogDate { get; set; } = DateTime.UtcNow;

    /// <summary>Thang điểm đau VAS trước khi tập (0: Không đau, 10: Đau dữ dội)</summary>
    public int PainPreWorkout { get; set; } = 0;

    /// <summary>Thang điểm đau VAS sau khi tập (0: Không đau, 10: Đau dữ dội)</summary>
    public int PainScoreVAS { get; set; }

    /// <summary>Thời gian ngủ của ngày hôm đó (giờ)</summary>
    public double? SleepHours { get; set; }

    /// <summary>Tâm trạng: Vui vẻ, Tích cực, Bình thường, Mệt mỏi, Lo lắng, Căng thẳng</summary>
    public string? MoodLevel { get; set; } = "Bình thường";

    /// <summary>Biên độ vận động đo được (độ góc - Range of Motion)</summary>
    public double? ROMMeasurement { get; set; }

    /// <summary>Mức độ mệt mỏi/căng cơ (1: Rất nhẹ, 5: Rất kiệt sức)</summary>
    public int FatigueLevel { get; set; } = 2;

    /// <summary>Tỷ lệ hoàn thành buổi tập (0 - 100%)</summary>
    public double CompletionRate { get; set; } = 100.0;

    /// <summary>Thời gian tập luyện thực tế (phút)</summary>
    public int DurationMinutes { get; set; } = 15;

    /// <summary>Ghi chú cảm nhận riêng của bệnh nhân</summary>
    public string? PatientNote { get; set; }

    /// <summary>Đường dẫn hình ảnh hoặc video tự kiểm tra tư thế (JSON Array)</summary>
    public string? MediaUrlsJson { get; set; }

    /// <summary>Bác sĩ có phản hồi hay chưa</summary>
    public string? DoctorFeedback { get; set; }

    public DateTime? FeedbackAt { get; set; }

    public int? DoctorId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual PatientProfile Patient { get; set; } = null!;

    public virtual Exercise? Exercise { get; set; }

    public virtual User? Doctor { get; set; }
}
