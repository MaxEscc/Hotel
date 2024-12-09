    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using HotelReservation.Data;
    using HotelReservation.Models;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using System.Security.Claims;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace HotelReservation.Controllers
    {
        //[Authorize] // Requiere que el usuario esté autenticado
        public class BookingsController : Controller
        {




[Authorize(Roles = "Admin")] // Only admins can generate reports
public async Task<IActionResult> GenerateReport()
{
    // Get all bookings for the report, including related room and user details
    var bookings = _context.Bookings.Include(b => b.Room).Include(b => b.User).ToList();

    // Create a PDF file in memory
    using (var stream = new MemoryStream())
    {
        // Create a PdfWriter for the file in memory
        using (var writer = new PdfWriter(stream))
        {
            // Create the PDF document
            using (var pdf = new PdfDocument(writer))
            {
                var document = new Document(pdf);

                // Title of the report
                document.Add(new Paragraph("Reporte de Reservas")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(18));

                // Add a table with the bookings
                var table = new Table(10, true); // Set the columns to auto-size
                table.SetWidth(500); // You can adjust the overall width here (in points)

                // Table header
                table.AddCell("ID Reserva");
                table.AddCell("Nombre Cliente");
                table.AddCell("Correo Cliente");
                table.AddCell("Teléfono Cliente");
                table.AddCell("Habitación");
                table.AddCell("Fecha Entrada");
                table.AddCell("Fecha Salida");
                table.AddCell("Número de Huéspedes");
                table.AddCell("Solicitudes Especiales");
                table.AddCell("Precio Total");

                // Add each booking as a row in the table
                foreach (var booking in bookings)
                {
                    // Calculate TotalNights and TotalCost
                    booking.TotalNights = (booking.CheckOutDate - booking.CheckInDate).Days;  // Calculate the number of nights
                    booking.TotalCost = booking.TotalNights * booking.PricePerNight;  // Calculate the total cost

                    table.AddCell(booking.BookingId.ToString());
                    table.AddCell(booking.CustomerName);
                    table.AddCell(booking.CustomerEmail);
                    table.AddCell(booking.CustomerPhone);
                    table.AddCell(booking.Room?.RoomType ?? "N/A");
                    table.AddCell(booking.CheckInDate.ToString("dd/MM/yyyy"));
                    table.AddCell(booking.CheckOutDate.ToString("dd/MM/yyyy"));
                    table.AddCell(booking.NumberOfGuests.ToString());
                    table.AddCell(booking.SpecialRequests ?? "N/A");
                    table.AddCell(booking.TotalCost.ToString("")); // Format as currency
                }

                // Add the table to the document, it will split automatically across pages if necessary
                document.Add(table);
            }
        }

        // Set content type and PDF file download header
        Response.ContentType = "application/pdf";
        Response.Headers.Add("Content-Disposition", "inline; filename=report.pdf");

        // Write the PDF stream to the response asynchronously
        await Response.Body.WriteAsync(stream.ToArray(), 0, stream.ToArray().Length);
        return new EmptyResult(); // No view associated with the action
    }
}












            
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
        return NotFound("La reserva no fue encontrada.");
    }

    // Calcular el número de noches y el precio total
    DateTime checkIn = booking.CheckInDate.Date;
    DateTime checkOut = booking.CheckOutDate.Date;
    booking.TotalNights = (checkOut - checkIn).Days; // La diferencia entre las fechas en días

    // Calcular el precio total
    if (booking.Room != null)
    {
        booking.TotalCost = booking.TotalNights * booking.Room.Price; // TotalNights * Price
    }

    return View(booking); // Pasar la reserva con el precio total a la vista
}

[Authorize]
public IActionResult Create()
{
    // Cargar las habitaciones disponibles
    ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType");
    return View();
}


