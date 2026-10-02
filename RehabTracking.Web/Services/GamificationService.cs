using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Services;

public class GamificationRewardResult
{
    public int XPEarned { get; set; }
    public int TotalXP { get; set; }
    public string Tier { get; set; } = "Rookie";
    public int CurrentStreak { get; set; }
    public bool StreakMaintained { get; set; }
    public List<string> NewlyUnlockedBadges { get; set; } = new();
    public string EncouragementMessage { get; set; } = string.Empty;
    public bool IsCapReached { get; set; } = false;
}

public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Tier { get; set; } = "Rookie";
    public int TotalXP { get; set; }
    public int Streak { get; set; }
    public bool IsCurrentUser { get; set; }
}

public class QuestDto
{
    public int QuestId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int XPBonus { get; set; }
    public string QuestType { get; set; } = "Daily";
    public int Progress { get; set; }
    public int Target { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsClaimed { get; set; }
    public double ProgressPercentage => Target > 0 ? Math.Min(100.0, (double)Progress / Target * 100.0) : 0;
}

public class GamificationService
{
    private readonly RehabTrackingContext _db;
    private readonly AuditService _auditService;
    private readonly AppNotificationService _notificationService;

    public const int DAILY_WORKOUT_XP_CAP = 200; // Giới hạn an toàn y khoa 200 XP/ngày từ tập luyện

