using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Features.Healthcare.DoctorDashboard;

public class GetPatientsQuery : IRequest<List<PatientProfile>>
{
    public int DoctorId { get; set; }
}

public class GetPatientsQueryHandler : IRequestHandler<GetPatientsQuery, List<PatientProfile>>
{
    private readonly RehabTrackingContext _context;
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

    public GetPatientsQueryHandler(
        RehabTrackingContext context,
        Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<PatientProfile>> Handle(GetPatientsQuery request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            return new List<PatientProfile>();

        var userIdString = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out int doctorUserId))
            return new List<PatientProfile>();

        // Override the request.DoctorId with the authenticated doctor's actual userId
        return await _context.PatientProfiles
            .Where(p => p.DoctorId == doctorUserId)
            .Include(p => p.User)
            .Include(p => p.ExerciseSessions)
            .Include(p => p.TreatmentPlans) // ✅ Fixed: Include treatment plans so modal shows correct values
            .ToListAsync(cancellationToken);
    }
}
