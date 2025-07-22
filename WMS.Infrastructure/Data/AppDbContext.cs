using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Security;
using System;
using WMS.Domain.Models;
using WMS.Domain.Interfaces;
using WMS.Infrastructure.Data.Configurations;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WMS.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        private readonly ITenantService? _tenantService;
        private readonly ICurrentUserService? _currentUserService;
        private readonly IDateTime? _dateTime;
        private readonly ILogger<AppDbContext>? _logger;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ITenantService tenantService,
            ICurrentUserService currentUserService,
            ILogger<AppDbContext> logger,
            IDateTime dateTime) : base(options)
        {
            _tenantService = tenantService;
            _currentUserService = currentUserService;
            _logger = logger;
            _dateTime = dateTime;
        }
        // Constructor for design-time without tenant service, current user service, and datetime
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
            _tenantService = null;
            _currentUserService = null;
            _logger = null;
            _dateTime = null;
        }

        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseConfiguration> WarehouseConfigurations { get; set; }
        public DbSet<Zone> Zones { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<ClientConfiguration> ClientConfigurations { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<InventoryMovement> InventoryMovements { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderLine> OrderLines { get; set; }
        public DbSet<OrderLineAllocation> OrderLineAllocations { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<JobTask> JobTasks { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserClaim> UserClaims { get; set; }
        private Guid CurrentWarehouseId => _tenantService?.CurrentWarehouseId ?? Guid.Empty;
        public DbSet<GIV_AuditLog> GIV_AuditLogs { get; set; }
        public DbSet<GIV_FinishedGood> GIV_FinishedGoods { get; set; }
        public DbSet<GIV_InventoryMovement> GIV_InventoryMovements { get; set; }
        public DbSet<GIV_FG_Receive> GIV_FG_Receives  { get; set; }
        public DbSet<GIV_FG_ReceivePallet> GIV_FG_ReceivePallets { get; set; }
        public DbSet<GIV_FG_ReceivePalletItem> GIV_FG_ReceivePalletItems { get; set; }
        public DbSet<GIV_FG_ReceivePalletPhoto> GIV_FG_ReceivePalletPhotos { get; set; }
        public DbSet<GIV_FG_Release> GIV_FG_Releases { get; set; }
        public DbSet<GIV_FG_ReleaseDetails> GIV_FG_ReleaseDetails { get; set; }
        public DbSet<GIV_RawMaterial> GIV_RawMaterials { get; set; }
        public DbSet<GIV_RM_Receive> GIV_RM_Receives { get; set; }
        public DbSet<GIV_RM_ReceivePallet> GIV_RM_ReceivePallets { get; set; }
        public DbSet<GIV_RM_ReceivePalletPhoto> GIV_RM_ReceivePalletPhotos { get; set; }
        public DbSet<GIV_RM_ReceivePalletItem> GIV_RM_ReceivePalletItems{ get; set; }
        public DbSet<GIV_RM_Release> GIV_RM_Releases { get; set; }
        public DbSet<GIV_RM_ReleaseDetails> GIV_RM_ReleaseDetails { get; set; }
        public DbSet <GIV_Container> GIV_Containers { get; set; }
        public DbSet<GeneralCodeType> GeneralCodeTypes { get; set; }
        public DbSet<GeneralCode> GeneralCodes { get; set; }
        public DbSet<FileUpload> FileUploads { get; set; }
        public DbSet<FileUploadItem> FileUploadItems { get; set; }
        public DbSet<GIV_ContainerPhoto> GIV_ContainerPhotos { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Product>().ToTable("TB_Product");
            modelBuilder.Entity<Inventory>().ToTable("TB_Inventory");
            modelBuilder.Entity<InventoryMovement>().ToTable("TB_InventoryMovement");
            // Exclude these entities from migrations
            //modelBuilder.Entity<Product>().ToTable("TB_Product", t => t.ExcludeFromMigrations());
            //modelBuilder.Entity<Inventory>().ToTable("TB_Inventory", t => t.ExcludeFromMigrations());
            //modelBuilder.Entity<InventoryMovement>().ToTable("TB_InventoryMovement", t => t.ExcludeFromMigrations());

            modelBuilder.Entity<Order>().ToTable("TB_Order", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<OrderLine>().ToTable("TB_OrderLine", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<OrderLineAllocation>().ToTable("TB_OrderLineAllocation", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<JobTask>().ToTable("TB_JobTask", t => t.ExcludeFromMigrations());

            // remove FK required from auditlog due to login process still dont have warehouse
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.Warehouse)
                .WithMany()
                .HasForeignKey(a => a.WarehouseId)
                .IsRequired(false) // Make FK not required
                .OnDelete(DeleteBehavior.SetNull); // When warehouse is deleted, set FK to null

            // Apply all entity configurations from the current assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // Global query filters for isdeleted
            ApplyGlobalFilters(modelBuilder);

            // Apply authentication configurations
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
            modelBuilder.ApplyConfiguration(new UserClaimConfiguration());

            // Apply file upload configurations
            modelBuilder.ApplyConfiguration(new FileUploadConfiguration());
            modelBuilder.ApplyConfiguration(new FileUploadItemConfiguration());
        }

        private void ApplyGlobalFilters(ModelBuilder modelBuilder)
        {
            // Apply soft delete filter for all entities that inherit from BaseEntity
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Skip entities that don't inherit from BaseEntity
                if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                    continue;

                // Create parameter expression for the entity
                var parameter = Expression.Parameter(entityType.ClrType, "e");

                // Create a check for IsDeleted == false
                var isDeletedProperty = Expression.Property(parameter, "IsDeleted");
                var isNotDeletedExpression = Expression.Equal(isDeletedProperty, Expression.Constant(false));

                // Create lambda and set filter
                var lambda = Expression.Lambda(isNotDeletedExpression, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_tenantService != null && _currentUserService != null && _logger != null)
            {
                // Step 1: Collect entities that will be audited
                var entries = ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                    .ToList();

                // Step 2: Create audit entries with JSON changes
                var auditEntries = new List<AuditLog>();
                var now = _dateTime.UtcNow;
                var username = _currentUserService.GetCurrentUsername ?? "System";
                var userId = _currentUserService.UserId ?? Guid.Empty.ToString();
                //var warehouseId = _currentUserService.CurrentWarehouseId;
                // This is the critical change - use a nullable warehouse ID
                Guid? warehouseId = null;

                // Only set warehouseId if it's valid (not empty)
                if (_currentUserService.CurrentWarehouseId != Guid.Empty)
                {
                    warehouseId = _currentUserService.CurrentWarehouseId;
                }

                // Automatically update audit properties and ensure all DateTime properties are in UTC
                foreach (var entry in ChangeTracker.Entries<BaseEntity>())
                {
                    // Convert any DateTime properties to UTC
                    foreach (var property in entry.Properties)
                    {
                        if (property.CurrentValue is DateTime dateTime && dateTime.Kind != DateTimeKind.Utc)
                        {
                            // If it's Unspecified or Local, convert to UTC
                            property.CurrentValue = dateTime.Kind == DateTimeKind.Unspecified
                                ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                                : dateTime.ToUniversalTime();
                        }
                    }

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            entry.Entity.CreatedBy = userId;
                            entry.Entity.CreatedAt = now; // Already UTC from _dateTime.UtcNow
                            break;
                        case EntityState.Modified:
                            entry.Entity.ModifiedBy = userId;
                            entry.Entity.ModifiedAt = now; // Already UTC from _dateTime.UtcNow
                            break;
                    }
                }

                foreach (var entry in entries)
                {
                    // Skip audit logs themselves
                    if (entry.Entity is AuditLog || entry.Entity is RefreshToken)
                        continue;

                    var entityType = entry.Entity.GetType().Name;
                    var action = entry.State.ToString();

                    // For new entities, we'll fill in the ID after SaveChanges
                    var entityId = entry.State != EntityState.Added
                        ? (Guid)entry.Property("Id").CurrentValue
                        : Guid.Empty;

                    // Create a simple dictionary for changes
                    var changeData = new Dictionary<string, Dictionary<string, string>>();

                    // Enhanced version of your code with proper DateTime handling
                    if (entry.State == EntityState.Added)
                    {
                        // For new entities, store new values
                        foreach (var prop in entry.Properties)
                        {
                            if (ShouldSkipProperty(prop.Metadata.Name))
                                continue;

                            // Special handling for DateTime properties
                            if (prop.CurrentValue is DateTime dateTimeValue)
                            {
                                // Convert to UTC and format in ISO 8601
                                var utcValue = dateTimeValue.Kind == DateTimeKind.Unspecified
                                    ? DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc)
                                    : dateTimeValue.ToUniversalTime();

                                changeData[prop.Metadata.Name] = new Dictionary<string, string> {
                                    { "new", utcValue.ToString("o") } // ISO 8601 format
                                };
                            }
                            else
                            {
                                // Handle non-DateTime properties as before
                                changeData[prop.Metadata.Name] = new Dictionary<string, string> {
                                    { "new", prop.CurrentValue?.ToString() }
                                };
                            }
                        }
                    }
                    else if (entry.State == EntityState.Modified)
                    {
                        // For modifications, store both old and new values
                        foreach (var prop in entry.Properties.Where(p => p.IsModified))
                        {
                            if (ShouldSkipProperty(prop.Metadata.Name))
                                continue;

                            // Special handling for DateTime properties
                            if (prop.OriginalValue is DateTime originalDateTimeValue ||
                                prop.CurrentValue is DateTime currentDateTimeValue)
                            {
                                var formattedOriginal = string.Empty;
                                var formattedCurrent = string.Empty;

                                // Format original value if it exists
                                if (prop.OriginalValue is DateTime origDateTime)
                                {
                                    var utcOriginal = origDateTime.Kind == DateTimeKind.Unspecified
                                        ? DateTime.SpecifyKind(origDateTime, DateTimeKind.Utc)
                                        : origDateTime.ToUniversalTime();
                                    formattedOriginal = utcOriginal.ToString("o");
                                }

                                // Format current value if it exists
                                if (prop.CurrentValue is DateTime currDateTime)
                                {
                                    var utcCurrent = currDateTime.Kind == DateTimeKind.Unspecified
                                        ? DateTime.SpecifyKind(currDateTime, DateTimeKind.Utc)
                                        : currDateTime.ToUniversalTime();
                                    formattedCurrent = utcCurrent.ToString("o");
                                }

                                changeData[prop.Metadata.Name] = new Dictionary<string, string> {
                                    { "old", formattedOriginal },
                                    { "new", formattedCurrent }
                                };
                            }
                            else
                            {
                                // Handle non-DateTime properties as before
                                changeData[prop.Metadata.Name] = new Dictionary<string, string> {
                                    { "old", prop.OriginalValue?.ToString() },
                                    { "new", prop.CurrentValue?.ToString() }
                                };
                            }
                        }
                    }
                    else if (entry.State == EntityState.Deleted)
                    {
                        // For deletions, store old values
                        foreach (var prop in entry.Properties)
                        {
                            if (ShouldSkipProperty(prop.Metadata.Name))
                                continue;

                            // Special handling for DateTime properties
                            if (prop.OriginalValue is DateTime originalDateTimeValue)
                            {
                                var utcOriginal = originalDateTimeValue.Kind == DateTimeKind.Unspecified
                                    ? DateTime.SpecifyKind(originalDateTimeValue, DateTimeKind.Utc)
                                    : originalDateTimeValue.ToUniversalTime();

                                changeData[prop.Metadata.Name] = new Dictionary<string, string> {
                                    { "old", utcOriginal.ToString("o") }
                                };
                            }
                            else
                            {
                                // Handle non-DateTime properties as before
                                changeData[prop.Metadata.Name] = new Dictionary<string, string> {
                                    { "old", prop.OriginalValue?.ToString() }
                                };
                            }
                        }
                    }
                    // Filter only changed properties
                    var actualChanges = changeData.Where(kvp =>
                    {
                        var oldValue = kvp.Value.GetValueOrDefault("old");
                        var newValue = kvp.Value.GetValueOrDefault("new");

                        // Handle null cases properly
                        if (oldValue == null && newValue == null) return false;
                        if (oldValue == null || newValue == null) return true;

                        // Compare trimmed strings to avoid whitespace issues
                        return !string.Equals(oldValue.Trim(), newValue.Trim(), StringComparison.OrdinalIgnoreCase);
                    }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    // Simple JSON serialization of changes
                    var changesJson = JsonSerializer.Serialize(actualChanges);
                    // Create audit log
                    var auditLog = new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        EntityName = entityType,
                        EntityId = entityId,
                        Action = action,
                        ChangesJson = changesJson,
                        CreatedBy = userId,
                        CreatedAt = now,
                        Username = username,
                        WarehouseId = warehouseId,
                        // Store temporary reference
                        TempEntry = entry.State == EntityState.Added ? entry : null
                    };
                    auditEntries.Add(auditLog);
                }

                // Step 3: Save main entities to get IDs
                var result = await base.SaveChangesAsync(cancellationToken);

                // Step 4: Update audit entries with real IDs
                foreach (var auditEntry in auditEntries.Where(a => a.TempEntry != null))
                {
                    auditEntry.EntityId = (Guid)auditEntry.TempEntry.Property("Id").CurrentValue;
                    auditEntry.TempEntry = null; // Clear reference
                }

                // Step 5: Save audit logs
                if (auditEntries.Any())
                {
                    try
                    {
                        AuditLogs.AddRange(auditEntries);
                        await base.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving audit logs");
                    }
                }
                return result;
            }
            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .LogTo(Console.WriteLine, new[] { RelationalEventId.CommandExecuted })
                .EnableSensitiveDataLogging();
        }
        private bool ShouldSkipProperty(string propertyName)
        {
            // Skip navigation collections and sensitive data
            return propertyName.Contains("Collection") ||
                   propertyName == "PasswordHash" ||
                   propertyName == "SecurityStamp" ||
                   propertyName.Contains("Password");
        }
    }
}