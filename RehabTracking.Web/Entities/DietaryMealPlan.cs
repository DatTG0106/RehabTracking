using System;

namespace RehabTracking.Web.Entities;

/// <summary>
/// Thực đơn mẫu y khoa theo từng bệnh lý cơ xương khớp và giai đoạn phục hồi
/// </summary>
public class DietaryMealPlan
{
    public int MealPlanId { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Bệnh lý mục tiêu: Gãy xương / Liền xương, Thoái hóa khớp / Viêm khớp, Hồi phục sau mổ ACL, Teo cơ người cao tuổi
    /// </summary>
    public string TargetCondition { get; set; } = "Thoái hóa khớp / Viêm khớp";

    /// <summary>
    /// Giai đoạn hồi phục: Giai đoạn cấp (Tuần 1-2), Giai đoạn phục hồi tích cực (Tuần 3-8), Giai đoạn củng cố & duy trì
    /// </summary>
    public string Phase { get; set; } = "Giai đoạn phục hồi tích cực (Tuần 3-8)";

    /// <summary>
    /// Năng lượng mục tiêu ước tính (kcal/ngày)
    /// </summary>
    public int CaloriesTarget { get; set; } = 1800;

    /// <summary>
    /// Lượng Protein khuyến nghị (gram/ngày)
    /// </summary>
    public int ProteinGrams { get; set; } = 85;

    /// <summary>
    /// Lượng Canxi khuyến nghị (mg/ngày)
    /// </summary>
    public int CalciumMg { get; set; } = 1000;

    /// <summary>
    /// Thực đơn bữa sáng
    /// </summary>
    public string BreakfastMenu { get; set; } = string.Empty;

    /// <summary>
    /// Thực đơn bữa trưa
    /// </summary>
    public string LunchMenu { get; set; } = string.Empty;

    /// <summary>
    /// Thực đơn bữa tối
    /// </summary>
    public string DinnerMenu { get; set; } = string.Empty;

    /// <summary>
    /// Thực đơn bữa phụ (sữa chua, hạt óc chó, trái cây giàu vitamin C)
    /// </summary>
    public string SnacksMenu { get; set; } = string.Empty;

    /// <summary>
    /// Chỉ dẫn lâm sàng và lưu ý y khoa (tương tác thuốc, hạn chế muối, dị ứng)
    /// </summary>
    public string ClinicalNotes { get; set; } = string.Empty;

    public string AuthorDoctor { get; set; } = "BS. CKII Trần Minh Khoa";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
