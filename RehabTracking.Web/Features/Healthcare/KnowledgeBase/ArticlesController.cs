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
public class ArticlesController : ControllerBase
{
    private readonly KnowledgeService _knowledgeService;

    public ArticlesController(KnowledgeService knowledgeService)
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
    /// Danh sách bài viết cẩm nang y khoa
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<NutritionArticle>>> GetArticles(
        [FromQuery] string? keyword = null,
        [FromQuery] string? category = null,
        [FromQuery] string? comorbidity = null)
    {
        var list = await _knowledgeService.GetArticlesAsync(keyword, category, comorbidity);
        return Ok(list);
    }

    /// <summary>
    /// Xem chi tiết một bài viết cẩm nang
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<NutritionArticle>> GetArticle(int id)
    {
        var article = await _knowledgeService.GetArticleByIdAsync(id);
        if (article == null) return NotFound();
        return Ok(article);
    }

    /// <summary>
    /// Bác sĩ / Quản trị viên tạo bài viết mới
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> CreateArticle([FromBody] UpsertArticleRequest req)
    {
        var (userId, role) = GetCurrentUser();
        req.ArticleId = 0;
        var created = await _knowledgeService.UpsertArticleAsync(req, userId, role);
        return CreatedAtAction(nameof(GetArticle), new { id = created.ArticleId }, created);
    }

    /// <summary>
    /// Bác sĩ / Quản trị viên cập nhật bài viết
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdateArticle(int id, [FromBody] UpsertArticleRequest req)
    {
        var (userId, role) = GetCurrentUser();
        req.ArticleId = id;
        try
        {
            var updated = await _knowledgeService.UpsertArticleAsync(req, userId, role);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Bác sĩ / Quản trị viên xóa bài viết
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> DeleteArticle(int id)
    {
        var (userId, role) = GetCurrentUser();
        var success = await _knowledgeService.DeleteArticleAsync(id, userId, role);
        if (!success) return NotFound();
        return Ok(new { success = true });
    }
}
