using System;
using System.Collections.Generic;

namespace RehabTracking.Web.Entities;

/// <summary>
/// Hồ sơ điểm thưởng, chuỗi streak và xếp hạng gamification
/// </summary>
public partial class GamificationProfile
{
    public int ProfileId { get; set; }

    public int UserId { get; set; }

    /// <summary>Tổng điểm kinh nghiệm tích lũy</summary>
    public int TotalXP { get; set; } = 0;

    /// <summary>Cấp bậc: Rookie (Tân binh), Resilient (Bền bỉ), Warrior (Chiến binh), Mastery (Phục hồi xuất sắc)</summary>
    public string CurrentTier { get; set; } = "Rookie";

    /// <summary>Chuỗi ngày tập liên tục hiện tại</summary>
    public int CurrentStreak { get; set; } = 0;

    /// <summary>Kỷ lục chuỗi ngày dài nhất</summary>
    public int LongestStreak { get; set; } = 0;

    /// <summary>Ngày ghi nhận hoạt động gần nhất (để tính streak)</summary>
    public DateTime? LastActivityDate { get; set; }

    /// <summary>Số khiên bảo vệ chuỗi ngày bận rộn</summary>
    public int StreakShieldCount { get; set; } = 1;

    /// <summary>Có bật chế độ ẩn danh trên Bảng xếp hạng hay không (Bảo mật y tế)</summary>
    public bool IsAnonymousLeaderboard { get; set; } = true;

    /// <summary>Bí danh ẩn danh trên bảng xếp hạng (ví dụ: Chiến binh #4092)</summary>
    public string? AnonymousDisplayName { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
}
