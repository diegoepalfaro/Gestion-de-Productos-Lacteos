using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestion_de_Productos_Lacteos.Models;

public partial class Devolucion
{
    [Key]
    public int IdDevolucion { get; set; }

    [Column("idVenta")]
    public int? IdVenta { get; set; }

    [Column("idLote")]
    public int? IdLote { get; set; }

    [Column("cantidad")]
    public int? Cantidad { get; set; }

    [Column("motivo")]
    public string? Motivo { get; set; }

    [Column("estadoProducto")]
    public string? EstadoProducto { get; set; }

    [Column("fechaDevolucion")]
    public DateTime? FechaDevolucion { get; set; }

    [Column("montoDevuelto", TypeName = "decimal(10, 4)")]
    public decimal? MontoDevuelto { get; set; }

    [ForeignKey("IdLote")]
    public virtual Lote? LoteNavigation { get; set; }

    [ForeignKey("IdVenta")]
    public virtual Venta? VentaNavigation { get; set; }
}