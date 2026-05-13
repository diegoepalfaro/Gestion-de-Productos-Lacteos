using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Gestion_de_Productos_Lacteos.Models;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace SistemaInventarioLacteos.Services
{
    public class VerificadorVencimientoService : BackgroundService
    {
        private readonly ILogger<VerificadorVencimientoService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly EmailSettings _emailSettings;

        // Configura aquí la hora a la que quieres que se envíe el correo (0-23)
        private const int HORA_ENVIO = 11;

        public VerificadorVencimientoService(
            ILogger<VerificadorVencimientoService> logger,
            IServiceProvider serviceProvider,
            IOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _emailSettings = emailSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de alertas programado para las {0}:00 AM.", HORA_ENVIO);

            while (!stoppingToken.IsCancellationRequested)
            {
                var ahora = DateTime.Now;

                // Solo actúa si es la hora configurada (ej. las 8:00 AM)
                if (ahora.Hour == HORA_ENVIO)
                {
                    _logger.LogInformation("Es hora de enviar alertas. Iniciando proceso...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<SistemaInventarioLacteosContext>();
                        await ProcesarAlertasDiarias(context);
                    }

                    // IMPORTANTE: Después de enviar el correo, esperamos 1 hora y 1 minuto.
                    // Esto evita que el bucle vuelva a entrar en "ahora.Hour == 8" inmediatamente.
                    _logger.LogInformation("Proceso completado. Entrando en espera prolongada para evitar duplicados.");
                    await Task.Delay(TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(1)), stoppingToken);
                }

                // Espera un minuto antes de volver a revisar la hora
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcesarAlertasDiarias(SistemaInventarioLacteosContext context)
        {
            try
            {
                var hoy = DateTime.Today; // 
                var fechaLimite = hoy.AddDays(7); // 

                // 1. Buscamos los lotes que vencen pronto 
                var lotesPorVencer = await context.Lotes
                    .Include(l => l.ProductoNavigation)
                    .Where(l => l.Cantidad > 0 &&
                                l.FechaVencimiento.HasValue &&
                                l.FechaVencimiento.Value.Date <= fechaLimite.Date &&
                                l.FechaVencimiento.Value.Date >= hoy.Date)
                    .ToListAsync();

                if (!lotesPorVencer.Any())
                {
                    _logger.LogInformation("No hay productos próximos a vencer para notificar hoy.");
                    return;
                }

                // 2. Filtramos lotes que no hayan sido notificados hoy para evitar spam 
                var lotesParaNotificar = new List<Lote>();
                foreach (var lote in lotesPorVencer)
                {
                    bool yaNotificado = await context.AlertasVencimientos
                        .AnyAsync(a => a.IdLote == lote.IdLote && a.FechaAlerta.Value.Date == hoy.Date);

                    if (!yaNotificado) lotesParaNotificar.Add(lote);
                }

                if (!lotesParaNotificar.Any()) return;

                // 3. OBTENER TODOS LOS USUARIOS ACTIVOS CON CORREO 
                var destinatarios = await context.Usuarios
                    .Where(u => u.Estado == true && !string.IsNullOrEmpty(u.Correo))
                    .Select(u => u.Correo)
                    .ToListAsync();

                if (destinatarios.Any())
                {
                    _logger.LogInformation("Enviando alertas a {0} usuarios del sistema...", destinatarios.Count);
                    // 4. Enviamos el correo a cada uno 
                    foreach (var email in destinatarios)
                    {
                        await EnviarCorreoAlerta(email, lotesParaNotificar);
                    }

                    // 5. Registramos en la BD que ya se notificaron estos lotes hoy 
                    foreach (var lote in lotesParaNotificar)
                    {
                        context.AlertasVencimientos.Add(new AlertasVencimiento
                        {
                            IdLote = lote.IdLote,
                            FechaAlerta = hoy,
                            Estado = "Enviado"
                        });
                    }
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error al procesar alertas múltiples: {0}", ex.Message);
            }
        }

        private async Task<bool> EnviarCorreoAlerta(string correoDestino, List<Lote> lotes)
        {
            try
            {
                using (var client = new SmtpClient(_emailSettings.Host, _emailSettings.Port))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(_emailSettings.From, _emailSettings.Password);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_emailSettings.From, "Sistema de Inventario Lácteos"),
                        Subject = $"⚠️ Alerta de Vencimiento - {DateTime.Now:dd/MM/yyyy}",
                        Body = GenerarCuerpoHtml(lotes),
                        IsBodyHtml = true,
                        Priority = MailPriority.High
                    };

                    mailMessage.To.Add(correoDestino);
                    await client.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Error de SMTP: {0}", ex.Message);
                return false;
            }
        }

        private string GenerarCuerpoHtml(List<Lote> lotes)
        {
            var sb = new StringBuilder();
            sb.Append("<html><body style='font-family: Arial, sans-serif;'>");
            sb.Append("<h2 style='color: #e67e22;'>Resumen de Productos Próximos a Vencer</h2>");
            sb.Append("<table border='1' cellpadding='10' style='border-collapse: collapse; width: 100%;'>");
            sb.Append("<tr style='background-color: #f2f2f2;'><th>Producto</th><th>Lote</th><th>Vencimiento</th><th>Stock</th></tr>");

            foreach (var lote in lotes)
            {
                sb.Append("<tr>");
                sb.Append($"<td>{lote.ProductoNavigation?.NombreProducto}</td>");
                sb.Append($"<td>{lote.NumeroLote}</td>");
                sb.Append($"<td style='color: red;'>{lote.FechaVencimiento?.ToString("dd/MM/yyyy")}</td>");
                sb.Append($"<td>{lote.Cantidad}</td>");
                sb.Append("</tr>");
            }

            sb.Append("</table>");
            sb.Append("<p>Por favor, tome las medidas necesarias para la rotación de estos productos.</p>");
            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}