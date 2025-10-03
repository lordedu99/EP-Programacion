using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PortalAcademico.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && await _userManager.IsInRoleAsync(user, "Coordinador"))
                {
                    // Redirige directamente al panel de coordinador
                    return RedirectToAction("Index", "Coordinador");
                }
            }

            // Usuario normal o no autenticado ve la página normal de inicio
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