    public GamificationService(
        RehabTrackingContext db, 
        AuditService auditService,
        AppNotificationService notificationService)
    {
        _db = db;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public async Task<GamificationProfile> GetOrCreateProfileAsync(int userId)
    {
        var profile = await _db.GamificationProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            var user = await _db.Users.FindAsync(userId);
            var randomCode = (userId * 37 + 1042) % 9000 + 1000;
            profile = new GamificationProfile
            {
                UserId = userId,
                TotalXP = 0,
                CurrentTier = "Rookie",
                CurrentStreak = 0,
                LongestStreak = 0,
                StreakShieldCount = 1,
                IsAnonymousLeaderboard = true,
                AnonymousDisplayName = $"Chiến binh #{randomCode}",
                UpdatedAt = DateTime.UtcNow
            };
            _db.GamificationProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }
        return profile;
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của buổi tập (Anti-cheat): Tối thiểu 30 giây để tránh click spam
    /// </summary>
    public static bool ValidateWorkoutIntegrity(int elapsedSeconds, int completedExercisesCount)
    {
        if (completedExercisesCount <= 0) return false;
        // Mỗi bài tập cần tối thiểu 10 giây thực hiện chuẩn xác
        return elapsedSeconds >= Math.Min(completedExercisesCount * 10, 30);
    }

    /// <summary>
    /// Tính toán thưởng XP và cập nhật chuỗi Streak kèm giới hạn trần an toàn y tế (Daily XP Cap)
    /// </summary>
    public async Task<GamificationRewardResult> ProcessWorkoutCompletionAsync(
        int userId, 
        double completionRate, 
        int painVAS, 
        bool hasNotes = false, 
        bool hasMedia = false,
        int elapsedSeconds = 60)
    {
        var profile = await GetOrCreateProfileAsync(userId);
        var today = DateTime.UtcNow.Date;
        var result = new GamificationRewardResult();

        // 0. Anti-cheat check thời gian tập
        if (!ValidateWorkoutIntegrity(elapsedSeconds, 1))
        {
            result.EncouragementMessage = "Thời gian tập luyện quá ngắn để ghi nhận XP. Hãy kiên trì thực hiện đầy đủ từng động tác bạn nhé!";
            result.TotalXP = profile.TotalXP;
            result.CurrentStreak = profile.CurrentStreak;
            result.Tier = profile.CurrentTier;
            return result;
        }

        // 1. Tính toán điểm kinh nghiệm (XP) thô
        int baseXP = 50;
        int performanceXP = (int)(baseXP * (Math.Clamp(completionRate, 0, 100) / 100.0));
        
        // Streak Bonus (tối đa 50 XP)
        int streakBonus = Math.Min(profile.CurrentStreak * 5, 50);
        
        // Log Quality Bonus
        int qualityBonus = 0;
        if (hasNotes) qualityBonus += 20;
        if (hasMedia) qualityBonus += 30;

        int rawSessionXP = performanceXP + streakBonus + qualityBonus;

        // 2. Áp dụng Daily Cap (Tối đa 200 XP/ngày)
        var todayLogs = await _db.RecoveryLogs
            .Where(r => r.PatientId == userId && r.LogDate >= today && r.LogDate < today.AddDays(1))
            .ToListAsync();
        
        int estimatedTodayEarned = todayLogs.Count * 60; // Ước lượng XP đã nhận hôm nay
        int allowableXP = Math.Max(0, DAILY_WORKOUT_XP_CAP - estimatedTodayEarned);
        int finalXP = Math.Min(rawSessionXP, allowableXP);

        if (finalXP < rawSessionXP)
        {
            result.IsCapReached = true;
        }
        result.XPEarned = finalXP;

        // 3. Tính toán chuỗi Streak
        bool streakIncremented = false;
        if (profile.LastActivityDate == null)
        {
            profile.CurrentStreak = 1;
            streakIncremented = true;
        }
        else
        {
            var lastDate = profile.LastActivityDate.Value.Date;
            var daysDiff = (today - lastDate).TotalDays;

            if (daysDiff == 1)
            {
                profile.CurrentStreak += 1;
                streakIncremented = true;
            }
            else if (daysDiff == 0)
            {
                // Đã tập trong ngày, duy trì streak
                streakIncremented = false;
            }
            else
            {
                // Bị lỡ ngày: Kiểm tra khiên bảo vệ
                if (profile.StreakShieldCount > 0)
                {
                    profile.StreakShieldCount -= 1;
                    profile.CurrentStreak += 1;
                    result.EncouragementMessage = "Khiên bảo vệ đã giữ lại chuỗi ngày của bạn! Hãy cố gắng duy trì nhé.";
                }
                else
                {
                    profile.CurrentStreak = 1;
                }
            }
        }

        if (profile.CurrentStreak > profile.LongestStreak)
        {
            profile.LongestStreak = profile.CurrentStreak;
        }

        profile.LastActivityDate = DateTime.UtcNow;
        profile.TotalXP += finalXP;

        // 4. Đánh giá Cấp bậc (Tier)
        profile.CurrentTier = EvaluateTier(profile.TotalXP);
        profile.UpdatedAt = DateTime.UtcNow;

        result.TotalXP = profile.TotalXP;
        result.Tier = profile.CurrentTier;
        result.CurrentStreak = profile.CurrentStreak;
        result.StreakMaintained = streakIncremented;

        // Micro-copy động lực
        if (string.IsNullOrEmpty(result.EncouragementMessage))
        {
            result.EncouragementMessage = GetEncouragementCopy(profile.CurrentStreak, completionRate);
        }

        // 5. Mở khóa Huy hiệu (Badges)
        await CheckAndUnlockBadgesAsync(userId, profile, completionRate, painVAS, result);

        // 6. Cập nhật tiến độ Quests tự động
        await RecordQuestProgressAsync(userId, "DAILY_WORKOUT", 1);
        if (hasNotes)
        {
            await RecordQuestProgressAsync(userId, "DAILY_LOG", 1);
        }

        await _db.SaveChangesAsync();
        return result;
    }

    /// <summary>
    /// Bác sĩ hoặc Quản trị viên thu hồi điểm XP bất thường (Anti-cheat & Clinical Oversight)
    /// </summary>
    public async Task<(bool Success, string Message)> RevokeSuspiciousXPAsync(
        int actorUserId, 
        string actorRole, 
        int targetUserId, 
        int xpToDeduct, 
        string clinicalReason)
    {
        if (actorRole != "Doctor" && actorRole != "Admin")
        {
            return (false, "Chỉ Bác sĩ điều trị hoặc Quản trị viên mới có quyền điều chỉnh điểm y khoa.");
        }

        var profile = await _db.GamificationProfiles.FirstOrDefaultAsync(p => p.UserId == targetUserId);
        if (profile == null) return (false, "Không tìm thấy hồ sơ người dùng.");

        int oldXP = profile.TotalXP;
        profile.TotalXP = Math.Max(0, profile.TotalXP - xpToDeduct);
        profile.CurrentTier = EvaluateTier(profile.TotalXP);
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Ghi Audit Log
        await _auditService.LogAsync(
            action: "REVOKE_XP",
            entityName: "GamificationProfile",
            recordId: profile.ProfileId.ToString(),
            targetPatientId: targetUserId,
            details: $"Thu hồi {xpToDeduct} XP (Từ {oldXP} -> {profile.TotalXP}). Lý do lâm sàng: {clinicalReason}",
            explicitUserId: actorUserId,
            explicitUserRole: actorRole);

        // Gửi thông báo đến bệnh nhân
        await _notificationService.CreateNotificationAsync(
            targetUserId,
            "Điều chỉnh điểm tích lũy y khoa",
            $"Bác sĩ điều trị đã hiệu chỉnh lại {xpToDeduct} XP trong hồ sơ của bạn. Lý do: {clinicalReason}",
            "ClinicalAlert",
            "/patient/ranking");

        return (true, $"Đã thu hồi thành công {xpToDeduct} XP của bệnh nhân.");
    }

    /// <summary>
    /// Lấy danh sách nhiệm vụ Daily & Weekly đang hoạt động
    /// </summary>
    public async Task<List<QuestDto>> GetActiveQuestsAsync(int userId)
    {
        var today = DateTime.UtcNow.Date;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);

        // Khởi tạo nhiệm vụ Daily hôm nay nếu chưa có
        var existingDaily = await _db.UserQuests
            .Where(q => q.UserId == userId && q.QuestType == "Daily" && q.CycleDate == today)
            .ToListAsync();

        if (existingDaily.Count == 0)
        {
            _db.UserQuests.AddRange(
                new UserQuest
                {
                    UserId = userId,
                    Code = "DAILY_LOGIN",
                    Title = "Khởi động ngày mới",
                    Description = "Đăng nhập và kiểm tra phác đồ phục hồi hôm nay",
                    XPBonus = 15,
                    QuestType = "Daily",
                    Progress = 1,
                    Target = 1,
                    IsCompleted = true,
                    IsClaimed = false,
                    CycleDate = today
                },
                new UserQuest
                {
                    UserId = userId,
                    Code = "DAILY_WORKOUT",
                    Title = "Chiến binh kiên trì",
                    Description = "Hoàn thành trọn vẹn 1 bài tập phục hồi chức năng",
                    XPBonus = 50,
                    QuestType = "Daily",
                    Progress = 0,
                    Target = 1,
                    IsCompleted = false,
                    IsClaimed = false,
                    CycleDate = today
                },
                new UserQuest
                {
                    UserId = userId,
                    Code = "DAILY_LOG",
                    Title = "Lắng nghe cơ thể",
                    Description = "Ghi nhật ký cảm giác đau (VAS) và tầm vận động",
                    XPBonus = 25,
                    QuestType = "Daily",
                    Progress = 0,
                    Target = 1,
                    IsCompleted = false,
                    IsClaimed = false,
                    CycleDate = today
                }
            );
            await _db.SaveChangesAsync();
        }

        // Khởi tạo nhiệm vụ Weekly trong tuần nếu chưa có
        var existingWeekly = await _db.UserQuests
            .Where(q => q.UserId == userId && q.QuestType == "Weekly" && q.CycleDate == startOfWeek)
            .ToListAsync();

        if (existingWeekly.Count == 0)
        {
            _db.UserQuests.AddRange(
                new UserQuest
                {
                    UserId = userId,
                    Code = "WEEKLY_5DAYS",
                    Title = "Kỷ luật thép 5 ngày",
                    Description = "Luyện tập tích cực tối thiểu 5 ngày trong tuần",
                    XPBonus = 120,
                    QuestType = "Weekly",
                    Progress = 0,
                    Target = 5,
                    IsCompleted = false,
                    IsClaimed = false,
                    CycleDate = startOfWeek
                },
                new UserQuest
                {
                    UserId = userId,
                    Code = "WEEKLY_ROM_GOAL",
                    Title = "Bứt phá tầm vận động",
                    Description = "Ghi nhận mức cải thiện biên độ khớp ROM tích cực",
                    XPBonus = 80,
                    QuestType = "Weekly",
                    Progress = 0,
                    Target = 1,
                    IsCompleted = false,
                    IsClaimed = false,
                    CycleDate = startOfWeek
                }
            );
            await _db.SaveChangesAsync();
        }

        return await _db.UserQuests
            .AsNoTracking()
            .Where(q => q.UserId == userId && 
                        ((q.QuestType == "Daily" && q.CycleDate == today) ||
                         (q.QuestType == "Weekly" && q.CycleDate == startOfWeek)))
            .OrderBy(q => q.QuestType)
            .ThenBy(q => q.QuestId)
            .Select(q => new QuestDto
            {
                QuestId = q.QuestId,
                Code = q.Code,
                Title = q.Title,
                Description = q.Description,
                XPBonus = q.XPBonus,
                QuestType = q.QuestType,
                Progress = q.Progress,
                Target = q.Target,
                IsCompleted = q.IsCompleted,
                IsClaimed = q.IsClaimed
            })
            .ToListAsync();
    }

