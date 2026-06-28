using System;
using System.ComponentModel.DataAnnotations;

namespace RehabTracking.Web.Entities;

public class Review
{
    [Key]
    public int ReviewId { get; set; }
    public int ProductId { get; set; }
    public int CustomerId { get; set; }
    public int RatingValue { get; set; } // 1-5
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Product Product { get; set; } = null!;
    public virtual User Customer { get; set; } = null!;
}
