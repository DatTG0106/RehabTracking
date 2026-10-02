using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using RehabTracking.Web.Services;
using Xunit;

namespace RehabTracking.Tests;

public class KnowledgeAndNutritionTests
{
    private RehabTrackingContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<RehabTrackingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RehabTrackingContext(options);
    }

    [Fact]
    public async Task GetArticles_FilterByKeywordAndCategory_ShouldReturnCorrectSubset()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var audit = new AuditService(db);
        var service = new KnowledgeService(db, audit);

        db.NutritionArticles.AddRange(
            new NutritionArticle
            {
                Title = "Chế độ ăn giàu Omega-3 kháng viêm khớp gối",
                Category = "Kháng viêm",
                ComorbidityTags = "Thoái hóa khớp, Gout",
                Summary = "Các loại cá béo và hạt giúp giảm sưng đau khớp.",
                ContentHtml = "<p>Nội dung...</p>"
            },
            new NutritionArticle
            {
                Title = "Bổ sung Canxi và Vitamin D3 cho người gãy xương",
                Category = "Tái tạo sụn khớp",
                ComorbidityTags = "Gãy xương",
                Summary = "Hướng dẫn liều lượng canxi mỗi ngày.",
                ContentHtml = "<p>Nội dung...</p>"
            },
            new NutritionArticle
            {
                Title = "Phương pháp chườm lạnh sau buổi tập VLTL",
                Category = "Chăm sóc sau tập",
                Summary = "Quy tắc 20 phút chườm đá giảm phù nề.",
                ContentHtml = "<p>Nội dung...</p>"
            }
        );
        await db.SaveChangesAsync();

        // Act 1: Lọc theo chuyên mục "Kháng viêm"
        var antiInflammatory = await service.GetArticlesAsync(category: "Kháng viêm");
        Assert.Single(antiInflammatory);
        Assert.Contains("Omega-3", antiInflammatory[0].Title);

        // Act 2: Tìm kiếm theo từ khóa "chườm"
        var searchResult = await service.GetArticlesAsync(keyword: "chườm");
        Assert.Single(searchResult);
        Assert.Equal("Chăm sóc sau tập", searchResult[0].Category);

        // Act 3: Lọc theo bệnh lý đi kèm "Gãy xương"
        var fractureArticles = await service.GetArticlesAsync(comorbidity: "Gãy xương");
        Assert.Single(fractureArticles);
        Assert.Contains("Canxi", fractureArticles[0].Title);
    }

    [Fact]
    public async Task UpsertArticle_DoctorCreateAndAudit_ShouldPersistAndLog()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var audit = new AuditService(db);
        var service = new KnowledgeService(db, audit);

        int doctorId = 505;
        db.Users.Add(new User { UserId = doctorId, FullName = "BS. Minh Khoa", Email = "khoa@test.com", RoleId = 2, PasswordHash = "hash123" });
        await db.SaveChangesAsync();

        var req = new UpsertArticleRequest
        {
            ArticleId = 0,
            Title = "Tư thế ngồi công thái học cho nhân viên văn phòng",
            Category = "Tư thế sinh hoạt",
            Summary = "Cách chỉnh chiều cao ghế và màn hình máy tính chuẩn y khoa.",
            ContentHtml = "<p>Màn hình ngang tầm mắt...</p>",
            AuthorDoctor = "BS. Minh Khoa",
            ReadTimeMinutes = 6,
            IsFeatured = true
        };

        // Act: Bác sĩ đăng bài viết mới
        var created = await service.UpsertArticleAsync(req, doctorId, "Doctor");

        // Assert
        Assert.NotNull(created);
        Assert.True(created.ArticleId > 0);
        Assert.True(created.IsFeatured);

        var articleInDb = await db.NutritionArticles.FindAsync(created.ArticleId);
        Assert.NotNull(articleInDb);
        Assert.Equal("Tư thế sinh hoạt", articleInDb!.Category);

        // Kiểm tra Audit Log được ghi nhận
        var auditEntry = await db.AuditLogs.FirstOrDefaultAsync(a => a.Action == "CREATE_ARTICLE");
        Assert.NotNull(auditEntry);
        Assert.Equal("NutritionArticle", auditEntry!.EntityName);
        Assert.Equal(doctorId, auditEntry.UserId);
        Assert.Contains("Tư thế ngồi công thái học", auditEntry.Details);
    }

    [Fact]
    public async Task GetMealPlans_FilterByConditionAndPhase_ShouldReturnMatchingPlans()
    {
        // Arrange
        using var db = CreateInMemoryContext();
        var audit = new AuditService(db);
        var service = new KnowledgeService(db, audit);

        db.DietaryMealPlans.AddRange(
            new DietaryMealPlan
            {
                Title = "Thực đơn phục hồi gãy xương tích cực",
                TargetCondition = "Gãy xương / Liền xương",
                Phase = "Giai đoạn phục hồi tích cực (Tuần 3-8)",
                CaloriesTarget = 2000,
                ProteinGrams = 95,
                CalciumMg = 1200,
                BreakfastMenu = "Phở bò, sữa canxi",
                LunchMenu = "Cơm, cá hồi",
                DinnerMenu = "Gà rim hạt sen",
                AuthorDoctor = "BS. Khoa"
            },
            new DietaryMealPlan
            {
                Title = "Thực đơn kháng viêm khớp duy trì",
                TargetCondition = "Thoái hóa khớp / Viêm khớp",
                Phase = "Giai đoạn củng cố & duy trì",
                CaloriesTarget = 1600,
                ProteinGrams = 75,
                CalciumMg = 900,
                BreakfastMenu = "Cháo yến mạch tôm",
                LunchMenu = "Cá thu kho",
                DinnerMenu = "Salad cá ngừ",
                AuthorDoctor = "BS. Nhung"
            }
        );
        await db.SaveChangesAsync();

        // Act 1: Lọc theo bệnh lý "Gãy xương"
        var fracturePlans = await service.GetMealPlansAsync(targetCondition: "Gãy xương");
        Assert.Single(fracturePlans);
        Assert.Equal(1200, fracturePlans[0].CalciumMg);

        // Act 2: Lọc theo giai đoạn "duy trì"
        var maintenancePlans = await service.GetMealPlansAsync(phase: "duy trì");
        Assert.Single(maintenancePlans);
        Assert.Contains("kháng viêm", maintenancePlans[0].Title, StringComparison.OrdinalIgnoreCase);
    }
}
