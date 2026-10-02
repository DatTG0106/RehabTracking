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

namespace RehabTracking.Web.Features.Healthcare.ExerciseCatalog;

public class ExerciseFilterDto
{
    public string? Keyword { get; set; }
    public string? TargetArea { get; set; }
    public string? RecoveryPhase { get; set; }
    public int? Difficulty { get; set; }
    public string? Equipment { get; set; }
    public string? Condition { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class CreateUpdateExerciseDto
{
    [Required(ErrorMessage = "Tên bài tập không được để trống")]
    [MaxLength(200, ErrorMessage = "Tên bài tập tối đa 200 ký tự")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Vùng cơ khớp không được để trống")]
    public string TargetArea { get; set; } = null!;

    public string RecoveryPhase { get; set; } = "Phục hồi";

    [Range(1, 5, ErrorMessage = "Độ khó từ 1 (Dễ) đến 5 (Nâng cao)")]
    public int Difficulty { get; set; } = 1;

    [Range(1, 10, ErrorMessage = "Ngưỡng đau tối đa khuyến nghị từ 1 đến 10")]
    public int RecommendedPainMax { get; set; } = 4;

    [Range(30, 3600, ErrorMessage = "Thời lượng từ 30 đến 3600 giây")]
    public int DefaultDurationSeconds { get; set; } = 300;

    [Range(1, 20, ErrorMessage = "Số hiệp từ 1 đến 20")]
    public int DefaultSets { get; set; } = 3;

    [Range(1, 100, ErrorMessage = "Số lần lặp mỗi hiệp từ 1 đến 100")]
    public int DefaultReps { get; set; } = 10;

    public int HoldSeconds { get; set; } = 5;
    public int RestSeconds { get; set; } = 30;

    public string Description { get; set; } = string.Empty;
    public string Equipment { get; set; } = "Không cần dụng cụ";
    public string ApplicableConditions { get; set; } = string.Empty;
    public string? Contraindications { get; set; }

    public string? VideoUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? StepInstructionsJson { get; set; }
    public string? CommonMistakesJson { get; set; }
    public string? RedFlagWarnings { get; set; }
    public string? TherapeuticBenefits { get; set; }
}

public class SmartRuleEvaluationRequest
{
    public int PreWorkoutPainVAS { get; set; }
    public int PostWorkoutPainVAS { get; set; }
    public double CompletionRate { get; set; }
    public int RecommendedPainMax { get; set; } = 4;
    public int ConsecutiveHighPainSessions { get; set; } = 0;
}

public class SmartRuleEvaluationResponse
{
    public bool CanProceedWorkout { get; set; } = true;
    public string SafetyStatus { get; set; } = "Normal"; // Normal, Warning, DangerStop
    public string ClinicalRecommendation { get; set; } = string.Empty;
    public double LoadAdjustmentMultiplier { get; set; } = 1.0; // 0.7 = giảm 30%, 1.1 = tăng 10%
    public bool TriggerDoctorAlert { get; set; } = false;
}

[Route("api/[controller]")]
[ApiController]
public class ExercisesController : ControllerBase
{
    private readonly RehabTrackingContext _db;
    private readonly AuditService _auditService;

    public ExercisesController(RehabTrackingContext db, AuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    /// <summary>
    /// Tra cứu danh sách bài tập kèm bộ lọc đa tiêu chí và phân trang
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<Exercise>>> GetExercises([FromQuery] ExerciseFilterDto filter)
    {
        var query = _db.Exercises.Where(e => e.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim().ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(kw) 
                                  || e.Description.ToLower().Contains(kw) 
                                  || e.ApplicableConditions.ToLower().Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetArea))
        {
            query = query.Where(e => e.TargetArea == filter.TargetArea);
        }

        if (!string.IsNullOrWhiteSpace(filter.RecoveryPhase))
        {
            query = query.Where(e => e.RecoveryPhase == filter.RecoveryPhase);
        }

        if (filter.Difficulty.HasValue && filter.Difficulty > 0)
        {
            query = query.Where(e => e.Difficulty == filter.Difficulty.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Equipment))
        {
            query = query.Where(e => e.Equipment.Contains(filter.Equipment));
        }

        if (!string.IsNullOrWhiteSpace(filter.Condition))
        {
            query = query.Where(e => e.ApplicableConditions.Contains(filter.Condition));
        }

        var total = await query.CountAsync();
        var page = Math.Max(1, filter.PageIndex);
        var size = Math.Clamp(filter.PageSize, 1, 50);

        var items = await query
            .OrderBy(e => e.Difficulty)
            .ThenBy(e => e.ExerciseId)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return Ok(new PagedResultDto<Exercise>
        {
            Items = items,
            TotalCount = total,
            PageIndex = page,
            PageSize = size
        });
    }

    /// <summary>
    /// Lấy chi tiết một bài tập
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Exercise>> GetExerciseById(int id)
    {
        var ex = await _db.Exercises.FirstOrDefaultAsync(e => e.ExerciseId == id && e.IsActive);
        if (ex == null) return NotFound(new { message = "Không tìm thấy bài tập yêu cầu" });
        return Ok(ex);
    }

    /// <summary>
    /// Bác sĩ / Admin tạo bài tập mới
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<ActionResult<Exercise>> CreateExercise([FromBody] CreateUpdateExerciseDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ex = new Exercise
        {
            Title = dto.Title.Trim(),
            TargetArea = dto.TargetArea.Trim(),
            RecoveryPhase = dto.RecoveryPhase,
            Difficulty = dto.Difficulty,
            RecommendedPainMax = dto.RecommendedPainMax,
            DefaultDurationSeconds = dto.DefaultDurationSeconds,
            DefaultSets = dto.DefaultSets,
            DefaultReps = dto.DefaultReps,
            HoldSeconds = dto.HoldSeconds,
            RestSeconds = dto.RestSeconds,
            Description = dto.Description,
            Equipment = dto.Equipment,
            ApplicableConditions = dto.ApplicableConditions,
            Contraindications = dto.Contraindications,
            VideoUrl = dto.VideoUrl,
            ThumbnailUrl = dto.ThumbnailUrl,
            StepInstructionsJson = dto.StepInstructionsJson,
            CommonMistakesJson = dto.CommonMistakesJson,
            RedFlagWarnings = dto.RedFlagWarnings,
            TherapeuticBenefits = dto.TherapeuticBenefits,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Exercises.Add(ex);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Tao_BaiTap_Moi",
            entityName: nameof(Exercise),
            recordId: ex.ExerciseId.ToString(),
            details: $"Tạo bài tập '{ex.Title}' vùng {ex.TargetArea}");

        return CreatedAtAction(nameof(GetExerciseById), new { id = ex.ExerciseId }, ex);
    }

    /// <summary>
    /// Bác sĩ / Admin cập nhật bài tập
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdateExercise(int id, [FromBody] CreateUpdateExerciseDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var ex = await _db.Exercises.FindAsync(id);
        if (ex == null || !ex.IsActive) return NotFound(new { message = "Không tìm thấy bài tập cần cập nhật" });

        ex.Title = dto.Title.Trim();
        ex.TargetArea = dto.TargetArea.Trim();
        ex.RecoveryPhase = dto.RecoveryPhase;
        ex.Difficulty = dto.Difficulty;
        ex.RecommendedPainMax = dto.RecommendedPainMax;
        ex.DefaultDurationSeconds = dto.DefaultDurationSeconds;
        ex.DefaultSets = dto.DefaultSets;
        ex.DefaultReps = dto.DefaultReps;
        ex.HoldSeconds = dto.HoldSeconds;
        ex.RestSeconds = dto.RestSeconds;
        ex.Description = dto.Description;
        ex.Equipment = dto.Equipment;
        ex.ApplicableConditions = dto.ApplicableConditions;
        ex.Contraindications = dto.Contraindications;
        ex.VideoUrl = dto.VideoUrl;
        ex.ThumbnailUrl = dto.ThumbnailUrl;
        ex.StepInstructionsJson = dto.StepInstructionsJson;
        ex.CommonMistakesJson = dto.CommonMistakesJson;
        ex.RedFlagWarnings = dto.RedFlagWarnings;
        ex.TherapeuticBenefits = dto.TherapeuticBenefits;

        await _db.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "CapNhat_BaiTap",
            entityName: nameof(Exercise),
            recordId: ex.ExerciseId.ToString(),
            details: $"Cập nhật bài tập '{ex.Title}'");

        return Ok(ex);
    }

    /// <summary>
    /// Xóa mềm bài tập
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> DeleteExercise(int id)
    {
        var ex = await _db.Exercises.FindAsync(id);
        if (ex == null) return NotFound(new { message = "Không tìm thấy bài tập" });

        ex.IsActive = false;
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "Xoa_BaiTap",
            entityName: nameof(Exercise),
            recordId: id.ToString(),
            details: $"Xóa mềm bài tập '{ex.Title}'");

        return Ok(new { message = "Đã vô hiệu hóa bài tập thành công" });
    }

    /// <summary>
    /// Đánh giá quy tắc thông minh (Rule-based Clinical Engine) dựa trên mức đau trước/sau và tỷ lệ hoàn thành
    /// </summary>
    [HttpPost("rule-evaluate")]
    public ActionResult<SmartRuleEvaluationResponse> EvaluateRules([FromBody] SmartRuleEvaluationRequest req)
    {
        var resp = new SmartRuleEvaluationResponse();

        // 1. Kiểm tra mức đau trước tập
        if (req.PreWorkoutPainVAS >= 7)
        {
            resp.CanProceedWorkout = false;
            resp.SafetyStatus = "DangerStop";
            resp.ClinicalRecommendation = "🚨 Mức đau trước tập quá cao (VAS " + req.PreWorkoutPainVAS + "/10). Khuyến cáo KHÔNG tập hôm nay. Nghỉ ngơi, chườm lạnh và liên hệ Bác sĩ chuyên khoa nếu cơn đau không thuyên giảm.";
            resp.LoadAdjustmentMultiplier = 0.0;
            resp.TriggerDoctorAlert = true;
            return Ok(resp);
        }

        if (req.PreWorkoutPainVAS > req.RecommendedPainMax)
        {
            resp.CanProceedWorkout = true;
            resp.SafetyStatus = "Warning";
            resp.ClinicalRecommendation = "⚠️ Mức đau trước tập (" + req.PreWorkoutPainVAS + "/10) vượt ngưỡng khuyến nghị (" + req.RecommendedPainMax + "/10). Tự động giảm 30% cường độ tập để bảo vệ ổ khớp.";
            resp.LoadAdjustmentMultiplier = 0.7;
            return Ok(resp);
        }

        // 2. Kiểm tra mức đau sau tập và độ biến thiên
        int painDelta = req.PostWorkoutPainVAS - req.PreWorkoutPainVAS;

        if (req.PostWorkoutPainVAS >= 7 || req.ConsecutiveHighPainSessions >= 2)
        {
            resp.SafetyStatus = "DangerStop";
            resp.ClinicalRecommendation = "⚠️ Điểm đau sau tập cao bất thường (" + req.PostWorkoutPainVAS + "/10). Hệ thống đã tự động cảnh báo tới Bác sĩ điều trị. Bạn nên chườm lạnh 15 phút và tạm nghỉ buổi tập tiếp theo.";
            resp.LoadAdjustmentMultiplier = 0.5;
            resp.TriggerDoctorAlert = true;
            return Ok(resp);
        }

        if (painDelta >= 3)
        {
            resp.SafetyStatus = "Warning";
            resp.ClinicalRecommendation = "Cơn đau tăng đáng kể sau buổi tập (tăng +" + painDelta + " điểm VAS). Buổi tới sẽ tự động giảm số reps và tăng thời gian nghỉ.";
            resp.LoadAdjustmentMultiplier = 0.75;
            return Ok(resp);
        }

        // 3. Tiến bộ tốt: Hoàn thành trọn vẹn và đau rất thấp
        if (req.CompletionRate >= 95 && req.PostWorkoutPainVAS <= 2)
        {
            resp.SafetyStatus = "Normal";
            resp.ClinicalRecommendation = "👏 Thích ứng vận động tuyệt vời! Khớp dung nạp bài tập tốt. Bạn có thể sẵn sàng tăng dần số lần lặp hoặc độ khó ở giai đoạn kế tiếp.";
            resp.LoadAdjustmentMultiplier = 1.1; // Khuyến khích tăng 10%
            return Ok(resp);
        }

        resp.SafetyStatus = "Normal";
        resp.ClinicalRecommendation = "Buổi tập ổn định, đáp ứng tốt với phác đồ hiện tại.";
        resp.LoadAdjustmentMultiplier = 1.0;
        return Ok(resp);
    }
}
