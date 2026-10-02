using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Services;
using Xunit;

namespace RehabTracking.Tests;

public class RecoveryDiaryTests
{
    private RehabTrackingContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<RehabTrackingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RehabTrackingContext(options);
    }

    [Fact]
    public async Task SummaryStats_PainImprovement_ShouldCalculateCorrectPercentage()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var audit = new AuditService(db);
        var notif = new AppNotificationService(db);
        var gamification = new GamificationService(db, audit, notif);
        var service = new RecoveryTrackingService(db, gamification);

        int patientId = 10;
        var now = DateTime.UtcNow;

        // Tuần 2 (trước): đau 6, đau 8 -> trung bình 7.0
        db.RecoveryLogs.Add(new RecoveryLog { PatientId = patientId, LogDate = now.AddDays(-10), PainScoreVAS = 6, CompletionRate = 100 });
        db.RecoveryLogs.Add(new RecoveryLog { PatientId = patientId, LogDate = now.AddDays(-9), PainScoreVAS = 8, CompletionRate = 100 });

        // Tuần 1 (gần nhất): đau 3, đau 4 -> trung bình 3.5
        db.RecoveryLogs.Add(new RecoveryLog { PatientId = patientId, LogDate = now.AddDays(-3), PainScoreVAS = 3, CompletionRate = 100 });
        db.RecoveryLogs.Add(new RecoveryLog { PatientId = patientId, LogDate = now.AddDays(-1), PainScoreVAS = 4, CompletionRate = 100 });

        await db.SaveChangesAsync();

        // Act
        var stats = await service.GetRecoverySummaryStatsAsync(patientId);

        // Assert
        Assert.Equal(4, stats.TotalSessions);
        Assert.Equal(3.5, stats.AvgPainCurrentWeek);
        Assert.Equal(7.0, stats.AvgPainPreviousWeek);
        // Cải thiện = (7.0 - 3.5) / 7.0 * 100 = 50.0%
        Assert.Equal(50.0, stats.PainImprovementPercent);
    }

    [Fact]
    public async Task CalendarHeatmap_ShouldGenerateExactDaysWithColorLevels()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var audit = new AuditService(db);
        var notif = new AppNotificationService(db);
        var gamification = new GamificationService(db, audit, notif);
        var service = new RecoveryTrackingService(db, gamification);

        int patientId = 20;
        var today = DateTime.UtcNow.Date;

        // Ghi nhận tập hôm nay hoàn thành 100% (level 3)
        db.RecoveryLogs.Add(new RecoveryLog
        {
            PatientId = patientId,
            LogDate = today.AddHours(9),
            CompletionRate = 100,
            PainScoreVAS = 2,
            DurationMinutes = 30
        });

        await db.SaveChangesAsync();

        // Act
        var heatmap = await service.GetCalendarHeatmapDataAsync(patientId, 7);

        // Assert
        Assert.Equal(7, heatmap.Count);
        var todayCell = heatmap.Find(h => h.Date.Date == today);
        Assert.NotNull(todayCell);
        Assert.True(todayCell!.HasWorkout);
        Assert.Equal(3, todayCell.ColorLevel); // 100% -> level 3
    }

    [Fact]
    public async Task AddDoctorFeedback_ShouldUpdateFeedbackAndTimestamp()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var audit = new AuditService(db);
        var notif = new AppNotificationService(db);
        var gamification = new GamificationService(db, audit, notif);
        var service = new RecoveryTrackingService(db, gamification);

        var log = new RecoveryLog
        {
            PatientId = 1,
            LogDate = DateTime.UtcNow,
            PainScoreVAS = 4,
            CompletionRate = 100
        };
        db.RecoveryLogs.Add(log);
        await db.SaveChangesAsync();

        // Act
        var result = await service.AddDoctorFeedbackAsync(log.LogId, 99, "Tiến độ hồi phục rất khả quan.");

        // Assert
        Assert.True(result);
        var updated = await db.RecoveryLogs.FindAsync(log.LogId);
        Assert.NotNull(updated);
        Assert.Equal("Tiến độ hồi phục rất khả quan.", updated!.DoctorFeedback);
        Assert.Equal(99, updated.DoctorId);
        Assert.NotNull(updated.FeedbackAt);
    }
}
