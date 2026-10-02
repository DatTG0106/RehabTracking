using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Services;
using Xunit;

namespace RehabTracking.Tests;

public class AppointmentAndReminderTests
{
    private RehabTrackingContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<RehabTrackingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RehabTrackingContext(options);
    }

    [Fact]
    public async Task BookAppointment_ValidData_ShouldCreatePendingAppointmentAndNotifyDoctor()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var notifService = new AppNotificationService(db);
        var service = new AppointmentService(db, notifService);

        int patientId = 11;
        int doctorId = 22;

        db.Users.Add(new User { UserId = patientId, FullName = "Nguyễn Văn A", Email = "a@test.com", RoleId = 3, PasswordHash = "hash123" });
        db.Users.Add(new User { UserId = doctorId, FullName = "BS. Trần B", Email = "b@test.com", RoleId = 2, PasswordHash = "hash123" });
        await db.SaveChangesAsync();

        var request = new BookAppointmentRequest
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentDate = DateTime.UtcNow.AddDays(2).Date.AddHours(9),
            DurationMinutes = 30,
            AppointmentType = "PeriodicReview",
            PatientReason = "Tái khám kiểm tra biên độ gập khớp gối"
        };

        // Act
        var result = await service.BookAppointmentAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Appointment);
        Assert.Equal("Pending", result.Appointment.Status);

        // Kiểm tra thông báo được gửi cho Bác sĩ
        var notif = await db.InAppNotifications.FirstOrDefaultAsync(n => n.UserId == doctorId);
        Assert.NotNull(notif);
        Assert.Equal("Appointment", notif.Type);
        Assert.Contains("Nguyễn Văn A", notif.Message);
    }

    [Fact]
    public async Task UpdateAppointmentStatus_Confirmed_ShouldUpdateStatusAndNotifyPatient()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var notifService = new AppNotificationService(db);
        var service = new AppointmentService(db, notifService);

        int patientId = 33;
        int doctorId = 44;

        db.Users.Add(new User { UserId = patientId, FullName = "Lê Thị C", Email = "c@test.com", RoleId = 3, PasswordHash = "hash123" });
        db.Users.Add(new User { UserId = doctorId, FullName = "BS. Phạm D", Email = "d@test.com", RoleId = 2, PasswordHash = "hash123" });

        var apt = new DoctorAppointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            AppointmentDate = DateTime.UtcNow.AddDays(1),
            DurationMinutes = 30,
            AppointmentType = "OnlineROMCheck",
            Status = "Pending",
            PatientReason = "Đo ROM online",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.DoctorAppointments.Add(apt);
        await db.SaveChangesAsync();

        // Act: Bác sĩ xác nhận và gửi link phòng khám
        string meetingLink = "https://meet.google.com/test-room";
        var result = await service.UpdateStatusAsync(
            appointmentId: apt.AppointmentId,
            doctorId: doctorId,
            newStatus: "Confirmed",
            notes: "Vui lòng chuẩn bị trang phục thể thao thoải mái để đo khớp.",
            meetingLink: meetingLink);

        // Assert
        Assert.True(result.Success);

        var updatedApt = await db.DoctorAppointments.FindAsync(apt.AppointmentId);
        Assert.Equal("Confirmed", updatedApt!.Status);
        Assert.Equal(meetingLink, updatedApt.MeetingLink);

        // Kiểm tra thông báo gửi cho Bệnh nhân
        var patientNotif = await db.InAppNotifications.FirstOrDefaultAsync(n => n.UserId == patientId);
        Assert.NotNull(patientNotif);
        Assert.Contains("xác nhận", patientNotif.Title);
        Assert.Contains("BS. Phạm D", patientNotif.Message);
    }

    [Fact]
    public async Task ReminderSchedule_ToggleAndUpsert_ShouldWorkProperly()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var service = new ReminderService(db);

        int patientId = 55;
        db.Users.Add(new User { UserId = patientId, FullName = "Hoàng E", Email = "e@test.com", RoleId = 3, PasswordHash = "hash123" });
        await db.SaveChangesAsync();

        // Act 1: Lấy danh sách lần đầu (tự sinh 2 lịch mặc định)
        var schedules = await service.GetPatientSchedulesAsync(patientId);
        Assert.Equal(2, schedules.Count);
        Assert.True(schedules[0].IsActive);

        // Act 2: Tắt kích hoạt lịch thứ nhất
        var firstId = schedules[0].ReminderId;
        var toggleRes = await service.ToggleScheduleAsync(firstId, patientId);
        Assert.True(toggleRes);

        var updatedList = await service.GetPatientSchedulesAsync(patientId);
        Assert.False(updatedList.First(s => s.ReminderId == firstId).IsActive);

        // Act 3: Thêm một lịch mới
        var newSchedule = await service.UpsertScheduleAsync(new UpsertReminderScheduleRequest
        {
            PatientId = patientId,
            Title = "Uống thuốc sau bữa tối",
            ReminderType = "Medication",
            TimeOfDay = new TimeSpan(20, 0, 0),
            DaysOfWeek = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday",
            IsActive = true
        });

        Assert.True(newSchedule.ReminderId > 0);
        var finalCount = await db.ReminderSchedules.CountAsync(r => r.PatientId == patientId);
        Assert.Equal(3, finalCount);
    }
}
