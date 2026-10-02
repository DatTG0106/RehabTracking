using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Services;

public class ReminderScheduleDto
{
    public int ReminderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ReminderType { get; set; } = "Exercise";
    public TimeSpan TimeOfDay { get; set; }
    public string DaysOfWeek { get; set; } = "All";
    public bool IsActive { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
}

public class UpsertReminderScheduleRequest
{
    public int ReminderId { get; set; }
    public int PatientId { get; set; }
    public string Title { get; set; } = "Nhắc nhở tập luyện";
    public string ReminderType { get; set; } = "Exercise";
    public TimeSpan TimeOfDay { get; set; } = new TimeSpan(8, 0, 0);
    public string DaysOfWeek { get; set; } = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday";
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Quản lý lịch nhắc nhở cá nhân cho bệnh nhân
/// </summary>
public class ReminderService
{
    private readonly RehabTrackingContext _db;

    public ReminderService(RehabTrackingContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Lấy danh sách lịch nhắc nhở của bệnh nhân
    /// </summary>
    public async Task<List<ReminderScheduleDto>> GetPatientSchedulesAsync(int patientId)
    {
        var schedules = await _db.ReminderSchedules
            .AsNoTracking()
            .Where(r => r.PatientId == patientId)
            .OrderBy(r => r.TimeOfDay)
            .Select(r => new ReminderScheduleDto
            {
                ReminderId = r.ReminderId,
                Title = r.Title,
                ReminderType = r.ReminderType,
                TimeOfDay = r.TimeOfDay,
                DaysOfWeek = r.DaysOfWeek,
                IsActive = r.IsActive,
                LastTriggeredAt = r.LastTriggeredAt
            })
            .ToListAsync();

        // Nếu bệnh nhân chưa có lịch nhắc nào, tạo sẵn 2 lịch mặc định
        if (schedules.Count == 0)
        {
            await CreateDefaultSchedulesAsync(patientId);
            return await GetPatientSchedulesAsync(patientId);
        }

        return schedules;
    }

    private async Task CreateDefaultSchedulesAsync(int patientId)
    {
        var default1 = new ReminderSchedule
        {
            PatientId = patientId,
            Title = "Tập vật lý trị liệu buổi sáng",
            ReminderType = "Exercise",
            TimeOfDay = new TimeSpan(8, 30, 0),
            DaysOfWeek = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var default2 = new ReminderSchedule
        {
            PatientId = patientId,
            Title = "Đo tầm vận động (ROM) & Ghi nhật ký",
            ReminderType = "Measurement",
            TimeOfDay = new TimeSpan(19, 30, 0),
            DaysOfWeek = "Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.ReminderSchedules.AddRange(default1, default2);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Thêm mới hoặc cập nhật lịch nhắc nhở
    /// </summary>
    public async Task<ReminderSchedule> UpsertScheduleAsync(UpsertReminderScheduleRequest req)
    {
        if (req.ReminderId == 0)
        {
            var schedule = new ReminderSchedule
            {
                PatientId = req.PatientId,
                Title = req.Title,
                ReminderType = req.ReminderType,
                TimeOfDay = req.TimeOfDay,
                DaysOfWeek = req.DaysOfWeek,
                IsActive = req.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            _db.ReminderSchedules.Add(schedule);
            await _db.SaveChangesAsync();
            return schedule;
        }
        else
        {
            var existing = await _db.ReminderSchedules
                .FirstOrDefaultAsync(r => r.ReminderId == req.ReminderId && r.PatientId == req.PatientId);

            if (existing == null) throw new InvalidOperationException("Không tìm thấy lịch nhắc.");

            existing.Title = req.Title;
            existing.ReminderType = req.ReminderType;
            existing.TimeOfDay = req.TimeOfDay;
            existing.DaysOfWeek = req.DaysOfWeek;
            existing.IsActive = req.IsActive;

            await _db.SaveChangesAsync();
            return existing;
        }
    }

    /// <summary>
    /// Bật/Tắt kích hoạt lịch nhắc nhở
    /// </summary>
    public async Task<bool> ToggleScheduleAsync(int reminderId, int patientId)
    {
        var existing = await _db.ReminderSchedules
            .FirstOrDefaultAsync(r => r.ReminderId == reminderId && r.PatientId == patientId);

        if (existing == null) return false;

        existing.IsActive = !existing.IsActive;
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Xóa lịch nhắc nhở
    /// </summary>
    public async Task<bool> DeleteScheduleAsync(int reminderId, int patientId)
    {
        var existing = await _db.ReminderSchedules
            .FirstOrDefaultAsync(r => r.ReminderId == reminderId && r.PatientId == patientId);

        if (existing == null) return false;

        _db.ReminderSchedules.Remove(existing);
        await _db.SaveChangesAsync();
        return true;
    }
}
