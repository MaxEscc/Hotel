using HotelReservation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HotelReservation.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, UserManager<ApplicationUser> userManager)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Verifica si la base de datos tiene roles existentes
            if (!roleManager.Roles.Any())
            {
                // Crear roles si no existen
                string[] roleNames = { "Admin", "Client" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Crear un usuario administrador predeterminado si no existe
                var adminEmail = "admin@hotel.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);

                if (adminUser == null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail
                    };
                    // Cambia la contraseña aquí si es necesario
                    var result = await userManager.CreateAsync(admin, "AdminPassword123!");

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, "Admin");
                    }
                    else
                    {
                        // Manejo de errores en caso de que la creación falle
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"Error creando el usuario admin: {error.Description}");
                        }
                    }
                }
            }
        }
    }
}
