using HotelReservation.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.Data
{
    public class HotelDbContext : IdentityDbContext<ApplicationUser>
    {
        public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options)
        {
        }

                    public DbSet<Room> Rooms { get; set; } = null!;
            public DbSet<Booking> Bookings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configurar el tipo de columna para Price en Room
            modelBuilder.Entity<Room>()
                .Property(r => r.Price)
                .HasColumnType("decimal(18,2)");

            // Configurar la relación entre Booking y ApplicationUser
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)  // Indica que Booking tiene una relación con ApplicationUser
                .WithMany(u => u.Bookings)  // Indica que un ApplicationUser puede tener muchas reservas
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);  // Establece la clave foránea

            base.OnModelCreating(modelBuilder);
        }
    }
}
