using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Services;

namespace RehabTracking.Web.Features.Healthcare.EHR;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EhrController : ControllerBase
{
    private readonly EhrService _ehrService;
    private readonly RehabTrackingContext _db;

    public EhrController(EhrService ehrService, RehabTrackingContext db)
    {
        _ehrService = ehrService;
        _db = db;
    }

    private (int UserId, string Role) GetCurrentUser()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(idStr, out int id);
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Patient";
        return (id, role);
    }

    /// <summary>
    /// Lấy hồ sơ sức khỏe điện tử (EHR) của bệnh nhân
    /// Bảo mật: Bệnh nhân chỉ được xem EHR của chính mình
    /// </summary>
    [HttpGet("{patientId:int}")]
    public async Task<ActionResult<ElectronicHealthRecord>> GetEhr(int patientId)
    {
        var (viewerId, viewerRole) = GetCurrentUser();

        if (viewerRole == "Patient")
        {
            // Bệnh nhân chỉ được xem EHR của chính mình – resolve từ token, không cho dùng patientId tùy ý
            var myProfile = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == viewerId);
            if (myProfile == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ bệnh nhân." });
            if (myProfile.PatientId != patientId && viewerId != patientId)
                return StatusCode(403, new { message = "Bạn không có quyền xem hồ sơ sức khỏe của bệnh nhân khác." });
            
            patientId = myProfile.PatientId;
        }

        var ehr = await _ehrService.GetEhrByPatientIdAsync(patientId, viewerId, viewerRole);
        if (ehr == null) return NotFound();
        return Ok(ehr);
    }

    /// <summary>
    /// Bác sĩ / Quản trị viên cập nhật hồ sơ bệnh án điện tử
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdateEhr([FromBody] UpdateEhrRequest req)
    {
        var (doctorId, doctorRole) = GetCurrentUser();
        var updated = await _ehrService.UpdateEhrAsync(req, doctorId, doctorRole);
        return Ok(updated);
    }

    /// <summary>
    /// Lấy Dòng thời gian phục hồi lâm sàng (Medical Recovery Timeline)
    /// </summary>
    [HttpGet("{patientId:int}/timeline")]
    public async Task<ActionResult<List<RecoveryTimelineEventDto>>> GetTimeline(int patientId)
    {
        var (viewerId, viewerRole) = GetCurrentUser();

        if (viewerRole == "Patient")
        {
            var myProfile = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == viewerId);
            if (myProfile == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ bệnh nhân." });
            if (myProfile.PatientId != patientId && viewerId != patientId)
                return StatusCode(403, new { message = "Bạn không có quyền xem dòng thời gian của bệnh nhân khác." });

            patientId = myProfile.PatientId;
        }

        var timeline = await _ehrService.GetRecoveryTimelineAsync(patientId);
        return Ok(timeline);
    }
}
