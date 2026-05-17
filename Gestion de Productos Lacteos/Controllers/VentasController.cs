using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gestion_de_Productos_Lacteos.Models;
using SistemaInventarioLacteos.Services;
using QuestPDF.Fluent;


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

                // ---- BLOQUE AVANZADO DE GENERACIÓN DE COMPROBANTE COMERCIAL PDF ----
                try
                {
                    // Cargamos la venta con detalles, lotes y sus nombres de producto asociados
                    var ventaCompleta = await _context.Venta
                        .Include(v => v.DetalleVenta)
                            .ThenInclude(d => d.LoteNavigation)
                                .ThenInclude(l => l.ProductoNavigation)
                        .FirstOrDefaultAsync(v => v.IdVenta == venta.IdVenta);

                    if (ventaCompleta != null)
                    {
                        var cliente = await _context.Clientes.FindAsync(ventaCompleta.IdCliente);
                        var vendedor = await _context.Usuarios.FindAsync(ventaCompleta.IdUsuario);

                        string correoCliente = cliente?.Correo;
                        string correoVendedor = vendedor?.Correo;
                        string nombreCliente = cliente?.Nombre ?? "Cliente General";

                        // Datos informativos para la formalidad del documento
                        string duiNitCliente = !string.IsNullOrEmpty(cliente?.Dui) ? cliente.Dui : (!string.IsNullOrEmpty(cliente?.Nit) ? cliente.Nit : "N/A");
                        string nombreVendedor = vendedor?.Nombre ?? "Caja Central";
                        string fechaEmision = ventaCompleta.FechaVenta?.ToString("dd/MM/yyyy hh:mm tt") ?? DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");

                        if (!string.IsNullOrEmpty(correoCliente) || !string.IsNullOrEmpty(correoVendedor))
                        {
                            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                            var pdfData = QuestPDF.Fluent.Document.Create(container =>
                            {
                                container.Page(page =>
                                {
                                    // Márgenes y tipografía base
                                    page.Margin(1.5f, QuestPDF.Infrastructure.Unit.Centimetre);
                                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                                    // 1. ENCABEZADO FORMAL DE LA EMPRESA
                                    page.Header().Row(row =>
                                    {
                                        row.RelativeColumn().Column(col =>
                                        {
                                            col.Item().Text("DISTRIBUIDORA DE PRODUCTOS LÁCTEOS").FontSize(14).Bold().FontColor(QuestPDF.Helpers.Colors.Blue.Darken3);
                                            col.Item().Text("Santa Ana, El Salvador").SemiBold();
                                            col.Item().Text("Dirección: Sucursal Central Santa Ana").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                                            col.Item().Text("Teléfono: +503 2440-XXXX").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                                        });

                                        // SOLUCIÓN DEFINITIVA: Pasamos un único argumento numérico calculando los puntos equivalentes a 5cm (5 * 28.34f)
                                        row.ConstantColumn(141.7f)
                                           .Border(1)
                                           .BorderColor(QuestPDF.Helpers.Colors.Blue.Darken3)
                                           .Background(QuestPDF.Helpers.Colors.Grey.Lighten4)
                                           .Padding(10)
                                           .Column(col =>
                                           {
                                               col.Item().AlignCenter().Text("COMPROBANTE DE VENTA").Bold().FontSize(11).FontColor(QuestPDF.Helpers.Colors.Blue.Darken3);
                                               col.Item().AlignCenter().Text("SINFAC-001").FontSize(9);
                                               col.Item().EnsureSpace(4);
                                               col.Item().AlignCenter().Text($"No: #{ventaCompleta.IdVenta}").Bold().FontSize(14).FontColor(QuestPDF.Helpers.Colors.Red.Medium);
                                           });
                                    });
                                    // 2. BLOQUE DE DATOS DE EMISIÓN Y CLIENTE
                                    page.Content().PaddingVertical(0.8f, QuestPDF.Infrastructure.Unit.Centimetre).Column(col =>
                                    {
                                        col.Item().Background(QuestPDF.Helpers.Colors.Blue.Lighten5).Padding(6).Row(row =>
                                        {
                                            row.RelativeColumn().Text(t => { t.Span("Fecha/Hora Emisión: ").Bold(); t.Span(fechaEmision); });
                                            row.RelativeColumn().Text(t => { t.Span("Atendido por: ").Bold(); t.Span(nombreVendedor); });
                                        });

                                        col.Item().PaddingTop(10).Border(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(8).Column(c =>
                                        {
                                            c.Item().Text("DATOS DEL COMPRADOR").Bold().FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Darken3);
                                            c.Item().EnsureSpace(3);
                                            c.Item().Row(r =>
                                            {
                                                r.RelativeColumn(2).Text(t => { t.Span("Cliente: ").Bold(); t.Span(nombreCliente); });
                                                r.RelativeColumn().Text(t => { t.Span("Doc. Identidad: ").Bold(); t.Span(duiNitCliente); });
                                            });
                                        });

                                        col.Item().PaddingTop(15);

                                        // 3. TABLA FORMAL DE ARTÍCULOS
                                        col.Item().Table(table =>
                                        {
                                            table.ColumnsDefinition(columns => {
                                                columns.ConstantColumn(1.5f, QuestPDF.Infrastructure.Unit.Centimetre); // Cantidad
                                                columns.RelativeColumn(4);                                             // Descripción / Producto
                                                columns.RelativeColumn(1.5f);                                           // Lote
                                                columns.RelativeColumn();                                               // Precio Unitario
                                                columns.RelativeColumn();                                               // Subtotal
                                            });

                                            // Fila de Cabecera de Tabla (Sintaxis fluida corregida)
                                            table.Header(header => {
                                                header.Cell().Background(QuestPDF.Helpers.Colors.Blue.Darken3).Padding(5).AlignCenter().Text("CANT").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                                header.Cell().Background(QuestPDF.Helpers.Colors.Blue.Darken3).Padding(5).Text("DESCRIPCIÓN DEL PRODUCTO").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                                header.Cell().Background(QuestPDF.Helpers.Colors.Blue.Darken3).Padding(5).AlignCenter().Text("LOTE").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                                header.Cell().Background(QuestPDF.Helpers.Colors.Blue.Darken3).Padding(5).AlignRight().Text("P. UNIT").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                                header.Cell().Background(QuestPDF.Helpers.Colors.Blue.Darken3).Padding(5).AlignRight().Text("SUBTOTAL").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                            });

                                            // Filas dinámicas
                                            foreach (var item in ventaCompleta.DetalleVenta)
                                            {
                                                string nombreProd = item.LoteNavigation?.ProductoNavigation?.NombreProducto ?? "Producto Lácteo";
                                                string numLote = item.LoteNavigation?.NumeroLote ?? "N/A";

                                                table.Cell().BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(item.Cantidad?.ToString() ?? "0");
                                                table.Cell().BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).Text(nombreProd);
                                                table.Cell().BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(numLote);
                                                table.Cell().BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${item.Precio:N2}");
                                                table.Cell().BorderBottom(0.5f).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"${item.Subtotal:N2}");
                                            }
                                        });

                                        // 4. BLOQUE DE TOTALES CONTABLES
                                        col.Item().AlignRight().PaddingTop(10).Width(6, QuestPDF.Infrastructure.Unit.Centimetre).Table(tTotal =>
                                        {
                                            tTotal.ColumnsDefinition(cDef => {
                                                cDef.RelativeColumn();
                                                cDef.RelativeColumn();
                                            });

                                            tTotal.Cell().Padding(3).Text("Ventas No Sujetas:").FontSize(9);
                                            tTotal.Cell().Padding(3).AlignRight().Text("$0.00").FontSize(9);

                                            tTotal.Cell().Padding(3).Text("Ventas Exentas:").FontSize(9);
                                            tTotal.Cell().Padding(3).AlignRight().Text("$0.00").FontSize(9);

                                            tTotal.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten1).Padding(3).Text("Ventas Afectas:").FontSize(9);
                                            tTotal.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten1).Padding(3).AlignRight().Text($"${ventaCompleta.Total:N2}").FontSize(9);

                                            tTotal.Cell().Padding(5).Text("TOTAL A PAGAR:").Bold().FontSize(11).FontColor(QuestPDF.Helpers.Colors.Blue.Darken3);
                                            tTotal.Cell().Padding(5).AlignRight().Text($"${ventaCompleta.Total:N2}").Bold().FontSize(11).FontColor(QuestPDF.Helpers.Colors.Blue.Darken3);
                                        });
                                    });

                                    // 5. PIE DE PÁGINA COMERCIAL
                                    page.Footer().Column(col =>
                                    {
                                        col.Item().LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten1);
                                        col.Item().PaddingTop(4).Row(row =>
                                        {
                                            row.RelativeColumn().Text("Gracias por su confianza. Revise su producto antes de retirarse.").FontSize(8).Italic().FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                                            row.RelativeColumn().AlignRight().Text(t => {
                                                t.Span("Página ").FontSize(8);
                                                t.CurrentPageNumber().FontSize(8);
                                            });
                                        });
                                    });
                                });
                            }).GeneratePdf();

                            // Despacho asíncrono al EmailService
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    using var scope = HttpContext.RequestServices.CreateScope();
                                    var emailServ = scope.ServiceProvider.GetRequiredService<IEmailService>();

                                    await emailServ.EnviarComprobanteVentaAsync(
                                        correoCliente,
                                        correoVendedor,
                                        ventaCompleta,
                                        ventaCompleta.DetalleVenta.ToList(),
                                        nombreCliente,
                                        pdfData
                                    );
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"❌ Error SMTP: {ex.Message}");
                                }
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error al construir documento de venta: {ex.Message}");
                }

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