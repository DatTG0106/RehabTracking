using System;
using System.Collections.Generic;

namespace RehabTracking.Web.Entities;

public partial class DailyHealthMetric
{
    public int MetricId { get; set; }

    public int PatientId { get; set; }

    public DateOnly LogDate { get; set; }

    public int? StepsCount { get; set; }

    public double? DistanceKm { get; set; }

    public double? CaloriesBurned { get; set; }

    public double? BloodGlucoseLevel { get; set; }

    public string? HealthAlertStatus { get; set; }

    public virtual PatientProfile Patient { get; set; } = null!;
}
