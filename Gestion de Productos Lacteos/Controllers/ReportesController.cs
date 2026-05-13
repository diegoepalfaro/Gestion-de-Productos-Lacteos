using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gestion_de_Productos_Lacteos.Models;

namespace Gestion_de_Productos_Lacteos.Controllers
{
    public class ReportesController : Controller
    {
        private readonly SistemaInventarioLacteosContext _context;

        public ReportesController(SistemaInventarioLacteosContext context)
        {
            _context = context;
        }

        public IActionResult Ventas() => View();
        public IActionResult Devoluciones() => View();

        [HttpGet]
        public async Task<IActionResult> FiltrarVentas(int? id, DateTime? inicio, DateTime? fin)
        {
            var query = _context.Venta
                .Include(v => v.IdClienteNavigation)
                .Include(v => v.IdUsuarioNavigation)
                .AsQueryable();

            // Filtro por ID específico
            if (id.HasValue)
            {
                query = query.Where(v => v.IdVenta == id);
            }
            else
            {
                // Filtro por fecha (Default: Mes actual)
                var fechaInicio = inicio ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var fechaFin = fin ?? DateTime.Now;
                query = query.Where(v => v.FechaVenta >= fechaInicio && v.FechaVenta <= fechaFin);
            }

            var resultados = await query
                .OrderByDescending(v => v.FechaVenta)
                .Select(v => new {
                    idVenta = v.IdVenta,
                    fecha = v.FechaVenta.Value.ToString("dd/MM/yyyy HH:mm"),
                    cliente = v.IdClienteNavigation.Nombre,
                    vendedor = v.IdUsuarioNavigation.Nombre,
                    total = v.Total
                }).ToListAsync();

            return Json(resultados);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDetalleDevoluciones(int idVenta)
        {
            var devoluciones = await _context.Devoluciones
                .Include(d => d.LoteNavigation)
                    .ThenInclude(l => l.ProductoNavigation)
                .Where(d => d.IdVenta == idVenta)
                .Select(d => new {
                    producto = d.LoteNavigation.ProductoNavigation.NombreProducto,
                    cantidad = d.Cantidad,
                    motivo = d.Motivo,
                    estado = d.EstadoProducto,
                    monto = d.MontoDevuelto,
                    fecha = d.FechaDevolucion.Value.ToString("dd/MM/yyyy")
                }).ToListAsync();

            return Json(devoluciones);
        }
    }
}