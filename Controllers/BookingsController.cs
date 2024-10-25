using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelReservation.Data;
using HotelReservation.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization; // Agregar este espacio de nombres

namespace HotelReservation.Controllers
{
    //[Authorize] // Requiere que el usuario esté autenticado
    public class BookingsController : Controller
    {
        private readonly HotelDbContext _context;

        public BookingsController(HotelDbContext context)
        {
            _context = context;
        }

         [Authorize(Roles = "Admin")] // Solo los administradores pueden crear reservas
        // GET: Bookings
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings.Include(b => b.Room).ToListAsync();
            return View(bookings);
        }

        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(m => m.BookingId == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // GET: Bookings/Create
        [Authorize(Roles = "Admin")] // Solo los administradores pueden crear reservas
        public IActionResult Create()
        {
            ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType");
            return View();
        }

        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // Solo los administradores pueden crear reservas
        public async Task<IActionResult> Create([Bind("BookingId,RoomId,CustomerName,CustomerEmail,CustomerPhone,CheckInDate,CheckOutDate,NumberOfGuests,SpecialRequests")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                // Check availability
                bool isRoomAvailable = !_context.Bookings.Any(b =>
                    b.RoomId == booking.RoomId &&
                    ((b.CheckInDate <= booking.CheckInDate && b.CheckOutDate > booking.CheckInDate) || 
                     (b.CheckInDate < booking.CheckOutDate && b.CheckOutDate >= booking.CheckOutDate)));

                if (!isRoomAvailable)
                {
                    ModelState.AddModelError(string.Empty, "La habitación no está disponible en las fechas seleccionadas. Intenta con otras fechas.");
                    ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
                    return View(booking);
                }

                _context.Add(booking);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
            return View(booking);
        }

        // GET: Bookings/Edit/5
        [Authorize(Roles = "Admin")] // Solo los administradores pueden editar reservas
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // Solo los administradores pueden editar reservas
        public async Task<IActionResult> Edit(int id, [Bind("BookingId,RoomId,CustomerName,CustomerEmail,CustomerPhone,CheckInDate,CheckOutDate,NumberOfGuests,SpecialRequests")] Booking booking)
        {
            if (id != booking.BookingId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.BookingId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex) // Captura cualquier excepción general
                {
                    Console.WriteLine($"Error al actualizar la reserva: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "Ocurrió un error inesperado al intentar actualizar la reserva.");
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
            return View(booking);
        }

        // GET: Bookings/Delete/5
        [Authorize(Roles = "Admin")] // Solo los administradores pueden eliminar reservas
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(m => m.BookingId == id);
            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }

        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // Solo los administradores pueden eliminar reservas
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.BookingId == id);
        }

        // Vista del cliente: mostrar habitaciones disponibles
        public IActionResult ClientIndex()
        {
            var rooms = _context.Rooms.Where(r => r.IsAvailable).ToList();
            return View(rooms);
        }

        // GET: Bookings/Reserve/5
        public IActionResult Reserve(int id)
        {
            var room = _context.Rooms.Find(id);
            if (room == null || !room.IsAvailable)
            {
                return NotFound("La habitación no está disponible.");
            }

            var booking = new Booking { RoomId = id };

            var existingBookings = _context.Bookings
                .Where(b => b.RoomId == id)
                .Select(b => new { b.CheckInDate, b.CheckOutDate })
                .ToList();

            ViewBag.ExistingBookings = existingBookings;
            ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", id);
            return View(booking);
        }

        // POST: Bookings/Reserve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve([Bind("RoomId,CustomerName,CustomerEmail,CustomerPhone,CheckInDate,CheckOutDate,NumberOfGuests,SpecialRequests")] Booking booking)
        {
            var existingBookings = _context.Bookings
                .Where(b => b.RoomId == booking.RoomId)
                .Select(b => new { b.CheckInDate, b.CheckOutDate })
                .ToList();

            if (!ModelState.IsValid)
            {
                ViewBag.ExistingBookings = existingBookings; 
                var room = await _context.Rooms.FindAsync(booking.RoomId);
                ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
                return View(booking);
            }

            bool isRoomAvailable = !_context.Bookings.Any(b =>
                b.RoomId == booking.RoomId &&
                ((b.CheckInDate <= booking.CheckInDate && b.CheckOutDate > booking.CheckInDate) || 
                 (b.CheckInDate < booking.CheckOutDate && b.CheckOutDate >= booking.CheckOutDate)));

            if (!isRoomAvailable)
            {
                ModelState.AddModelError(string.Empty, "La habitación no está disponible en las fechas seleccionadas.");
                ViewBag.ExistingBookings = existingBookings;
                ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
                return View(booking);
            }

            _context.Add(booking);
            await _context.SaveChangesAsync();
            return RedirectToAction("ClientIndex");
        }

        // Método para obtener fechas disponibles
        private List<DateTime> GetAvailableDates(int roomId)
        {
            var existingBookings = _context.Bookings
                .Where(b => b.RoomId == roomId)
                .Select(b => new { b.CheckInDate, b.CheckOutDate })
                .ToList();

            List<DateTime> availableDates = new List<DateTime>();
            if (existingBookings.Any())
            {
                DateTime lastCheckOut = existingBookings.Max(b => b.CheckOutDate);
                availableDates.Add(lastCheckOut.AddDays(1)); // Primer día disponible después de la última reserva
            }
            else
            {
                availableDates.Add(DateTime.Today);
            }

            return availableDates;
        }
    }
}
