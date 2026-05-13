using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Gestion_de_Productos_Lacteos.Models;

namespace SistemaInventarioLacteos.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task EnviarComprobanteVentaAsync(string correoCliente, string correoVendedor, Venta venta, List<DetalleVenta> detalles, string nombreCliente)
        {
            try
            {
                using var client = new SmtpClient(_emailSettings.Host, _emailSettings.Port);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(_emailSettings.From, _emailSettings.Password);

                // Construcción de las filas de la tabla con navegación a Lote y Producto
                var filasDetalle = detalles.Select(d => $@"
                    <tr>
                        <td style='border: 1px solid #ddd; padding: 8px;'>{d.Cantidad}</td>
                        <td style='border: 1px solid #ddd; padding: 8px;'>
                            {d.LoteNavigation?.ProductoNavigation?.NombreProducto ?? "Producto"} 
                            <br><small style='color: #666;'>Lote: {d.LoteNavigation?.NumeroLote ?? "N/A"}</small>
                        </td>
                        <td style='border: 1px solid #ddd; padding: 8px; text-align: right;'>${d.Subtotal:N2}</td>
                    </tr>");

                var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px;'>
                        <h2 style='color: #0d6efd; text-align: center;'>Comprobante de Venta #{venta.IdVenta}</h2>
                        <p>Estimado/a <b>{nombreCliente}</b>, gracias por su compra.</p>
                        <hr>
                        <p><b>Fecha:</b> {venta.FechaVenta:dd/MM/yyyy HH:mm}</p>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <thead>
                                <tr style='background: #f8f9fa;'>
                                    <th style='border: 1px solid #ddd; padding: 8px;'>Cant.</th>
                                    <th style='border: 1px solid #ddd; padding: 8px;'>Producto</th>
                                    <th style='border: 1px solid #ddd; padding: 8px;'>Subtotal</th>
                                </tr>
                            </thead>
                            <tbody>
                                {string.Join("", filasDetalle)}
                            </tbody>
                        </table>
                        <h3 style='text-align: right;'>TOTAL: ${venta.Total:N2}</h3>
                    </div>
                </body>
                </html>";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.From, "Sistema Lácteos - Ventas"),
                    Subject = $"Ticket de Venta #{venta.IdVenta} - Lácteos",
                    Body = body,
                    IsBodyHtml = true
                };

                if (!string.IsNullOrEmpty(correoCliente)) mailMessage.To.Add(correoCliente);
                if (!string.IsNullOrEmpty(correoVendedor)) mailMessage.CC.Add(correoVendedor);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception) { /* Loguear error pero no detener la venta */ }
        }
    }
}