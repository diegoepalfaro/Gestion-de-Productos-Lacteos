using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestion_de_Productos_Lacteos.Models;

public partial class Lote
{
    public int IdLote { get; set; }

    public int? IdProducto { get; set; }

    public string? NumeroLote { get; set; }

    public DateOnly? FechaProduccion { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public int? Cantidad { get; set; }

    public virtual ICollection<AlertasVencimiento> AlertasVencimientos { get; set; } = new List<AlertasVencimiento>();

    public virtual ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();

    public virtual Producto? IdProductoNavigation { get; set; }

    [NotMapped]
    public int DiasParaVencer
    {
        get
        {
            // Si la fecha de vencimiento es nula, devolvemos un valor neutro (ej. 0)
            if (!FechaVencimiento.HasValue) return 0;

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Now);

            // .Value nos permite acceder a DayNumber del DateOnly original
            return FechaVencimiento.Value.DayNumber - hoy.DayNumber;
        }
    }
}
