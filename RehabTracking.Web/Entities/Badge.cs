using System;
using System.Collections.Generic;

namespace RehabTracking.Web.Entities;

public partial class Badge
{
    public int BadgeId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public string IconClass { get; set; } = "bi-award";

    public int XPBonus { get; set; } = 50;

    public virtual ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
}

public partial class UserBadge
{
    public int UserBadgeId { get; set; }

    public int UserId { get; set; }

    public int BadgeId { get; set; }

    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;

    public virtual Badge Badge { get; set; } = null!;
}
