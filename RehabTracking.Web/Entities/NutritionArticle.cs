using System;

namespace RehabTracking.Web.Entities;

/// <summary>
/// Cẩm nang dinh dưỡng và chăm sóc khi tập vật lý trị liệu
/// </summary>
public partial class NutritionArticle
{
    public int ArticleId { get; set; }

    public string Title { get; set; } = null!;

    /// <summary>Danh mục: Kháng viêm, Tái tạo sụn khớp, Tăng cường cơ bắp, Chăm sóc sau tập, Tư thế sinh hoạt</summary>
    public string Category { get; set; } = "Kháng viêm";

    /// <summary>Thẻ lọc bệnh lý đi kèm (Comma-separated: Tiểu đường, Tim mạch, Thoái hóa khớp, Gout...)</summary>
    public string ComorbidityTags { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string ContentHtml { get; set; } = string.Empty;

    public string? CoverImageUrl { get; set; }

    public string? AuthorDoctor { get; set; }

    public int ReadTimeMinutes { get; set; } = 4;

    public bool IsFeatured { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
