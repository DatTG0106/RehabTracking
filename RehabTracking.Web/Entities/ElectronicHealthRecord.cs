using System;

namespace RehabTracking.Web.Entities;

/// <summary>
/// Hồ sơ sức khỏe điện tử (EHR) phục vụ theo dõi phục hồi chức năng
/// </summary>
public partial class ElectronicHealthRecord
{
    public int RecordId { get; set; }

    public int PatientId { get; set; }

    public int? DoctorInChargeId { get; set; }

    /// <summary>Chẩn đoán ban đầu / Lý do vào viện</summary>
    public string InitialDiagnosis { get; set; } = null!;

    /// <summary>Vùng tổn thương giải phẫu</summary>
    public string AffectedAnatomy { get; set; } = string.Empty;

    /// <summary>Tiền sử bệnh & Dị ứng</summary>
    public string? MedicalHistory { get; set; }

    /// <summary>Danh sách link phim chụp X-quang/MRI/CT (JSON Array)</summary>
    public string? DiagnosticImagingJson { get; set; }

    /// <summary>Đơn thuốc và thực phẩm bổ sung đi kèm (JSON Array)</summary>
    public string? MedicationScheduleJson { get; set; }

    /// <summary>Chống chỉ định vận động đặc biệt</summary>
    public string? Contraindications { get; set; }

    /// <summary>Mục tiêu điều trị ngắn hạn & dài hạn</summary>
    public string? TreatmentGoals { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public virtual PatientProfile Patient { get; set; } = null!;

    public virtual User? DoctorInCharge { get; set; }
}
