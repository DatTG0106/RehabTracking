using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Services;

public class RecoveryChartPointDto
{
    public string DateLabel { get; set; } = string.Empty;
    public int PainPreWorkout { get; set; }
    public int PainScoreVAS { get; set; }
    public double ROM { get; set; }
    public double CompletionRate { get; set; }
    public int DurationMinutes { get; set; }
    public double? SleepHours { get; set; }
    public string? MoodLevel { get; set; }
}

public class CalendarHeatmapDayDto
{
    public DateTime Date { get; set; }
    public string DateString => Date.ToString("yyyy-MM-dd");
    public string ShortLabel => Date.ToString("dd/MM");
    public int DayOfWeek => (int)Date.DayOfWeek;
    public bool HasWorkout { get; set; }
    public double CompletionRate { get; set; }
    public int PainScoreVAS { get; set; }
    public int ColorLevel { get; set; } // 0: không tập, 1: <50%, 2: 50-80%, 3: 80-100%
    public string Tooltip { get; set; } = string.Empty;
}

public class RecoverySummaryStatsDto
{
    public int TotalSessions { get; set; }
    public double AvgPainCurrentWeek { get; set; }
    public double AvgPainPreviousWeek { get; set; }
    public double PainImprovementPercent { get; set; }
    public double LatestROM { get; set; }
    public double ROMImprovementDegrees { get; set; }
    public double ComplianceRate { get; set; }
    public int HighPainSessionsCount { get; set; }
}

public class ProgressReportDto
{
    public string PatientName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string? PatientGender { get; set; }
    public string? DateOfBirth { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;
    public RecoverySummaryStatsDto Stats { get; set; } = new();
    public List<RecoveryLog> RecentLogs { get; set; } = new();
    public string GeneralDoctorAssessment { get; set; } = string.Empty;
}

public class RecoveryTrackingService
{
    private readonly RehabTrackingContext _db;
    private readonly GamificationService _gamificationService;

    public RecoveryTrackingService(RehabTrackingContext db, GamificationService gamificationService)
    {
        _db = db;
        _gamificationService = gamificationService;
    }

    public async Task<(RecoveryLog Log, GamificationRewardResult Reward, bool HighPainAlert, string ClinicalAdvice)> LogSessionProgressAsync(
        int patientId,
        int? exerciseId,
        int painVAS,
        double? rom,
        int fatigueLevel,
        double completionRate,
        int durationMinutes,
        string? note,
        string? mediaUrlsJson,
        int painPreWorkout = 0,
        double? sleepHours = null,
        string? moodLevel = "Bình thường")
    {
        var patient = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.PatientId == patientId);
        if (patient == null)
            throw new ArgumentException("Bệnh nhân không tồn tại.");

        // Chống gửi trùng lặp / spam click liên tiếp trong vòng 3 giây
        var recentDuplicate = await _db.RecoveryLogs
            .Where(l => l.PatientId == patientId 
                     && l.ExerciseId == exerciseId 
                     && l.PainScoreVAS == Math.Clamp(painVAS, 0, 10)
                     && l.CreatedAt >= DateTime.UtcNow.AddSeconds(-3))
            .FirstOrDefaultAsync();
        if (recentDuplicate != null)
        {
            return (recentDuplicate, new GamificationRewardResult { EncouragementMessage = "Nhật ký đã được ghi nhận trước đó." }, painVAS >= 7, "Nhật ký đã được ghi nhận thành công.");
        }

        var log = new RecoveryLog
        {
            PatientId = patientId,
            ExerciseId = exerciseId,
            LogDate = DateTime.UtcNow,
            PainPreWorkout = Math.Clamp(painPreWorkout, 0, 10),
            PainScoreVAS = Math.Clamp(painVAS, 0, 10),
            SleepHours = sleepHours,
            MoodLevel = moodLevel,
            ROMMeasurement = rom,
            FatigueLevel = fatigueLevel,
            CompletionRate = completionRate,
            DurationMinutes = durationMinutes,
            PatientNote = note,
            MediaUrlsJson = mediaUrlsJson,
            DoctorId = patient.DoctorId,
            CreatedAt = DateTime.UtcNow
        };

        _db.RecoveryLogs.Add(log);
        await _db.SaveChangesAsync();

