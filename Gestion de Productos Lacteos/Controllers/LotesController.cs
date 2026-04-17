using Gestion_de_Productos_Lacteos.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Gestion_de_Productos_Lacteos.Controllers
{
    public class LotesController : Controller
    {
        private readonly SistemaInventarioLacteosContext _context;

        public LotesController(SistemaInventarioLacteosContext context)
        {
            _context = context;
        }

        // GET: Lotes
        public async Task<IActionResult> Index(string? buscar, int? idProducto, string? estado, string? orden)
        {
            if (HttpContext.Session.GetString("UsuarioNombre") == null)
                return RedirectToAction("Login", "Home");

            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var query = _context.Lotes.Include(l => l.IdProductoNavigation).AsQueryable();

            // Filtro por Nombre de Producto o Número de Lote
            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(l => l.IdProductoNavigation.NombreProducto.Contains(buscar) ||
                                         l.NumeroLote.Contains(buscar));
            }

            // Filtro por Producto específico
            if (idProducto.HasValue)
            {
                query = query.Where(l => l.IdProducto == idProducto);
            }

            // Filtro por Estado (Vencidos / Disponibles)
            if (estado == "vencidos")
            {
                query = query.Where(l => l.FechaVencimiento < hoy);
            }
            else if (estado == "disponibles")
            {
                query = query.Where(l => l.FechaVencimiento >= hoy);
            }

            // Ordenamiento por Fecha de Caducidad
            query = orden switch
            {
                "fecha_asc" => query.OrderBy(l => l.FechaVencimiento),
                "fecha_desc" => query.OrderByDescending(l => l.FechaVencimiento),
                _ => query.OrderBy(l => l.FechaVencimiento) // Default: Próximos a vencer
            };

            var lotes = await query.ToListAsync();

            // Bag para mantener los valores en los filtros de la vista
            ViewBag.IdProducto = new SelectList(_context.Productos.OrderBy(p => p.NombreProducto), "IdProducto", "NombreProducto", idProducto);
            ViewBag.BuscarActual = buscar;
            ViewBag.EstadoActual = estado;
            ViewBag.OrdenActual = orden;

            return View(lotes);
        }

        // GET: Lotes/GetLote/5 (Para cargar datos en el modal de edición)
        [HttpGet]
        public async Task<IActionResult> GetLote(int id)
        {
            var lote = await _context.Lotes.FindAsync(id);
            if (lote == null) return NotFound();

            // Devolvemos un objeto simplificado para evitar errores de referencia circular
            return Json(new
            {
                idLote = lote.IdLote,
                idProducto = lote.IdProducto,
                numeroLote = lote.NumeroLote,
                cantidad = lote.Cantidad,
                fechaProduccion = lote.FechaProduccion?.ToString("yyyy-MM-dd"),
                fechaVencimiento = lote.FechaVencimiento?.ToString("yyyy-MM-dd")
            });
        }

        // POST: Lotes/Save (Unificado para Crear y Editar)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Lote lote)
        {
            try
            {
                if (lote.IdLote == 0)
                {
                    _context.Add(lote);
                    TempData["Success"] = "Lote creado correctamente.";
                }
                else
                {
                    _context.Update(lote);
                    TempData["Success"] = "Lote actualizado correctamente.";
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al guardar: " + ex.Message });
            }
        }

        // POST: Lotes/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var lote = await _context.Lotes.FindAsync(id);
            if (lote == null) return Json(new { success = false, message = "No se encontró el lote." });

            _context.Lotes.Remove(lote);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        [HttpPost]
        public async Task<IActionResult> MarcarComoVencido(int id)
        {
            var lote = await _context.Lotes.FindAsync(id);
            if (lote == null) return Json(new { success = false, message = "Lote no encontrado." });

            // Ponemos la fecha de ayer para asegurar que el sistema lo detecte como vencido de inmediato
            lote.FechaVencimiento = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));

            _context.Update(lote);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "El lote ha sido marcado como vencido." });
        }


    }
}