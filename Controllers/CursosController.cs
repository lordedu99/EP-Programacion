using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;

namespace PortalAcademico.Controllers
{
    [Authorize]
    public class CursosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CursosController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Cursos
        public async Task<IActionResult> Index(string nombre, int? creditosMin, int? creditosMax, TimeSpan? horarioInicio, TimeSpan? horarioFin)
        {
            var query = _context.Cursos.Where(c => c.Activo).AsQueryable();

            if (!string.IsNullOrWhiteSpace(nombre))
                query = query.Where(c => c.Nombre.Contains(nombre));

            if (creditosMin.HasValue)
                query = query.Where(c => c.Creditos >= creditosMin.Value);

            if (creditosMax.HasValue)
                query = query.Where(c => c.Creditos <= creditosMax.Value);

            if (horarioInicio.HasValue)
                query = query.Where(c => c.HorarioInicio >= horarioInicio.Value);

            if (horarioFin.HasValue)
                query = query.Where(c => c.HorarioFin <= horarioFin.Value);

            // Guardamos filtros para mostrarlos en la vista
            ViewData["FilterNombre"] = nombre;
            ViewData["FilterCreditosMin"] = creditosMin?.ToString();
            ViewData["FilterCreditosMax"] = creditosMax?.ToString();
            ViewData["FilterHorarioInicio"] = horarioInicio?.ToString(@"hh\:mm");
            ViewData["FilterHorarioFin"] = horarioFin?.ToString(@"hh\:mm");

            var cursos = await query.ToListAsync();
            return View(cursos);
        }

        // GET: Cursos/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null) return NotFound();
            return View(curso);
        }

        // GET: Cursos/Matricular/5
        public async Task<IActionResult> Matricular(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            // Validar que no se haya matriculado antes
            var existe = await _context.Matriculas.AnyAsync(m => m.CursoId == id && m.UsuarioId == userId);
            if (existe)
            {
                TempData["Error"] = "Ya estás matriculado en este curso.";
                return RedirectToAction(nameof(Index));
            }

            // Validar cupo
            var cupoActual = await _context.Matriculas.CountAsync(m => m.CursoId == id && m.Estado == EstadoMatricula.Confirmada);
            if (cupoActual >= curso.CupoMaximo)
            {
                TempData["Error"] = "El cupo máximo de este curso ya fue alcanzado.";
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
