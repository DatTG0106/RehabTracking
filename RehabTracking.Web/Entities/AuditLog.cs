using System;

namespace RehabTracking.Web.Entities;

/// <summary>
/// Nhật ký kiểm toán bảo mật dữ liệu y tế (Audit Log)
/// Ghi vết tất cả các thao tác truy cập hồ sơ sức khỏe và phác đồ điều trị
/// </summary>
public class AuditLog
{
    public int AuditLogId { get; set; }

    /// <summary>ID người dùng thực hiện thao tác</summary>
    public int UserId { get; set; }

    /// <summary>Vai trò tại thời điểm thực hiện (Patient, Doctor, Admin)</summary>
    public string UserRole { get; set; } = string.Empty;

    /// <summary>Hành động: Xem_HoSo_EHR, Sua_HoSo_EHR, Xuat_BaoCao_PDF, Giao_GiaoAn, CapNhat_DonThuoc</summary>
    public string Action { get; set; } = null!;

    /// <summary>Thực thể bị tác động (ElectronicHealthRecord, TreatmentPlan, RecoveryLog...)</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>ID của bản ghi bị tác động</summary>
    public string? RecordId { get; set; }

    /// <summary>ID bệnh nhân sở hữu dữ liệu y tế</summary>
    public int? TargetPatientId { get; set; }

    /// <summary>Địa chỉ IP người gửi yêu cầu</summary>
    public string? IpAddress { get; set; }

    /// <summary>Chi tiết tóm tắt hoặc lý do truy cập</summary>
    public string? Details { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public virtual User? User { get; set; }
}