    /// <summary>
    /// Cập nhật tiến độ nhiệm vụ
    /// </summary>
    public async Task RecordQuestProgressAsync(int userId, string questCode, int increment = 1)
    {
        var today = DateTime.UtcNow.Date;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);

        var quest = await _db.UserQuests
            .FirstOrDefaultAsync(q => q.UserId == userId && q.Code == questCode &&
                                      ((q.QuestType == "Daily" && q.CycleDate == today) ||
                                       (q.QuestType == "Weekly" && q.CycleDate == startOfWeek)));

        if (quest != null && !quest.IsCompleted)
        {
            quest.Progress += increment;
            if (quest.Progress >= quest.Target)
            {
                quest.Progress = quest.Target;
                quest.IsCompleted = true;
            }
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Nhận thưởng XP từ nhiệm vụ đã hoàn thành
    /// </summary>
    public async Task<(bool Success, string Message, int XPEarned)> ClaimQuestRewardAsync(int questId, int userId)
    {
        var quest = await _db.UserQuests.FirstOrDefaultAsync(q => q.QuestId == questId && q.UserId == userId);
        if (quest == null) return (false, "Nhiệm vụ không tồn tại.", 0);
        if (!quest.IsCompleted) return (false, "Nhiệm vụ chưa hoàn thành mục tiêu.", 0);
        if (quest.IsClaimed) return (false, "Bạn đã nhận phần thưởng này rồi.", 0);

        quest.IsClaimed = true;

        var profile = await GetOrCreateProfileAsync(userId);
        profile.TotalXP += quest.XPBonus;
        profile.CurrentTier = EvaluateTier(profile.TotalXP);
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Tạo thông báo nhận thưởng
        await _notificationService.CreateNotificationAsync(
            userId,
            "Nhận thưởng nhiệm vụ thành công!",
            $"Chúc mừng bạn đã hoàn thành nhiệm vụ '{quest.Title}' và nhận được +{quest.XPBonus} XP.",
            "Gamification",
            "/patient/ranking");

        return (true, $"Nhận thưởng thành công! +{quest.XPBonus} XP", quest.XPBonus);
    }

    private static string EvaluateTier(int xp)
    {
        if (xp >= 5000) return "Mastery";   // Phục hồi xuất sắc
        if (xp >= 2000) return "Warrior";   // Chiến binh
        if (xp >= 500)  return "Resilient"; // Bền bỉ
        return "Rookie";                    // Tân binh
    }

    private static string GetEncouragementCopy(int streak, double completion)
    {
        if (streak >= 21) return "🔥 21 ngày liên tiếp! Vận động phục hồi đã chính thức trở thành thói quen vàng của bạn!";
        if (streak >= 7)  return "⚡ 1 tuần kiên trì không bỏ cuộc! Các nhóm cơ và khớp đang tự tái tạo tích cực.";
        if (streak >= 3)  return "🌱 Bước khởi đầu xuất sắc! Chuỗi 3 ngày tuyệt vời, hãy tiếp tục phát huy!";
        if (completion >= 100) return "👏 Hoàn thành trọn vẹn 100% mục tiêu hôm nay! Bạn đang tiến gần hơn đến phục hồi hoàn toàn.";
        return "💪 Bạn đã hoàn thành buổi tập hôm nay! Mỗi bước đi nhỏ đều tạo nên sự khác biệt lớn.";
    }

    private async Task CheckAndUnlockBadgesAsync(
        int userId, 
        GamificationProfile profile, 
        double completionRate, 
        int painVAS, 
        GamificationRewardResult result)
    {
        var existingBadgeCodes = await _db.UserBadges
            .Where(ub => ub.UserId == userId)
            .Select(ub => ub.Badge.Code)
            .ToListAsync();

        var badgesToAward = new List<string>();

        if (profile.CurrentStreak >= 3 && !existingBadgeCodes.Contains("STREAK_3D"))
            badgesToAward.Add("STREAK_3D");

        if (profile.CurrentStreak >= 7 && !existingBadgeCodes.Contains("STREAK_7D"))
            badgesToAward.Add("STREAK_7D");

        if (completionRate >= 100 && !existingBadgeCodes.Contains("PERFECT_SESSION"))
            badgesToAward.Add("PERFECT_SESSION");

        if (profile.TotalXP >= 500 && !existingBadgeCodes.Contains("ROOKIE_GRADUATE"))
            badgesToAward.Add("ROOKIE_GRADUATE");

        if (painVAS <= 2 && !existingBadgeCodes.Contains("LOW_PAIN_CHAMP"))
            badgesToAward.Add("LOW_PAIN_CHAMP");

        foreach (var code in badgesToAward)
        {
            var badge = await _db.Badges.FirstOrDefaultAsync(b => b.Code == code);
            if (badge != null)
            {
                _db.UserBadges.Add(new UserBadge
                {
                    UserId = userId,
                    BadgeId = badge.BadgeId,
                    UnlockedAt = DateTime.UtcNow
                });
                profile.TotalXP += badge.XPBonus;
                result.NewlyUnlockedBadges.Add(badge.Name);
            }
        }
    }

    /// <summary>
    /// Lấy bảng xếp hạng thi đua tôn trọng quyền riêng tư y tế có hỗ trợ khung thời gian
    /// </summary>
    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(
        int currentUserId, 
        string timeframe = "all", 
        int limit = 20)
    {
        var profiles = await _db.GamificationProfiles
            .Include(p => p.User)
            .OrderByDescending(p => p.TotalXP)
            .Take(limit)
            .ToListAsync();

        var list = new List<LeaderboardEntryDto>();
        int rank = 1;

        foreach (var p in profiles)
        {
            string name = p.IsAnonymousLeaderboard 
                ? (p.AnonymousDisplayName ?? $"Chiến binh #{p.UserId * 17 % 9000 + 1000}")
                : (p.User?.FullName ?? "Bệnh nhân");

            list.Add(new LeaderboardEntryDto
            {
                Rank = rank++,
                UserId = p.UserId,
                DisplayName = name,
                Tier = p.CurrentTier,
                TotalXP = p.TotalXP,
                Streak = p.CurrentStreak,
                IsCurrentUser = p.UserId == currentUserId
            });
        }

        return list;
    }
}
