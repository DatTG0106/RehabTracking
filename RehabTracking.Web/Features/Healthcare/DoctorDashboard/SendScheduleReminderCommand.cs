using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace RehabTracking.Web.Features.Healthcare.DoctorDashboard;

public class SendScheduleReminderCommand : IRequest<SendReminderResult>
{
    public int PatientId { get; set; }
    public List<ExerciseRoutineItem> Exercises { get; set; } = new();
    public WorkoutSchedule Schedule { get; set; } = new();
}

public class SendReminderResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
}

public class SendScheduleReminderCommandHandler : IRequestHandler<SendScheduleReminderCommand, SendReminderResult>
{
    private readonly RehabTrackingContext _context;
    private readonly IConfiguration _config;

    public SendScheduleReminderCommandHandler(
        RehabTrackingContext context,
        IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<SendReminderResult> Handle(SendScheduleReminderCommand request, CancellationToken cancellationToken)
    {
        // Get patient email
        var patient = await _context.PatientProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.PatientId == request.PatientId, cancellationToken);

        if (patient?.User == null)
            return new SendReminderResult { Success = false, Message = "Không tìm thấy bệnh nhân." };

        var patientEmail = patient.User.Email;
        var patientName = patient.User.FullName;

        // Build email body
        var body = BuildEmailBody(patientName, request.Exercises, request.Schedule);

        try
        {
            var smtpHost = _config["Email:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var smtpUser = _config["Email:Username"] ?? "";
            var smtpPass = _config["Email:Password"] ?? "";
            var fromEmail = _config["Email:From"] ?? smtpUser;
            var fromName = _config["Email:FromName"] ?? "STEP RehabTracking";

            if (string.IsNullOrEmpty(smtpUser))
            {
                // Demo mode: log to console instead of sending
                Console.WriteLine($"[EMAIL DEMO] To: {patientEmail}");
                Console.WriteLine($"[EMAIL DEMO] Subject: Lịch tập phục hồi chức năng của bạn");
                Console.WriteLine($"[EMAIL DEMO] Body: {body}");

                return new SendReminderResult
                {
                    Success = true,
                    Message = $"[Demo] Email nhắc hẹn đã được ghi log (chưa cấu hình SMTP). Gửi tới: {patientEmail}",
                    PatientEmail = patientEmail
                };
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = "📅 Lịch tập phục hồi chức năng của bạn - STEP",
                Body = body,
                IsBodyHtml = true
            };
            mail.To.Add(patientEmail);

            await client.SendMailAsync(mail, cancellationToken);

            return new SendReminderResult
            {
                Success = true,
                Message = $"Đã gửi email nhắc hẹn thành công tới {patientEmail}",
                PatientEmail = patientEmail
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
            return new SendReminderResult
            {
                Success = false,
                Message = $"Lỗi gửi email: {ex.Message}",
                PatientEmail = patientEmail
            };
        }
    }

    private static string BuildEmailBody(string name, List<ExerciseRoutineItem> exercises, WorkoutSchedule schedule)
    {
        var dayNames = new[] { "Chủ nhật", "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7" };
        var days = schedule.DaysOfWeek.Select(d => dayNames[Math.Clamp(d, 0, 6)]);
        var daysStr = string.Join(", ", days);

        var sb = new StringBuilder();
        sb.Append($"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="UTF-8"></head>
        <body style="font-family: 'Segoe UI', Arial, sans-serif; background: #f8fafc; margin: 0; padding: 0;">
          <div style="max-width: 600px; margin: 32px auto; background: white; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,0.08);">
            <div style="background: linear-gradient(135deg, #4f46e5, #7c3aed); padding: 32px; text-align: center;">
              <h1 style="color: white; margin: 0; font-size: 1.8rem;">🏥 STEP RehabTracking</h1>
              <p style="color: rgba(255,255,255,0.85); margin: 8px 0 0;">Hệ thống theo dõi phục hồi chức năng</p>
            </div>
            <div style="padding: 32px;">
              <h2 style="color: #1e293b; margin-top: 0;">Xin chào {name}! 👋</h2>
              <p style="color: #64748b;">Bác sĩ của bạn đã cập nhật <strong>lộ trình tập luyện</strong>. Dưới đây là lịch tập của bạn:</p>
              
              <div style="background: #f0f9ff; border-left: 4px solid #4f46e5; padding: 16px; border-radius: 8px; margin: 20px 0;">
                <p style="margin: 0; font-weight: 600; color: #1e293b;">📅 Lịch tập: {daysStr}</p>
                <p style="margin: 4px 0 0; color: #4f46e5; font-weight: 600;">⏰ Giờ tập: {schedule.ReminderTime}</p>
              </div>

              <h3 style="color: #1e293b;">📋 Danh sách bài tập ({exercises.Count} bài)</h3>
        """);

        for (int i = 0; i < exercises.Count; i++)
        {
            var ex = exercises[i];
            sb.Append($"""
              <div style="border: 1px solid #e2e8f0; border-radius: 12px; padding: 16px; margin-bottom: 12px;">
                <div style="display: flex; justify-content: space-between; align-items: center;">
                  <div>
                    <h4 style="margin: 0; color: #1e293b;">{i + 1}. {ex.Name}</h4>
                    <p style="margin: 4px 0 0; color: #64748b; font-size: 0.9rem;">{ex.Sets} sets × {ex.Reps} reps • {ex.DurationSeconds}s giữ • Nghỉ {ex.RestSeconds}s</p>
                    {(string.IsNullOrEmpty(ex.Notes) ? "" : $"<p style=\"margin: 4px 0 0; color: #94a3b8; font-size: 0.85rem; font-style: italic;\">💬 {ex.Notes}</p>")}
                  </div>
                  <span style="background: #f0f9ff; color: #4f46e5; padding: 4px 12px; border-radius: 20px; font-weight: 600; white-space: nowrap;">{ex.Sets * ex.Reps} lần</span>
                </div>
              </div>
            """);
        }

        sb.Append($"""
              <div style="text-align: center; margin-top: 28px;">
                <a href="http://localhost:5003/patient/workout" 
                   style="display: inline-block; background: linear-gradient(135deg, #4f46e5, #7c3aed); color: white; padding: 14px 32px; border-radius: 50px; text-decoration: none; font-weight: 600; font-size: 1rem;">
                  🏃 Bắt đầu tập ngay
                </a>
              </div>
              <p style="color: #94a3b8; text-align: center; margin-top: 24px; font-size: 0.85rem;">
                Email này được gửi tự động từ hệ thống STEP RehabTracking.<br>Vui lòng không trả lời email này.
              </p>
            </div>
          </div>
        </body>
        </html>
        """);

        return sb.ToString();
    }
}
