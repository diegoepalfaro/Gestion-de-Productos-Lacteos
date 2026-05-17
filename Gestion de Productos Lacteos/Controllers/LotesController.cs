using Gestion_de_Productos_Lacteos.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            var hoy = DateTime.Today;
            // Incluimos tanto el Producto como el Proveedor para las descripciones en la tabla
            var query = _context.Lotes
                .Include(l => l.ProductoNavigation)
                .Include(l => l.IdProveedorNavigation)
                .AsQueryable();

            // Filtro por Nombre de Producto, Número de Lote o Descripción del Lote
            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(l => l.ProductoNavigation.NombreProducto.Contains(buscar) ||
                                         l.NumeroLote.Contains(buscar) ||
                                         l.Descripcion.Contains(buscar));
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

            // CARGA DE PRODUCTOS ACTIVOS
            ViewBag.IdProducto = new SelectList(
                _context.Productos.Where(p => p.Estado == true).OrderBy(p => p.NombreProducto),
                "IdProducto",
                "NombreProducto",
                idProducto
            );

            // CARGA DE PROVEEDORES REGISTRADOS PARA EL MODAL
            ViewBag.IdProveedor = new SelectList(
                _context.Proveedors.OrderBy(p => p.NombreProveedor),
                "IdProveedor",
                "NombreProveedor"
            );

            ViewBag.BuscarActual = buscar;
            ViewBag.EstadoActual = estado;
            ViewBag.OrdenActual = orden;

            return View(lotes);
        }

        [HttpGet]
        public async Task<IActionResult> GetLote(int id)
        {
            var lote = await _context.Lotes.FindAsync(id);
            if (lote == null) return NotFound();

            return Json(new
            {
                idLote = lote.IdLote,
                idProducto = lote.IdProducto,
                numeroLote = lote.NumeroLote,
                descripcion = lote.Descripcion,
                cantidad = lote.Cantidad,
                fechaProduccion = lote.FechaProduccion?.ToString("yyyy-MM-dd"),
                fechaVencimiento = lote.FechaVencimiento?.ToString("yyyy-MM-dd"),
                vtaNeta = lote.VtaNeta,
                ivaConsumidor = lote.IvaConsumidor,
                ccfSiva = lote.CcfSiva,
                ivaContribuyente = lote.IvaContribuyente,
                precioFactura = lote.PrecioFactura,
                // Nuevos campos mapeados en JSON
                idProveedor = lote.IdProveedor,
                fechaIngreso = lote.FechaIngreso?.ToString("yyyy-MM-dd"),
                costoCompra = lote.CostoCompra
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Lote lote)
        {
            try
            {
                // AUDITORÍA: Guardamos la fecha y hora exacta de cualquier interacción
                lote.UltimaModificacion = DateTime.Now;

                if (lote.IdLote == 0)
                {
                    // Si es nuevo y no se seleccionó fecha, se asienta la fecha actual
                    if (lote.FechaIngreso == null)
                    {
                        lote.FechaIngreso = DateTime.Now;
                    }
                    _context.Add(lote);
                }
                else
                {
                    // Si editamos, recuperamos la fecha de ingreso original para que no se sobreescriba en nulo
                    var loteExistente = await _context.Lotes.AsNoTracking().FirstOrDefaultAsync(l => l.IdLote == lote.IdLote);
                    if (loteExistente != null && lote.FechaIngreso == null)
                    {
                        lote.FechaIngreso = loteExistente.FechaIngreso;
                    }

                    _context.Update(lote);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al guardar: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarcarComoVencido(int id)
        {
            var lote = await _context.Lotes.FindAsync(id);
            if (lote == null) return Json(new { success = false, message = "Lote no encontrado." });

            if (lote.Cantidad <= 0)
                return Json(new { success = false, message = "El lote ya está agotado." });

            lote.FechaVencimiento = DateTime.Today.AddDays(-1);

            // AUDITORÍA: El cambio manual de estado también cuenta como modificación
            lote.UltimaModificacion = DateTime.Now;

            _context.Update(lote);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "El lote ha sido marcado como vencido." });
        }
    }
}