using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gestion_de_Productos_Lacteos.Models;
using SistemaInventarioLacteos.Services;

namespace Gestion_de_Productos_Lacteos.Controllers
{
    public class VentasController : Controller
    {
        private readonly SistemaInventarioLacteosContext _context;
        private readonly IEmailService _emailService;

        public VentasController(SistemaInventarioLacteosContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Nueva()
        {
            if (HttpContext.Session.GetString("UsuarioNombre") == null)
                return RedirectToAction("Login", "Home");

            ViewBag.Clientes = await _context.Clientes.OrderBy(c => c.Nombre).ToListAsync();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SugerirLotes(string term)
        {
            if (string.IsNullOrEmpty(term)) return Json(new List<object>());

            var sugerencias = await _context.Lotes
                .Include(l => l.ProductoNavigation) // Nombre corregido
                .Where(l => (l.NumeroLote.Contains(term) || l.ProductoNavigation.NombreProducto.Contains(term))
                            && l.Cantidad > 0)
                .Take(8)
                .Select(l => new
                {
                    label = $"{l.NumeroLote} - {l.ProductoNavigation.NombreProducto} (Stock: {l.Cantidad})",
                    val = l.NumeroLote
                })
                .ToListAsync();

            return Json(sugerencias);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarPorLote(string codigo)
        {
            var lote = await _context.Lotes
                .Include(l => l.ProductoNavigation) // Nombre corregido
                .FirstOrDefaultAsync(l => l.NumeroLote == codigo && l.Cantidad > 0);

            if (lote == null) return NotFound();

            return Json(new
            {
                idLote = lote.IdLote,
                nombre = lote.ProductoNavigation.NombreProducto,
                precio = lote.PrecioFactura,
                stock = lote.Cantidad,
                numeroLote = lote.NumeroLote
            });
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarVenta([FromBody] VentaRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var idUsuario = HttpContext.Session.GetInt32("UsuarioId") ?? 1;

                var venta = new Venta
                {
                    IdCliente = request.IdCliente,
                    IdUsuario = idUsuario,
                    FechaVenta = DateTime.Now,
                    Total = request.Items.Sum(i => i.Cantidad * i.Precio),
                    TipoComprobante = "Consumidor Final"
                };

                _context.Venta.Add(venta);
                await _context.SaveChangesAsync();

                foreach (var item in request.Items)
                {
                    var lote = await _context.Lotes.FindAsync(item.IdLote);

                    if (lote == null || lote.Cantidad < item.Cantidad)
                        throw new Exception($"El lote {item.NumeroLote} no tiene stock suficiente.");

                    lote.Cantidad -= item.Cantidad;

                    var detalle = new DetalleVenta
                    {
                        IdVenta = venta.IdVenta,
                        IdLote = item.IdLote, // Asegúrate que DetalleVenta tenga idLote y no idProducto
                        Cantidad = item.Cantidad,
                        Precio = item.Precio,
                        Subtotal = item.Cantidad * item.Precio
                    };

                    _context.DetalleVenta.Add(detalle);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, idVenta = venta.IdVenta });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Esto nos dirá exactamente qué columna o tabla falla
                var mensajeCompleto = ex.Message;
                if (ex.InnerException != null)
                {
                    mensajeCompleto += " | Detalle: " + ex.InnerException.Message;
                }
                return Json(new { success = false, message = mensajeCompleto });
            }
        }
    }

    public class VentaRequest
    {
        public int IdCliente { get; set; }
        public List<VentaItemRequest> Items { get; set; }
    }

    public class VentaItemRequest
    {
        public int IdLote { get; set; }
        public string NumeroLote { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
    }
}