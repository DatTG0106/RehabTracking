using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Services;

public enum TriageSeverity
{
    GreenSafe,
    AmberCaution,
    RedEmergency
}

public class AssistantResponseDto
{
    public string Message { get; set; } = string.Empty;
    public TriageSeverity Severity { get; set; } = TriageSeverity.GreenSafe;
    public bool IsEmergency { get; set; } = false;
    public List<string> SuggestedActions { get; set; } = new();
    public List<string> RelevantArticles { get; set; } = new();
    public List<string> RelevantExercises { get; set; } = new();
}

public class AiHealthAssistantService
{
    private readonly RehabTrackingContext _db;

    public AiHealthAssistantService(RehabTrackingContext db)
    {
        _db = db;
    }

    public async Task<AssistantResponseDto> ProcessUserQueryAsync(string userMessage, int? patientId = null)
    {
        var response = new AssistantResponseDto();
        var lower = userMessage.ToLower().Trim();

        // 1. KIỂM TRA RED FLAGS (CẢNH BÁO NGUY CẤP Y TẾ)
        var redFlagResult = CheckRedFlags(lower);
        if (redFlagResult.IsEmergency)
        {
            response.Severity = TriageSeverity.RedEmergency;
            response.IsEmergency = true;
            response.Message = "🚨 **CẢNH BÁO NGUY CẤP Y TẾ (RED FLAG):** " + redFlagResult.WarningMessage;
            response.SuggestedActions.AddRange(new[]
            {
                "Dừng ngay lập tức mọi bài tập vận động!",
                "Liên hệ Trung tâm Cấp cứu 115 hoặc đến ngay Bệnh viện / Phòng khám gần nhất",
                "Gọi điện trực tiếp cho Bác sĩ phụ trách điều trị của bạn",
                "Tuyệt đối không tự ý nắn bóp, bẻ khớp hoặc đắp thuốc lá dân gian"
            });
            return response;
        }

        // 2. KIỂM TRA CẢNH BÁO ĐAU VỪA (AMBER CAUTION)
        if (lower.Contains("đau nhói") || lower.Contains("đau tăng") || lower.Contains("sưng nhẹ") || lower.Contains("nóng khớp") || lower.Contains("chấn thương mới"))
        {
            response.Severity = TriageSeverity.AmberCaution;
            response.SuggestedActions.Add("Áp dụng nguyên tắc R.I.C.E (Nghỉ ngơi - Chườm lạnh 15-20 phút - Băng ép nhẹ - Kê cao chi)");
            response.SuggestedActions.Add("Tạm thời dừng bài tập gây đau và ghi nhận điểm đau vào Nhật ký phục hồi");
            response.SuggestedActions.Add("Theo dõi thêm 24h, nếu không giảm hãy liên hệ bác sĩ");
        }

        // 3. RAG SEARCH: TRUY VẤN CẨM NANG & BÀI TẬP LIÊN QUAN
        var keywords = lower.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(k => k.Length > 2)
            .ToList();

        var articles = await _db.NutritionArticles
            .Where(a => keywords.Any(k => a.Title.Contains(k) || a.Category.Contains(k) || a.ComorbidityTags.Contains(k)))
            .Take(3)
            .ToListAsync();

        var exercises = await _db.Exercises
            .Where(e => keywords.Any(k => e.Title.Contains(k) || e.TargetArea.Contains(k)))
            .Take(3)
            .ToListAsync();

        foreach (var art in articles)
        {
            response.RelevantArticles.Add($"📖 {art.Title} ({art.Category})");
        }

        foreach (var ex in exercises)
        {
            response.RelevantExercises.Add($"🏋️ {ex.Title} - Vùng {ex.TargetArea} (Độ khó {ex.Difficulty}/5)");
        }

        // 4. TỔNG HỢP NỘI DUNG TƯ VẤN
        if (lower.Contains("chườm") || lower.Contains("lạnh") || lower.Contains("nóng"))
        {
            response.Message = "🧊 **Quy tắc Chườm Lạnh vs Chườm Nóng chuẩn Y khoa:**\n\n" +
                "- **Chườm LẠNH (Ice):** Dành cho chấn thương cấp tính (trong vòng 48h đầu), khi khớp/cơ có dấu hiệu sưng, nóng, đỏ, viêm. Chườm 15-20 phút/lần qua một lớp khăn mỏng.\n" +
                "- **Chườm NÓNG (Heat):** Dành cho cơn đau cơ mãn tính, co thắt cứng cơ vào buổi sáng hoặc trước khi bắt đầu bài tập kéo giãn để làm mềm mô liên kết.\n\n" +
                "⚠️ *Lưu ý:* Không chườm lạnh trực tiếp đá lên da và không chườm nóng lên vết thương hở hoặc khớp đang sưng viêm cấp.";
            response.SuggestedActions.Add("Đọc thêm cẩm nang: Chăm sóc phục hồi sau tập");
        }
        else if (lower.Contains("dinh dưỡng") || lower.Contains("ăn gì") || lower.Contains("kháng viêm") || lower.Contains("sụn"))
        {
            response.Message = "🥗 **Chế độ dinh dưỡng hỗ trợ phục hồi & Kháng viêm:**\n\n" +
                "- **Thực phẩm kháng viêm:** Tăng cường acid béo Omega-3 (cá hồi, hạt chia), củ nghệ (Curcumin), quả mọng (dâu tây, việt quất) và rau lá xanh đậm.\n" +
                "- **Tái tạo sụn & xương dưới sụn:** Bổ sung thực phẩm giàu Collagen Type 2, Glucosamine tự nhiên từ nước hầm xương, Vitamin C và Vitamin D3 + K2.\n" +
                "- **Cần hạn chế:** Đồ chiên rán nhiều dầu mỡ, đường tinh luyện, thịt đỏ chế biến sẵn và rượu bia vì làm gia tăng phản ứng viêm tại ổ khớp.";
        }
        else if (lower.Contains("tập") || lower.Contains("bài tập") || lower.Contains("gối") || lower.Contains("lưng") || lower.Contains("cổ"))
        {
            response.Message = "💪 **Hướng dẫn an toàn khi tập Vật lý trị liệu:**\n\n" +
                "- Luôn khởi động nhẹ 3-5 phút trước khi vào bài tập chính.\n" +
                "- Giữ nhịp thở đều đặn (thở ra khi dùng lực, hít vào khi thả lỏng), tuyệt đối không nín thở.\n" +
                "- Tuân thủ ngưỡng đau an toàn (VAS $\\le$ 3/10). Nếu xuất hiện cảm giác đau nhói như kim châm hoặc lan dọc xuống chân/tay, hãy ấn nút **Dừng Khẩn Cấp** trên Smart Player ngay.";
        }
        else
        {
            response.Message = "Chào bạn! Tôi là **Trợ lý Sức khỏe Ảo STEP (AI Health Assistant)**. Tôi có thể hỗ trợ bạn:\n\n" +
                "1. Tra cứu kỹ thuật bài tập phục hồi và phân tích lỗi sai tư thế.\n" +
                "2. Hướng dẫn quy tắc xử lý sau tập (chườm nóng/lạnh, nghỉ ngơi R.I.C.E).\n" +
                "3. Tư vấn thực đơn dinh dưỡng kháng viêm và tái tạo sụn khớp.\n" +
                "4. Sàng lọc triệu chứng đau và cảnh báo dấu hiệu nguy hiểm (Red Flags).\n\n" +
                "Bạn đang gặp khó khăn hay cần hỗ trợ về vùng khớp hoặc bài tập nào?";
        }

        return response;
    }

