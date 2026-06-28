using System;
using System.Collections.Generic;

namespace RehabTracking.Web.Entities;

public partial class PatientProfile
{
    public int PatientId { get; set; }

    public int UserId { get; set; }

    public int? DoctorId { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? MedicalHistory { get; set; }

    public virtual ICollection<DailyHealthMetric> DailyHealthMetrics { get; set; } = new List<DailyHealthMetric>();

    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();

    public virtual User? Doctor { get; set; }

    public virtual ICollection<ExerciseSession> ExerciseSessions { get; set; } = new List<ExerciseSession>();

    public virtual ICollection<TreatmentPlan> TreatmentPlans { get; set; } = new List<TreatmentPlan>();

    public virtual User User { get; set; } = null!;
}
