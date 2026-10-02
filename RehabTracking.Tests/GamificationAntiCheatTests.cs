using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Services;
using Xunit;

namespace RehabTracking.Tests;

public class GamificationAntiCheatTests
{
    private RehabTrackingContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<RehabTrackingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RehabTrackingContext(options);
    }

    [Fact]
    public async Task WorkoutAntiCheat_ShortDuration_ShouldNotAwardXP()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var audit = new AuditService(db);
        var notif = new AppNotificationService(db);
        var gamification = new GamificationService(db, audit, notif);

        int userId = 101;
        db.Users.Add(new User { UserId = userId, FullName = "Bệnh nhân A", Email = "patA@test.com", RoleId = 3, PasswordHash = "hash123" });
        await db.SaveChangesAsync();

        // Act: Hoàn thành bài tập chỉ trong 5 giây (Spam click)
        var result = await gamification.ProcessWorkoutCompletionAsync(
            userId: userId,
            completionRate: 100,
            painVAS: 2,
            hasNotes: false,
            hasMedia: false,
            elapsedSeconds: 5);

        // Assert: Không được thưởng XP vì vi phạm tối thiểu 30s
        Assert.Equal(0, result.XPEarned);
        Assert.Contains("quá ngắn", result.EncouragementMessage);
    }

    [Fact]
    public async Task Doctor_RevokeSuspiciousXP_ShouldDeductXPAndLogAudit()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var audit = new AuditService(db);
        var notif = new AppNotificationService(db);
        var gamification = new GamificationService(db, audit, notif);

        int doctorId = 201;
        int patientId = 301;

        db.Users.Add(new User { UserId = doctorId, FullName = "BS. Khoa", Email = "doc@test.com", RoleId = 2, PasswordHash = "hash123" });
        db.Users.Add(new User { UserId = patientId, FullName = "Bệnh nhân B", Email = "patB@test.com", RoleId = 3, PasswordHash = "hash123" });

        var profile = await gamification.GetOrCreateProfileAsync(patientId);
        profile.TotalXP = 500;
        await db.SaveChangesAsync();

        // Act: Bác sĩ thu hồi 150 XP do nghi vấn hoàn thành bất thường
        var result = await gamification.RevokeSuspiciousXPAsync(
            actorUserId: doctorId,
            actorRole: "Doctor",
            targetUserId: patientId,
            xpToDeduct: 150,
            clinicalReason: "Phát hiện chỉ số tập luyện hoàn thành phi thực tế (10 bài tập trong 2 phút)");

        // Assert
        Assert.True(result.Success);

        var updatedProfile = await db.GamificationProfiles.FirstAsync(p => p.UserId == patientId);
        Assert.Equal(350, updatedProfile.TotalXP);

        // Kiểm tra Audit Log được lưu
        var auditEntry = await db.AuditLogs.FirstOrDefaultAsync(a => a.UserId == doctorId && a.Action == "REVOKE_XP");
        Assert.NotNull(auditEntry);
        Assert.Equal("GamificationProfile", auditEntry.EntityName);
        Assert.Equal(patientId, auditEntry.TargetPatientId);
        Assert.Contains("phi thực tế", auditEntry.Details);

        // Kiểm tra bệnh nhân nhận được thông báo
        var notification = await db.InAppNotifications.FirstOrDefaultAsync(n => n.UserId == patientId && n.Type == "ClinicalAlert");
        Assert.NotNull(notification);
        Assert.Contains("150 XP", notification.Message);
    }

    [Fact]
    public async Task ClaimQuestReward_CompletedQuest_ShouldAwardXPAndMarkClaimed()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var audit = new AuditService(db);
        var notif = new AppNotificationService(db);
        var gamification = new GamificationService(db, audit, notif);

        int patientId = 401;
        db.Users.Add(new User { UserId = patientId, FullName = "Bệnh nhân C", Email = "patC@test.com", RoleId = 3, PasswordHash = "hash123" });
        await db.SaveChangesAsync();

        // Khởi tạo quests
        var quests = await gamification.GetActiveQuestsAsync(patientId);
        var loginQuest = quests.First(q => q.Code == "DAILY_LOGIN");
        Assert.True(loginQuest.IsCompleted);
        Assert.False(loginQuest.IsClaimed);

        // Act: Nhận thưởng quest
        var claimResult = await gamification.ClaimQuestRewardAsync(loginQuest.QuestId, patientId);

        // Assert
        Assert.True(claimResult.Success);
        Assert.Equal(loginQuest.XPBonus, claimResult.XPEarned);

        var profile = await gamification.GetOrCreateProfileAsync(patientId);
        Assert.Equal(loginQuest.XPBonus, profile.TotalXP);

        // Thử nhận lần 2 phải bị từ chối
        var secondClaim = await gamification.ClaimQuestRewardAsync(loginQuest.QuestId, patientId);
        Assert.False(secondClaim.Success);
        Assert.Contains("đã nhận", secondClaim.Message);
    }
}
