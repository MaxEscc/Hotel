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

 // GET: Bookings/Create (Unificada con Reserve)
    [Authorize] // Requiere que el usuario esté autenticado para reservar
    public IActionResult Create(int? id) 
    {
        var booking = new Booking();

        if (id.HasValue)
        {
            var room = _context.Rooms.Find(id);
            if (room == null || !room.IsAvailable)
            {
                return NotFound("La habitación no está disponible.");
            }

            booking.RoomId = id.Value;
        }

        ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType");
        return View(booking);
    }

    // POST: Bookings/Create (Unificada con Reserve)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize] // Requiere que el usuario esté autenticado para reservar
    public async Task<IActionResult> Create([Bind("RoomId,CustomerName,CustomerEmail,CustomerPhone,CheckInDate,CheckOutDate,NumberOfGuests,SpecialRequests")] Booking booking)
    {
        if (ModelState.IsValid)
        {
            // Validar si las fechas son válidas
            if (booking.CheckInDate < DateTime.Today)
            {
                ModelState.AddModelError("CheckInDate", "La fecha de entrada no puede ser en el pasado.");
            }

            if (booking.CheckOutDate <= booking.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "La fecha de salida debe ser después de la fecha de entrada.");
            }

            if (!ModelState.IsValid)
            {
                // Vuelve a mostrar la lista de habitaciones disponibles
                ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
                return View(booking);
            }

            


            // Validar disponibilidad de la habitación
            bool isRoomAvailable = !_context.Bookings.Any(b =>
                b.RoomId == booking.RoomId &&
                 !b.IsCancelled &&
                b.CheckInDate < booking.CheckOutDate && b.CheckOutDate > booking.CheckInDate);

            if (!isRoomAvailable)
            {
                ModelState.AddModelError(string.Empty, "La habitación no está disponible en las fechas seleccionadas.");
                ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
                return View(booking);
            }

            // Verificar si el usuario está autenticado
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Debe iniciar sesión para reservar.");
                return View(booking);
            }

            booking.UserId = user.Id; // Asocia la reserva con el Id del usuario autenticado

            _context.Add(booking); // Guardar la reserva
            await _context.SaveChangesAsync();
            return RedirectToAction("ClientIndex"); // Redirigir al usuario a la lista de habitaciones disponibles
        }

        ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
        return View(booking);
    }


    // GET: Bookings/Edit/5
[Authorize]
public IActionResult Edit(int? id)
{
    if (!id.HasValue)
    {
        return NotFound("No se proporcionó un ID válido para la reserva.");
    }

    var booking = _context.Bookings.Find(id);
    if (booking == null)
    {
        return NotFound("No se encontró la reserva.");
    }

    // Obtener reservas existentes de la misma habitación (excluyendo la actual)
    var existingBookings = _context.Bookings
        .Where(b => b.RoomId == booking.RoomId && b.BookingId != booking.BookingId)
        .Select(b => new { b.CheckInDate, b.CheckOutDate })
        .ToList();

    ViewBag.ExistingBookings = existingBookings;
    ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.RoomId == booking.RoomId || r.IsAvailable), "RoomId", "RoomType", booking.RoomId);

    return View(booking);
}

