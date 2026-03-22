using System;
using System.Collections.Generic;

namespace Gestion_de_Productos_Lacteos.Models;

public partial class MovimientosInventario
{
    public int IdMovimiento { get; set; }

    public int? IdProducto { get; set; }

    public string? TipoMovimiento { get; set; }

    public int? Cantidad { get; set; }

    public DateTime? Fecha { get; set; }

    public string? Descripcion { get; set; }

    public virtual Producto? IdProductoNavigation { get; set; }
}
