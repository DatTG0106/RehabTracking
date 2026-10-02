using System;
using System.Collections.Generic;

namespace RehabTracking.Web.Entities;

/// <summary>
/// Thực thể bài tập vật lý trị liệu & phục hồi chức năng
/// </summary>
public partial class Exercise
{
    public int ExerciseId { get; set; }

    /// <summary>Tên bài tập</summary>
    public string Title { get; set; } = null!;

    /// <summary>Vùng cơ/khớp: Khớp gối, Cột sống cổ, Cột sống thắt lưng, Khớp vai, Cổ chân, Khớp háng</summary>
    public string TargetArea { get; set; } = null!;

    /// <summary>Giai đoạn hồi phục: Acute (Cấp tính), Rehab (Phục hồi), Strength (Tăng cường)</summary>
    public string RecoveryPhase { get; set; } = "Rehab";

    /// <summary>Độ khó: 1 (Dễ) đến 5 (Nâng cao)</summary>
    public int Difficulty { get; set; } = 1;

    /// <summary>Mức độ đau tối đa khuyến nghị được phép tập (thang 1-10, ví dụ: 4)</summary>
    public int RecommendedPainMax { get; set; } = 4;

    /// <summary>Thời gian chuẩn mặc định (giây)</summary>
    public int DefaultDurationSeconds { get; set; } = 300;

    /// <summary>Số sets chuẩn</summary>
    public int DefaultSets { get; set; } = 3;

    /// <summary>Số reps chuẩn mỗi set</summary>
    public int DefaultReps { get; set; } = 10;

    /// <summary>Thời gian giữ mỗi rep (giây) nếu là bài isometric/kéo giãn</summary>
    public int HoldSeconds { get; set; } = 5;

    /// <summary>Thời gian nghỉ giữa các sets (giây)</summary>
    public int RestSeconds { get; set; } = 30;

    /// <summary>Mô tả tổng quát bài tập</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Link video hướng dẫn (YouTube hoặc file video)</summary>
    public string? VideoUrl { get; set; }

    /// <summary>Ảnh minh họa bài tập</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Mảng JSON các bước hướng dẫn chi tiết</summary>
    public string? StepInstructionsJson { get; set; }

    /// <summary>Mảng JSON các lỗi sai tư thế thường gặp</summary>
    public string? CommonMistakesJson { get; set; }

    /// <summary>Cảnh báo các dấu hiệu nguy hiểm (Red Flags) phải dừng tập khẩn cấp</summary>
    public string? RedFlagWarnings { get; set; }

    /// <summary>Lợi ích trị liệu của bài tập</summary>
    public string? TherapeuticBenefits { get; set; }

    /// <summary>Dụng cụ yêu cầu: Không cần dụng cụ, Thảm tập, Dây kháng lực, Quả tạ nhẹ, Khăn, Con lăn...</summary>
    public string Equipment { get; set; } = "Không cần dụng cụ";

    /// <summary>Tình trạng / Chấn thương áp dụng (Phục hồi dây chằng ACL, Thoái hóa gối, Thoát vị L4-L5...)</summary>
    public string ApplicableConditions { get; set; } = string.Empty;

    /// <summary>Chống chỉ định tuyệt đối hoặc tương đối</summary>
    public string? Contraindications { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<RecoveryLog> RecoveryLogs { get; set; } = new List<RecoveryLog>();
}
