using System.Collections.Generic;
using System.Threading.Tasks;
using Gestion_de_Productos_Lacteos.Models;

namespace SistemaInventarioLacteos.Services // <-- Asegúrate que diga exactamente esto
{
    public interface IEmailService
    {
        Task EnviarComprobanteVentaAsync(string correoCliente, string correoVendedor, Venta venta, List<DetalleVenta> detalles, string nombreCliente, byte[] pdfData);
    }
}