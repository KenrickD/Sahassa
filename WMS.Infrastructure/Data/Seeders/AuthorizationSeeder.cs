using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WMS.Domain.DTOs;
using WMS.Domain.Models;

namespace WMS.Infrastructure.Data.Seeders
{
    public static class AuthorizationSeeder
    {
        public static async Task SeedAuthorizationDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

            // Seed roles
            var roles = new List<Role>
            {
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Name = AppConsts.Roles.SYSTEM_ADMIN,
                    Description = "System Administrator with full access",
                    IsSystemRole = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    Name = AppConsts.Roles.WAREHOUSE_MANAGER,
                    Description = "Warehouse Manager with warehouse-wide access",
                    IsSystemRole = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                    Name = AppConsts.Roles.WAREHOUSE_USER,
                    Description = "Standard warehouse user",
                    IsSystemRole = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                    Name = AppConsts.Roles.CLIENT_USER,
                    Description = "Client user with client-specific access",
                    IsSystemRole = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
                    Name = AppConsts.Roles.MOBILE_APP_USER,
                    Description = "User for mobile application access",
                    IsSystemRole = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },

            };

            foreach (var role in roles)
            {
                if (!await context.Roles.AnyAsync(r => r.Id == role.Id))
                {
                    context.Roles.Add(role);
                }
            }

            // Seed permissions
            var permissions = new List<Permission>
            {
                // Warehouse permissions
                //new Permission
                //{
                //    Id = Guid.Parse("00000000-0000-0000-0001-000000000001"),
                //    Name = "Warehouse.AccessAll",
                //    Description = "Access all warehouses",
                //    Module = "Warehouse",
                //    CreatedAt = DateTime.UtcNow,
                //    CreatedBy = "System"
                //},
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0001-000000000001"),
                    Name = "Warehouse.Read",
                    Description = "View warehouse data",
                    Module = "Warehouse",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0001-000000000002"),
                    Name = "Warehouse.Write",
                    Description = "Create and update warehouses",
                    Module = "Warehouse",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0001-000000000003"),
                    Name = "Warehouse.Delete",
                    Description = "Delete warehouses",
                    Module = "Warehouse",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
    
                // Client permissions
                //new Permission
                //{
                //    Id = Guid.Parse("00000000-0000-0000-0002-000000000001"),
                //    Name = "Client.AccessAll",
                //    Description = "Access all clients",
                //    Module = "Client",
                //    CreatedAt = DateTime.UtcNow,
                //    CreatedBy = "System"
                //},
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0002-000000000001"),
                    Name = "Client.Read",
                    Description = "View client data",
                    Module = "Client",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0002-000000000002"),
                    Name = "Client.Write",
                    Description = "Create and update clients",
                    Module = "Client",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0002-000000000003"),
                    Name = "Client.Delete",
                    Description = "Delete clients",
                    Module = "Client",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
    
                // User management permissions
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0003-000000000001"),
                    Name = "User.Read",
                    Description = "View users",
                    Module = "User",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0003-000000000002"),
                    Name = "User.Write",
                    Description = "Create and update users",
                    Module = "User",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0003-000000000003"),
                    Name = "User.Delete",
                    Description = "Delete users",
                    Module = "User",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0004-000000000001"),
                    Name = "Role.Read",
                    Description = "View Roles",
                    Module = "Role",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0004-000000000002"),
                    Name = "Role.Write",
                    Description = "Create and update Roles",
                    Module = "Role",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0004-000000000003"),
                    Name = "Role.Delete",
                    Description = "Delete Roles",
                    Module = "Role",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
    
                // Zone Module 
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0005-000000000001"),
                    Name = "Zone.Read",
                    Description = "View Zones",
                    Module = "Zone",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0005-000000000002"),
                    Name = "Zone.Write",
                    Description = "Create and update Zones",
                    Module = "Zone",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0005-000000000003"),
                    Name = "Zone.Delete",
                    Description = "Delete Zones",
                    Module = "Zone",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },

