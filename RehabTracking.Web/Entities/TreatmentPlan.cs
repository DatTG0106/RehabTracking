using System;
using System.Collections.Generic;

namespace RehabTracking.Web.Entities;

public partial class TreatmentPlan
{
    public int PlanId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int TargetRepetitions { get; set; }

    public int TargetDuration { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsActive { get; set; }

    public virtual User Doctor { get; set; } = null!;

    public virtual PatientProfile Patient { get; set; } = null!;
}
