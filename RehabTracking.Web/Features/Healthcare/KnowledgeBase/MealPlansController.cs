using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Services;

namespace RehabTracking.Web.Features.Healthcare.KnowledgeBase;

[ApiController]
[Route("api/[controller]")]
public class MealPlansController : ControllerBase
{
    private readonly KnowledgeService _knowledgeService;

    public MealPlansController(KnowledgeService knowledgeService)
    {
        _knowledgeService = knowledgeService;
    }

    private (int UserId, string Role) GetCurrentUser()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(idStr, out int id);
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Doctor";
        return (id, role);
    }

    /// <summary>
    /// Danh sách thực đơn mẫu phục hồi chức năng
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<DietaryMealPlan>>> GetMealPlans(
        [FromQuery] string? targetCondition = null,
        [FromQuery] string? phase = null)
    {
        var list = await _knowledgeService.GetMealPlansAsync(targetCondition, phase);
        return Ok(list);
    }

    /// <summary>
    /// Xem chi tiết một thực đơn mẫu
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<DietaryMealPlan>> GetMealPlan(int id)
    {
        var plan = await _knowledgeService.GetMealPlanByIdAsync(id);
        if (plan == null) return NotFound();
        return Ok(plan);
    }

    /// <summary>
    /// Bác sĩ / Quản trị viên tạo thực đơn mẫu mới
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> CreateMealPlan([FromBody] UpsertMealPlanRequest req)
    {
        var (userId, role) = GetCurrentUser();
        req.MealPlanId = 0;
        var created = await _knowledgeService.UpsertMealPlanAsync(req, userId, role);
        return CreatedAtAction(nameof(GetMealPlan), new { id = created.MealPlanId }, created);
    }

    /// <summary>
    /// Bác sĩ / Quản trị viên cập nhật thực đơn mẫu
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdateMealPlan(int id, [FromBody] UpsertMealPlanRequest req)
    {
        var (userId, role) = GetCurrentUser();
        req.MealPlanId = id;
        try
        {
            var updated = await _knowledgeService.UpsertMealPlanAsync(req, userId, role);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Bác sĩ / Quản trị viên xóa thực đơn mẫu
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> DeleteMealPlan(int id)
    {
        var (userId, role) = GetCurrentUser();
        var success = await _knowledgeService.DeleteMealPlanAsync(id, userId, role);
        if (!success) return NotFound();
        return Ok(new { success = true });
    }
}
