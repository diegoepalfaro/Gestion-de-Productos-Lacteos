using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Gestion_de_Productos_Lacteos.Models;

public partial class SistemaInventarioLacteosContext : DbContext
{
    public SistemaInventarioLacteosContext()
    {
    }

    public SistemaInventarioLacteosContext(DbContextOptions<SistemaInventarioLacteosContext> options)
        : base(options)
    {
    }
    public virtual DbSet<Devolucion> Devoluciones { get; set; }
    public virtual DbSet<AlertasVencimiento> AlertasVencimientos { get; set; }
    public virtual DbSet<Cliente> Clientes { get; set; }
    public virtual DbSet<Compra> Compras { get; set; }
    public virtual DbSet<DetalleCompra> DetalleCompras { get; set; }
    public virtual DbSet<DetalleVenta> DetalleVenta { get; set; }
    public virtual DbSet<Inventario> Inventarios { get; set; }
    public virtual DbSet<Lote> Lotes { get; set; }
    public virtual DbSet<MovimientosInventario> MovimientosInventarios { get; set; }
    public virtual DbSet<Producto> Productos { get; set; }
    public virtual DbSet<Proveedor> Proveedors { get; set; }
    public virtual DbSet<Rol> Rols { get; set; }
    public virtual DbSet<Usuario> Usuarios { get; set; }
    public virtual DbSet<Venta> Venta { get; set; }
    public virtual DbSet<Categoria> Categoria { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Server=DIEGO;Database=SistemaInventarioLacteos;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Devolucion>(entity =>
        {
            entity.HasKey(e => e.IdDevolucion).HasName("PK__Devolucion");
            entity.ToTable("Devolucion");

            entity.HasOne(d => d.LoteNavigation).WithMany()
                .HasForeignKey(d => d.IdLote)
                .HasConstraintName("FK_Devolucion_Lote");

            entity.HasOne(d => d.VentaNavigation).WithMany()
                .HasForeignKey(d => d.IdVenta)
                .HasConstraintName("FK_Devolucion_Venta");
        });

        modelBuilder.Entity<AlertasVencimiento>(entity =>
        {
            entity.HasKey(e => e.IdAlerta);
            entity.ToTable("AlertasVencimiento");
            entity.Property(e => e.IdAlerta).HasColumnName("idAlerta");
            entity.Property(e => e.Estado).HasMaxLength(20).IsUnicode(false).HasColumnName("estado");
            entity.Property(e => e.FechaAlerta).HasColumnName("fechaAlerta");
            entity.Property(e => e.IdLote).HasColumnName("idLote");
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.IdCliente);
            entity.ToTable("Cliente");
            entity.HasIndex(e => e.Nit, "UQ__Cliente__NIT").IsUnique();
            entity.Property(e => e.IdCliente).HasColumnName("idCliente");
            entity.Property(e => e.Correo).HasMaxLength(100).IsUnicode(false).HasColumnName("correo");
            entity.Property(e => e.Direccion).HasMaxLength(200).IsUnicode(false).HasColumnName("direccion");
            entity.Property(e => e.Dui).HasMaxLength(10).IsUnicode(false).HasColumnName("dui");
            entity.Property(e => e.Nit).HasMaxLength(20).IsUnicode(false).HasColumnName("nit");
            entity.Property(e => e.Nombre).HasMaxLength(150).IsUnicode(false).HasColumnName("nombre");
            entity.Property(e => e.Nrc).HasMaxLength(20).IsUnicode(false).HasColumnName("nrc");
            entity.Property(e => e.Telefono).HasMaxLength(20).IsUnicode(false).HasColumnName("telefono");
            entity.Property(e => e.TipoCliente).HasMaxLength(20).IsUnicode(false).HasColumnName("tipoCliente");
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.HasKey(e => e.IdCompra);
            entity.ToTable("Compra");
            entity.Property(e => e.IdCompra).HasColumnName("idCompra");
            entity.Property(e => e.FechaCompra).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fechaCompra");
            entity.Property(e => e.IdProveedor).HasColumnName("idProveedor");
            entity.Property(e => e.Total).HasColumnType("decimal(10, 2)").HasColumnName("total");
        });

