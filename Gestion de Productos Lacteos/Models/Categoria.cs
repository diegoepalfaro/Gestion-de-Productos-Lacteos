using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gestion_de_Productos_Lacteos.Models
{
    public partial class Categoria
    {
        public Categoria()
        {
            // Inicializamos la colección para evitar errores de referencia nula
            Productos = new HashSet<Producto>();
        }

        [Key]
        public int IdCategoria { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Categoría")]
        public string NombreCategoria { get; set; } = null!;

        [StringLength(255)]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        // Propiedad de navegación: Una categoría tiene muchos productos
        public virtual ICollection<Producto> Productos { get; set; }
    }
}