namespace RehabTracking.Web.Entities;

/// <summary>
/// DTO mô tả một bài tập trong lộ trình phục hồi.
/// Không ánh xạ trực tiếp vào DB – được serialize thành JSON và lưu vào TreatmentPlan.ExerciseRoutineJson.
/// </summary>
public class ExerciseRoutineItem
{
    public Guid ExerciseId { get; set; } = Guid.NewGuid();

    /// <summary>Tên bài tập (ví dụ: Gập duỗi gối)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Số sets (hiệp)</summary>
    public int Sets { get; set; } = 3;

    /// <summary>Số reps (lần lặp) mỗi set</summary>
    public int Reps { get; set; } = 10;

    /// <summary>Thời gian giữ mỗi rep (giây), nếu là bài giữ tư thế</summary>
    public int DurationSeconds { get; set; } = 5;

    /// <summary>Thời gian nghỉ giữa các sets (giây)</summary>
    public int RestSeconds { get; set; } = 30;

    /// <summary>URL video YouTube mẫu (dạng watch hoặc embed)</summary>
    public string? VideoUrl { get; set; }

    /// <summary>Ghi chú hướng dẫn thêm từ bác sĩ</summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Lịch tập hàng tuần được bác sĩ thiết lập.
/// Lưu JSON vào TreatmentPlan.WorkoutScheduleJson.
/// </summary>
public class WorkoutSchedule
{
    /// <summary>Danh sách thứ trong tuần (0=CN, 1=T2, ..., 6=T7)</summary>
    public List<int> DaysOfWeek { get; set; } = new();

    /// <summary>Giờ nhắc tập (HH:mm)</summary>
    public string ReminderTime { get; set; } = "08:00";

    /// <summary>Ghi chú lịch</summary>
    public string? Note { get; set; }
}
