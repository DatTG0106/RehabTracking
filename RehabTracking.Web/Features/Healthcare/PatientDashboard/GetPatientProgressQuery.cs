using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Features.Healthcare.PatientDashboard;

public class GetPatientProgressQuery : IRequest<PatientProgressResult>
{
    public int PatientId { get; set; }
}

public class PatientProgressResult
{
    public List<ExerciseSession> Sessions { get; set; } = new();
    public TreatmentPlan? TreatmentPlan { get; set; }
    /// <summary>True if the user has activated a device and has a PatientProfile.</summary>
    public bool HasPatientProfile { get; set; } = false;
}

public class GetPatientProgressQueryHandler : IRequestHandler<GetPatientProgressQuery, PatientProgressResult>
{
    private readonly RehabTrackingContext _context;
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

    public GetPatientProgressQueryHandler(
        RehabTrackingContext context,
        Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PatientProgressResult> Handle(GetPatientProgressQuery request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            return new PatientProgressResult();

        var userIdString = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int currentUserId))
            return new PatientProgressResult();

        // Get patient profile for this user
        var patientProfile = await _context.PatientProfiles
            .FirstOrDefaultAsync(p => p.UserId == currentUserId, cancellationToken);

        if (patientProfile == null)
            return new PatientProgressResult { HasPatientProfile = false };

        // Get exercise sessions
        var sessions = await _context.ExerciseSessions
            .Where(e => e.PatientId == patientProfile.PatientId)
            .OrderByDescending(e => e.StartTime)
            .Take(30)
            .ToListAsync(cancellationToken);

        // Get active treatment plan
        var treatmentPlan = await _context.TreatmentPlans
            .Where(t => t.PatientId == patientProfile.PatientId && t.IsActive == true)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return new PatientProgressResult
        {
            Sessions = sessions,
            TreatmentPlan = treatmentPlan,
            HasPatientProfile = true
        };
    }
}
