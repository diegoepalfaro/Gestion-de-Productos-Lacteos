using System;
using System.Collections.Generic;

namespace Gestion_de_Productos_Lacteos.Models;

public partial class Inventario
{
    public int IdInventario { get; set; }

    public int? IdProducto { get; set; }

    public int? StockActual { get; set; }

    public int? StockMinimo { get; set; }

    public virtual Producto? IdProductoNavigation { get; set; }
}
