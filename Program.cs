using HotelReservation.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HotelReservation.Models;

var builder = WebApplication.CreateBuilder(args);

// Configura HotelDbContext con la cadena de conexión "DefaultConnection"
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseSqlServer(connectionString ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

// Agrega servicios de Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<HotelDbContext>()
    .AddDefaultTokenProviders();

// Configuración de cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login"; // Ruta personalizada para inicio de sesión
    options.AccessDeniedPath = "/Account/AccessDenied"; // Ruta para acceso denegado
});

// Agrega controladores y vistas
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Asegúrate de que los roles y el usuario admin se inicialicen al iniciar la aplicación
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    
    try
    {
        await SeedData.Initialize(services, userManager); // Inicializa roles y usuarios
    }
    catch (Exception ex)
    {
        // Maneja el error según sea necesario, por ejemplo, registrándolo
        Console.WriteLine($"Error inicializando datos: {ex.Message}");
    }

    
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    dbContext.Database.Migrate(); // Aplica las migraciones
}


// Configura el pipeline de solicitudes HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Bookings/ClientIndex"); // Manejo de excepciones
    app.UseHsts(); // Protocolo de seguridad para HTTP
}

app.UseHttpsRedirection(); // Redirección a HTTPS
app.UseStaticFiles(); // Archivos estáticos

app.UseRouting(); // Configura el enrutamiento

app.UseAuthentication();  // Asegúrate de que el middleware de autenticación esté en su lugar
app.UseAuthorization();   // Asegúrate de que el middleware de autorización esté en su lugar


// Ruta vacía: Página de inicio personalizada, usando _Layout
app.MapControllerRoute(
    name: "client",
    pattern: "",
    defaults: new { controller = "Bookings", action = "ClientIndex" });

// Ruta específica para el controlador de reservas
app.MapControllerRoute(
    name: "bookings",
    pattern: "Bookings/{action=Index}/{id?}",
    defaults: new { controller = "Bookings", action = "Index" });

// Ruta específica para el controlador de habitaciones
app.MapControllerRoute(
    name: "rooms",
    pattern: "Rooms/{action=Index}/{id?}",
    defaults: new { controller = "Rooms", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


    

// Ejecuta la aplicación
app.Run();
