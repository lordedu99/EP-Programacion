using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// --- Configuración de conexiones ---
var env = builder.Environment;
var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
var sqliteConn = builder.Configuration.GetConnectionString("SQLiteConnection");

// DB: PostgreSQL en producción, SQLite en desarrollo
if (env.IsProduction())
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(defaultConn));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(sqliteConn));
}

// Identity
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        // endurece un poco por si se usa el seed
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Redis (IDistributedCache) para sesiones
builder.Services.AddStackExchangeRedisCache(options =>
{
    // Render: config por variable de entorno Redis__ConnectionString
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
    options.InstanceName = "PortalAcademico_";
});

// Sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // Detrás de proxy (Render), fuerza Secure y ajusta SameSite
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Forwarded headers para funcionar bien detrás del proxy de Render (TLS termina allí)
builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Limpia listas para aceptar los de Render
    opts.KnownNetworks.Clear();
    opts.KnownProxies.Clear();
});

var app = builder.Build();

// --- Middleware ---
app.UseForwardedHeaders(); // ¡Primero!
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// --- Migraciones + Seed en arranque ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Aplica migraciones automáticamente en cualquier entorno
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    // Crea rol y usuario Coordinador
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    string[] roles = new[] { "Coordinador" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var adminEmail = "coordinador@portal.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        // Asegúrate de cambiar esta clave en producción
        await userManager.CreateAsync(adminUser, "Password123!");
        await userManager.AddToRoleAsync(adminUser, "Coordinador");
    }

    // Seed de datos propio del dominio
    await SeedData.InitializeAsync(services);
}

// Rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