    private static (bool IsEmergency, string WarningMessage) CheckRedFlags(string text)
    {
        // Danh sách triệu chứng khẩn cấp y tế
        if (text.Contains("mất kiểm soát tiểu") || text.Contains("bí tiểu đột ngột") || text.Contains("đại tiểu tiện"))
        {
            return (true, "Nghi ngờ hội chứng Chùm đuôi ngựa (Cauda Equina Syndrome). Cần cấp cứu ngoại thần kinh ngay lập tức!");
        }
        if (text.Contains("sốt cao") && (text.Contains("sưng") || text.Contains("mủ") || text.Contains("đỏ nóng")))
        {
            return (true, "Nghi ngờ Viêm khớp nhiễm khuẩn cấp hoặc nhiễm trùng sau phẫu thuật!");
        }
        if (text.Contains("tê liệt") || text.Contains("mất hoàn toàn cảm giác") || text.Contains("liệt chân") || text.Contains("liệt tay"))
        {
            return (true, "Dấu hiệu tổn thương thần kinh vận động nghiêm trọng hoặc tai biến!");
        }
        if (text.Contains("đau ngực") || text.Contains("khó thở dữ dội") || text.Contains("choáng ngất"))
        {
            return (true, "Dấu hiệu nguy cấp tim mạch hoặc hô hấp khi vận động!");
        }
        return (false, string.Empty);
    }
}
