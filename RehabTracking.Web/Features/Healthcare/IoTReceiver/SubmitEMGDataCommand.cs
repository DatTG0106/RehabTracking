using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using Microsoft.AspNetCore.SignalR;

namespace RehabTracking.Web.Features.Healthcare.IoTReceiver;

public class SubmitEMGDataCommand : IRequest<bool>
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessageResourceName = "RequiredField", ErrorMessageResourceType = typeof(RehabTracking.Web.Resources.ErrorMessages))]
    public int PatientId { get; set; }
    
    [System.ComponentModel.DataAnnotations.Range(0.0, 1000.0, ErrorMessageResourceName = "InvalidAvgEmg", ErrorMessageResourceType = typeof(RehabTracking.Web.Resources.ErrorMessages))]
    public double AvgEmg { get; set; }
    
    [System.ComponentModel.DataAnnotations.Range(0.0, 180.0, ErrorMessageResourceName = "InvalidMaxRom", ErrorMessageResourceType = typeof(RehabTracking.Web.Resources.ErrorMessages))]
    public double MaxRom { get; set; }
    
    [System.ComponentModel.DataAnnotations.Range(0, 1000, ErrorMessageResourceName = "PositiveRepetitions", ErrorMessageResourceType = typeof(RehabTracking.Web.Resources.ErrorMessages))]
    public int RepetitionsCount { get; set; }
    
    [System.ComponentModel.DataAnnotations.Range(1, 1440, ErrorMessageResourceName = "InvalidDurationMinutes", ErrorMessageResourceType = typeof(RehabTracking.Web.Resources.ErrorMessages))]
    public int DurationMinutes { get; set; }
    
    [System.ComponentModel.DataAnnotations.Required(ErrorMessageResourceName = "RequiredField", ErrorMessageResourceType = typeof(RehabTracking.Web.Resources.ErrorMessages))]
    [System.ComponentModel.DataAnnotations.StringLength(50, ErrorMessageResourceName = "MaxLength", ErrorMessageResourceType = typeof(RehabTracking.Web.Resources.ErrorMessages))]
    public string DeviceType { get; set; } = string.Empty;
}

public class SubmitEMGDataCommandHandler : IRequestHandler<SubmitEMGDataCommand, bool>
{
    private readonly RehabTrackingContext _context;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<RehabTracking.Web.Infrastructure.SignalRHubs.DashboardHub> _hubContext;

    public SubmitEMGDataCommandHandler(
        RehabTrackingContext context,
        Microsoft.AspNetCore.SignalR.IHubContext<RehabTracking.Web.Infrastructure.SignalRHubs.DashboardHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<bool> Handle(SubmitEMGDataCommand request, CancellationToken cancellationToken)
    {
        var session = new ExerciseSession
        {
            PatientId = request.PatientId,
            StartTime = DateTime.UtcNow.AddMinutes(-request.DurationMinutes),
            EndTime = DateTime.UtcNow,
            AvgEmg = request.AvgEmg,
            MaxRom = request.MaxRom,
            RepetitionsCount = request.RepetitionsCount,
            DurationMinutes = request.DurationMinutes,
            DeviceType = request.DeviceType
        };

        _context.ExerciseSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        // Phát sóng dữ liệu mới cho Dashboard của bệnh nhân này
        await _hubContext.Clients.Group($"Patient_{request.PatientId}")
            .SendCoreAsync("ReceiveNewMetrics", new object[] { request.AvgEmg, request.MaxRom }, cancellationToken);

        return true;
    }
}
