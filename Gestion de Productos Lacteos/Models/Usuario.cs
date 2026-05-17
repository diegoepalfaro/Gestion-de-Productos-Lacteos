using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema; 

namespace Gestion_de_Productos_Lacteos.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string Nombre { get; set; } = null!;

    public string Usuario1 { get; set; } = null!;

    public string Contraseña { get; set; } = null!;

    public string? Correo { get; set; }

    public int? IdRol { get; set; }

    public bool? Estado { get; set; }

    [ForeignKey("IdRol")]
    public virtual Rol? IdRolNavigation { get; set; }

    public virtual ICollection<Venta> Venta { get; set; } = new List<Venta>();
}