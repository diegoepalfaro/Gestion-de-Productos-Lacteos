using Gestion_de_Productos_Lacteos.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gestion_de_Productos_Lacteos.Controllers
{
    public class ClientesController : Controller
    {
        private readonly SistemaInventarioLacteosContext _context;

        public ClientesController(SistemaInventarioLacteosContext context)
        {
            _context = context;
        }

        // GET: Clientes
        public async Task<IActionResult> Index(string? buscar, string? tipo)
        {
            if (HttpContext.Session.GetString("UsuarioNombre") == null)
                return RedirectToAction("Login", "Home");

            var query = _context.Clientes.AsQueryable();

            // Búsqueda por Nombre, DUI o NIT
            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(c => c.Nombre.Contains(buscar) ||
                                         c.Dui.Contains(buscar) ||
                                         c.Nit.Contains(buscar));
            }

            // Filtro por Tipo de Cliente
            if (!string.IsNullOrEmpty(tipo))
            {
                query = query.Where(c => c.TipoCliente == tipo);
            }

            var clientes = await query.OrderBy(c => c.Nombre).ToListAsync();

            ViewBag.BuscarActual = buscar;
            ViewBag.TipoActual = tipo;

            return View(clientes);
        }

        // GET: Clientes/GetCliente/5
        [HttpGet]
        public async Task<IActionResult> GetCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();
            return Json(cliente);
        }

        // POST: Clientes/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Cliente cliente)
        {
            if (ModelState.IsValid)
            {
                if (cliente.IdCliente == 0) _context.Add(cliente);
                else _context.Update(cliente);

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Datos inválidos" });
        }

        // POST: Clientes/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return Json(new { success = false });

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}