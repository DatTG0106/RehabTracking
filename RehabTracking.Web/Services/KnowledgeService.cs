using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Services;

public class UpsertArticleRequest
{
    public int ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Kháng viêm";
    public string ComorbidityTags { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? AuthorDoctor { get; set; } = "BS. CKII Trần Minh Khoa";
    public int ReadTimeMinutes { get; set; } = 5;
    public bool IsFeatured { get; set; } = false;
}

public class UpsertMealPlanRequest
{
    public int MealPlanId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TargetCondition { get; set; } = "Thoái hóa khớp / Viêm khớp";
    public string Phase { get; set; } = "Giai đoạn phục hồi tích cực (Tuần 3-8)";
    public int CaloriesTarget { get; set; } = 1800;
    public int ProteinGrams { get; set; } = 85;
    public int CalciumMg { get; set; } = 1000;
    public string BreakfastMenu { get; set; } = string.Empty;
    public string LunchMenu { get; set; } = string.Empty;
    public string DinnerMenu { get; set; } = string.Empty;
    public string SnacksMenu { get; set; } = string.Empty;
    public string ClinicalNotes { get; set; } = string.Empty;
    public string AuthorDoctor { get; set; } = "BS. CKII Trần Minh Khoa";
}

/// <summary>
/// Quản lý cẩm nang kiến thức y khoa và thực đơn mẫu phục hồi chức năng
/// </summary>
public class KnowledgeService
{
    private readonly RehabTrackingContext _db;
    private readonly AuditService _auditService;

    public KnowledgeService(RehabTrackingContext db, AuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    #region Bài viết Cẩm nang (NutritionArticle)

    public async Task<List<NutritionArticle>> GetArticlesAsync(
        string? keyword = null, 
        string? category = null, 
        string? comorbidity = null)
    {
        var query = _db.NutritionArticles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(a => a.Title.Contains(keyword) || 
                                     a.Summary.Contains(keyword) || 
                                     a.ContentHtml.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(a => a.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(comorbidity))
        {
            query = query.Where(a => a.ComorbidityTags.Contains(comorbidity));
        }

        return await query
            .OrderByDescending(a => a.IsFeatured)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<NutritionArticle?> GetArticleByIdAsync(int id)
    {
        return await _db.NutritionArticles.FindAsync(id);
    }

    public async Task<NutritionArticle> UpsertArticleAsync(
        UpsertArticleRequest req, 
        int actorUserId, 
        string actorRole)
    {
        if (req.ArticleId == 0)
        {
            var article = new NutritionArticle
            {
                Title = req.Title,
                Category = req.Category,
                ComorbidityTags = req.ComorbidityTags,
                Summary = req.Summary,
                ContentHtml = req.ContentHtml,
                CoverImageUrl = req.CoverImageUrl,
                AuthorDoctor = req.AuthorDoctor,
                ReadTimeMinutes = req.ReadTimeMinutes,
                IsFeatured = req.IsFeatured,
                CreatedAt = DateTime.UtcNow
            };

            _db.NutritionArticles.Add(article);
            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "CREATE_ARTICLE",
                entityName: "NutritionArticle",
                recordId: article.ArticleId.ToString(),
                details: $"Bác sĩ tạo bài viết: {article.Title}",
                explicitUserId: actorUserId,
                explicitUserRole: actorRole);

            return article;
        }
        else
        {
            var existing = await _db.NutritionArticles.FindAsync(req.ArticleId);
            if (existing == null) throw new InvalidOperationException("Không tìm thấy bài viết.");

            existing.Title = req.Title;
            existing.Category = req.Category;
            existing.ComorbidityTags = req.ComorbidityTags;
            existing.Summary = req.Summary;
            existing.ContentHtml = req.ContentHtml;
            if (!string.IsNullOrEmpty(req.CoverImageUrl)) existing.CoverImageUrl = req.CoverImageUrl;
            existing.AuthorDoctor = req.AuthorDoctor;
            existing.ReadTimeMinutes = req.ReadTimeMinutes;
            existing.IsFeatured = req.IsFeatured;

            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "UPDATE_ARTICLE",
                entityName: "NutritionArticle",
                recordId: existing.ArticleId.ToString(),
                details: $"Cập nhật bài viết: {existing.Title}",
                explicitUserId: actorUserId,
                explicitUserRole: actorRole);

            return existing;
        }
    }

    public async Task<bool> DeleteArticleAsync(int id, int actorUserId, string actorRole)
    {
        var existing = await _db.NutritionArticles.FindAsync(id);
        if (existing == null) return false;

        _db.NutritionArticles.Remove(existing);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "DELETE_ARTICLE",
            entityName: "NutritionArticle",
            recordId: id.ToString(),
            details: $"Xóa bài viết: {existing.Title}",
            explicitUserId: actorUserId,
            explicitUserRole: actorRole);

        return true;
    }

