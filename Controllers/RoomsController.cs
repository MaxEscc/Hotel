using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelReservation.Data;
using HotelReservation.Models;
using Microsoft.AspNetCore.Authorization;

namespace HotelReservation.Controllers
{
        [Authorize(Roles = "Admin")]
    public class RoomsController : Controller
    {
        
        private readonly HotelDbContext _context;

        public RoomsController(HotelDbContext context)
        {
            _context = context;
        }

        // GET: Rooms
        public async Task<IActionResult> Index()
        {
            return View(await _context.Rooms.ToListAsync());
        }

        // GET: Rooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var room = await _context.Rooms
                .FirstOrDefaultAsync(m => m.RoomId == id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        // GET: Rooms/Create
        public IActionResult Create()
        {
            return View();
        }

        // GET: Rooms/Edit/5
public async Task<IActionResult> Edit(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var room = await _context.Rooms.FindAsync(id);
    if (room == null)
    {
        return NotFound();
    }

    return View(room);
}


     [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create([Bind("RoomId,RoomType,Price,Description,IsAvailable,ImageUrl,Capacity,Size,CheckInTime,CheckOutTime,Amenities")] Room room, IFormFile imageFile)
{
    if (ModelState.IsValid)
    {
        if (imageFile != null)
        {
            // Validación del tamaño del archivo (máximo 5 MB)
            if (imageFile.Length > 1024 * 1024 * 5) // Limite de 5 MB
            {
                ModelState.AddModelError("ImageUrl", "The image file size is too large. Maximum allowed size is 5 MB.");
            }

            // Validación de la extensión del archivo
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("ImageUrl", "Only .jpg, .jpeg, and .png files are allowed.");
            }

            // Si no hay errores, guardar la imagen
            if (ModelState.IsValid)
            {
                // Generar un nombre único para la imagen
                var fileName = Path.GetFileName(imageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                // Guardar la imagen en la carpeta wwwroot/images
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Asignar la ruta de la imagen al campo ImageUrl
                room.ImageUrl = "/images/" + fileName;
            }
        }
        else
        {
            ModelState.AddModelError("ImageUrl", "Please upload an image.");
        }

        // Guardar el objeto Room en la base de datos
        if (ModelState.IsValid)
        {
            _context.Add(room);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }

    return View(room);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, [Bind("RoomId,RoomType,Price,Description,IsAvailable,ImageUrl,Capacity,Size,CheckInTime,CheckOutTime,Amenities")] Room room, IFormFile? imageFile)
{
    if (id != room.RoomId)
    {
        return NotFound();
    }

    if (ModelState.IsValid)
    {
        var existingRoom = await _context.Rooms.FindAsync(id);
        if (existingRoom == null)
        {
            return NotFound();
        }

        try
        {
            // Si se cargó una nueva imagen
            if (imageFile != null && imageFile.Length > 0)
            {
                // Guardar la nueva imagen
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Eliminar la imagen anterior
                if (!string.IsNullOrEmpty(existingRoom.ImageUrl))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingRoom.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Actualizar la URL de la imagen
                existingRoom.ImageUrl = "/images/" + fileName;
            }

            // Actualizar los demás campos
            existingRoom.RoomType = room.RoomType;
            existingRoom.Price = room.Price;
            existingRoom.Description = room.Description;
            existingRoom.IsAvailable = room.IsAvailable;
            existingRoom.Capacity = room.Capacity;
            existingRoom.Size = room.Size;
            existingRoom.CheckInTime = room.CheckInTime;
            existingRoom.CheckOutTime = room.CheckOutTime;
            existingRoom.Amenities = room.Amenities;

            _context.Update(existingRoom);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!RoomExists(room.RoomId))
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

    return View(room);
}

// GET: Rooms/Delete/5
public async Task<IActionResult> Delete(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var room = await _context.Rooms
        .FirstOrDefaultAsync(m => m.RoomId == id);
    if (room == null)
    {
        return NotFound();
    }

    return View(room);
}


        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null)
            {
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.RoomId == id);
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

    }


    
}
