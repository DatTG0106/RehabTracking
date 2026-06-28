using System;
using System.Collections.Generic;

namespace RehabTracking.Web.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public int RoleId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<PatientProfile> PatientProfileDoctors { get; set; } = new List<PatientProfile>();

    public virtual PatientProfile? PatientProfileUser { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<TreatmentPlan> TreatmentPlans { get; set; } = new List<TreatmentPlan>();
}
