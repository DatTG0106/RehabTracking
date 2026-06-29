using MediatR;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Features.Healthcare.PatientDashboard;

/// <summary>
/// Command lưu một exercise session vào database khi bệnh nhân kết thúc tập luyện.
/// BUG FIX: Trước đây StopSession() chỉ lưu vào local state, không persist vào DB.
/// </summary>
public class SaveSessionCommand : IRequest<bool>
{
    public int PatientId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public int RepetitionsCount { get; set; }
    public int MaxRom { get; set; }
    public double AvgEmg { get; set; }
    public string DeviceType { get; set; } = "Simulator";
}

public class SaveSessionCommandHandler : IRequestHandler<SaveSessionCommand, bool>
{
    private readonly RehabTrackingContext _context;

    public SaveSessionCommandHandler(RehabTrackingContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(SaveSessionCommand request, CancellationToken cancellationToken)
    {
        if (request.PatientId <= 0 || request.DurationMinutes <= 0)
            return false;

        // Xác minh PatientProfile tồn tại
        var patientProfile = await _context.PatientProfiles
            .FindAsync(new object[] { request.PatientId }, cancellationToken);

        if (patientProfile == null)
            return false;

        var session = new ExerciseSession
        {
            PatientId = request.PatientId,
            StartTime = request.StartTime.ToUniversalTime(),
            EndTime = request.EndTime.ToUniversalTime(),
            DurationMinutes = request.DurationMinutes,
            RepetitionsCount = request.RepetitionsCount,
            MaxRom = request.MaxRom,
            AvgEmg = request.AvgEmg,
            DeviceType = request.DeviceType
        };

        _context.ExerciseSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
