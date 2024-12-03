using System.ComponentModel.DataAnnotations;

namespace HotelReservation.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required]
        [StringLength(100)]
        public string? CustomerName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string? CustomerEmail { get; set; }

        [Required]
        [Phone]
        [StringLength(15)]
        public string? CustomerPhone { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Required]
        public int NumberOfGuests { get; set; }

        [StringLength(500)]
        public string? SpecialRequests { get; set; }

        // Propiedad para referenciar al usuario
          public string? UserId { get; set; }

        //Campo para cancelar la reserva
        public bool IsCancelled{get; set;}

        //
        public DateTime? CancellationDate { get; set; }

        // Propiedades de navegación
        public virtual Room? Room { get; set; }
        public virtual ApplicationUser? User { get; set; }

            public double PricePerNight { get; set; }

               public double TotalCost { get; set; }
        public double TotalNights { get; internal set; }


        
    }
}