                // Location Module
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0006-000000000001"),
                    Name = "Location.Read",
                    Description = "View Locations",
                    Module = "Location",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0006-000000000002"),
                    Name = "Location.Write",
                    Description = "Create and update Locations",
                    Module = "Location",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0006-000000000003"),
                    Name = "Location.Delete",
                    Description = "Delete Locations",
                    Module = "Location",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },

                // Product Module
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0007-000000000001"),
                    Name = "Product.Read",
                    Description = "View Products",
                    Module = "Product",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0007-000000000002"),
                    Name = "Product.Write",
                    Description = "Create and update Products",
                    Module = "Product",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0007-000000000003"),
                    Name = "Product.Delete",
                    Description = "Delete Products",
                    Module = "Product",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                //General code type and general code
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0008-000000000001"),
                    Name = "GeneralCodeType.Read",
                    Description = "View GeneralCodeTypes",
                    Module = "GeneralCodeType",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0008-000000000002"),
                    Name = "GeneralCodeType.Write",
                    Description = "Create and update GeneralCodeTypes",
                    Module = "GeneralCodeType",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0008-000000000003"),
                    Name = "GeneralCodeType.Delete",
                    Description = "Delete GeneralCodeTypes",
                    Module = "GeneralCodeType",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0009-000000000001"),
                    Name = "GeneralCode.Read",
                    Description = "View GeneralCodes",
                    Module = "GeneralCode",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0009-000000000002"),
                    Name = "GeneralCode.Write",
                    Description = "Create and update GeneralCodes",
                    Module = "GeneralCode",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.Parse("00000000-0000-0000-0009-000000000003"),
                    Name = "GeneralCode.Delete",
                    Description = "Delete GeneralCodes",
                    Module = "GeneralCode",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                 new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = "RawMaterial.Read",
                    Description = "View Raw Material",
                    Module = "RawMaterial",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = "RawMaterial.Write",
                    Description = "Create and update Raw Material",
                    Module = "RawMaterial",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = "RawMaterial.Delete",
                    Description = "Delete Raw Material",
                    Module = "RawMaterial",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = "FinishedGoods.Read",
                    Description = "View Finished Goods",
                    Module = "FinishedGoods",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = "FinishedGoods.Write",
                    Description = "Create and update Finished Goods",
                    Module = "FinishedGoods",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = "FinishedGoods.Delete",
                    Description = "Delete Finished Goods",
                    Module = "FinishedGoods",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = "Container.Read",
                    Description = "View Container",
                    Module = "Container",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = "Container.Write",
                    Description = "Create and update Container",
                    Module = "Container",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = "Container.Delete",
                    Description = "Delete Container",
                    Module = "Container",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new Permission
                {
                    Id = Guid.NewGuid(),
                    Name = "LocationGridDashboard.Read",
                    Description = "View Location Grid Dashboard",
                    Module = "LocationGridDashboard",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                // Inventory permissions (commented out for now)
                // new Permission
                // {
                //     Id = Guid.Parse("00000000-0000-0000-0004-000000000001"),
                //     Name = "Inventory.Read",
                //     Description = "View inventory",
                //     Module = "Inventory",
                //     CreatedAt = DateTime.UtcNow,
                //     CreatedBy = "System"
                // },
                // new Permission
                // {
                //     Id = Guid.Parse("00000000-0000-0000-0004-000000000002"),
                //     Name = "Inventory.Write",
                //     Description = "Create and update inventory",
                //     Module = "Inventory",
                //     CreatedAt = DateTime.UtcNow,
                //     CreatedBy = "System"
                // },
                // new Permission
                // {
                //     Id = Guid.Parse("00000000-0000-0000-0004-000000000003"),
                //     Name = "Inventory.Delete",
                //     Description = "Delete inventory",
                //     Module = "Inventory",
                //     CreatedAt = DateTime.UtcNow,
                //     CreatedBy = "System"
                // },
    
                // Order permissions (commented out for now)
                // new Permission
                // {
                //     Id = Guid.Parse("00000000-0000-0000-0005-000000000001"),
                //     Name = "Orders.Read",
                //     Description = "View orders",
                //     Module = "Orders",
                //     CreatedAt = DateTime.UtcNow,
                //     CreatedBy = "System"
                // },
                // new Permission
                // {
                //     Id = Guid.Parse("00000000-0000-0000-0005-000000000002"),
                //     Name = "Orders.Write",
                //     Description = "Create and update orders",
                //     Module = "Orders",
                //     CreatedAt = DateTime.UtcNow,
                //     CreatedBy = "System"
                // },
                // new Permission
                // {
                //     Id = Guid.Parse("00000000-0000-0000-0005-000000000003"),
                //     Name = "Orders.Delete",
                //     Description = "Delete orders",
                //     Module = "Orders",
                //     CreatedAt = DateTime.UtcNow,
                //     CreatedBy = "System"
                // }
            };

            foreach (var permission in permissions)
            {
                var existsByName = await context.Permissions.AnyAsync(p => p.Name == permission.Name);
                var existsById = await context.Permissions.AnyAsync(p => p.Id == permission.Id);

                if (!existsByName && !existsById)
                {
                    context.Permissions.Add(permission);
                }
            }

            await context.SaveChangesAsync();

            // Seed role permissions
            var rolePermissions = new List<RolePermission>
            {
                // SystemAdmin - all permissions
                // Warehouse permissions
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // SystemAdmin
                    PermissionId = Guid.Parse("00000000-0000-0000-0001-000000000001"), // Warehouse.Read
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // SystemAdmin
                    PermissionId = Guid.Parse("00000000-0000-0000-0001-000000000002"), // Warehouse.Write
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // SystemAdmin
                    PermissionId = Guid.Parse("00000000-0000-0000-0001-000000000003"), // Warehouse.Delete
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
    
                // Client permissions
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // SystemAdmin
                    PermissionId = Guid.Parse("00000000-0000-0000-0002-000000000001"), // Client.Read
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // SystemAdmin
                    PermissionId = Guid.Parse("00000000-0000-0000-0002-000000000002"), // Client.Write
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // SystemAdmin
                    PermissionId = Guid.Parse("00000000-0000-0000-0002-000000000003"), // Client.Delete
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
    
                // User permissions
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // SystemAdmin
                    PermissionId = Guid.Parse("00000000-0000-0000-0003-000000000001"), // Users.Read
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // SystemAdmin
                    PermissionId = Guid.Parse("00000000-0000-0000-0003-000000000002"), // Users.Write
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // SystemAdmin
                    PermissionId = Guid.Parse("00000000-0000-0000-0003-000000000003"), // Users.Delete
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
    
                // WarehouseManager - warehouse management, client read, user read
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000002"), // WarehouseManager
                    PermissionId = Guid.Parse("00000000-0000-0000-0001-000000000002"), // Warehouse.Read
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000002"), // WarehouseManager
                    PermissionId = Guid.Parse("00000000-0000-0000-0001-000000000003"), // Warehouse.Write
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000002"), // WarehouseManager
                    PermissionId = Guid.Parse("00000000-0000-0000-0002-000000000002"), // Client.Read
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000002"), // WarehouseManager
                    PermissionId = Guid.Parse("00000000-0000-0000-0002-000000000003"), // Client.Write
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000002"), // WarehouseManager
                    PermissionId = Guid.Parse("00000000-0000-0000-0003-000000000001"), // Users.Read
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
    
                // WarehouseUser - basic read permissions
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000003"), // WarehouseUser
                    PermissionId = Guid.Parse("00000000-0000-0000-0001-000000000002"), // Warehouse.Read
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = Guid.Parse("00000000-0000-0000-0000-000000000003"), // WarehouseUser
                    PermissionId = Guid.Parse("00000000-0000-0000-0002-000000000002"), // Client.Read
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                },
    
                // ClientUser - limited permissions (if exists)
                // new RolePermission
                // {
                //     Id = Guid.NewGuid(),
                //     RoleId = Guid.Parse("00000000-0000-0000-0000-000000000004"), // ClientUser
                //     PermissionId = Guid.Parse("00000000-0000-0000-0002-000000000002"), // Client.Read
                //     CreatedAt = DateTime.UtcNow,
                //     CreatedBy = "System"
                // }
    
                // Future inventory permissions (when uncommented)
                // new RolePermission
                // {
                //     Id = Guid.NewGuid(),
                //     RoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // SystemAdmin
                //     PermissionId = Guid.Parse("00000000-0000-0000-0004-000000000001"), // Inventory.Read
                //     CreatedAt = DateTime.UtcNow,
                //     CreatedBy = "System"
                // },
                // ...
    
                // Future order permissions (when uncommented)
                // new RolePermission
                // {
                //     Id = Guid.NewGuid(),
                //     RoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"), // SystemAdmin
                //     PermissionId = Guid.Parse("00000000-0000-0000-0005-000000000001"), // Orders.Read
                //     CreatedAt = DateTime.UtcNow,
                //     CreatedBy = "System"
                // },
                // ...
            };
            foreach (var rolePermission in rolePermissions)
            {
                if (!await context.RolePermissions.AnyAsync(rp =>
                    rp.RoleId == rolePermission.RoleId &&
                    rp.PermissionId == rolePermission.PermissionId))
                {
                    context.RolePermissions.Add(rolePermission);
                }
            }

            await context.SaveChangesAsync();

            ////Warehouse (only uncomment when 1st migrate to have at least 1 Warehouse)
            //var warehouseNew = new Warehouse
            //{
            //    Id = Guid.NewGuid(),
            //    Name = "HSC 20 GUL WAY",
            //    Code = "HSCGW01",
            //    Address = "20 GUL WAY",
            //    City = "Singapore",
            //    State = "Singapore",
            //    Country = "Singapore",
            //    ZipCode = "629196",
            //    ContactPerson = "",
            //    ContactEmail = "",
            //    ContactPhone = "",
            //    IsActive = true,
            //    CreatedAt = DateTime.UtcNow,
            //    CreatedBy = "System",
            //    IsDeleted = false

            //};

            //if (!await context.Warehouses.AnyAsync(wh => wh.Code == warehouseNew.Code))
            //{
            //    context.Warehouses.Add(warehouseNew);

            //}
            //await context.SaveChangesAsync();

            // Seed default admin user
            const string adminUsername = "Admin";
            if (!await context.Users.AnyAsync(u => u.Username == adminUsername))
            {
                // Get the first warehouse
                var warehouse = await context.Warehouses.FirstOrDefaultAsync();
                if (warehouse != null)
                {
                    var adminUser = new User
                    {
                        Id = Guid.NewGuid(),
                        Username = adminUsername,
                        Email = "admin@hsc.sg",
                        FirstName = "System",
                        LastName = "Administrator",
                        WarehouseId = warehouse.Id,
                        IsActive = true,
                        EmailConfirmed = true,
                        LockoutEnabled = false,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    };

                    adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "protectMe!");
                    adminUser.SecurityStamp = Guid.NewGuid().ToString();

                    context.Users.Add(adminUser);

                    // Assign SystemAdmin role
                    context.UserRoles.Add(new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = adminUser.Id,
                        RoleId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    });

                    await context.SaveChangesAsync();
                }
            }
        }
    }
}