using System;
using System.Collections.Generic;

namespace RehabTracking.Web.Entities;

public partial class Device
{
    public int DeviceId { get; set; }

    public string DeviceSerialNumber { get; set; } = null!;

    public int? PatientId { get; set; }

    public string? Status { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public virtual PatientProfile? Patient { get; set; }
}