        // Xử lý Gamification
        var reward = await _gamificationService.ProcessWorkoutCompletionAsync(
            patient.UserId,
            completionRate,
            painVAS,
            !string.IsNullOrWhiteSpace(note),
            !string.IsNullOrWhiteSpace(mediaUrlsJson)
        );

        // Kiểm tra lịch sử 2 buổi gần nhất để phát hiện đau cao liên tục
        var previousLogs = await _db.RecoveryLogs
            .Where(l => l.PatientId == patientId && l.LogId != log.LogId)
            .OrderByDescending(l => l.LogDate)
            .Take(2)
            .ToListAsync();

        bool highPainAlert = painVAS >= 7;
        bool consecutiveHighPain = previousLogs.Any() && previousLogs.All(l => l.PainScoreVAS >= 6) && painVAS >= 6;

        string clinicalAdvice;
        if (highPainAlert || consecutiveHighPain)
        {
            clinicalAdvice = "🚨 CẢNH BÁO Y KHOA: Mức đau sau tập cao hoặc kéo dài liên tục nhiều buổi. Hệ thống đã kích hoạt cờ theo dõi gửi đến Bác sĩ điều trị. Bạn nên chườm lạnh 15-20 phút, giảm tải trọng buổi sau và liên hệ bác sĩ nếu cơn đau không dịu đi.";
        }
        else if (painVAS - painPreWorkout >= 3)
        {
            clinicalAdvice = "⚠️ Lưu ý: Mức độ đau tăng đáng kể sau buổi tập. Hãy chú ý nghỉ ngơi đủ giấc và không gượng ép vận động quá sức.";
        }
        else if (completionRate >= 95 && painVAS <= 2)
        {
            clinicalAdvice = "👏 Tuyệt vời! Bạn đang thích ứng rất tốt với bài tập. Cơ và khớp đang hồi phục đúng tiến độ!";
        }
        else
        {
            clinicalAdvice = "Buổi tập đã hoàn thành an toàn. Duy trì đều đặn để đạt hiệu quả phục hồi tối đa.";
        }

        return (log, reward, highPainAlert || consecutiveHighPain, clinicalAdvice);
    }

    public async Task<List<RecoveryChartPointDto>> GetRecoveryChartDataAsync(int patientId, int days = 14)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var logs = await _db.RecoveryLogs
            .Where(l => l.PatientId == patientId && l.LogDate >= cutoff)
            .OrderBy(l => l.LogDate)
            .ToListAsync();

