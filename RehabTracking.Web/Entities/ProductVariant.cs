using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RehabTracking.Web.Entities;

public class ProductVariant
{
    [Key]
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string Size { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal AdditionalPrice { get; set; }
    
    public int Stock { get; set; }

    public virtual Product Product { get; set; } = null!;
}
