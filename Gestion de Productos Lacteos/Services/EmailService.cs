using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Gestion_de_Productos_Lacteos.Models;
using MimeKit;
using MailKit.Net.Smtp;

namespace SistemaInventarioLacteos.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task EnviarComprobanteVentaAsync(string correoCliente, string correoVendedor, Venta venta, List<DetalleVenta> detalles, string nombreCliente, byte[] pdfData)
        {
            try
            {
                var message = new MimeMessage();

                // Emisor: El correo institucional configurado en tu appsettings
                message.From.Add(new MailboxAddress("Sistema de Inventario Lácteos", _emailSettings.From));

                // Destinatario principal: El Cliente
                if (!string.IsNullOrEmpty(correoCliente))
                {
                    message.To.Add(new MailboxAddress(nombreCliente, correoCliente));
                }

                // Con Copia (Cc): El Cobrador logueado
                if (!string.IsNullOrEmpty(correoVendedor))
                {
                    message.Cc.Add(new MailboxAddress("Respaldo de Caja / Cobrador", correoVendedor));
                }

                message.Subject = $"Comprobante de Venta Electrónico - Ticket #{venta.IdVenta}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; color: #333; max-width: 600px; border: 1px solid #0d6efd; border-radius: 10px;'>
                        <h2 style='color: #0d6efd; text-align: center;'>¡Gracias por tu compra!</h2>
                        <p>Estimado/a <b>{nombreCliente}</b>,</p>
                        <p>Adjunto a este correo compartimos el comprobante digital en formato PDF correspondiente a su compra con el número de Ticket <b>#{venta.IdVenta}</b>.</p>
                        <p>Monto Total Procesado: <b>${venta.Total:N2}</b></p>
                        <hr style='border: 0; border-top: 1px solid #ddd;'>
                        <p style='font-size: 11px; color: #666; text-align: center;'>Este es un correo automático emitido por el control de facturación. Por favor no lo responda.</p>
                    </div>"
                };

                // Adjuntar el PDF generado en memoria
                bodyBuilder.Attachments.Add($"Ticket_{venta.IdVenta}.pdf", pdfData, new ContentType("application", "pdf"));
                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_emailSettings.From, _emailSettings.Password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error enviando comprobante por correo: " + ex.Message);
            }
        }
    }
}