using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gestion_de_Productos_Lacteos.Models;

public partial class Producto
{
    [Key]
    public int IdProducto { get; set; }

    [Required]
    public string NombreProducto { get; set; } = null!;

    // Mapeo explícito a la nueva columna
    [Column("idCategoria")]
    public int? IdCategoria { get; set; }

    public string? Descripcion { get; set; }

    public decimal? PrecioCompra { get; set; }

    public decimal? PrecioVenta { get; set; }

    public bool Estado { get; set; } = true;

    [ForeignKey("IdCategoria")]
    public virtual Categoria? CategoriaNavigation { get; set; }

    public virtual ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();

    public virtual ICollection<DetalleVenta> DetalleVenta { get; set; } = new List<DetalleVenta>();

    public virtual ICollection<Inventario> Inventarios { get; set; } = new List<Inventario>();

    public virtual ICollection<Lote> Lotes { get; set; } = new List<Lote>();

    public virtual ICollection<MovimientosInventario> MovimientosInventarios { get; set; } = new List<MovimientosInventario>();
}