using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Services;

namespace RehabTracking.Web.Features.Healthcare.DoctorDashboard;

public class SavePlanApiDto
{
    public int PatientId { get; set; }
    public string Title { get; set; } = "Phác đồ Phục hồi Chức năng";
    public List<ExerciseRoutineItem> Exercises { get; set; } = new();
    public WorkoutSchedule? Schedule { get; set; }
    public string? DoctorNotes { get; set; }
}

[Route("api/[controller]")]
[ApiController]
public class TreatmentPlansController : ControllerBase
{
    private readonly RehabTrackingContext _db;
    private readonly AuditService _auditService;

    public TreatmentPlansController(RehabTrackingContext db, AuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    /// <summary>
    /// Lấy phác đồ điều trị đang kích hoạt của một bệnh nhân
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [Authorize]
    public async Task<IActionResult> GetPatientPlan(int patientId)
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(currentUserIdStr, out int currentUserId);
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        // Kiểm soát bảo mật bản ghi y tế: Bệnh nhân chỉ xem được phác đồ của mình
        if (currentRole == "Patient")
        {
            var myProfile = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == currentUserId);
            if (myProfile == null || (myProfile.PatientId != patientId && currentUserId != patientId))
            {
                return StatusCode(403, new { message = "Bạn không có quyền xem phác đồ của bệnh nhân khác." });
            }
        }

        var plan = await _db.TreatmentPlans
            .Include(t => t.Doctor)
            .FirstOrDefaultAsync(t => t.PatientId == patientId && t.IsActive == true);

        if (plan == null)
        {
            return NotFound(new { message = "Bệnh nhân chưa có phác đồ điều trị nào đang kích hoạt." });
        }

        TreatmentPlanData? planData = null;
        if (!string.IsNullOrEmpty(plan.Description))
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                planData = JsonSerializer.Deserialize<TreatmentPlanData>(plan.Description, options);
            }
            catch { }
        }

        return Ok(new
        {
            plan.PlanId,
            plan.PatientId,
            plan.DoctorId,
            DoctorName = plan.Doctor?.FullName ?? "Bác sĩ phụ trách",
            plan.Title,
            plan.TargetRepetitions,
            plan.TargetDuration,
            plan.CreatedAt,
            PlanData = planData
        });
    }

    /// <summary>
    /// Bệnh nhân lấy phác đồ điều trị của chính mình
    /// </summary>
    [HttpGet("my-plan")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyPlan()
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(currentUserIdStr, out int currentUserId))
            return Unauthorized();

        var profile = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == currentUserId);
        if (profile == null)
            return NotFound(new { message = "Không tìm thấy hồ sơ bệnh nhân." });

        return await GetPatientPlan(profile.PatientId);
    }

    /// <summary>
    /// Bác sĩ thiết lập và giao giáo án bài tập cho bệnh nhân
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> SaveTreatmentPlan([FromBody] SavePlanApiDto dto)
    {
        if (dto.PatientId <= 0) return BadRequest(new { message = "Mã bệnh nhân không hợp lệ" });

        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(currentUserIdStr, out int doctorId);
        if (doctorId <= 0) doctorId = 1;

        var plan = await _db.TreatmentPlans
            .FirstOrDefaultAsync(t => t.PatientId == dto.PatientId && t.IsActive == true);

        if (plan == null)
        {
            plan = new TreatmentPlan
            {
                PatientId = dto.PatientId,
                DoctorId = doctorId,
                Title = string.IsNullOrWhiteSpace(dto.Title) ? "Phác đồ Phục hồi Chức năng" : dto.Title,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                TargetRepetitions = dto.Exercises.Sum(e => e.Sets * e.Reps),
                TargetDuration = 30
            };
            _db.TreatmentPlans.Add(plan);
        }
        else
        {
            plan.DoctorId = doctorId;
            if (!string.IsNullOrWhiteSpace(dto.Title)) plan.Title = dto.Title;
        }

        var planData = new TreatmentPlanData
        {
            ExerciseRoutine = dto.Exercises,
            Schedule = dto.Schedule,
            DoctorNotes = dto.DoctorNotes,
            LastUpdated = DateTime.UtcNow
        };

        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        plan.Description = JsonSerializer.Serialize(planData, serializerOptions);
        plan.TargetRepetitions = dto.Exercises.Any() ? dto.Exercises.Sum(e => e.Sets * e.Reps) : plan.TargetRepetitions;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Giao_GiaoAn_BaiTap",
            entityName: nameof(TreatmentPlan),
            recordId: plan.PlanId.ToString(),
            targetPatientId: dto.PatientId,
            details: $"Bác sĩ ID {doctorId} giao giáo án ({dto.Exercises.Count} bài tập) cho bệnh nhân ID {dto.PatientId}");

        return Ok(new { message = "Lưu và giao giáo án bài tập cho bệnh nhân thành công!", planId = plan.PlanId });
    }
}
