using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Services;
using Xunit;

namespace RehabTracking.Tests;

public class ExerciseAndTreatmentPlanTests
{
    private RehabTrackingContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<RehabTrackingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RehabTrackingContext(options);
    }

    [Fact]
    public async Task ExerciseCatalog_FilterByTargetAreaAndDifficulty_ShouldReturnCorrectExercises()
    {
        using var db = CreateInMemoryContext();

        db.Exercises.AddRange(
            new Exercise
            {
                ExerciseId = 1,
                Title = "Gập duỗi gối tư thế ngồi",
                TargetArea = "Khớp gối",
                Difficulty = 1,
                IsActive = true
            },
            new Exercise
            {
                ExerciseId = 2,
                Title = "Squat có tựa ghế",
                TargetArea = "Khớp gối",
                Difficulty = 3,
                IsActive = true
            },
            new Exercise
            {
                ExerciseId = 3,
                Title = "Xoay khớp vai với gậy",
                TargetArea = "Khớp vai",
                Difficulty = 1,
                IsActive = true
            }
        );
        await db.SaveChangesAsync();

        // Act: Filter by Khớp gối và Difficulty = 1 (Dễ)
        var kneeEasyExercises = await db.Exercises
            .Where(e => e.TargetArea == "Khớp gối" && e.Difficulty == 1)
            .ToListAsync();

        // Assert
        Assert.Single(kneeEasyExercises);
        Assert.Equal("Gập duỗi gối tư thế ngồi", kneeEasyExercises[0].Title);
    }

    [Fact]
    public async Task TreatmentPlan_DoctorCreatesAndAssignsToPatient_ShouldSaveSuccessfully()
    {
        using var db = CreateInMemoryContext();

        // Doctor (User 2) and Patient (User 4)
        db.Users.AddRange(
            new User { UserId = 2, FullName = "BS. Minh Khoa", Email = "doctor@test.com", PasswordHash = "hash123", RoleId = 2 },
            new User { UserId = 4, FullName = "Bệnh nhân Văn An", Email = "patient@test.com", PasswordHash = "hash123", RoleId = 3 }
        );

        var plan = new TreatmentPlan
        {
            PlanId = 100,
            PatientId = 4,
            DoctorId = 2,
            Title = "Phác đồ phục hồi sau phẫu thuật dây chằng chéo",
            Description = "Tập 3 buổi/tuần, tăng biên độ ROM từ từ",
            TargetRepetitions = 30,
            TargetDuration = 45,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.TreatmentPlans.Add(plan);
        await db.SaveChangesAsync();

        // Act: Fetch patient's active treatment plan
        var patientPlan = await db.TreatmentPlans
            .FirstOrDefaultAsync(p => p.PatientId == 4 && p.PlanId == 100);

        // Assert
        Assert.NotNull(patientPlan);
        Assert.Equal(2, patientPlan.DoctorId);
        Assert.Equal("Phác đồ phục hồi sau phẫu thuật dây chằng chéo", patientPlan.Title);
        Assert.True(patientPlan.IsActive);
    }

    [Fact]
    public async Task Notification_HighPainAlert_ShouldCreateAndMarkAsRead()
    {
        using var db = CreateInMemoryContext();
        var notifService = new AppNotificationService(db);

        int doctorId = 2;

        // Act 1: Create notification
        var notif = await notifService.CreateNotificationAsync(
            userId: doctorId,
            title: "Cảnh báo Đau cao",
            message: "Bệnh nhân Nguyễn Văn An báo cáo đau 9/10 sau buổi tập gối",
            type: "Alert",
            actionUrl: "/doctor-dashboard"
        );

        Assert.NotNull(notif);
        var unreadCount = await notifService.GetUnreadCountAsync(doctorId);
        Assert.Equal(1, unreadCount);

        var notifs = await notifService.GetNotificationsAsync(doctorId);
        Assert.Single(notifs);
        Assert.Equal("Cảnh báo Đau cao", notifs[0].Title);
        Assert.Contains("9/10", notifs[0].Message);
        Assert.False(notifs[0].IsRead);

        // Act 2: Mark as read
        var readSuccess = await notifService.MarkAsReadAsync(notif.NotificationId, doctorId);
        Assert.True(readSuccess);

        unreadCount = await notifService.GetUnreadCountAsync(doctorId);
        Assert.Equal(0, unreadCount);
    }
}
