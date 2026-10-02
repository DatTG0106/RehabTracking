using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Services;
using Xunit;

namespace RehabTracking.Tests;

public class EhrAndAuditTests
{
    private RehabTrackingContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<RehabTrackingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RehabTrackingContext(options);
    }

    [Fact]
    public async Task GetEhr_ShouldReturnRecordAndCreateAuditLog()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var audit = new AuditService(db);
        var ehrService = new EhrService(db, audit);

        int patientId = 601;
        int doctorId = 701;

        db.Users.Add(new User { UserId = patientId, FullName = "Bệnh nhân EHR", Email = "ehr_pat@test.com", RoleId = 3, PasswordHash = "hash123" });
        db.Users.Add(new User { UserId = doctorId, FullName = "BS. Điều Trị", Email = "ehr_doc@test.com", RoleId = 2, PasswordHash = "hash123" });
        db.PatientProfiles.Add(new PatientProfile { PatientId = patientId, UserId = patientId, DoctorId = doctorId });
        await db.SaveChangesAsync();

        // Act: Bác sĩ xem hồ sơ bệnh án điện tử
        var ehr = await ehrService.GetEhrByPatientIdAsync(patientId, doctorId, "Doctor");

        // Assert
        Assert.NotNull(ehr);
        Assert.Equal(patientId, ehr!.PatientId);
        Assert.Contains("ACL", ehr.InitialDiagnosis);

        // Kiểm tra Audit Log được lưu
        var auditEntry = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "VIEW_EHR" && a.TargetPatientId == patientId);
        Assert.NotNull(auditEntry);
        Assert.Equal(doctorId, auditEntry!.UserId);
        Assert.Equal("Doctor", auditEntry.UserRole);
        Assert.Equal("ElectronicHealthRecord", auditEntry.EntityName);
    }

    [Fact]
    public async Task UpdateEhr_DoctorUpdate_ShouldUpdateFieldsAndLogAudit()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var audit = new AuditService(db);
        var ehrService = new EhrService(db, audit);

        int patientId = 602;
        int doctorId = 702;

        db.Users.Add(new User { UserId = patientId, FullName = "Bệnh nhân Cập Nhật", Email = "upd_pat@test.com", RoleId = 3, PasswordHash = "hash123" });
        db.Users.Add(new User { UserId = doctorId, FullName = "BS. Chấn Thương", Email = "upd_doc@test.com", RoleId = 2, PasswordHash = "hash123" });
        db.PatientProfiles.Add(new PatientProfile { PatientId = patientId, UserId = patientId, DoctorId = doctorId });

        var initialEhr = new ElectronicHealthRecord
        {
            PatientId = patientId,
            InitialDiagnosis = "Chẩn đoán cũ",
            AffectedAnatomy = "Khớp gối",
            CreatedAt = DateTime.UtcNow
        };
        db.ElectronicHealthRecords.Add(initialEhr);
        await db.SaveChangesAsync();

        var updateReq = new UpdateEhrRequest
        {
            RecordId = initialEhr.RecordId,
            PatientId = patientId,
            InitialDiagnosis = "Thoái hóa khớp gối nguyên phát độ 2 theo thang Kellgren-Lawrence",
            AffectedAnatomy = "Khớp gối hai bên, Sụn chêm trong",
            MedicalHistory = "Dị ứng Aspirin",
            Contraindications = "Không chạy bộ cường độ cao trên nền cứng",
            TreatmentGoals = "Tăng biên độ gập gối 130 độ, kiểm soát cân nặng"
        };

        // Act: Bác sĩ cập nhật chẩn đoán
        var updated = await ehrService.UpdateEhrAsync(updateReq, doctorId, "Doctor");

        // Assert
        Assert.NotNull(updated);
        Assert.Contains("Kellgren-Lawrence", updated.InitialDiagnosis);
        Assert.Equal("Dị ứng Aspirin", updated.MedicalHistory);

        // Kiểm tra Audit Log được lưu
        var auditEntry = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "UPDATE_EHR" && a.TargetPatientId == patientId);
        Assert.NotNull(auditEntry);
        Assert.Equal(doctorId, auditEntry!.UserId);
        Assert.Contains("Kellgren-Lawrence", auditEntry.Details);
    }

    [Fact]
    public async Task RecoveryTimeline_ShouldAggregateDiagnosisAppointmentsAndNotableLogs()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var audit = new AuditService(db);
        var ehrService = new EhrService(db, audit);

        int patientId = 603;
        int doctorId = 703;

        db.Users.Add(new User { UserId = patientId, FullName = "Bệnh nhân Timeline", Email = "tl_pat@test.com", RoleId = 3, PasswordHash = "hash123" });
        var doctor = new User { UserId = doctorId, FullName = "BS. Nguyễn Văn E", Email = "tl_doc@test.com", RoleId = 2, PasswordHash = "hash123" };
        db.Users.Add(doctor);
        db.PatientProfiles.Add(new PatientProfile { PatientId = patientId, UserId = patientId, DoctorId = doctorId });

        // 1. Hồ sơ EHR
        db.ElectronicHealthRecords.Add(new ElectronicHealthRecord
        {
            PatientId = patientId,
            InitialDiagnosis = "Gãy kín mâm chày gối trái",
            AffectedAnatomy = "Mâm chày trái",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        });

        // 2. Lịch tái khám
        db.DoctorAppointments.Add(new DoctorAppointment
        {
            PatientId = patientId,
            DoctorId = doctorId,
            Doctor = doctor,
            AppointmentDate = DateTime.UtcNow.AddDays(-15),
            Status = "Completed",
            AppointmentType = "PeriodicReview",
            DoctorNotes = "Xương liền can tốt, chỉ định gập gối chủ động",
            PatientReason = "Tái khám sau 2 tuần"
        });

        // 3. Nhật ký ROM
        db.RecoveryLogs.Add(new RecoveryLog
        {
            PatientId = patientId,
            LogDate = DateTime.UtcNow.AddDays(-5),
            ROMMeasurement = 110,
            PainScoreVAS = 2,
            CompletionRate = 100,
            DoctorFeedback = "Góc vận động đạt 110 độ xuất sắc"
        });

        await db.SaveChangesAsync();

        // Act: Lấy dòng thời gian
        var timeline = await ehrService.GetRecoveryTimelineAsync(patientId);

        // Assert
        Assert.Equal(3, timeline.Count);

        // Kiểm tra có sự kiện chẩn đoán, tái khám và cột mốc ROM
        Assert.Contains(timeline, t => t.EventType == "InitialDiagnosis");
        Assert.Contains(timeline, t => t.EventType == "Checkup");
        Assert.Contains(timeline, t => t.EventType == "Milestone" && t.MetricBadge == "ROM 110°");
    }
}
