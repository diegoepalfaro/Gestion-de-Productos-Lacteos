using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestion_de_Productos_Lacteos.Models;

public partial class Lote
{
    [Key]
    public int IdLote { get; set; }

    [Column("idProducto")]
    public int? IdProducto { get; set; }

    public string? NumeroLote { get; set; }

    public string? Descripcion { get; set; }

    public DateTime? FechaProduccion { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public int? Cantidad { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal VtaNeta { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal IvaConsumidor { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal CcfSiva { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal IvaContribuyente { get; set; }

    [Column(TypeName = "decimal(10, 4)")]
    public decimal PrecioFactura { get; set; }

    [Column("idProveedor")]
    public int? IdProveedor { get; set; }

    [Column("fechaIngreso")]
    public DateTime? FechaIngreso { get; set; }

    [Column("costoCompra", TypeName = "decimal(10, 4)")]
    public decimal? CostoCompra { get; set; }

    [Column("ultimaModificacion")]
    public DateTime? UltimaModificacion { get; set; }
    // ---------------------------------------------------

    [ForeignKey("IdProducto")]
    public virtual Producto? ProductoNavigation { get; set; }

    [ForeignKey("IdProveedor")]
    public virtual Proveedor? IdProveedorNavigation { get; set; }

    public virtual ICollection<AlertasVencimiento> AlertasVencimientos { get; set; } = new List<AlertasVencimiento>();

    public virtual ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();

    public virtual ICollection<DetalleVenta> DetalleVentas { get; set; } = new List<DetalleVenta>();
}