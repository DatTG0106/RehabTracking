using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Services;

namespace RehabTracking.Web.Features.Healthcare.RecoveryDiary;

public class CreateRecoveryLogApiDto
{
    public int? PatientId { get; set; }
    public int? ExerciseId { get; set; }

    [Range(0, 10, ErrorMessage = "Điểm đau trước tập từ 0 đến 10")]
    public int PainPreWorkout { get; set; } = 0;

    [Range(0, 10, ErrorMessage = "Điểm đau sau tập từ 0 đến 10")]
    public int PainScoreVAS { get; set; }

    [Range(0, 180, ErrorMessage = "Biên độ ROM từ 0 đến 180 độ")]
    public double? ROMMeasurement { get; set; }

    [Range(1, 5, ErrorMessage = "Mức độ mệt mỏi từ 1 đến 5")]
    public int FatigueLevel { get; set; } = 2;

    [Range(0, 100, ErrorMessage = "Tỷ lệ hoàn thành từ 0% đến 100%")]
    public double CompletionRate { get; set; } = 100.0;

    [Range(1, 300, ErrorMessage = "Thời lượng từ 1 đến 300 phút")]
    public int DurationMinutes { get; set; } = 15;

    public double? SleepHours { get; set; }
    public string? MoodLevel { get; set; } = "Bình thường";
    public string? PatientNote { get; set; }
    public string? MediaUrlsJson { get; set; }
}

public class DoctorFeedbackApiDto
{
    [Required(ErrorMessage = "Nội dung nhận xét y khoa không được để trống")]
    public string Feedback { get; set; } = null!;
}

[Route("api/[controller]")]
[ApiController]
public class RecoveryLogsController : ControllerBase
{
    private readonly RehabTrackingContext _db;
    private readonly RecoveryTrackingService _recoveryService;
    private readonly AuditService _auditService;

    public RecoveryLogsController(
        RehabTrackingContext db,
        RecoveryTrackingService recoveryService,
        AuditService auditService)
    {
        _db = db;
        _recoveryService = recoveryService;
        _auditService = auditService;
    }

    /// <summary>
    /// Lấy danh sách nhật ký phục hồi (bảo mật theo vai trò: Bệnh nhân xem của mình, Bác sĩ xem của bệnh nhân)
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetRecoveryLogs(
        [FromQuery] int? patientId,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 15)
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(currentUserIdStr, out int currentUserId);
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        int targetPatientId = 0;