    #endregion

    #region Thực đơn mẫu y khoa (DietaryMealPlan)

    public async Task<List<DietaryMealPlan>> GetMealPlansAsync(
        string? targetCondition = null, 
        string? phase = null)
    {
        var query = _db.DietaryMealPlans.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(targetCondition))
        {
            query = query.Where(m => m.TargetCondition.Contains(targetCondition));
        }

        if (!string.IsNullOrWhiteSpace(phase))
        {
            query = query.Where(m => m.Phase.Contains(phase));
        }

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<DietaryMealPlan?> GetMealPlanByIdAsync(int id)
    {
        return await _db.DietaryMealPlans.FindAsync(id);
    }

    public async Task<DietaryMealPlan> UpsertMealPlanAsync(
        UpsertMealPlanRequest req, 
        int actorUserId, 
        string actorRole)
    {
        if (req.MealPlanId == 0)
        {
            var plan = new DietaryMealPlan
            {
                Title = req.Title,
                TargetCondition = req.TargetCondition,
                Phase = req.Phase,
                CaloriesTarget = req.CaloriesTarget,
                ProteinGrams = req.ProteinGrams,
                CalciumMg = req.CalciumMg,
                BreakfastMenu = req.BreakfastMenu,
                LunchMenu = req.LunchMenu,
                DinnerMenu = req.DinnerMenu,
                SnacksMenu = req.SnacksMenu,
                ClinicalNotes = req.ClinicalNotes,
                AuthorDoctor = req.AuthorDoctor,
                CreatedAt = DateTime.UtcNow
            };

            _db.DietaryMealPlans.Add(plan);
            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "CREATE_MEAL_PLAN",
                entityName: "DietaryMealPlan",
                recordId: plan.MealPlanId.ToString(),
                details: $"Tạo thực đơn mẫu y khoa: {plan.Title}",
                explicitUserId: actorUserId,
                explicitUserRole: actorRole);

            return plan;
        }
        else
        {
            var existing = await _db.DietaryMealPlans.FindAsync(req.MealPlanId);
            if (existing == null) throw new InvalidOperationException("Không tìm thấy thực đơn mẫu.");

            existing.Title = req.Title;
            existing.TargetCondition = req.TargetCondition;
            existing.Phase = req.Phase;
            existing.CaloriesTarget = req.CaloriesTarget;
            existing.ProteinGrams = req.ProteinGrams;
            existing.CalciumMg = req.CalciumMg;
            existing.BreakfastMenu = req.BreakfastMenu;
            existing.LunchMenu = req.LunchMenu;
            existing.DinnerMenu = req.DinnerMenu;
            existing.SnacksMenu = req.SnacksMenu;
            existing.ClinicalNotes = req.ClinicalNotes;
            existing.AuthorDoctor = req.AuthorDoctor;

            await _db.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "UPDATE_MEAL_PLAN",
                entityName: "DietaryMealPlan",
                recordId: existing.MealPlanId.ToString(),
                details: $"Cập nhật thực đơn mẫu: {existing.Title}",
                explicitUserId: actorUserId,
                explicitUserRole: actorRole);

            return existing;
        }
    }

    public async Task<bool> DeleteMealPlanAsync(int id, int actorUserId, string actorRole)
    {
        var existing = await _db.DietaryMealPlans.FindAsync(id);
        if (existing == null) return false;

        _db.DietaryMealPlans.Remove(existing);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(
            action: "DELETE_MEAL_PLAN",
            entityName: "DietaryMealPlan",
            recordId: id.ToString(),
            details: $"Xóa thực đơn mẫu: {existing.Title}",
            explicitUserId: actorUserId,
            explicitUserRole: actorRole);

        return true;
    }

    #endregion
}
