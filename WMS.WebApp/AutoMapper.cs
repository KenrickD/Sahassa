using AutoMapper;
using WMS.Domain.DTOs.Clients;
using WMS.Domain.DTOs.GeneralCodes;
using WMS.Domain.DTOs.Locations;
using WMS.Domain.DTOs.Products;
using WMS.Domain.DTOs.Roles;
using WMS.Domain.DTOs.Users;
using WMS.Domain.DTOs.Warehouses;
using WMS.Domain.DTOs.Zones;
using WMS.Domain.Models;
using WMS.WebApp.Models.Locations;
using WMS.WebApp.Models.Products;
using WMS.WebApp.Models.Users;
using WMS.WebApp.Models.Zones;

namespace WMS.Application
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Domain to DTO
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Roles, opt => opt.MapFrom(src =>
                    src.UserRoles != null
                        ? src.UserRoles.Select(ur => ur.Role.Name).ToList()
                        : new List<string>()))
                .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.Client.Name))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse.Name))
                .ForMember(dest => dest.ProfileImageUrl, opt => opt.Ignore());

            // DTO to ViewModel
            CreateMap<UserDto, UserItemViewModel>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src =>
                    $"{src.FirstName} {src.LastName}".Trim()));

            // Add these to your existing AutoMapper profile class
            CreateMap<Client, ClientDto>()
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse.Name));
            CreateMap<ClientCreateDto, Client>();
            CreateMap<ClientUpdateDto, Client>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<ClientConfiguration, ClientConfigurationDto>();
            CreateMap<ClientConfigurationUpdateDto, ClientConfiguration>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Warehouse, WarehouseDto>()
                .ForMember(dest => dest.ClientCount, opt => opt.MapFrom(src => src.Clients.Count))
                .ForMember(dest => dest.ZoneCount, opt => opt.MapFrom(src => src.Zones.Count));
            CreateMap<WarehouseCreateDto, Warehouse>();
            CreateMap<WarehouseUpdateDto, Warehouse>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<WarehouseConfiguration, WarehouseConfigurationDto>();
            CreateMap<WarehouseConfigurationUpdateDto, WarehouseConfiguration>();
                //.ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<Role, RoleDto>()
                .ForMember(dest => dest.UserCount, opt => opt.MapFrom(src => src.UserRoles != null ? src.UserRoles.Count : 0))
                .ForMember(dest => dest.PermissionCount, opt => opt.MapFrom(src => src.RolePermissions != null ? src.RolePermissions.Count : 0));
            CreateMap<RoleCreateDto, Role>();
            CreateMap<RoleUpdateDto, Role>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<Permission, PermissionDto>();

            // Zone mappings
            CreateMap<Zone, ZoneDto>()
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))
                .ForMember(dest => dest.LocationCount, opt => opt.Ignore()); // This will be set manually in the service

            CreateMap<ZoneCreateDto, Zone>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore())
                .ForMember(dest => dest.Locations, opt => opt.Ignore());

            CreateMap<ZoneUpdateDto, Zone>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.WarehouseId, opt => opt.Ignore()) // Warehouse cannot be changed after creation
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore())
                .ForMember(dest => dest.Locations, opt => opt.Ignore());

            // Zone ViewModel mappings
            CreateMap<ZoneDto, ZoneViewModel>()
                .ReverseMap();

            CreateMap<ZoneDto, ZoneItemViewModel>();

            // Add these mappings to your AutoMapper profile (usually in AutoMapper.cs or a mapping profile)

            // Location mappings
            CreateMap<Location, LocationDto>()
                .ForMember(dest => dest.ZoneName, opt => opt.MapFrom(src => src.Zone != null ? src.Zone.Name : string.Empty))
                .ForMember(dest => dest.WarehouseId, opt => opt.MapFrom(src => src.Zone != null ? src.Zone.WarehouseId : Guid.Empty))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Zone != null && src.Zone.Warehouse != null ? src.Zone.Warehouse.Name : string.Empty))
                .ForMember(dest => dest.InventoryCount, opt => opt.Ignore()) // This will be set manually in the service
                .ForMember(dest => dest.CurrentUtilization, opt => opt.Ignore()); // This will be calculated in the service

            CreateMap<LocationCreateDto, Location>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.IsEmpty, opt => opt.Ignore()) // Will be set to true by default
                .ForMember(dest => dest.FullLocationCode, opt => opt.Ignore()) // Will be generated
                .ForMember(dest => dest.Zone, opt => opt.Ignore())
                .ForMember(dest => dest.Inventories, opt => opt.Ignore());

            CreateMap<LocationUpdateDto, Location>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ZoneId, opt => opt.Ignore()) // Zone cannot be changed after creation
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.IsEmpty, opt => opt.Ignore()) // Will be managed by inventory operations
                .ForMember(dest => dest.FullLocationCode, opt => opt.Ignore()) // Will be regenerated
                .ForMember(dest => dest.Zone, opt => opt.Ignore())
                .ForMember(dest => dest.Inventories, opt => opt.Ignore());

            // Location ViewModel mappings
            CreateMap<LocationDto, LocationViewModel>()
                .ReverseMap()
                .ForMember(dest => dest.ZoneName, opt => opt.Ignore())
                .ForMember(dest => dest.WarehouseName, opt => opt.Ignore())
                .ForMember(dest => dest.InventoryCount, opt => opt.Ignore())
                .ForMember(dest => dest.CurrentUtilization, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore());

            CreateMap<LocationDto, LocationItemViewModel>()
                .ForMember(dest => dest.TypeDisplay, opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.AccessTypeDisplay, opt => opt.MapFrom(src => src.AccessType.ToString()))
                .ForMember(dest => dest.StatusDisplay, opt => opt.MapFrom(src => src.IsActive ? "Active" : "Inactive"))
                .ForMember(dest => dest.AvailabilityDisplay, opt => opt.MapFrom(src => src.IsEmpty ? "Empty" : "Occupied"))
                .ForMember(dest => dest.PositionDisplay, opt => opt.Ignore()); // Will be calculated in view model

            CreateMap<LocationViewModel, LocationUpdateDto>();

            // GeneralCodeType Entity to DTO mappings
            CreateMap<GeneralCodeType, GeneralCodeTypeDto>()
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))
                .ForMember(dest => dest.CodesCount, opt => opt.Ignore()) // Will be set manually in service
                .ForMember(dest => dest.HasWriteAccess, opt => opt.Ignore()) // Will be set manually in controller
                .ForMember(dest => dest.HasDeleteAccess, opt => opt.Ignore()); // Will be set manually in controller

            // Create DTOs to Entity mappings
            CreateMap<GeneralCodeTypeCreateDto, GeneralCodeType>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Will be set manually
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Will be set manually
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Will be set manually
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore()) // Not needed for create
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore()) // Not needed for create
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false)) // Default value
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore()); // Navigation property

            // Update DTOs to Entity mappings
            CreateMap<GeneralCodeTypeUpdateDto, GeneralCodeType>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Preserve existing
                .ForMember(dest => dest.WarehouseId, opt => opt.Ignore()) // Cannot change warehouse
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Preserve existing
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Preserve existing
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore()) // Will be set manually
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore()) // Will be set manually
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore()) // Preserve existing
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore()); // Navigation property

            // GeneralCode Entity to DTO mappings
            CreateMap<GeneralCode, GeneralCodeDto>()
                .ForMember(dest => dest.GeneralCodeTypeName, opt => opt.MapFrom(src => src.GeneralCodeType != null ? src.GeneralCodeType.Name : string.Empty))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))
                .ForMember(dest => dest.HasWriteAccess, opt => opt.Ignore()) // Will be set manually in controller
                .ForMember(dest => dest.HasDeleteAccess, opt => opt.Ignore()); // Will be set manually in controller

            // Create DTOs to Entity mappings
            CreateMap<GeneralCodeCreateDto, GeneralCode>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Will be set manually
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Will be set manually
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Will be set manually
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore()) // Not needed for create
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore()) // Not needed for create
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false)) // Default value
                .ForMember(dest => dest.GeneralCodeType, opt => opt.Ignore()) // Navigation property
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore()); // Navigation property

            // Update DTOs to Entity mappings
            CreateMap<GeneralCodeUpdateDto, GeneralCode>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Preserve existing
                .ForMember(dest => dest.GeneralCodeTypeId, opt => opt.Ignore()) // Cannot change code type
                .ForMember(dest => dest.WarehouseId, opt => opt.Ignore()) // Cannot change warehouse
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // Preserve existing
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Preserve existing
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore()) // Will be set manually
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore()) // Will be set manually
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore()) // Preserve existing
                .ForMember(dest => dest.GeneralCodeType, opt => opt.Ignore()) // Navigation property
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore()); // Navigation property

            // Special mapping for hierarchical display
            CreateMap<GeneralCodeTypeDto, GeneralCodeWithTypeDto>()
                .ForMember(dest => dest.CodeType, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Codes, opt => opt.Ignore()) // Will be set manually
                .ForMember(dest => dest.IsExpanded, opt => opt.MapFrom(src => false)); // Default collapsed

            // Product Mapping
            // Product Entity to ProductDto
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.Client != null ? src.Client.Name : string.Empty))
                .ForMember(dest => dest.ClientCode, opt => opt.MapFrom(src => src.Client != null ? src.Client.Code : string.Empty))
                .ForMember(dest => dest.WarehouseId, opt => opt.MapFrom(src => src.WarehouseId))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))
                .ForMember(dest => dest.ProductTypeName, opt => opt.MapFrom(src => src.ProductType != null ? src.ProductType.Name : string.Empty))
                .ForMember(dest => dest.ProductCategoryName, opt => opt.MapFrom(src => src.ProductCategory != null ? src.ProductCategory.Name : string.Empty))
                .ForMember(dest => dest.UnitOfMeasureCodeName, opt => opt.MapFrom(src => src.UnitOfMeasureCode != null ? src.UnitOfMeasureCode.Name : string.Empty))
                .ForMember(dest => dest.UnitOfMeasureCodeId, opt => opt.MapFrom(src => src.UnitOfMeasureCodeId))
                .ForMember(dest => dest.InventoryCount, opt => opt.MapFrom(src => src.Inventories != null ? src.Inventories.Count : 0))
                .ForMember(dest => dest.CurrentStockLevel, opt => opt.MapFrom(src =>
                    src.Inventories != null ? src.Inventories.Where(i => !i.IsDeleted).Sum(i => i.Quantity) : 0))
                .ForMember(dest => dest.HasWriteAccess, opt => opt.Ignore())
                .ForMember(dest => dest.HasDeleteAccess, opt => opt.Ignore());

            // ProductCreateDto to Product Entity
            CreateMap<ProductCreateDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()) // Will be set by service after image upload
                .ForMember(dest => dest.Client, opt => opt.Ignore())
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore())
                .ForMember(dest => dest.Inventories, opt => opt.Ignore())
                .ForMember(dest => dest.WarehouseId, opt => opt.Ignore()) // Will be set by service based on ClientId
                .ForMember(dest => dest.ProductType, opt => opt.Ignore()) // Navigation property - will be loaded by EF
                .ForMember(dest => dest.ProductCategory, opt => opt.Ignore()) // Navigation property - will be loaded by EF
                .ForMember(dest => dest.UnitOfMeasureCode, opt => opt.Ignore()) // Navigation property - will be loaded by EF
                .ForMember(dest => dest.UnitOfMeasureCodeId, opt => opt.MapFrom(src => src.UnitOfMeasureCodeId));

            // ProductUpdateDto to Product Entity (for updating existing entity)
            CreateMap<ProductUpdateDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ClientId, opt => opt.Ignore()) // Client cannot be changed after creation
                .ForMember(dest => dest.WarehouseId, opt => opt.Ignore()) // Warehouse cannot be changed after creation
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore()) // Will be set by service
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore()) // Will be set by service
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()) // Will be handled by service
                .ForMember(dest => dest.Client, opt => opt.Ignore())
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore())
                .ForMember(dest => dest.Inventories, opt => opt.Ignore())
                .ForMember(dest => dest.ProductType, opt => opt.Ignore()) // Navigation property - will be loaded by EF
                .ForMember(dest => dest.ProductCategory, opt => opt.Ignore()) // Navigation property - will be loaded by EF
                .ForMember(dest => dest.UnitOfMeasureCode, opt => opt.Ignore()) // Navigation property - will be loaded by EF
                .ForMember(dest => dest.UnitOfMeasureCodeId, opt => opt.MapFrom(src => src.UnitOfMeasureCodeId));

            // ProductDto to ProductViewModel
            CreateMap<ProductDto, ProductViewModel>()
                .ForMember(dest => dest.ProductImage, opt => opt.Ignore())
                .ForMember(dest => dest.RemoveProductImage, opt => opt.Ignore());

            // ProductViewModel to ProductCreateDto
            CreateMap<ProductViewModel, ProductCreateDto>()
                .ForMember(dest => dest.ProductImage, opt => opt.MapFrom(src => src.ProductImage));

            // ProductViewModel to ProductUpdateDto
            CreateMap<ProductViewModel, ProductUpdateDto>()
                .ForMember(dest => dest.ProductImage, opt => opt.MapFrom(src => src.ProductImage))
                .ForMember(dest => dest.RemoveProductImage, opt => opt.MapFrom(src => src.RemoveProductImage));

            // ProductDto to ProductItemViewModel
            CreateMap<ProductDto, ProductItemViewModel>()
                .ForMember(dest => dest.StatusDisplay, opt => opt.Ignore()) // Computed property
                .ForMember(dest => dest.StockStatusDisplay, opt => opt.Ignore()) // Computed property
                .ForMember(dest => dest.StockStatusColor, opt => opt.Ignore()); // Computed property

            // Product Entity to ProductItemViewModel (direct mapping for lists)
            CreateMap<Product, ProductItemViewModel>()
                .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.Client != null ? src.Client.Name : string.Empty))
                .ForMember(dest => dest.InventoryCount, opt => opt.MapFrom(src => src.Inventories != null ? src.Inventories.Count : 0))
                .ForMember(dest => dest.CurrentStockLevel, opt => opt.MapFrom(src =>
                    src.Inventories != null ? src.Inventories.Where(i => !i.IsDeleted).Sum(i => i.Quantity) : 0))
                .ForMember(dest => dest.StatusDisplay, opt => opt.Ignore()) // Computed property
                .ForMember(dest => dest.StockStatusDisplay, opt => opt.Ignore()) // Computed property
                .ForMember(dest => dest.StockStatusColor, opt => opt.Ignore()); // Computed property

            // Product Entity to ProductViewModel (for edit scenarios)
            CreateMap<Product, ProductViewModel>()
                .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.Client != null ? src.Client.Name : string.Empty))
                .ForMember(dest => dest.ProductTypeName, opt => opt.MapFrom(src => src.ProductType != null ? src.ProductType.Name : string.Empty))
                .ForMember(dest => dest.ProductCategoryName, opt => opt.MapFrom(src => src.ProductCategory != null ? src.ProductCategory.Name : string.Empty))
                .ForMember(dest => dest.UnitOfMeasureCodeName, opt => opt.MapFrom(src => src.UnitOfMeasureCode != null ? src.UnitOfMeasureCode.Name : string.Empty))
                .ForMember(dest => dest.UnitOfMeasureCodeId, opt => opt.MapFrom(src => src.UnitOfMeasureCodeId))
                .ForMember(dest => dest.InventoryCount, opt => opt.MapFrom(src => src.Inventories != null ? src.Inventories.Count : 0))
                .ForMember(dest => dest.CurrentStockLevel, opt => opt.MapFrom(src =>
                    src.Inventories != null ? src.Inventories.Where(i => !i.IsDeleted).Sum(i => i.Quantity) : 0))
                .ForMember(dest => dest.ProductImage, opt => opt.Ignore())
                .ForMember(dest => dest.RemoveProductImage, opt => opt.Ignore());

            // Reverse mappings for completeness
            CreateMap<ProductViewModel, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.Client, opt => opt.Ignore())
                .ForMember(dest => dest.Warehouse, opt => opt.Ignore())
                .ForMember(dest => dest.Inventories, opt => opt.Ignore())
                .ForMember(dest => dest.WarehouseId, opt => opt.Ignore()) // Will be set by service
                .ForMember(dest => dest.ProductType, opt => opt.Ignore()) // Navigation property
                .ForMember(dest => dest.ProductCategory, opt => opt.Ignore()) // Navigation property
                .ForMember(dest => dest.UnitOfMeasureCode, opt => opt.Ignore()) // Navigation property
                .ForMember(dest => dest.UnitOfMeasureCodeId, opt => opt.MapFrom(src => src.UnitOfMeasureCodeId));
        }
    }
}
