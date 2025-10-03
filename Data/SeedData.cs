using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Models;

namespace PortalAcademico.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // Aplicar migraciones
            await context.Database.MigrateAsync();

            // Rol Coordinador
            const string roleName = "Coordinador";
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole(roleName));

            // Usuario Coordinador
            var coordEmail = "coordinador@uni.edu";
            var coord = await userManager.FindByEmailAsync(coordEmail);
            if (coord == null)
            {
                coord = new IdentityUser { UserName = coordEmail, Email = coordEmail, EmailConfirmed = true };
                await userManager.CreateAsync(coord, "P@ssw0rd!");
                await userManager.AddToRoleAsync(coord, roleName);
            }

            // Seed cursos (3 activos)
            if (!await context.Cursos.AnyAsync())
            {
                var cursos = new List<Curso>
                {
                    new Curso
                    {
                        Codigo = "MAT101",
                        Nombre = "Matemáticas I",
                        Creditos = 4,
                        CupoMaximo = 30,
                        HorarioInicio = TimeSpan.FromHours(9),
                        HorarioFin = TimeSpan.FromHours(11),
                        Activo = true
                    },
                    new Curso
                    {
                        Codigo = "FIS201",
                        Nombre = "Física General",
                        Creditos = 3,
                        CupoMaximo = 25,
                        HorarioInicio = TimeSpan.FromHours(11.5),
                        HorarioFin = TimeSpan.FromHours(13),
                        Activo = true
                    },
                    new Curso
                    {
                        Codigo = "PROG301",
                        Nombre = "Programación Avanzada",
                        Creditos = 5,
                        CupoMaximo = 20,
                        HorarioInicio = TimeSpan.FromHours(14),
                        HorarioFin = TimeSpan.FromHours(16),
                        Activo = true
                    }
                };
                context.Cursos.AddRange(cursos);
                await context.SaveChangesAsync();
            }
        }
    }
}
