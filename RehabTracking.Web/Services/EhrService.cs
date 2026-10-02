using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Services;

public class DiagnosticImageDto
{
    public string ImageUrl { get; set; } = string.Empty;
    public string ImageType { get; set; } = "X-Ray"; // "X-Ray", "MRI", "CT-Scan", "Ultrasound"
    public string Title { get; set; } = string.Empty;
    public DateTime TakenDate { get; set; } = DateTime.UtcNow;
    public string Findings { get; set; } = string.Empty;
}

public class RecoveryTimelineEventDto
{
    public DateTime EventDate { get; set; }
    public string EventType { get; set; } = "Milestone"; // "InitialDiagnosis", "Surgery", "Checkup", "Milestone", "WorkoutProgress"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DoctorName { get; set; }
    public string? MetricBadge { get; set; }
    public string IconClass { get; set; } = "bi-flag-fill";
    public string ColorClass { get; set; } = "primary";
}

public class UpdateEhrRequest
{
    public int RecordId { get; set; }
    public int PatientId { get; set; }
    public string InitialDiagnosis { get; set; } = string.Empty;
    public string AffectedAnatomy { get; set; } = string.Empty;
    public string? MedicalHistory { get; set; }
    public string? Contraindications { get; set; }
    public string? TreatmentGoals { get; set; }
    public List<DiagnosticImageDto> DiagnosticImages { get; set; } = new();
    public string? Medications { get; set; }
}

/// <summary>
/// Quản lý Hồ sơ sức khỏe điện tử (EHR), Dòng thời gian lâm sàng và Hình ảnh chẩn đoán
/// </summary>
public class EhrService
{
    private readonly RehabTrackingContext _db;
    private readonly AuditService _auditService;

