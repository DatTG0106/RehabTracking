using System;
using System.Collections.Generic;

namespace RehabTracking.Web.Entities;

public partial class HotelReservation
{
    public int Id { get; set; }

    public string GuestName { get; set; } = null!;

    public string RoomNumber { get; set; } = null!;

    public string RoomType { get; set; } = null!;

    public int NumberOfGuests { get; set; }

    public double TotalPrice { get; set; }
}
