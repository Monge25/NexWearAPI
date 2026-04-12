using Microsoft.EntityFrameworkCore;
using NexWearAPI.Models;
using System.Collections;

namespace NexWearAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<PasswordResetCode> PasswordResetCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Users ────────────────────────────────────────────
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();   // Guarda "Customer"/"Admin" en lugar de 0/1

            // ── Products ─────────────────────────────────────────
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Category);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.IsActive);

            // ── Orders ───────────────────────────────────────────
            modelBuilder.Entity<Order>()
                .Property(o => o.Total)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion<string>();   // Guarda "Pending"/"Paid"/... en lugar de 0/1/...

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);  // No borrar usuario si tiene órdenes

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.UserId);

            modelBuilder.Entity<Order>()
                .Property(o => o.PaymentMethod)
                .HasMaxLength(20)
                .HasDefaultValue("paypal");

            modelBuilder.Entity<Order>()
                .Property(o => o.PaypalOrderId)
                .HasMaxLength(50);   

            modelBuilder.Entity<Order>()
                .Property(o => o.PaymentMethod)
                .HasMaxLength(20)
                .HasDefaultValue("card");

            modelBuilder.Entity<Order>()
                .Property(o => o.PaypalOrderId)
                .HasMaxLength(100);        

            // ── OrderItems ───────────────────────────────────────
            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(10, 2);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);   // Si se borra la orden, se borran sus items

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);  // No borrar producto si tiene órdenes

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Variant)
                .WithMany()
                .HasForeignKey(oi => oi.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── CartItems ────────────────────────────────────────
            modelBuilder.Entity<CartItem>()
                .HasOne(c => c.User)
                .WithMany(u => u.CartItems)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(c => c.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(c => c.Variant)
                .WithMany()
                .HasForeignKey(c => c.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // UNIQUE: un usuario no puede tener la misma variante dos veces
            modelBuilder.Entity<CartItem>()
                .HasIndex(c => new { c.UserId, c.VariantId })
                .IsUnique();

            // ── Reviews ──────────────────────────────────────────
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Un usuario solo puede reseñar un producto una vez
            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.UserId, r.ProductId })
                .IsUnique();

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasIndex(r => r.ProductId);

            // PhotoUrls como array de PostgreSQL
            modelBuilder.Entity<Review>()
                .Property(r => r.PhotoUrls)
                .HasColumnType("text[]");

            // ── ProductVariants ──────────────────────────────────────────
            modelBuilder.Entity<ProductVariant>()
                .Property(v => v.PriceModifier)
                .HasPrecision(10, 2);

            modelBuilder.Entity<ProductVariant>()
                .HasOne(v => v.Product);    // Índice para cargar variantes rápido

            // ── Addresses ─────────────────────────────────────────────────
            modelBuilder.Entity<Address>()
                .HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Address>()
                .HasIndex(a => a.UserId);

            // ── PasswordResetCodes ────────────────────────────────────────
            modelBuilder.Entity<PasswordResetCode>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasswordResetCode>()
                .HasIndex(p => p.UserId);
        }
    }
}