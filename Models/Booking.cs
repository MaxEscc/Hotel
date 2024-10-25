using System;
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

        // Propiedad de navegación
        public virtual Room? Room { get; set; }
    }
}