[HttpPost]
[ValidateAntiForgeryToken]
[Authorize]
public async Task<IActionResult> Create([Bind("RoomId,CustomerName,CustomerEmail,CustomerPhone,CheckInDate,CheckOutDate,NumberOfGuests,SpecialRequests")] Booking booking)
{
    // Validar que la habitación existe
    var room = _context.Rooms.Find(booking.RoomId);
    if (room == null)
    {
        ModelState.AddModelError("RoomId", "La habitación seleccionada no existe.");
    }

    // Validar que las fechas sean correctas
    if (booking.CheckInDate < DateTime.Today)
    {
        ModelState.AddModelError("CheckInDate", "La fecha de entrada no puede ser en el pasado.");
    }

    if (booking.CheckOutDate <= booking.CheckInDate)
    {
        ModelState.AddModelError("CheckOutDate", "La fecha de salida debe ser después de la fecha de entrada.");
    }

    // Validar disponibilidad de la habitación
    if (room != null)
    {
        bool isRoomAvailable = !_context.Bookings.Any(b =>
            b.RoomId == booking.RoomId &&
            !b.IsCancelled &&
            b.CheckInDate < booking.CheckOutDate && b.CheckOutDate > booking.CheckInDate);

        if (!isRoomAvailable)
        {
            ModelState.AddModelError("RoomId", "La habitación no está disponible en las fechas seleccionadas.");
        }
    }

    // Verificar el estado del modelo antes de proceder
    if (!ModelState.IsValid)
    {
        // Volver a asignar el precio de la habitación en caso de error
        if (room != null)
        {
            booking.PricePerNight = room.Price;
        }

        // Recargar las habitaciones disponibles para la vista
        ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
        return View(booking);
    }

    // Ajustar las horas de check-in y check-out
    const int checkInHour = 14; // 2 PM
    const int checkOutHour = 12; // 12 PM

    DateTime adjustedCheckIn = booking.CheckInDate.Date.AddHours(checkInHour);
    DateTime adjustedCheckOut = booking.CheckOutDate.Date.AddHours(checkOutHour);

    // Calcular la duración de la reserva y el costo total
    TimeSpan bookingDuration = adjustedCheckOut - adjustedCheckIn;
    double totalNights = Math.Ceiling(bookingDuration.TotalHours / 22); // Redondear hacia arriba
    booking.TotalCost = totalNights * room.Price;

    // Asociar la reserva al usuario autenticado
    var user = await _userManager.GetUserAsync(User);
    booking.UserId = user?.Id;

    // Guardar la reserva
    _context.Add(booking);
    await _context.SaveChangesAsync();
    // Agregar mensaje de éxito
                TempData["SuccessMessage"] = "La Modificacón se ha realizado exitosamente.";

    return RedirectToAction("MyBookings");
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
        // // Validar fechas
        // if (booking.CheckInDate < DateTime.Today)
        // {
        //     ModelState.AddModelError("CheckInDate", "La fecha de entrada no puede ser en el pasado.");
        // }

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

                // Agregar mensaje de éxito
                TempData["SuccessMessage"] = "La Modificacón se ha realizado exitosamente.";

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
                    // Agregar mensaje de éxito
                TempData["SuccessMessage"] = "La Modificacón se ha realizado exitosamente.";
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
                // Filtrar las habitaciones disponibles, excluyendo la habitación con RoomId == 5
                var rooms = _context.Rooms
                                    .Where(r => r.IsAvailable && r.RoomId != 5)  // Excluir la habitación con RoomId 5
                                    .ToList();

                return View(rooms);
            }

                // Vista del cliente: mostrar habitaciones disponibles
                    public IActionResult RoomsView()
            {
                // Filtrar las habitaciones disponibles, excluyendo la habitación con RoomId == 5
                var rooms = _context.Rooms
                                    .Where(r => r.IsAvailable)  // Excluir la habitación con RoomId 5
                                    .ToList();

                return View(rooms);
            }





            [Authorize]
            public IActionResult Reserve(int id)
            {
                // Obtener la habitación seleccionada
                var room = _context.Rooms.Find(id);

                if (room == null || !room.IsAvailable)
                {
                    return NotFound("La habitación no está disponible.");
                }

                // Crear el objeto Booking y asignar el precio de la habitación al modelo Booking
                var booking = new Booking
                {
                    RoomId = id,
                    PricePerNight = room.Price // Asignamos el precio de la habitación a la propiedad PricePerNight
                };
    


                // Pasar el modelo a la vista
                return View(booking);
            }


             [HttpPost]
[ValidateAntiForgeryToken]
[Authorize] // Solo usuarios autenticados pueden reservar
public async Task<IActionResult> Reserve([Bind("RoomId,CustomerName,CustomerEmail,CustomerPhone,CheckInDate,CheckOutDate,NumberOfGuests,SpecialRequests")] Booking booking)
{
    // Obtener la habitación seleccionada
    var room = _context.Rooms.Find(booking.RoomId);

    if (room == null)
    {
        ModelState.AddModelError(string.Empty, "La habitación seleccionada no existe.");
        return View(booking);
    }

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
        // Volver a asignar el precio de la habitación al modelo Booking en caso de error
        booking.PricePerNight = room.Price;

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
        ModelState.AddModelError("CheckOutDate", "La habitación no está disponible en las fechas seleccionadas.");
        
        // Volver a asignar el precio de la habitación al modelo Booking en caso de error
        booking.PricePerNight = room.Price;
        
        ViewBag.Rooms = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomId", "RoomType", booking.RoomId);
        return View(booking);
    }

    // Calcular el precio total basado en las fechas
    double totalCost = 0;
    DateTime adjustedCheckIn = booking.CheckInDate.Date.AddHours(14); // 2 PM
    DateTime adjustedCheckOut = booking.CheckOutDate.Date.AddHours(12); // 12 PM
    TimeSpan bookingDuration = adjustedCheckOut - adjustedCheckIn;
    double totalNights = Math.Ceiling(bookingDuration.TotalHours / 22); // Redondear hacia arriba

    // Calcular el costo total
    totalCost = totalNights * booking.PricePerNight;
    booking.TotalCost = totalCost;

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
      // Agregar mensaje de éxito
    TempData["SuccessMessage"] = "La reserva se ha realizado exitosamente.";
    return RedirectToAction("MyBookings");
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

    // Calcular las noches y el precio total para cada reserva
    foreach (var booking in bookings)
    {
        // Calcular el número de noches
        DateTime checkIn = booking.CheckInDate.Date;
        DateTime checkOut = booking.CheckOutDate.Date;
        booking.TotalNights = (checkOut - checkIn).Days; // La diferencia entre las fechas en días

        // Calcular el precio total
        if (booking.Room != null)
        {
            booking.TotalCost = booking.TotalNights * booking.Room.Price; // TotalNights * PricePerNight
        }
    }

    if (!bookings.Any())
    {
        ViewBag.Message = "No tienes reservas.";
    }

    return View("MyBookings", bookings); // Muestra la vista con las reservas del usuario
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
        return RedirectToAction("MyBookings");
    }
    return RedirectToAction("MyBookings");
}

}

    

        }
         
    