    public EhrService(RehabTrackingContext db, AuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    /// <summary>
    /// Lấy hồ sơ EHR của bệnh nhân (kèm ghi nhận Audit Log bảo mật)
    /// </summary>
    public async Task<ElectronicHealthRecord?> GetEhrByPatientIdAsync(
        int patientId, 
        int viewerUserId, 
        string viewerRole)
    {
        var ehr = await _db.ElectronicHealthRecords
            .Include(e => e.DoctorInCharge)
            .Include(e => e.Patient)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(e => e.PatientId == patientId);

        if (ehr == null)
        {
            // Tự động khởi tạo hồ sơ EHR cơ bản nếu bệnh nhân chưa có
            ehr = await InitializeDefaultEhrAsync(patientId);
        }

        // Ghi nhận Audit Log: Truy cập hồ sơ bệnh án điện tử
        await _auditService.LogAsync(
            action: "VIEW_EHR",
            entityName: "ElectronicHealthRecord",
            recordId: ehr?.RecordId.ToString(),
            targetPatientId: patientId,
            details: $"Người dùng {viewerRole} (ID: {viewerUserId}) đã truy cập hồ sơ bệnh án điện tử.",
            explicitUserId: viewerUserId,
            explicitUserRole: viewerRole);

        return ehr;
    }

    private async Task<ElectronicHealthRecord> InitializeDefaultEhrAsync(int patientId)
    {
        var patientProfile = await _db.PatientProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.PatientId == patientId || p.UserId == patientId);

        int resolvedPatientId = patientProfile?.PatientId ?? patientId;

        var defaultImages = new List<DiagnosticImageDto>
        {
            new DiagnosticImageDto
            {
                ImageUrl = "/images/ehr/mri_knee_sample.jpg",
                ImageType = "MRI",
                Title = "Phim chụp MRI Khớp Gối Phải",
                TakenDate = DateTime.UtcNow.AddMonths(-1),
                Findings = "Hình ảnh đứt bán phần dây chằng chéo trước (ACL), tụ dịch bao hoạt dịch mức độ nhẹ."
            },
            new DiagnosticImageDto
            {
                ImageUrl = "/images/ehr/xray_sample.jpg",
                ImageType = "X-Ray",
                Title = "Phim chụp X-Quang Khớp Gối Thẳng - Nghiêng",
                TakenDate = DateTime.UtcNow.AddMonths(-2),
                Findings = "Không thấy hình ảnh tổn thương gãy xương hoặc trật khớp cấp tính."
            }
        };

        var newEhr = new ElectronicHealthRecord
        {
            PatientId = resolvedPatientId,
            InitialDiagnosis = "Tổn thương đứt bán phần dây chằng chéo trước (ACL) gối phải - Giai đoạn phục hồi chức năng sau phẫu thuật.",
            AffectedAnatomy = "Khớp gối phải, Dây chằng chéo trước (ACL), Nhóm cơ tứ đầu đùi",
            MedicalHistory = "Không có tiền sử dị ứng thuốc; Không mắc đái tháo đường; Huyết áp ổn định.",
            Contraindications = "Tránh các động tác vặn xoắn khớp gối đột ngột, không squat góc sâu quá 90 độ khi chưa có chỉ định của bác sĩ.",
            TreatmentGoals = "Mục tiêu ngắn hạn: Đạt góc gập gối 90 độ, giảm đau VAS < 3. Mục tiêu dài hạn: Tái hòa nhập sinh hoạt và vận động thể thao an toàn.",
            DiagnosticImagingJson = JsonSerializer.Serialize(defaultImages),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ElectronicHealthRecords.Add(newEhr);
        await _db.SaveChangesAsync();
        return newEhr;
    }

    /// <summary>
    /// Bác sĩ cập nhật hồ sơ EHR và ghi Audit Log
    /// </summary>
    public async Task<ElectronicHealthRecord> UpdateEhrAsync(
        UpdateEhrRequest req, 
        int doctorId, 
        string doctorRole)
    {
        var ehr = await _db.ElectronicHealthRecords.FindAsync(req.RecordId);
        if (ehr == null)
        {
            ehr = await InitializeDefaultEhrAsync(req.PatientId);
        }

        ehr.InitialDiagnosis = req.InitialDiagnosis;
        ehr.AffectedAnatomy = req.AffectedAnatomy;
        ehr.MedicalHistory = req.MedicalHistory;
        ehr.Contraindications = req.Contraindications;
        ehr.TreatmentGoals = req.TreatmentGoals;
        ehr.DoctorInChargeId = doctorId;
        ehr.UpdatedAt = DateTime.UtcNow;

        if (req.DiagnosticImages != null && req.DiagnosticImages.Count > 0)
        {
            ehr.DiagnosticImagingJson = JsonSerializer.Serialize(req.DiagnosticImages);
        }

        await _db.SaveChangesAsync();

        // Ghi Audit Log cập nhật bệnh án
        await _auditService.LogAsync(
            action: "UPDATE_EHR",
            entityName: "ElectronicHealthRecord",
            recordId: ehr.RecordId.ToString(),
            targetPatientId: ehr.PatientId,
            details: $"Bác sĩ (ID: {doctorId}) cập nhật hồ sơ EHR. Chẩn đoán: {ehr.InitialDiagnosis}",
            explicitUserId: doctorId,
            explicitUserRole: doctorRole);

        return ehr;
    }

    /// <summary>
    /// Tạo Dòng thời gian phục hồi (Medical Recovery Timeline) kết hợp đa nguồn
    /// </summary>
    public async Task<List<RecoveryTimelineEventDto>> GetRecoveryTimelineAsync(int patientId)
    {
        var timeline = new List<RecoveryTimelineEventDto>();

        // 1. Cột mốc khởi đầu từ EHR
        var ehr = await _db.ElectronicHealthRecords
            .Include(e => e.DoctorInCharge)
            .FirstOrDefaultAsync(e => e.PatientId == patientId);

        if (ehr != null)
        {
            timeline.Add(new RecoveryTimelineEventDto
            {
                EventDate = ehr.CreatedAt,
                EventType = "InitialDiagnosis",
                Title = "Thiết lập Hồ sơ Bệnh án & Khám Khởi Đầu",
                Description = ehr.InitialDiagnosis,
                DoctorName = ehr.DoctorInCharge?.FullName ?? "BS. CKII Trần Minh Khoa",
                MetricBadge = ehr.AffectedAnatomy,
                IconClass = "bi-file-earmark-medical-fill",
                ColorClass = "primary"
            });
        }

        // 2. Các buổi khám/tái khám đã hoàn tất (Appointments)
        var appointments = await _db.DoctorAppointments
            .Include(a => a.Doctor)
            .Where(a => a.PatientId == patientId && (a.Status == "Confirmed" || a.Status == "Completed"))
            .ToListAsync();

        foreach (var apt in appointments)
        {
            timeline.Add(new RecoveryTimelineEventDto
            {
                EventDate = apt.AppointmentDate,
                EventType = "Checkup",
                Title = $"Tái Khám Định Kỳ: {GetAppointmentTypeLabel(apt.AppointmentType)}",
                Description = string.IsNullOrEmpty(apt.DoctorNotes) ? apt.PatientReason : $"Đánh giá bác sĩ: {apt.DoctorNotes}",
                DoctorName = apt.Doctor.FullName,
                MetricBadge = apt.Status == "Completed" ? "Đã Khám Xong" : "Lịch Sắp Tới",
                IconClass = "bi-calendar-check-fill",
                ColorClass = apt.Status == "Completed" ? "success" : "info"
            });
        }

        // 3. Các cột mốc tập luyện đáng chú ý từ Nhật ký (RecoveryLog)
        var notableLogs = await _db.RecoveryLogs
            .Where(r => r.PatientId == patientId && (r.ROMMeasurement > 0 || r.CompletionRate >= 100))
            .OrderByDescending(r => r.LogDate)
            .Take(10)
            .ToListAsync();

        foreach (var log in notableLogs)
        {
            if (log.ROMMeasurement > 0)
            {
                timeline.Add(new RecoveryTimelineEventDto
                {
                    EventDate = log.LogDate,
                    EventType = "Milestone",
                    Title = $"Ghi Nhận Biên Độ Vận Động (ROM): {log.ROMMeasurement}°",
                    Description = !string.IsNullOrEmpty(log.DoctorFeedback) 
                        ? $"Bác sĩ nhận xét: {log.DoctorFeedback}" 
                        : $"Tầm vận động ghi nhận đạt {log.ROMMeasurement} độ. Đau sau tập VAS: {log.PainScoreVAS}/10.",
                    MetricBadge = $"ROM {log.ROMMeasurement}°",
                    IconClass = "bi-arrow-up-right-circle-fill",
                    ColorClass = "warning"
                });
            }
        }

        // Sắp xếp thứ tự thời gian từ gần nhất đến xa nhất (hoặc ngược lại)
        return timeline.OrderByDescending(t => t.EventDate).ToList();
    }

    private static string GetAppointmentTypeLabel(string type) => type switch
    {
        "InitialConsultation" => "Khám Khởi Đầu",
        "PeriodicReview" => "Tái Khám Định Kỳ",
        "OnlineROMCheck" => "Đo ROM Trực Tuyến",
        _ => type
    };
}
