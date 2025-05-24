using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PC3.Data;
using PC3.Services;
using PC3.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Configuración de base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configuración para MVC y API controllers
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IJsonPlaceholderService, JsonPlaceholderService>();
// Configuración de HttpClient para tu API externa
builder.Services.AddHttpClient<IJsonPlaceholderService, JsonPlaceholderService>(client =>
{
  client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com/");
});

// Configuración de Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Registro de tu servicio personalizado


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseMigrationsEndPoint();
}
else
{
  app.UseExceptionHandler("/Home/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Añadido - necesario para Identity
app.UseAuthorization();

// Mapeo de rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();