using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestion_de_Productos_Lacteos.Models;

public partial class AlertasVencimiento
{
    [Key]
    public int IdAlerta { get; set; }

    [Column("idLote")]
    public int? IdLote { get; set; }

    // CAMBIO CLAVE: Cambiar DateOnly? por DateTime?
    [Column("fechaAlerta")]
    public DateTime? FechaAlerta { get; set; }

    [Column("estado")]
    public string? Estado { get; set; }

    [ForeignKey("IdLote")]
    public virtual Lote? LoteNavigation { get; set; }
}