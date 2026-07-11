using MediatR;
using Microsoft.EntityFrameworkCore;
using RehabTracking.Web.Entities;
using System.Text.Json;

namespace RehabTracking.Web.Features.Healthcare.DoctorDashboard;

public class SaveExercisePlanCommand : IRequest<bool>
{
    public int PatientId { get; set; }
    public List<ExerciseRoutineItem> Exercises { get; set; } = new();
    public WorkoutSchedule? Schedule { get; set; }
}

public class SaveExercisePlanCommandHandler : IRequestHandler<SaveExercisePlanCommand, bool>
{
    private readonly RehabTrackingContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SaveExercisePlanCommandHandler(
        RehabTrackingContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> Handle(SaveExercisePlanCommand request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userIdString = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdString, out int doctorId);

        // Find active treatment plan or create new
        var plan = await _context.TreatmentPlans
            .FirstOrDefaultAsync(t => t.PatientId == request.PatientId && t.IsActive == true, cancellationToken);

        if (plan == null)
        {
            plan = new TreatmentPlan
            {
                PatientId = request.PatientId,
                DoctorId = doctorId > 0 ? doctorId : 1,
                Title = "Phác đồ Phục hồi Chức năng",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                TargetRepetitions = request.Exercises.Sum(e => e.Sets * e.Reps),
                TargetDuration = 30
            };
            _context.TreatmentPlans.Add(plan);
        }

        // Serialize entire plan data (exercises + schedule) to JSON in Description field
        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var planData = new TreatmentPlanData
        {
            ExerciseRoutine = request.Exercises,
            Schedule = request.Schedule,
            LastUpdated = DateTime.UtcNow
        };

        plan.Description = JsonSerializer.Serialize(planData, serializerOptions);
        plan.TargetRepetitions = request.Exercises.Any()
            ? request.Exercises.Sum(e => e.Sets * e.Reps)
            : plan.TargetRepetitions;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>
/// Wrapper lưu toàn bộ dữ liệu lộ trình vào trường Description của TreatmentPlan (JSON).
/// </summary>
public class TreatmentPlanData
{
    public List<ExerciseRoutineItem> ExerciseRoutine { get; set; } = new();
    public WorkoutSchedule? Schedule { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? DoctorNotes { get; set; }
}
