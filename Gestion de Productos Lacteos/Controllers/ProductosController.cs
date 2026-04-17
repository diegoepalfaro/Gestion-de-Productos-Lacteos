using Gestion_de_Productos_Lacteos.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Gestion_de_Productos_Lacteos.Controllers
{
    public class ProductosController : Controller
    {
        private readonly SistemaInventarioLacteosContext _context;

        public ProductosController(SistemaInventarioLacteosContext context)
        {
            _context = context;
        }

        // GET: /Productos
        public async Task<IActionResult> Index(string? buscar, int? idCategoria)
        {
            if (HttpContext.Session.GetString("UsuarioNombre") == null)
                return RedirectToAction("Login", "Home");

            var query = _context.Productos
                .Include(p => p.CategoriaNavigation)
                .Include(p => p.Lotes)
                .AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
                query = query.Where(p => p.NombreProducto.Contains(buscar));

            if (idCategoria.HasValue)
                query = query.Where(p => p.IdCategoria == idCategoria);

            var productos = await query.OrderBy(p => p.NombreProducto).ToListAsync();

            // Cargamos las categorías para el Panel de Filtros y para el Modal
            ViewBag.Categorias = await _context.Categoria.OrderBy(c => c.NombreCategoria).ToListAsync();
            ViewBag.IdCategoria = new SelectList(ViewBag.Categorias, "IdCategoria", "NombreCategoria");

            ViewBag.BuscarActual = buscar;
            ViewBag.CategoriaActual = idCategoria;

            return View(productos);
        }

        // GET: /Productos/GetProducto/5
        [HttpGet]
        public async Task<IActionResult> GetProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            return Json(producto);
        }

        // POST: /Productos/Save
        [HttpPost]
        public async Task<IActionResult> Save(Producto producto)
        {
            if (ModelState.IsValid)
            {
                if (producto.IdProducto == 0) _context.Add(producto);
                else _context.Update(producto);

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Datos inválidos" });
        }

        // Obtener lotes para el modal (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetLotes(int id)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);

            var lotes = await _context.Lotes
                .Where(l => l.IdProducto == id)
                .ToListAsync();

            var resultado = lotes.Select(l => new {
                l.NumeroLote,
                l.FechaProduccion,
                l.FechaVencimiento,
                l.Cantidad,
                // Lógica segura para DateOnly?
                DiasRestantes = l.FechaVencimiento.HasValue
                    ? l.FechaVencimiento.Value.DayNumber - hoy.DayNumber
                    : 0
            });

            return Json(resultado);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.IdCategoria = new SelectList(await _context.Categoria.ToListAsync(), "IdCategoria", "NombreCategoria");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto)
        {
            if (ModelState.IsValid)
            {
                _context.Add(producto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.IdCategoria = new SelectList(await _context.Categoria.ToListAsync(), "IdCategoria", "NombreCategoria", producto.IdCategoria);
            return View(producto);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            ViewBag.IdCategoria = new SelectList(await _context.Categoria.ToListAsync(), "IdCategoria", "NombreCategoria", producto.IdCategoria);
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto)
        {
            if (id != producto.IdProducto) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(producto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.IdCategoria = new SelectList(await _context.Categoria.ToListAsync(), "IdCategoria", "NombreCategoria", producto.IdCategoria);
            return View(producto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return Json(new { success = false });
            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}