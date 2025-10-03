using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PortalAcademico.Data;
using PortalAcademico.Models;
using System.Text.Json;

namespace PortalAcademico.Controllers
{
    [Authorize]
    public class CursosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDistributedCache _cache;

        public CursosController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IDistributedCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        // GET: Cursos
        public async Task<IActionResult> Index()
        {
            const string cacheKey = "cursosActivos";
            string cachedCursos = await _cache.GetStringAsync(cacheKey);
            List<Curso> cursos;

            if (!string.IsNullOrEmpty(cachedCursos))
            {
                cursos = JsonSerializer.Deserialize<List<Curso>>(cachedCursos)!;
            }
            else
            {
                cursos = await _context.Cursos.Where(c => c.Activo).ToListAsync();
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
                };
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cursos), options);
            }

            return View(cursos);
        }

        // GET: Cursos/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null) return NotFound();

            // Guardar último curso visitado en sesión
            HttpContext.Session.SetString("UltimoCursoId", curso.Id.ToString());
            HttpContext.Session.SetString("UltimoCursoNombre", curso.Nombre);

            return View(curso);
        }

        // GET: Cursos/Matricular/5
        public async Task<IActionResult> Matricular(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            // Validaciones de matrícula
            var existe = await _context.Matriculas.AnyAsync(m => m.CursoId == id && m.UsuarioId == userId);
            if (existe)
            {
                TempData["Error"] = "Ya estás matriculado en este curso.";
                return RedirectToAction(nameof(Index));
            }

            var cupoActual = await _context.Matriculas.CountAsync(m => m.CursoId == id && m.Estado == EstadoMatricula.Confirmada);
            if (cupoActual >= curso.CupoMaximo)
            {
                TempData["Error"] = "El cupo máximo de este curso ya fue alcanzado.";
                return RedirectToAction(nameof(Index));
            }

            // Validar solapamiento de horarios
            var solapado = await _context.Matriculas
                .Include(m => m.Curso)
                .AnyAsync(m => m.UsuarioId == userId && m.Estado == EstadoMatricula.Confirmada &&
                               m.Curso.HorarioInicio < curso.HorarioFin && curso.HorarioInicio < m.Curso.HorarioFin);

            if (solapado)
            {
                TempData["Error"] = "El horario de este curso se solapa con otro curso ya matriculado.";
                return RedirectToAction(nameof(Index));
            }

            // Crear matrícula pendiente
            _context.Matriculas.Add(new Matricula { CursoId = id, UsuarioId = userId, Estado = EstadoMatricula.Pendiente });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Matrícula registrada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
