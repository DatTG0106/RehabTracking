using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RehabTracking.Web.Entities;

public class Voucher
{
    [Key]
    public int VoucherId { get; set; }
    public string VoucherCode { get; set; } = string.Empty;
    public string DiscountType { get; set; } = "Percentage"; // Percentage, Fixed, Freeship
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Value { get; set; }
    
    public DateTime? ExpiryDate { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
}
