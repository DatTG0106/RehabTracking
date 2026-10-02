using System;

namespace RehabTracking.Web.Entities;

/// <summary>
/// Nhiệm vụ phục hồi hàng ngày và hàng tuần để tạo động lực và thưởng XP an toàn
/// </summary>
public class UserQuest
{
    public int QuestId { get; set; }

    public int UserId { get; set; }

    /// <summary>
    /// Mã định danh nhiệm vụ (ví dụ: DAILY_LOGIN, DAILY_WORKOUT, DAILY_LOG, WEEKLY_5DAYS, WEEKLY_ROM_GOAL)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Điểm XP thưởng khi hoàn thành
    /// </summary>
    public int XPBonus { get; set; } = 20;

    /// <summary>
    /// Loại nhiệm vụ: Daily (Hàng ngày), Weekly (Hàng tuần)
    /// </summary>
    public string QuestType { get; set; } = "Daily";

    /// <summary>
    /// Tiến độ hiện tại (ví dụ: 1/1, 3/5)
    /// </summary>
    public int Progress { get; set; } = 0;

    /// <summary>
    /// Mục tiêu cần đạt
    /// </summary>
    public int Target { get; set; } = 1;

    public bool IsCompleted { get; set; } = false;

    /// <summary>
    /// Đã bấm nhận thưởng XP hay chưa
    /// </summary>
    public bool IsClaimed { get; set; } = false;

    /// <summary>
    /// Ngày đặt lại chu kỳ nhiệm vụ (Date)
    /// </summary>
    public DateTime CycleDate { get; set; } = DateTime.UtcNow.Date;

    public virtual User User { get; set; } = null!;
}