        modelBuilder.Entity<DetalleCompra>(entity =>
        {
            entity.HasKey(e => e.IdDetalleCompra);
            entity.ToTable("DetalleCompra");
            entity.Property(e => e.IdDetalleCompra).HasColumnName("idDetalleCompra");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.IdCompra).HasColumnName("idCompra");
            entity.Property(e => e.IdLote).HasColumnName("idLote");
            entity.Property(e => e.IdProducto).HasColumnName("idProducto");
            entity.Property(e => e.Precio).HasColumnType("decimal(10, 4)").HasColumnName("precio");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(10, 4)").HasColumnName("subtotal");
        });

        modelBuilder.Entity<DetalleVenta>(entity =>
        {
            entity.HasKey(e => e.IdDetalleVenta);
            entity.ToTable("DetalleVenta");
            entity.Property(e => e.IdDetalleVenta).HasColumnName("idDetalleVenta");
            entity.Property(e => e.IdVenta).HasColumnName("idVenta");

            // CORRECCIÓN: Ahora es IdLote, ya no es IdProducto
            entity.Property(e => e.IdLote).HasColumnName("idLote");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.Precio).HasColumnType("decimal(10, 4)").HasColumnName("precio");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(10, 4)").HasColumnName("subtotal");

            // CORRECCIÓN: Navegación hacia Lote en lugar de Producto
            entity.HasOne(d => d.LoteNavigation)
                .WithMany(p => p.DetalleVentas)
                .HasForeignKey(d => d.IdLote)
                .HasConstraintName("FK_DetalleVenta_Lote");

            entity.HasOne(d => d.VentaNavigation)
                .WithMany(p => p.DetalleVenta)
                .HasForeignKey(d => d.IdVenta)
                .HasConstraintName("FK_DetalleVenta_Venta");
        });

        modelBuilder.Entity<Inventario>(entity =>
        {
            entity.HasKey(e => e.IdInventario);
            entity.ToTable("Inventario");
            entity.Property(e => e.IdInventario).HasColumnName("idInventario");
            entity.Property(e => e.IdProducto).HasColumnName("idProducto");
            entity.Property(e => e.StockActual).HasColumnName("stockActual");
            entity.Property(e => e.StockMinimo).HasColumnName("stockMinimo");
        });

        modelBuilder.Entity<Lote>(entity =>
        {
            entity.HasKey(e => e.IdLote);
            entity.ToTable("Lote");
            entity.Property(e => e.IdLote).HasColumnName("idLote");
            entity.Property(e => e.IdProducto).HasColumnName("idProducto");
            entity.Property(e => e.NumeroLote).HasMaxLength(50).IsUnicode(false).HasColumnName("numeroLote");

            // CORRECCIÓN: Nuevas columnas agregadas en la base de datos
            entity.Property(e => e.Descripcion).HasMaxLength(255).IsUnicode(false).HasColumnName("descripcion");
            entity.Property(e => e.FechaProduccion).HasColumnType("date").HasColumnName("fechaProduccion");
            entity.Property(e => e.FechaVencimiento).HasColumnType("date").HasColumnName("fechaVencimiento");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.VtaNeta).HasColumnType("decimal(10, 4)").HasColumnName("vtaNeta");
            entity.Property(e => e.IvaConsumidor).HasColumnType("decimal(10, 4)").HasColumnName("ivaConsumidor");
            entity.Property(e => e.CcfSiva).HasColumnType("decimal(10, 4)").HasColumnName("ccfSiva");
            entity.Property(e => e.IvaContribuyente).HasColumnType("decimal(10, 4)").HasColumnName("ivaContribuyente");
            entity.Property(e => e.PrecioFactura).HasColumnType("decimal(10, 4)").HasColumnName("precioFactura");

            // CORRECCIÓN: Navegación actualizada
            entity.HasOne(d => d.ProductoNavigation)
                .WithMany(p => p.Lotes)
                .HasForeignKey(d => d.IdProducto)
                .HasConstraintName("FK_Lote_Producto");
        });

        modelBuilder.Entity<MovimientosInventario>(entity =>
        {
            entity.HasKey(e => e.IdMovimiento);
            entity.ToTable("MovimientosInventario");
            entity.Property(e => e.IdMovimiento).HasColumnName("idMovimiento");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.Descripcion).HasMaxLength(200).IsUnicode(false).HasColumnName("descripcion");
            entity.Property(e => e.Fecha).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fecha");
            entity.Property(e => e.IdProducto).HasColumnName("idProducto");
            entity.Property(e => e.TipoMovimiento).HasMaxLength(20).IsUnicode(false).HasColumnName("tipoMovimiento");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.IdProducto);
            entity.ToTable("Producto");
            entity.Property(e => e.IdProducto).HasColumnName("idProducto");
            entity.Property(e => e.IdCategoria).HasColumnName("idCategoria");
            entity.Property(e => e.Descripcion).HasMaxLength(200).IsUnicode(false).HasColumnName("descripcion");
            entity.Property(e => e.NombreProducto).HasMaxLength(150).IsUnicode(false).HasColumnName("nombreProducto");
            entity.Property(e => e.Estado).HasDefaultValue(true).HasColumnName("estado");

            // CORRECCIÓN: Precios eliminados

            entity.HasOne(d => d.CategoriaNavigation)
                .WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdCategoria)
                .HasConstraintName("FK_Producto_Categoria");
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.IdCategoria);
            entity.ToTable("Categoria");
            entity.Property(e => e.IdCategoria).HasColumnName("idCategoria");
            entity.Property(e => e.NombreCategoria).HasMaxLength(50).HasColumnName("nombreCategoria");
            entity.Property(e => e.Descripcion).HasMaxLength(255).HasColumnName("descripcion");
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.IdProveedor);
            entity.ToTable("Proveedor");
            entity.Property(e => e.IdProveedor).HasColumnName("idProveedor");
            entity.Property(e => e.Correo).HasMaxLength(100).IsUnicode(false).HasColumnName("correo");
            entity.Property(e => e.Direccion).HasMaxLength(200).IsUnicode(false).HasColumnName("direccion");
            entity.Property(e => e.NombreProveedor).HasMaxLength(150).IsUnicode(false).HasColumnName("nombreProveedor");
            entity.Property(e => e.Telefono).HasMaxLength(20).IsUnicode(false).HasColumnName("telefono");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.IdRol);
            entity.ToTable("Rol");
            entity.Property(e => e.IdRol).HasColumnName("idRol");
            entity.Property(e => e.Descripcion).HasMaxLength(200).IsUnicode(false).HasColumnName("descripcion");
            entity.Property(e => e.NombreRol).HasMaxLength(50).IsUnicode(false).HasColumnName("nombreRol");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario);
            entity.ToTable("Usuario");
            entity.Property(e => e.IdUsuario).HasColumnName("idUsuario");
            entity.Property(e => e.Contraseña).HasMaxLength(255).IsUnicode(false).HasColumnName("contraseña");
            entity.Property(e => e.Correo).HasMaxLength(100).IsUnicode(false).HasColumnName("correo");
            entity.Property(e => e.Estado).HasDefaultValue(true).HasColumnName("estado");
            entity.Property(e => e.IdRol).HasColumnName("idRol");
            entity.Property(e => e.Nombre).HasMaxLength(100).IsUnicode(false).HasColumnName("nombre");
            entity.Property(e => e.Usuario1).HasMaxLength(50).IsUnicode(false).HasColumnName("usuario");
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.HasKey(e => e.IdVenta);
            entity.ToTable("Venta");
            entity.Property(e => e.IdVenta).HasColumnName("idVenta");
            entity.Property(e => e.FechaVenta).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("fechaVenta");
            entity.Property(e => e.IdCliente).HasColumnName("idCliente");
            entity.Property(e => e.IdUsuario).HasColumnName("idUsuario");
            entity.Property(e => e.TipoComprobante).HasMaxLength(30).IsUnicode(false).HasColumnName("tipoComprobante");
            entity.Property(e => e.Total).HasColumnType("decimal(10, 2)").HasColumnName("total");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}