using Microsoft.EntityFrameworkCore;
using Api.Models;

namespace Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }


    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<Warehouse> Warehouses { get; set; }
    public DbSet<ProductInventory> ProductInventories { get; set; }
    public DbSet<VariantInventory> VariantInventories { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseItem> PurchaseItems { get; set; }
    public DbSet<SalesOrder> SalesOrders { get; set; }
    public DbSet<SalesItem> SalesItems { get; set; }
    public DbSet<ProductAssembly> ProductAssemblies { get; set; }
    public DbSet<BillOfMaterial> BillOfMaterials { get; set; }
    public DbSet<ProductRequest> ProductRequests { get; set; }
    public DbSet<ProductRequestItem> ProductRequestItems { get; set; }
    public DbSet<ProductMovement> ProductMovements { get; set; }
    public DbSet<ProductMovementSummary> ProductMovementSummaries { get; set; }
    public DbSet<ShoppingCart> ShoppingCarts { get; set; }
    public DbSet<OrderTracking> OrderTrackings { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<PromoCode> PromoCodes { get; set; }
    public DbSet<PromoCodeUser> PromoCodeUsers { get; set; }
    public DbSet<PromoCodeProduct> PromoCodeProducts { get; set; }
    public DbSet<PromoCodeUsage> PromoCodeUsages { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<Settings> Settings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.FullName).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
        });

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired();
        });

        // Configure UserRole entity (many-to-many)
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Permission entity
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Resource).IsRequired();
            entity.Property(e => e.Action).IsRequired();
        });

        // Configure RolePermission entity (many-to-many)
        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });
            
            entity.HasOne(e => e.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Entity, e.EntityId });
            entity.HasIndex(e => e.ActorUserId);
            entity.HasIndex(e => e.At);
            
            entity.HasOne(e => e.ActorUser)
                .WithMany()
                .HasForeignKey(e => e.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Category entity
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
        });

        // Configure Product entity
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CompareAtPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Cost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Weight).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PackageLength).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PackageWidth).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PackageHeight).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasConversion<int>(); // Store enum as int
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.HasIndex(e => e.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");
            
            // Legacy one-to-many relationship (primary category)
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Many-to-many relationship with categories
            entity.HasMany(e => e.ProductCategories)
                .WithOne(pc => pc.Product)
                .HasForeignKey(pc => pc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure ProductCategory join table
        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.CategoryId });
            entity.HasIndex(e => new { e.ProductId, e.CategoryId }).IsUnique();
            
            entity.HasOne(e => e.Product)
                .WithMany(p => p.ProductCategories)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Category)
                .WithMany(c => c.ProductCategories)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure ProductVariant entity
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Color).IsRequired();
            entity.Property(e => e.PriceOverride).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Warehouse entity
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            // Configure relationship with User (Manager)
            entity.HasOne(e => e.ManagerUser)
                .WithMany()
                .HasForeignKey(e => e.ManagerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure ProductInventory entity
        modelBuilder.Entity<ProductInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MinimumStockLevel).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaximumStockLevel).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.Product)
                .WithMany(p => p.ProductInventories)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Warehouse)
                .WithMany(w => w.ProductInventories)
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Ensure unique combination of Product and Warehouse
            entity.HasIndex(e => new { e.ProductId, e.WarehouseId }).IsUnique();
        });

        // Configure VariantInventory entity
        modelBuilder.Entity<VariantInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MinimumStockLevel).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MaximumStockLevel).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.ProductVariant)
                .WithMany()
                .HasForeignKey(e => e.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Ensure unique combination of ProductVariant and Warehouse
            entity.HasIndex(e => new { e.ProductVariantId, e.WarehouseId }).IsUnique();
        });

        // Configure PurchaseOrder entity
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.Property(e => e.OrderNumber).IsRequired();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OrderDate).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.ApprovedByUser)
                .WithMany()
                .HasForeignKey(e => e.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure PurchaseItem entity
        modelBuilder.Entity<PurchaseItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.PurchaseOrder)
                .WithMany(po => po.PurchaseItems)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure SalesOrder entity
        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.Property(e => e.OrderNumber).IsRequired();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.OrderDate).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.ConfirmedByUser)
                .WithMany()
                .HasForeignKey(e => e.ConfirmedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure SalesItem entity
        modelBuilder.Entity<SalesItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.SalesOrder)
                .WithMany(so => so.SalesItems)
                .HasForeignKey(e => e.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.ProductVariant)
                .WithMany()
                .HasForeignKey(e => e.ProductVariantId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ProductAssembly entity
        modelBuilder.Entity<ProductAssembly>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.SalePrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.CompletedByUser)
                .WithMany()
                .HasForeignKey(e => e.CompletedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure BillOfMaterial entity
        modelBuilder.Entity<BillOfMaterial>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequiredQuantity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AvailableQuantity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.ProductAssembly)
                .WithMany(pa => pa.BillOfMaterials)
                .HasForeignKey(e => e.ProductAssemblyId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.RawProduct)
                .WithMany()
                .HasForeignKey(e => e.RawProductId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ProductRequest entity
        modelBuilder.Entity<ProductRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestDate).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.RequestedByUser)
                .WithMany()
                .HasForeignKey(e => e.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.ApprovedByUser)
                .WithMany()
                .HasForeignKey(e => e.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ProductRequestItem entity
        modelBuilder.Entity<ProductRequestItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QuantityRequested).HasColumnType("decimal(18,2)");
            entity.Property(e => e.QuantityApproved).HasColumnType("decimal(18,2)");
            entity.Property(e => e.QuantityReceived).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.ProductRequest)
                .WithMany(pr => pr.ProductRequestItems)
                .HasForeignKey(e => e.ProductRequestId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ProductMovement entity
        modelBuilder.Entity<ProductMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.MovementDate).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure ProductMovementSummary entity
        modelBuilder.Entity<ProductMovementSummary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OpeningBalance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalIn).HasColumnType("decimal(18,2)");
            entity.Property(e => e.TotalOut).HasColumnType("decimal(18,2)");
            entity.Property(e => e.ClosingBalance).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure ShoppingCart entity
        modelBuilder.Entity<ShoppingCart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18,2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.ProductVariant)
                .WithMany()
                .HasForeignKey(e => e.ProductVariantId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Ensure unique combination of User, Product, and Variant
            // This allows same product with different variants in cart
            entity.HasIndex(e => new { e.UserId, e.ProductId, e.ProductVariantId }).IsUnique();
        });

        // Configure OrderTracking entity
        modelBuilder.Entity<OrderTracking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Timestamp).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.Order)
                .WithMany()
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Customer entity
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            // Ensure unique phone number
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
        });

        // Configure PromoCode entity
        modelBuilder.Entity<PromoCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DiscountType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure PromoCodeUser entity (many-to-many)
        modelBuilder.Entity<PromoCodeUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.PromoCode)
                .WithMany(pc => pc.PromoCodeUsers)
                .HasForeignKey(e => e.PromoCodeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Ensure unique combination
            entity.HasIndex(e => new { e.PromoCodeId, e.UserId }).IsUnique();
        });

        // Configure PromoCodeProduct entity (many-to-many)
        modelBuilder.Entity<PromoCodeProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.PromoCode)
                .WithMany(pc => pc.PromoCodeProducts)
                .HasForeignKey(e => e.PromoCodeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Ensure unique combination
            entity.HasIndex(e => new { e.PromoCodeId, e.ProductId }).IsUnique();
        });

        // Configure PromoCodeUsage entity
        modelBuilder.Entity<PromoCodeUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.PromoCode)
                .WithMany(pc => pc.PromoCodeUsages)
                .HasForeignKey(e => e.PromoCodeId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.SalesOrder)
                .WithMany()
                .HasForeignKey(e => e.SalesOrderId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Wishlist entity
        modelBuilder.Entity<Wishlist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.ProductVariant)
                .WithMany()
                .HasForeignKey(e => e.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Ensure unique combination of User, Product, and Variant
            entity.HasIndex(e => new { e.UserId, e.ProductId, e.ProductVariantId }).IsUnique();
        });

        // Seed data removed - will be added for new project as needed
    }
}