        return logs.Select(l => new RecoveryChartPointDto
        {
            DateLabel = l.LogDate.ToString("dd/MM"),
            PainPreWorkout = l.PainPreWorkout,
            PainScoreVAS = l.PainScoreVAS,
            ROM = l.ROMMeasurement ?? 0,
            CompletionRate = l.CompletionRate,
            DurationMinutes = l.DurationMinutes,
            SleepHours = l.SleepHours,
            MoodLevel = l.MoodLevel
        }).ToList();
    }

    /// <summary>
    /// Lấy ma trận dữ liệu lịch Heatmap các ngày đã tập (chuẩn lịch GitHub/Calendar)
    /// </summary>
    public async Task<List<CalendarHeatmapDayDto>> GetCalendarHeatmapDataAsync(int patientId, int days = 28)
    {
        var today = DateTime.UtcNow.Date;
        var startDate = today.AddDays(-days + 1);

        var logs = await _db.RecoveryLogs
            .Where(l => l.PatientId == patientId && l.LogDate >= startDate)
            .ToListAsync();

        var logsByDate = logs
            .GroupBy(l => l.LogDate.Date)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.LogDate).First());

        var result = new List<CalendarHeatmapDayDto>();
        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            if (logsByDate.TryGetValue(date, out var log))
            {
                int colorLevel = log.CompletionRate >= 90 ? 3 : (log.CompletionRate >= 50 ? 2 : 1);
                result.Add(new CalendarHeatmapDayDto
                {
                    Date = date,
                    HasWorkout = true,
                    CompletionRate = log.CompletionRate,
                    PainScoreVAS = log.PainScoreVAS,
                    ColorLevel = colorLevel,
                    Tooltip = $"{date:dd/MM/yyyy}: Đã tập {log.DurationMinutes} phút • Đau: {log.PainScoreVAS}/10 • Hoàn thành: {log.CompletionRate:0}%"
                });
            }
            else
            {
                result.Add(new CalendarHeatmapDayDto
                {
                    Date = date,
                    HasWorkout = false,
                    CompletionRate = 0,
                    PainScoreVAS = 0,
                    ColorLevel = 0,
                    Tooltip = $"{date:dd/MM/yyyy}: Không có buổi tập"
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Thống kê xu hướng phục hồi: độ giảm đau, tiến bộ ROM, tuân thủ
    /// </summary>
    public async Task<RecoverySummaryStatsDto> GetRecoverySummaryStatsAsync(int patientId)
    {
        var logs = await _db.RecoveryLogs
            .Where(l => l.PatientId == patientId)
            .OrderByDescending(l => l.LogDate)
            .ToListAsync();

        var stats = new RecoverySummaryStatsDto();
        stats.TotalSessions = logs.Count;
        if (!logs.Any()) return stats;

        var now = DateTime.UtcNow;
        var week1Logs = logs.Where(l => (now - l.LogDate).TotalDays <= 7).ToList();
        var week2Logs = logs.Where(l => (now - l.LogDate).TotalDays > 7 && (now - l.LogDate).TotalDays <= 14).ToList();

        stats.AvgPainCurrentWeek = week1Logs.Any() ? Math.Round(week1Logs.Average(l => l.PainScoreVAS), 1) : Math.Round(logs.Take(3).Average(l => l.PainScoreVAS), 1);
        stats.AvgPainPreviousWeek = week2Logs.Any() ? Math.Round(week2Logs.Average(l => l.PainScoreVAS), 1) : stats.AvgPainCurrentWeek;

        if (stats.AvgPainPreviousWeek > 0)
        {
            stats.PainImprovementPercent = Math.Round(((stats.AvgPainPreviousWeek - stats.AvgPainCurrentWeek) / stats.AvgPainPreviousWeek) * 100.0, 1);
        }

        var romLogs = logs.Where(l => l.ROMMeasurement.HasValue).ToList();
        if (romLogs.Any())
        {
            stats.LatestROM = romLogs.First().ROMMeasurement!.Value;
            var earliestROM = romLogs.Last().ROMMeasurement!.Value;
            stats.ROMImprovementDegrees = Math.Round(stats.LatestROM - earliestROM, 1);
        }

        stats.ComplianceRate = Math.Round(logs.Average(l => l.CompletionRate), 1);
        stats.HighPainSessionsCount = logs.Count(l => l.PainScoreVAS >= 7);

        return stats;
    }

    /// <summary>
    /// Bác sĩ để lại nhận xét y khoa và hướng dẫn điều trị vào nhật ký
    /// </summary>
    public async Task<bool> AddDoctorFeedbackAsync(int logId, int doctorId, string feedback)
    {
        var log = await _db.RecoveryLogs.FindAsync(logId);
        if (log == null) return false;

        log.DoctorId = doctorId;
        log.DoctorFeedback = feedback.Trim();
        log.FeedbackAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Tổng hợp dữ liệu đầy đủ phục vụ xuất Báo cáo Tiến trình Phục hồi (PDF / Bản in y tế)
    /// </summary>
    public async Task<ProgressReportDto> GetProgressReportDataAsync(int patientId)
    {
        var patient = await _db.PatientProfiles
            .Include(p => p.User)
            .Include(p => p.Doctor)
            .FirstOrDefaultAsync(p => p.PatientId == patientId);

        var report = new ProgressReportDto();
        if (patient != null)
        {
            report.PatientName = patient.User?.FullName ?? "Bệnh nhân";
            report.PatientEmail = patient.User?.Email ?? "";
            report.PatientGender = patient.Gender ?? "Không xác định";
            report.DateOfBirth = patient.DateOfBirth?.ToString("dd/MM/yyyy") ?? "—";
            report.DoctorName = patient.Doctor?.FullName ?? "BS. Chuyên khoa VLTL";
        }

        // Đọc chẩn đoán từ EHR
        var ehr = await _db.ElectronicHealthRecords
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();
        if (ehr != null)
        {
            report.Diagnosis = ehr.InitialDiagnosis;
        }

        report.Stats = await GetRecoverySummaryStatsAsync(patientId);

        report.RecentLogs = await _db.RecoveryLogs
            .Where(l => l.PatientId == patientId)
            .Include(l => l.Exercise)
            .OrderByDescending(l => l.LogDate)
            .Take(20)
            .ToListAsync();

        return report;
    }
}
