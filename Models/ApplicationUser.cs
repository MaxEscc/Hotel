using Microsoft.AspNetCore.Identity;

namespace HotelReservation.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Aquí puedes agregar propiedades adicionales si es necesario
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
