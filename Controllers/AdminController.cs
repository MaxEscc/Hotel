using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelReservation.Data;
    using HotelReservation.Models;

namespace HotelReservation.Controllers
{
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly HotelDbContext _context;

    public AdminController(HotelDbContext context)
    {
        _context = context;
    }

    // Dashboard del Admin
    public IActionResult Dashboard()
    {
        return View();
    }

    // Método para gestionar habitaciones (redirige al Index de Rooms)
    public IActionResult ManageRooms()
    {
        return RedirectToAction("Index", "Rooms");
    }

    // Método para gestionar reservas (redirige al Index de Bookings)
    public IActionResult ManageBookings()
    {
        return RedirectToAction("Index", "Bookings");
    }
}
}

