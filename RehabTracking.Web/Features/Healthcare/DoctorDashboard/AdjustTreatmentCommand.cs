using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;

namespace RehabTracking.Web.Features.Healthcare.DoctorDashboard;

public class AdjustTreatmentCommand : IRequest<bool>
{
    public int PlanId { get; set; }
    public int PatientId { get; set; }

    [System.ComponentModel.DataAnnotations.Range(5, 100, ErrorMessage = RehabTracking.Web.Constants.AppMessages.RangeRepetitions)]
    public int TargetRepetitions { get; set; }

    [System.ComponentModel.DataAnnotations.Range(5, 300, ErrorMessage = RehabTracking.Web.Constants.AppMessages.RangeDuration)]
    public int TargetDuration { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(500, ErrorMessage = RehabTracking.Web.Constants.AppMessages.NoteMaxLength)]
    public string Description { get; set; } = string.Empty;
}

public class AdjustTreatmentCommandHandler : IRequestHandler<AdjustTreatmentCommand, bool>
{
    private readonly RehabTrackingContext _context;
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

    public AdjustTreatmentCommandHandler(
        RehabTrackingContext context,
        Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> Handle(AdjustTreatmentCommand request, CancellationToken cancellationToken)
    {
        // Get actual doctor ID from the authenticated user's claims
        var user = _httpContextAccessor.HttpContext?.User;
        var userIdString = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdString, out int doctorId);

        TreatmentPlan? plan = null;

        if (request.PlanId > 0)
        {
            plan = await _context.TreatmentPlans
                .FindAsync(new object[] { request.PlanId }, cancellationToken);
        }

        if (plan == null)
        {
            // Check if there's already a plan for this patient
            plan = await _context.TreatmentPlans
                .FirstOrDefaultAsync(t => t.PatientId == request.PatientId && t.IsActive == true, cancellationToken);
        }

        if (plan == null)
        {
            // Create a new treatment plan
            plan = new TreatmentPlan
            {
                PatientId = request.PatientId,
                DoctorId = doctorId > 0 ? doctorId : 1, // fallback to 1 for demo only
                Title = "Phác đồ Phục hồi Chức năng",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.TreatmentPlans.Add(plan);
        }

        plan.TargetRepetitions = request.TargetRepetitions;
        plan.TargetDuration = request.TargetDuration;
        plan.Description = request.Description;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
