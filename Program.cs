using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------
// Base de datos
// -------------------------------------------------
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Si la cadena contiene "Host=" asumimos PostgreSQL (Render)
    if (!string.IsNullOrEmpty(defaultConnection) && defaultConnection.Contains("Host="))
        options.UseNpgsql(defaultConnection);
    else
        options.UseSqlite(defaultConnection ?? "Data Source=portal.db"); // fallback SQLite
});

// -------------------------------------------------
// Identity
// -------------------------------------------------
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// -------------------------------------------------
// Cache + Sesiones (P4)
// -------------------------------------------------
if (builder.Environment.IsDevelopment())
{
    // 👉 En local usamos InMemory (no requiere Redis)
    builder.Services.AddDistributedMemoryCache();
}
else
{
    // 👉 En producción (Render) usamos Redis
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "PortalAcademico_";
    });
}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Necesario para acceder a Session en Layout
builder.Services.AddHttpContextAccessor();

// -------------------------------------------------
// Controllers + Views
// -------------------------------------------------
builder.Services.AddControllersWithViews();

var app = builder.Build();

// -------------------------------------------------
// Seed inicial (usuarios, roles, datos básicos)
// -------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.InitializeAsync(services);
}

// -------------------------------------------------
// Middleware pipeline
// -------------------------------------------------
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
