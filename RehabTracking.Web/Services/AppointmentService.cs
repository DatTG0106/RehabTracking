using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Services;

public class AppointmentDto
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public int DurationMinutes { get; set; }
    public string AppointmentType { get; set; } = "PeriodicReview";
    public string Status { get; set; } = "Pending";
    public string PatientReason { get; set; } = string.Empty;
    public string? DoctorNotes { get; set; }
    public string? MeetingLink { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BookAppointmentRequest
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string AppointmentType { get; set; } = "PeriodicReview";
    public string PatientReason { get; set; } = string.Empty;
}

/// <summary>
/// Quản lý lịch hẹn khám và điều trị phục hồi chức năng
/// </summary>
public class AppointmentService
{
    private readonly RehabTrackingContext _db;
    private readonly AppNotificationService _notificationService;

    public AppointmentService(RehabTrackingContext db, AppNotificationService notificationService)
    {
        _db = db;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Lấy danh sách lịch hẹn của bệnh nhân
    /// </summary>
    public async Task<List<AppointmentDto>> GetPatientAppointmentsAsync(int patientId)
    {
        return await _db.DoctorAppointments
            .AsNoTracking()
            .Include(a => a.Doctor)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => new AppointmentDto
            {
                AppointmentId = a.AppointmentId,
                PatientId = a.PatientId,
                DoctorId = a.DoctorId,
                DoctorName = a.Doctor.FullName,
                AppointmentDate = a.AppointmentDate,
                DurationMinutes = a.DurationMinutes,
                AppointmentType = a.AppointmentType,
                Status = a.Status,
                PatientReason = a.PatientReason,
                DoctorNotes = a.DoctorNotes,
                MeetingLink = a.MeetingLink,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    /// <summary>
    /// Lấy danh sách lịch hẹn của bác sĩ (có lọc theo trạng thái)
    /// </summary>
    public async Task<List<AppointmentDto>> GetDoctorAppointmentsAsync(int doctorId, string? status = null)
    {
        var query = _db.DoctorAppointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Where(a => a.DoctorId == doctorId);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(a => a.Status == status);
        }

        return await query
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => new AppointmentDto
            {
                AppointmentId = a.AppointmentId,
                PatientId = a.PatientId,
                PatientName = a.Patient.FullName,
                PatientPhone = a.Patient.PhoneNumber ?? "Chưa có",
                DoctorId = a.DoctorId,
                AppointmentDate = a.AppointmentDate,
                DurationMinutes = a.DurationMinutes,
                AppointmentType = a.AppointmentType,
                Status = a.Status,
                PatientReason = a.PatientReason,
                DoctorNotes = a.DoctorNotes,
                MeetingLink = a.MeetingLink,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    /// <summary>
    /// Đặt lịch hẹn khám mới
    /// </summary>
    public async Task<(bool Success, string Message, DoctorAppointment? Appointment)> BookAppointmentAsync(BookAppointmentRequest req)
    {
        if (req.AppointmentDate < DateTime.UtcNow.AddMinutes(15))
        {
            return (false, "Thời gian hẹn phải sau thời điểm hiện tại ít nhất 15 phút.", null);
        }

        // Kiểm tra bác sĩ có trùng lịch không (+/- 25 phút)
        var endTime = req.AppointmentDate.AddMinutes(req.DurationMinutes);
        var conflict = await _db.DoctorAppointments
            .AnyAsync(a => a.DoctorId == req.DoctorId 
                        && a.Status != "Cancelled"
                        && a.AppointmentDate < endTime 
                        && a.AppointmentDate.AddMinutes(a.DurationMinutes) > req.AppointmentDate);

        if (conflict)
        {
            return (false, "Bác sĩ đã có lịch hẹn trong khung giờ này. Vui lòng chọn thời gian khác.", null);
        }

        var appointment = new DoctorAppointment
        {
            PatientId = req.PatientId,
            DoctorId = req.DoctorId,
            AppointmentDate = req.AppointmentDate,
            DurationMinutes = req.DurationMinutes,
            AppointmentType = req.AppointmentType,
            Status = "Pending",
            PatientReason = req.PatientReason,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.DoctorAppointments.Add(appointment);
        await _db.SaveChangesAsync();

        // Gửi thông báo cho Bác sĩ
        var patient = await _db.Users.FindAsync(req.PatientId);
        await _notificationService.CreateNotificationAsync(
            req.DoctorId,
            "Yêu cầu đặt lịch khám mới",
            $"Bệnh nhân {patient?.FullName ?? "ẩn danh"} đã gửi yêu cầu hẹn khám vào lúc {req.AppointmentDate:dd/MM/yyyy HH:mm}.",
            "Appointment",
            "/doctor/appointments");

        return (true, "Đặt lịch hẹn thành công! Đang chờ bác sĩ xác nhận.", appointment);
    }

    /// <summary>
    /// Bác sĩ cập nhật trạng thái lịch hẹn (Xác nhận, Từ chối, Hoàn thành)
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateStatusAsync(
        int appointmentId, 
        int doctorId, 
        string newStatus, 
        string? notes = null, 
        string? meetingLink = null)
    {
        var apt = await _db.DoctorAppointments
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.DoctorId == doctorId);

        if (apt == null) return (false, "Không tìm thấy lịch hẹn hoặc bạn không có quyền cập nhật.");

        apt.Status = newStatus;
        apt.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(notes)) apt.DoctorNotes = notes;
        if (!string.IsNullOrEmpty(meetingLink)) apt.MeetingLink = meetingLink;

        await _db.SaveChangesAsync();

        // Gửi thông báo cho Bệnh nhân
        string notifTitle = newStatus switch
        {
            "Confirmed" => "Lịch hẹn khám đã được xác nhận",
            "Cancelled" => "Lịch hẹn khám đã bị hủy",
            "Completed" => "Buổi khám đã hoàn thành",
            _ => "Cập nhật lịch hẹn khám"
        };

        string notifMsg = newStatus switch
        {
            "Confirmed" => $"BS. {apt.Doctor.FullName} đã xác nhận lịch hẹn vào lúc {apt.AppointmentDate:dd/MM/yyyy HH:mm}." + (!string.IsNullOrEmpty(meetingLink) ? " Vui lòng kiểm tra liên kết phòng khám trực tuyến." : ""),
            "Cancelled" => $"BS. {apt.Doctor.FullName} đã từ chối/hủy lịch hẹn lúc {apt.AppointmentDate:dd/MM/yyyy HH:mm}." + (!string.IsNullOrEmpty(notes) ? $" Lý do: {notes}" : ""),
            "Completed" => $"BS. {apt.Doctor.FullName} đã hoàn tất buổi khám và để lại nhận xét chuyên môn.",
            _ => $"Lịch hẹn khám của bạn đã chuyển sang trạng thái: {newStatus}."
        };

        await _notificationService.CreateNotificationAsync(
            apt.PatientId,
            notifTitle,
            notifMsg,
            "Appointment",
            "/patient/appointments");

        return (true, $"Đã cập nhật trạng thái lịch hẹn thành {newStatus}.");
    }

    /// <summary>
    /// Lấy danh sách các bác sĩ có thể đặt lịch
    /// </summary>
    public async Task<List<User>> GetDoctorsListAsync()
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Role.RoleName == "Doctor" && (u.IsActive == true))
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }
}
