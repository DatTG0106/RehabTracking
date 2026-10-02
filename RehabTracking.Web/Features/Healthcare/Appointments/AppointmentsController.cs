using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Services;

namespace RehabTracking.Web.Features.Healthcare.Appointments;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentService _appointmentService;
    private readonly RehabTrackingContext _db;

    public AppointmentsController(AppointmentService appointmentService, RehabTrackingContext db)
    {
        _appointmentService = appointmentService;
        _db = db;
    }

    /// <summary>
    /// Lấy danh sách lịch hẹn của bệnh nhân
    /// Bảo mật: Bệnh nhân chỉ xem được lịch hẹn của chính mình (resolve từ token)
    /// Bác sĩ / Admin có thể xem theo patientId
    /// </summary>
    [HttpGet("patient/{patientId:int}")]
    public async Task<ActionResult<List<AppointmentDto>>> GetPatientAppointments(int patientId)
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(currentUserIdStr, out int currentUserId);
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Patient";

        if (currentRole == "Patient")
        {
            // Bệnh nhân chỉ được xem lịch hẹn của chính mình – resolve từ token
            var myProfile = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == currentUserId);
            if (myProfile == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ bệnh nhân." });
            if (myProfile.PatientId != patientId && currentUserId != patientId)
                return StatusCode(403, new { message = "Bạn không có quyền xem lịch hẹn của bệnh nhân khác." });

            patientId = currentUserId;
        }

        var list = await _appointmentService.GetPatientAppointmentsAsync(patientId);
        return Ok(list);
    }

    /// <summary>
    /// Lấy danh sách lịch hẹn của bác sĩ
    /// </summary>
    [HttpGet("doctor/{doctorId:int}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<ActionResult<List<AppointmentDto>>> GetDoctorAppointments(int doctorId, [FromQuery] string? status = null)
    {
        var list = await _appointmentService.GetDoctorAppointmentsAsync(doctorId, status);
        return Ok(list);
    }

    /// <summary>
    /// Đặt lịch hẹn khám mới
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentRequest req)
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(currentUserIdStr, out int currentUserId);
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Patient";

        if (currentRole == "Patient")
        {
            // Bệnh nhân chỉ được đặt lịch hẹn cho chính mình
            req.PatientId = currentUserId;
        }

        var result = await _appointmentService.BookAppointmentAsync(req);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }
        return Ok(new { message = result.Message, appointment = result.Appointment });
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = "Confirmed";
        public string? Notes { get; set; }
        public string? MeetingLink { get; set; }
    }

    /// <summary>
    /// Bác sĩ cập nhật trạng thái lịch hẹn (Xác nhận, Từ chối, Hoàn thành)
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto req)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out int doctorId))
        {
            return Unauthorized();
        }

        var result = await _appointmentService.UpdateStatusAsync(id, doctorId, req.Status, req.Notes, req.MeetingLink);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Danh sách bác sĩ phục hồi chức năng
    /// </summary>
    [HttpGet("doctors")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDoctors()
    {
        var list = await _appointmentService.GetDoctorsListAsync();
        return Ok(list.Select(d => new { d.UserId, d.FullName, d.Email, d.PhoneNumber }));
    }
}
