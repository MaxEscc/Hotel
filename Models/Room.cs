using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HotelReservation.Models
{
    public class Room
    {
        public int RoomId { get; set; }

        [Required(ErrorMessage = "Room type is required.")]
        [StringLength(100)]
        public string RoomType { get; set; } = string.Empty; // Inicialización predeterminada

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number.")]
        public double Price { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsAvailable { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; } // URL de una imagen
        public int Capacity { get; set; } // Capacidad de la habitación

        [StringLength(500)]
        public string? Amenities { get; set; } // Lista de comodidades

        [StringLength(100)]
        public string? Size { get; set; } // Tamaño de la habitación

        public TimeSpan CheckInTime { get; set; } // Horario de entrada
        public TimeSpan CheckOutTime { get; set; } // Horario de salida

        
    }
}