// POST: Bookings/Edit/5
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin")] // Restringir el acceso a administradores
public async Task<IActionResult> Edit(int id, [Bind("BookingId,RoomId,CustomerName,CustomerEmail,CustomerPhone,CheckInDate,CheckOutDate,NumberOfGuests,SpecialRequests")] Booking booking)
{
    if (id != booking.BookingId)
    {
        return NotFound("El ID de la reserva no coincide.");
    }

    // Obtener la reserva original
    var originalBooking = await _context.Bookings.FindAsync(id);
    if (originalBooking == null)
    {
        return NotFound("No se encontró la reserva para editar.");
    }

    if (ModelState.IsValid)
    {
        // Validar fechas
        if (booking.CheckInDate < DateTime.Today)
        {
            ModelState.AddModelError("CheckInDate", "La fecha de entrada no puede ser en el pasado.");
        }

        if (booking.CheckOutDate <= booking.CheckInDate)
        {
            ModelState.AddModelError("CheckOutDate", "La fecha de salida debe ser después de la fecha de entrada.");
        }

        // Validar disponibilidad de fechas (excluyendo la reserva actual)
        bool isDateAvailable = !_context.Bookings.Any(b =>
            b.RoomId == booking.RoomId &&
             !b.IsCancelled &&
            b.BookingId != booking.BookingId && // Excluir la reserva actual
            b.CheckInDate < booking.CheckOutDate &&
            b.CheckOutDate > booking.CheckInDate);

        if (!isDateAvailable)
        {
            ModelState.AddModelError(string.Empty, "La habitación no está disponible en las fechas seleccionadas.");
        }

        if (!ModelState.IsValid)
        {
            // Volver a cargar datos necesarios para la vista
            var existingBookings = _context.Bookings
                .Where(b => b.RoomId == booking.RoomId && b.BookingId != booking.BookingId)
                .Select(b => new { b.CheckInDate, b.CheckOutDate })
                .ToList();
            ViewBag.ExistingBookings = existingBookings;
            ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.RoomId == booking.RoomId || r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
            return View(booking);
        }

        try
        {
            // Mantener los valores que no deben cambiar
            booking.UserId = originalBooking.UserId; // Mantener la asociación con el usuario original

            _context.Entry(originalBooking).CurrentValues.SetValues(booking); // Actualizar la reserva
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Bookings"); // Redirigir al listado de reservas (administrador)
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Bookings.Any(b => b.BookingId == id))
            {
                return NotFound("No se encontró la reserva para actualizar.");
            }
            else
            {
                throw;
            }
        }
    }

    ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.RoomId == booking.RoomId || r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
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
[Authorize]
public IActionResult Reserve(int id)
{
    var room = _context.Rooms.Find(id);
    if (room == null || !room.IsAvailable)
    {
        return NotFound("La habitación no está disponible.");
    }

    var booking = new Booking { RoomId = id };

    // Filtrar las reservas activas (no canceladas) para mostrar solo las reservas activas
    var existingBookings = _context.Bookings
        .Where(b => b.RoomId == id && !b.IsCancelled)  // Filtra las canceladas
        .Select(b => new { b.CheckInDate, b.CheckOutDate })
        .ToList();

    ViewBag.ExistingBookings = existingBookings;
    ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", id);
    return View(booking);
}


    // POST: Bookings/Reserve
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize] // Solo usuarios autenticados pueden reservar
    public async Task<IActionResult> Reserve([Bind("RoomId,CustomerName,CustomerEmail,CustomerPhone,CheckInDate,CheckOutDate,NumberOfGuests,SpecialRequests")] Booking booking)
    {
        // Validar si las fechas son válidas
        if (booking.CheckInDate < DateTime.Today)
        {
            ModelState.AddModelError("CheckInDate", "La fecha de entrada no puede ser en el pasado.");
        }

        if (booking.CheckOutDate <= booking.CheckInDate)
        {
            ModelState.AddModelError("CheckOutDate", "La fecha de salida debe ser después de la fecha de entrada.");
        }

        // Validar el estado del modelo antes de proceder
        if (!ModelState.IsValid)
        {
            ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);

            // Cargar reservas existentes para mostrar al usuario
            var existingBookings = _context.Bookings
                .Where(b => b.RoomId == booking.RoomId)
                .Select(b => new { b.CheckInDate, b.CheckOutDate })
                .ToList();
            ViewBag.ExistingBookings = existingBookings;

            return View(booking);
        }

        // Validar disponibilidad de la habitación
        bool isRoomAvailable = !_context.Bookings.Any(b =>
            b.RoomId == booking.RoomId &&
              !b.IsCancelled &&
            b.CheckInDate < booking.CheckOutDate && b.CheckOutDate > booking.CheckInDate);

        if (!isRoomAvailable)
        {
            ModelState.AddModelError(string.Empty, "La habitación no está disponible en las fechas seleccionadas.");
            ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
            return View(booking);
        }

        // Verificar si el usuario está autenticado
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Debe iniciar sesión para reservar.");
            return View(booking);
        }

        booking.UserId = user.Id; // Asocia la reserva con el Id del usuario autenticado

        _context.Add(booking); // Guardar la reserva
        await _context.SaveChangesAsync();
        return RedirectToAction("ClientIndex");
    }

    // GET: Bookings/MyBookings
    [HttpGet]
    [Authorize] // Solo usuarios autenticados pueden ver sus reservas
    public async Task<IActionResult> MyBookings()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Obtiene el UserId del usuario autenticado
        if (userId == null)
        {
            ModelState.AddModelError(string.Empty, "Debe iniciar sesión para reservar.");
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

          // POST: Bookings/Cancel/5
[HttpPost]
[Authorize] // Solo usuarios autenticados pueden cancelar
public async Task<IActionResult> Cancel(int id)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Obtener el ID del usuario autenticado

    // Buscar la reserva que pertenece al usuario
    var booking = await _context.Bookings
        .FirstOrDefaultAsync(b => b.BookingId == id && (b.UserId == userId || User.IsInRole("Admin"))); // Permitir que admin también pueda cancelar

    if (booking == null)
    {
        // Si no existe la reserva o no pertenece al usuario, mostrar error
        return NotFound("Reserva no encontrada o no tienes permisos para cancelarla.");
    }

    // Marcar la reserva como cancelada
    booking.IsCancelled = true;
    booking.CancellationDate = DateTime.Now;
    _context.Update(booking);
    await _context.SaveChangesAsync(); // Guardar los cambios en la base de datos

    // Mensaje de confirmación
    TempData["SuccessMessage"] = "Reserva cancelada con éxito.";

    // Redirigir a la vista de las reservas del usuario o al índice de reservas si es admin
    if (User.IsInRole("Admin"))
    {
        return RedirectToAction("Index");
    }
    return RedirectToAction("MyBookings");
}



      }

        }
         
    