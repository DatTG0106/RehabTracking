using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RehabTracking.Web.Services;

namespace RehabTracking.Web.Features.Healthcare.Ranking;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GamificationController : ControllerBase
{
    private readonly GamificationService _gamificationService;

    public GamificationController(GamificationService gamificationService)
    {
        _gamificationService = gamificationService;
    }

    private int GetCurrentUserId()
    {
        var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(idStr, out int id) ? id : 0;
    }

    /// <summary>
    /// Lấy danh sách nhiệm vụ Daily và Weekly của người dùng
    /// </summary>
    [HttpGet("quests")]
    public async Task<ActionResult<List<QuestDto>>> GetQuests()
    {
        int userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var quests = await _gamificationService.GetActiveQuestsAsync(userId);
        return Ok(quests);
    }

    /// <summary>
    /// Nhận thưởng nhiệm vụ đã hoàn thành
    /// </summary>
    [HttpPost("quests/{questId:int}/claim")]
    public async Task<IActionResult> ClaimQuest(int questId)
    {
        int userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized();

        var result = await _gamificationService.ClaimQuestRewardAsync(questId, userId);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message, xpEarned = result.XPEarned });
    }

    /// <summary>
    /// Lấy bảng xếp hạng theo khung thời gian (week, month, all)
    /// </summary>
    [HttpGet("leaderboard")]
    [AllowAnonymous]
    public async Task<ActionResult<List<LeaderboardEntryDto>>> GetLeaderboard(
        [FromQuery] string timeframe = "all", 
        [FromQuery] int limit = 20)
    {
        int userId = GetCurrentUserId();
        var list = await _gamificationService.GetLeaderboardAsync(userId, timeframe, limit);
        return Ok(list);
    }

    public class RevokeXPRequest
    {
        public int TargetUserId { get; set; }
        public int XPToDeduct { get; set; }
        public string ClinicalReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Bác sĩ / Quản trị viên thu hồi điểm XP bất thường (Anti-cheat & Giám sát y khoa)
    /// </summary>
    [HttpPost("revoke-xp")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> RevokeXP([FromBody] RevokeXPRequest req)
    {
        int actorId = GetCurrentUserId();
        string actorRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Doctor";

        var result = await _gamificationService.RevokeSuspiciousXPAsync(
            actorId, 
            actorRole, 
            req.TargetUserId, 
            req.XPToDeduct, 
            req.ClinicalReason);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }
}
