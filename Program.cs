using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;

var builder = WebApplication.CreateBuilder(args);

// --- DbContext: PostgreSQL en Producción / cuando la cadena tiene Host=, si no SQLite ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
        builder.Environment.IsProduction())
    {
        // Render / Postgres
        options.UseNpgsql(connectionString);
        // Evita issues de compatibilidad de timestamps con Npgsql
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }
    else
    {
        // Local / SQLite
        options.UseSqlite(connectionString);
    }
});

// --- Identity ---
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// --- Session + Cache (sin Redis) ---
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// --- Migraciones + Seed en arranque ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Aplica migraciones antes del seed (Postgres/SQLite según proveedor)
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    // Seed de roles/usuario
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    if (!await roleManager.RoleExistsAsync("Coordinador"))
        await roleManager.CreateAsync(new IdentityRole("Coordinador"));

    var adminEmail = "coordinador@portal.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        await userManager.CreateAsync(adminUser, "Password123!");
        await userManager.AddToRoleAsync(adminUser, "Coordinador");
    }

    await SeedData.InitializeAsync(services);
}

// --- Middleware ---
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession(); // importante: antes de mapear rutas

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
