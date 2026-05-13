using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema; // <-- Obligatorio para [ForeignKey]

namespace Gestion_de_Productos_Lacteos.Models
{
    public class Venta
    {
        public int IdVenta { get; set; }

        public DateTime? FechaVenta { get; set; }

        public int? IdCliente { get; set; }

        public int? IdUsuario { get; set; }

        public string? TipoComprobante { get; set; }

        public decimal? Total { get; set; }

        public virtual ICollection<DetalleVenta> DetalleVenta { get; set; } = new List<DetalleVenta>();

        // Le indicamos explícitamente qué columna de la base de datos usar
        [ForeignKey("IdCliente")]
        public virtual Cliente? IdClienteNavigation { get; set; }

        [ForeignKey("IdUsuario")]
        public virtual Usuario? IdUsuarioNavigation { get; set; }
    }
}