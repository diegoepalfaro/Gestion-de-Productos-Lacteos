using System;
using System.Collections.Generic;

namespace Gestion_de_Productos_Lacteos.Models;

public partial class AlertasVencimiento
{
    public int IdAlerta { get; set; }

    public int? IdLote { get; set; }

    public DateOnly? FechaAlerta { get; set; }

    public string? Estado { get; set; }

    public virtual Lote? IdLoteNavigation { get; set; }
}
