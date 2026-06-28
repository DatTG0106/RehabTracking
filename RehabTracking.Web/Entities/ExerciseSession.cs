using System;
using System.Collections.Generic;

namespace RehabTracking.Web.Entities;

public partial class ExerciseSession
{
    public int SessionId { get; set; }

    public int PatientId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public double AvgEmg { get; set; }

    public double MaxRom { get; set; }

    public int RepetitionsCount { get; set; }

    public int DurationMinutes { get; set; }

    public string DeviceType { get; set; } = null!;

    public virtual PatientProfile Patient { get; set; } = null!;
}
