using Gestion_de_Productos_Lacteos.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gestion_de_Productos_Lacteos.Controllers
{
    public class ProveedoresController : Controller
    {
        private readonly SistemaInventarioLacteosContext _context;

        public ProveedoresController(SistemaInventarioLacteosContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? buscar)
        {
            if (HttpContext.Session.GetString("UsuarioNombre") == null)
                return RedirectToAction("Login", "Home");

            var query = _context.Proveedors.AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(p => p.NombreProveedor.Contains(buscar) ||
                                         p.Telefono.Contains(buscar));
            }

            var proveedores = await query.OrderBy(p => p.NombreProveedor).ToListAsync();
            ViewBag.BuscarActual = buscar;

            return View(proveedores);
        }

        [HttpGet]
        public async Task<IActionResult> GetProveedor(int id)
        {
            var proveedor = await _context.Proveedors.FindAsync(id);
            if (proveedor == null) return NotFound();
            return Json(proveedor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Proveedor proveedor)
        {
            if (ModelState.IsValid)
            {
                if (proveedor.IdProveedor == 0) _context.Add(proveedor);
                else _context.Update(proveedor);

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Datos inválidos" });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var proveedor = await _context.Proveedors.FindAsync(id);
            if (proveedor == null) return Json(new { success = false });

            _context.Proveedors.Remove(proveedor);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}