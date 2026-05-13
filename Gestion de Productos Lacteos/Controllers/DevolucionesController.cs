using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gestion_de_Productos_Lacteos.Models;

namespace Gestion_de_Productos_Lacteos.Controllers
{
    public class DevolucionesController : Controller
    {
        private readonly SistemaInventarioLacteosContext _context;

        public DevolucionesController(SistemaInventarioLacteosContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UsuarioNombre") == null)
                return RedirectToAction("Login", "Home");

            return View();
        }

        // Busca el ticket para cargar sus productos
        [HttpGet]
        public async Task<IActionResult> BuscarVenta(int idVenta)
        {
            // 1. Buscamos la venta con sus detalles
            var venta = await _context.Venta
                .Include(v => v.IdClienteNavigation)
                .Include(v => v.DetalleVenta)
                    .ThenInclude(d => d.LoteNavigation)
                        .ThenInclude(l => l.ProductoNavigation)
                .FirstOrDefaultAsync(v => v.IdVenta == idVenta);

            if (venta == null) return NotFound();

            // 2. Consultamos cuánto se ha devuelto ya de esta venta anteriormente
            var devolucionesPrevias = await _context.Devoluciones
                .Where(d => d.IdVenta == idVenta)
                .GroupBy(d => d.IdLote)
                .Select(g => new {
                    IdLote = g.Key,
                    CantidadDevuelta = g.Sum(x => x.Cantidad)
                })
                .ToListAsync();

            // 3. Cruzamos la información para calcular el saldo pendiente
            var items = venta.DetalleVenta.Select(d => {
                var yaDevuelto = devolucionesPrevias.FirstOrDefault(dp => dp.IdLote == d.IdLote)?.CantidadDevuelta ?? 0;
                return new
                {
                    idLote = d.IdLote,
                    nombreProducto = d.LoteNavigation?.ProductoNavigation?.NombreProducto,
                    numeroLote = d.LoteNavigation?.NumeroLote,
                    cantidadOriginal = d.Cantidad,
                    cantidadYaDevuelta = yaDevuelto,
                    cantidadDisponible = d.Cantidad - yaDevuelto, // Lo que realmente puede devolver ahora
                    precioVenta = d.Precio
                };
            }).ToList();

            // 4. Verificamos si ya no queda nada por devolver en toda la factura
            bool ventaCompletamenteDevuelta = items.All(i => i.cantidadDisponible <= 0);

            return Json(new
            {
                idVenta = venta.IdVenta,
                fecha = venta.FechaVenta?.ToString("dd/MM/yyyy HH:mm"),
                cliente = venta.IdClienteNavigation?.Nombre ?? "Cliente General",
                total = venta.Total,
                items = items,
                estaCompleta = ventaCompletamenteDevuelta
            });
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarDevolucion([FromBody] DevolucionRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in request.Items)
                {
                    // 1. Registro de la devolución
                    var devolucion = new Devolucion
                    {
                        IdVenta = request.IdVenta,
                        IdLote = item.IdLote,
                        Cantidad = item.Cantidad,
                        Motivo = request.Motivo,
                        EstadoProducto = request.EstadoProducto,
                        FechaDevolucion = DateTime.Now,
                        MontoDevuelto = item.Cantidad * item.PrecioUnitario
                    };

                    _context.Devoluciones.Add(devolucion);

                    // 2. Lógica Excepcional: Si el producto está en 'Buen Estado', se devuelve al Stock
                    if (request.EstadoProducto == "Buen Estado")
                    {
                        var lote = await _context.Lotes.FindAsync(item.IdLote);
                        if (lote != null)
                        {
                            lote.Cantidad += item.Cantidad; // Sumamos al inventario
                        }
                    }
                    // Si el estado es 'Arruinado', no se suma al stock (se registra como pérdida)
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Devolución registrada con éxito." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class DevolucionRequest
    {
        public int IdVenta { get; set; }
        public string Motivo { get; set; }
        public string EstadoProducto { get; set; }
        public List<DevolucionItemRequest> Items { get; set; }
    }

    public class DevolucionItemRequest
    {
        public int IdLote { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}