        if (currentRole == "Patient")
        {
            var myProfile = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == currentUserId);
            if (myProfile == null) return NotFound(new { message = "Không tìm thấy hồ sơ bệnh nhân." });
            targetPatientId = myProfile.PatientId;
        }
        else if (currentRole == "Doctor" || currentRole == "Admin")
        {
            if (!patientId.HasValue || patientId.Value <= 0)
                return BadRequest(new { message = "Bác sĩ cần truyền patientId để xem nhật ký của bệnh nhân cụ thể." });
            targetPatientId = patientId.Value;
        }

        var query = _db.RecoveryLogs
            .Where(l => l.PatientId == targetPatientId)
            .Include(l => l.Exercise)
            .Include(l => l.Doctor)
            .OrderByDescending(l => l.LogDate);

        var total = await query.CountAsync();
        var page = Math.Max(1, pageIndex);
        var size = Math.Clamp(pageSize, 1, 50);

        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();

        return Ok(new
        {
            items,
            totalCount = total,
            pageIndex = page,
            pageSize = size,
            totalPages = (int)Math.Ceiling((double)total / size)
        });
    }

    /// <summary>
    /// Ghi nhận nhật ký buổi tập & trạng thái phục hồi hàng ngày
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateRecoveryLog([FromBody] CreateRecoveryLogApiDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(currentUserIdStr, out int currentUserId);
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        int patientId = 0;
        if (currentRole == "Patient")
        {
            var myProfile = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == currentUserId);
            if (myProfile == null) return NotFound(new { message = "Không tìm thấy hồ sơ bệnh nhân." });
            patientId = myProfile.PatientId;
        }
        else
        {
            if (!dto.PatientId.HasValue) return BadRequest(new { message = "Cần truyền patientId" });
            patientId = dto.PatientId.Value;
        }

        var result = await _recoveryService.LogSessionProgressAsync(
            patientId: patientId,
            exerciseId: dto.ExerciseId,
            painVAS: dto.PainScoreVAS,
            rom: dto.ROMMeasurement,
            fatigueLevel: dto.FatigueLevel,
            completionRate: dto.CompletionRate,
            durationMinutes: dto.DurationMinutes,
            note: dto.PatientNote,
            mediaUrlsJson: dto.MediaUrlsJson,
            painPreWorkout: dto.PainPreWorkout,
            sleepHours: dto.SleepHours,
            moodLevel: dto.MoodLevel
        );

        await _auditService.LogAsync(
            action: "Ghi_NhatKy_PhucHoi",
            entityName: nameof(RecoveryLog),
            recordId: result.Log.LogId.ToString(),
            targetPatientId: patientId,
            details: $"Ghi nhật ký: VAS {dto.PainScoreVAS}/10, ROM {dto.ROMMeasurement}°, Hoàn thành {dto.CompletionRate}%");

        return Ok(new
        {
            logId = result.Log.LogId,
            highPainAlert = result.HighPainAlert,
            clinicalAdvice = result.ClinicalAdvice,
            reward = result.Reward
        });
    }

    /// <summary>
    /// Bác sĩ gửi nhận xét/chỉ đạo chuyên môn vào nhật ký của bệnh nhân
    /// </summary>
    [HttpPost("{id}/feedback")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> AddDoctorFeedback(int id, [FromBody] DoctorFeedbackApiDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(currentUserIdStr, out int doctorId);

        var success = await _recoveryService.AddDoctorFeedbackAsync(id, doctorId, dto.Feedback);
        if (!success) return NotFound(new { message = "Không tìm thấy nhật ký tương ứng" });

        await _auditService.LogAsync(
            action: "NhanXet_NhatKy_PhucHoi",
            entityName: nameof(RecoveryLog),
            recordId: id.ToString(),
            details: $"Bác sĩ gửi nhận xét y khoa: '{dto.Feedback}'");

        return Ok(new { message = "Đã lưu nhận xét y khoa của bác sĩ thành công!" });
    }

    /// <summary>
    /// Thống kê xu hướng (VAS, ROM, tuân thủ) và dữ liệu lịch Heatmap
    /// </summary>
    [HttpGet("stats")]
    [Authorize]
    public async Task<IActionResult> GetRecoveryStats([FromQuery] int? patientId, [FromQuery] int heatmapDays = 28)
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(currentUserIdStr, out int currentUserId);
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        int targetPatientId = 0;
        if (currentRole == "Patient")
        {
            var myProfile = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == currentUserId);
            if (myProfile == null) return NotFound(new { message = "Không tìm thấy hồ sơ bệnh nhân." });
            targetPatientId = myProfile.PatientId;
        }
        else
        {
            if (!patientId.HasValue) return BadRequest(new { message = "Cần truyền patientId" });
            targetPatientId = patientId.Value;
        }

        var chartData = await _recoveryService.GetRecoveryChartDataAsync(targetPatientId, 14);
        var heatmapData = await _recoveryService.GetCalendarHeatmapDataAsync(targetPatientId, heatmapDays);
        var summaryStats = await _recoveryService.GetRecoverySummaryStatsAsync(targetPatientId);

        return Ok(new
        {
            chartData,
            heatmapData,
            summaryStats
        });
    }

    /// <summary>
    /// Lấy dữ liệu báo cáo tiến trình phục hồi đầy đủ chuẩn bị cho in ấn / xuất PDF
    /// </summary>
    [HttpGet("report")]
    [Authorize]
    public async Task<IActionResult> GetProgressReport([FromQuery] int? patientId)
    {
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(currentUserIdStr, out int currentUserId);
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        int targetPatientId = 0;
        if (currentRole == "Patient")
        {
            var myProfile = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.UserId == currentUserId);
            if (myProfile == null) return NotFound(new { message = "Không tìm thấy hồ sơ bệnh nhân." });
            targetPatientId = myProfile.PatientId;
        }
        else
        {
            if (!patientId.HasValue) return BadRequest(new { message = "Cần truyền patientId" });
            targetPatientId = patientId.Value;
        }

        var report = await _recoveryService.GetProgressReportDataAsync(targetPatientId);

        await _auditService.LogAsync(
            action: "Xuat_BaoCao_PhucHoi",
            entityName: nameof(RecoveryLog),
            targetPatientId: targetPatientId,
            details: $"Tải dữ liệu báo cáo tiến trình phục hồi y tế của bệnh nhân ID {targetPatientId}");

        return Ok(report);
    }
}
