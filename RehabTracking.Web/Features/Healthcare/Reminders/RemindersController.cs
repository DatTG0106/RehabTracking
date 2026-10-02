using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RehabTracking.Web.Services;

namespace RehabTracking.Web.Features.Healthcare.Reminders;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RemindersController : ControllerBase
{
    private readonly ReminderService _reminderService;

    public RemindersController(ReminderService reminderService)
    {
        _reminderService = reminderService;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out int id) ? id : 0;
    }

    /// <summary>
    /// Lấy danh sách lịch nhắc nhở của bệnh nhân
    /// </summary>
    [HttpGet("my-schedules")]
    public async Task<ActionResult<List<ReminderScheduleDto>>> GetMySchedules()
    {
        int userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var list = await _reminderService.GetPatientSchedulesAsync(userId);
        return Ok(list);
    }

    /// <summary>
    /// Thêm hoặc cập nhật lịch nhắc nhở
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UpsertSchedule([FromBody] UpsertReminderScheduleRequest req)
    {
        int userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        req.PatientId = userId;
        var result = await _reminderService.UpsertScheduleAsync(req);
        return Ok(result);
    }

    /// <summary>
    /// Bật/Tắt lịch nhắc nhở
    /// </summary>
    [HttpPut("{id:int}/toggle")]
    public async Task<IActionResult> ToggleSchedule(int id)
    {
        int userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var success = await _reminderService.ToggleScheduleAsync(id, userId);
        if (!success) return NotFound();

        return Ok(new { success = true });
    }

    /// <summary>
    /// Xóa lịch nhắc nhở
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        int userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var success = await _reminderService.DeleteScheduleAsync(id, userId);
        if (!success) return NotFound();

        return Ok(new { success = true });
    }
}
