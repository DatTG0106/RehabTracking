using System;
using System.ComponentModel.DataAnnotations;

namespace RehabTracking.Web.Entities;

public class Payment
{
    [Key]
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public string Provider { get; set; } = "VNPay"; // VNPay, MoMo, COD
    public string TransactionNo { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Order Order { get; set; } = null!;
}
