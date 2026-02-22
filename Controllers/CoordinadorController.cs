using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;
using PortalAcademico.Models;

namespace PortalAcademico.Controllers
{
    [Authorize(Roles = "Coordinador")]
    public class CoordinadorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoordinadorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Panel principal: lista de cursos
        public async Task<IActionResult> Index()
        {
            var cursos = await _context.Cursos.ToListAsync();
            return View(cursos);
        }

        // Crear curso
        public IActionResult Crear() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Curso curso)
        {
            if (ModelState.IsValid)
            {
                _context.Add(curso);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Curso creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(curso);
        }

        // Editar curso
        public async Task<IActionResult> Editar(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null) return NotFound();
            return View(curso);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Curso curso)
        {
            if (id != curso.Id) return BadRequest();
            if (ModelState.IsValid)
            {
                _context.Update(curso);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Curso actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(curso);
        }

        // Desactivar curso
        public async Task<IActionResult> Desactivar(int id)
        {
            var curso = await _context.Cursos.FindAsync(id);
            if (curso == null) return NotFound();
            curso.Activo = false;
            _context.Update(curso);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Curso desactivado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Lista de matrículas por curso
        public async Task<IActionResult> Matriculas(int cursoId)
        {
            var curso = await _context.Cursos
                .Include(c => c.Matriculas)
                .ThenInclude(m => m.Usuario)
                .FirstOrDefaultAsync(c => c.Id == cursoId);

            if (curso == null) return NotFound();
            return View(curso);
        }

        // Confirmar matrícula
        public async Task<IActionResult> Confirmar(int id)
        {
            var matricula = await _context.Matriculas.FindAsync(id);
            if (matricula == null) return NotFound();
            matricula.Estado = EstadoMatricula.Confirmada;
            _context.Update(matricula);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Matrícula confirmada.";
            return RedirectToAction(nameof(Matriculas), new { cursoId = matricula.CursoId });
        }

        // Cancelar matrícula
        public async Task<IActionResult> Cancelar(int id)
        {
            var matricula = await _context.Matriculas.FindAsync(id);
            if (matricula == null) return NotFound();
            matricula.Estado = EstadoMatricula.Cancelada;
            _context.Update(matricula);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Matrícula cancelada.";
            return RedirectToAction(nameof(Matriculas), new { cursoId = matricula.CursoId });
        }
    }
}
