using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestion_de_Productos_Lacteos.Models;

public partial class DetalleVenta
{
    [Key]
    public int IdDetalleVenta { get; set; }

    [Column("idVenta")]
    public int? IdVenta { get; set; }

    [Column("idLote")] // Nombre exacto de la base de datos
    public int? IdLote { get; set; }

    public int? Cantidad { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal? Precio { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal? Subtotal { get; set; }

    [ForeignKey("IdLote")]
    public virtual Lote? LoteNavigation { get; set; }

    [ForeignKey("IdVenta")]
    public virtual Venta? VentaNavigation { get; set; }
}