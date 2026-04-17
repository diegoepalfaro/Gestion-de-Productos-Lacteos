using Gestion_de_Productos_Lacteos.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gestion_de_Productos_Lacteos.Controllers
{
    public class CategoriasController : Controller
    {
        private readonly SistemaInventarioLacteosContext _context;

        public CategoriasController(SistemaInventarioLacteosContext context)
        {
            _context = context;
        }

        // GET: Categorias
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UsuarioNombre") == null)
                return RedirectToAction("Login", "Home");

            return View(await _context.Categoria.OrderBy(c => c.NombreCategoria).ToListAsync());
        }

        // POST: Categorias/Create
        [HttpPost]
        public async Task<IActionResult> Create(Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                _context.Add(categoria);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Categoría creada correctamente." });
            }
            return Json(new { success = false, message = "Datos inválidos." });
        }

        // GET: Categorias/GetCategoria/5 (Para cargar el modal de edición)
        [HttpGet]
        public async Task<IActionResult> GetCategoria(int id)
        {
            var categoria = await _context.Categoria.FindAsync(id);
            if (categoria == null) return NotFound();
            return Json(categoria);
        }

        // POST: Categorias/Edit
        [HttpPost]
        public async Task<IActionResult> Edit(Categoria categoria)
        {
            if (ModelState.IsValid)
            {
                _context.Update(categoria);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Categoría actualizada." });
            }
            return Json(new { success = false, message = "Error al actualizar." });
        }

        // POST: Categorias/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var categoria = await _context.Categoria.FindAsync(id);
            if (categoria == null) return Json(new { success = false, message = "No encontrada." });

            // Verificar si hay productos usando esta categoría
            bool tieneProductos = await _context.Productos.AnyAsync(p => p.IdCategoria == id);
            if (tieneProductos)
            {
                return Json(new { success = false, message = "No se puede eliminar: existen productos asociados a esta categoría." });
            }

            _context.Categoria.Remove(categoria);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Categoría eliminada." });
        }
    }
}