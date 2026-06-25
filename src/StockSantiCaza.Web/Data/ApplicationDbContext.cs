using Microsoft.EntityFrameworkCore;
using StockSantiCaza.Web.Models;

namespace StockSantiCaza.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Arma> Armas => Set<Arma>();
    public DbSet<MunicionLote> MunicionLotes => Set<MunicionLote>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<CredencialCLU> CredencialesCLU => Set<CredencialCLU>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<MovimientoProveedor> MovimientosProveedor => Set<MovimientoProveedor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasIndex(x => x.Sku).IsUnique();
            entity.Property(x => x.PrecioUnitario).HasPrecision(18, 2);
            entity.Property(x => x.CostoUnitario).HasPrecision(18, 2);
            entity.Property(x => x.Sku).HasMaxLength(40);
            entity.Property(x => x.Nombre).HasMaxLength(180);
        });

        modelBuilder.Entity<Arma>(entity =>
        {
            entity.HasIndex(x => x.NumeroSerie).IsUnique();
            entity.Property(x => x.NumeroSerie).HasMaxLength(80);
            entity.Property(x => x.Calibre).HasMaxLength(40);
            entity.HasOne(x => x.Producto)
                .WithMany(x => x.Armas)
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ClienteActual)
                .WithMany(x => x.ArmasRegistradas)
                .HasForeignKey(x => x.ClienteActualId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MunicionLote>(entity =>
        {
            entity.HasIndex(x => new { x.ProductoId, x.NumeroLote }).IsUnique();
            entity.Property(x => x.NumeroLote).HasMaxLength(80);
            entity.Property(x => x.Calibre).HasMaxLength(40);
            entity.HasOne(x => x.Producto)
                .WithMany(x => x.LotesMunicion)
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasIndex(x => x.DniCuit).IsUnique();
            entity.Property(x => x.NombreRazonSocial).HasMaxLength(160);
            entity.Property(x => x.DniCuit).HasMaxLength(20);
            entity.Property(x => x.Activo).HasDefaultValue(true);
        });

        modelBuilder.Entity<CredencialCLU>(entity =>
        {
            entity.HasIndex(x => x.NumeroLegajo).IsUnique();
            entity.HasIndex(x => x.ClienteId).IsUnique();
            entity.HasOne(x => x.Cliente)
                .WithOne(x => x.CredencialCLU)
                .HasForeignKey<CredencialCLU>(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasIndex(x => x.Login).IsUnique();
            entity.Property(x => x.Nombre).HasMaxLength(120);
            entity.Property(x => x.Login).HasMaxLength(60);
            entity.Property(x => x.PasswordHash).HasMaxLength(256);
            entity.Property(x => x.Activo).HasDefaultValue(true);
        });

        modelBuilder.Entity<Venta>(entity =>
        {
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.DescuentoTotal).HasPrecision(18, 2);
            entity.Property(x => x.IvaTotal).HasPrecision(18, 2);
            entity.Property(x => x.Total).HasPrecision(18, 2);
            entity.Property(x => x.Vendedor).HasMaxLength(120);
            entity.HasOne(x => x.Cliente)
                .WithMany(x => x.Ventas)
                .HasForeignKey(x => x.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.VendedorUsuario)
                .WithMany(x => x.VentasRealizadas)
                .HasForeignKey(x => x.VendedorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DetalleVenta>(entity =>
        {
            entity.Property(x => x.PrecioUnitario).HasPrecision(18, 2);
            entity.Property(x => x.Descuento).HasPrecision(18, 2);
            entity.Property(x => x.AlicuotaIva).HasPrecision(5, 2);
            entity.Property(x => x.Subtotal).HasPrecision(18, 2);
            entity.Property(x => x.Iva).HasPrecision(18, 2);
            entity.Property(x => x.Total).HasPrecision(18, 2);
            entity.HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Arma)
                .WithMany()
                .HasForeignKey(x => x.ArmaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MunicionLote)
                .WithMany()
                .HasForeignKey(x => x.MunicionLoteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MovimientoStock>(entity =>
        {
            entity.HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Venta)
                .WithMany()
                .HasForeignKey(x => x.VentaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Arma)
                .WithMany()
                .HasForeignKey(x => x.ArmaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MunicionLote)
                .WithMany()
                .HasForeignKey(x => x.MunicionLoteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.Property(x => x.NombreRazonSocial).HasMaxLength(160);
            entity.Property(x => x.Activo).HasDefaultValue(true);
        });

        modelBuilder.Entity<MovimientoProveedor>(entity =>
        {
            entity.Property(x => x.Monto).HasPrecision(18, 2);
            entity.HasOne(x => x.Proveedor)
                .WithMany(x => x.Movimientos)
                .HasForeignKey(x => x.ProveedorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
