using Microsoft.EntityFrameworkCore;
namespace Gestion_de_Productos_Lacteos.Models
{
    public class GestionProductosLacteosDbContext : DbContext
    {
        public GestionProductosLacteosDbContext(DbContextOptions<GestionProductosLacteosDbContext> options) : base(options) { }

    }
}
