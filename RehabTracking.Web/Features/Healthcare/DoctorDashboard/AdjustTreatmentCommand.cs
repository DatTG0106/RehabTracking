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
        
        // Update DoctorNotes inside the JSON Description
        try
        {
            RehabTracking.Web.Features.Healthcare.DoctorDashboard.TreatmentPlanData planData;
            if (!string.IsNullOrWhiteSpace(plan.Description) && plan.Description.Trim().StartsWith("{"))
            {
                planData = System.Text.Json.JsonSerializer.Deserialize<RehabTracking.Web.Features.Healthcare.DoctorDashboard.TreatmentPlanData>(
                    plan.Description, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new RehabTracking.Web.Features.Healthcare.DoctorDashboard.TreatmentPlanData();
            }
            else
            {
                planData = new RehabTracking.Web.Features.Healthcare.DoctorDashboard.TreatmentPlanData();
                if (!string.IsNullOrWhiteSpace(plan.Description))
                {
                    planData.DoctorNotes = plan.Description; // Preserve old plain text note if any
                }
            }
            
            planData.DoctorNotes = request.Description;
            plan.Description = System.Text.Json.JsonSerializer.Serialize(
                planData, 
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
        }
        catch
        {
            // If failed to parse, overwrite with new JSON object
            var planData = new RehabTracking.Web.Features.Healthcare.DoctorDashboard.TreatmentPlanData { DoctorNotes = request.Description };
            plan.Description = System.Text.Json.JsonSerializer.Serialize(
                planData, 
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
