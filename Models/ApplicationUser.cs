using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace HotelReservation.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public virtual ICollection<Booking>? Bookings { get; set; } // Relación con reservas
    }
}
