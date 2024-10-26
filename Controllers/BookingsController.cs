using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelReservation.Data;
using HotelReservation.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims; 

namespace HotelReservation.Controllers
{
    //[Authorize] // Requiere que el usuario esté autenticado
    public class BookingsController : Controller
    {
        private readonly HotelDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingsController(HotelDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                .Include(b => b.Room) // Incluye Room para evitar referencias nulas en la vista
                .FirstOrDefaultAsync(m => m.BookingId == id);

            if (booking == null)
            {
                return NotFound("La reserva no fue encontrada."); // Manejo del caso de referencia nula
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
public async Task<IActionResult> Edit(int id, [Bind("BookingId,RoomId,CustomerName,CustomerEmail,CustomerPhone,CheckInDate,CheckOutDate,NumberOfGuests,SpecialRequests,UserId")] Booking booking)
{
    if (id != booking.BookingId)
    {
        return NotFound();
    }

    if (ModelState.IsValid)
    {
        // Verificar si la habitación ya está reservada en esas fechas
        var roomReservations = await _context.Bookings
            .Where(b => b.RoomId == booking.RoomId &&
                        b.BookingId != booking.BookingId && // Excluir la reserva actual
                        ((b.CheckInDate <= booking.CheckOutDate && b.CheckOutDate >= booking.CheckInDate)))
            .ToListAsync();

        if (roomReservations.Any())
        {
            ModelState.AddModelError("", "La habitación ya está reservada en esas fechas.");
            ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
            return View(booking); // Retornar a la vista de edición con el error
        }

        try
        {
            // No actualices el UserId si ya está asignado, para que no se sobrescriba
            var existingBooking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.BookingId == id);
            if (existingBooking != null && existingBooking.UserId != null)
            {
                booking.UserId = existingBooking.UserId;
            }

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
    // Validar disponibilidad de la habitación
    bool isRoomAvailable = !_context.Bookings.Any(b =>
        b.RoomId == booking.RoomId &&
        ((b.CheckInDate <= booking.CheckInDate && b.CheckOutDate > booking.CheckInDate) || 
         (b.CheckInDate < booking.CheckOutDate && b.CheckOutDate >= booking.CheckOutDate)));

    if (!isRoomAvailable)
    {
        ModelState.AddModelError(string.Empty, "La habitación no está disponible en las fechas seleccionadas.");
        ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
        return View(booking);
    }

    // Si el usuario está autenticado, asigna el UserId; de lo contrario, permite que sea nulo
    if (User.Identity != null && User.Identity.IsAuthenticated)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            booking.UserId = user.Id; // Asocia la reserva con el Id del usuario autenticado
        }
        else
        {
            ModelState.AddModelError(string.Empty, "No se pudo encontrar el usuario.");
            return View(booking);
        }
    }
    else
    {
        booking.UserId = null; // Reservas sin usuario registrado
    }

    _context.Add(booking); // Guardar la reserva
    await _context.SaveChangesAsync();
    return RedirectToAction("ClientIndex");
}   

    //ver reservas de usurios

[HttpGet]
[Authorize] // Solo usuarios autenticados pueden ver sus reservas
public async Task<IActionResult> MyBookings()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Obtiene el UserId del usuario autenticado
    if (userId == null)
    {
        return RedirectToAction("Login", "Account"); // Redirigir a Login si no está autenticado
    }

    var bookings = await _context.Bookings
        .Where(b => b.UserId == userId) // Filtra reservas por UserId
        .Include(b => b.Room) // Incluye la información de la habitación
        .ToListAsync();

    if (!bookings.Any())
    {
        ViewBag.Message = "No tienes reservas.";
    }

    return View("MyBookings", bookings); // Muestra la vista con las reservas del usuario
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
