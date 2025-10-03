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
        public async Task<IActionResult> Index()
        {
            var cursos = await _context.Cursos.Where(c => c.Activo).ToListAsync();
            return View(cursos);
        }

        // GET: Cursos/Detalle/5
        public async Task<IActionResult> Detalle(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null) return NotFound();

            return View(curso);
        }

        // POST: Cursos/Matricular/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Matricular(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                TempData["Error"] = "Debes iniciar sesión para matricularte.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            // Validar matrícula existente
            var existe = await _context.Matriculas
                .AnyAsync(m => m.CursoId == id && m.UsuarioId == userId);
            if (existe)
            {
                TempData["Error"] = "Ya estás matriculado en este curso.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            // Validar cupo
            var cupoActual = await _context.Matriculas
                .CountAsync(m => m.CursoId == id && m.Estado == EstadoMatricula.Confirmada);
            if (cupoActual >= curso.CupoMaximo)
            {
                TempData["Error"] = "El cupo máximo de este curso ya fue alcanzado.";
                return RedirectToAction(nameof(Detalle), new { id });
            }

            // Validar solapamiento horario
            var matriculasUsuario = await _context.Matriculas
                .Include(m => m.Curso)
                .Where(m => m.UsuarioId == userId && m.Estado == EstadoMatricula.Confirmada)
                .ToListAsync();

            foreach (var m in matriculasUsuario)
            {
                if (!(curso.HorarioFin <= m.Curso.HorarioInicio || curso.HorarioInicio >= m.Curso.HorarioFin))
                {
                    TempData["Error"] = $"No puedes matricularte, hay solapamiento con el curso {m.Curso.Nombre}.";
                    return RedirectToAction(nameof(Detalle), new { id });
                }
            }

            // Crear matrícula pendiente
            var matricula = new Matricula
            {
                CursoId = id,
                UsuarioId = userId,
                Estado = EstadoMatricula.Pendiente
            };

            _context.Matriculas.Add(matricula);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Matrícula registrada correctamente en estado Pendiente.";
            return RedirectToAction(nameof(Detalle), new { id });
        }
    }
}
