using Gestion_de_Productos_Lacteos.Models;

public interface IEmailService
{
    Task EnviarComprobanteVentaAsync(string correoCliente, string correoVendedor, Venta venta, List<DetalleVenta> detalles, string nombreCliente);
}