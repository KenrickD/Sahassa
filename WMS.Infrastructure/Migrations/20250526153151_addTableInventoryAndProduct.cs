using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addTableInventoryAndProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.CreateTable(
                name: "TB_Permission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_Permission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TB_Role",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TB_Warehouse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_Warehouse", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TB_RolePermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_RolePermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_RolePermission_TB_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "TB_Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_RolePermission_TB_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "TB_Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_AuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChangesJson = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_AuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_AuditLog_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TB_Client",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactPerson = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BillingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShippingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_Client", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_Client_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_AuditLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChangesJson = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangesDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_AuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_AuditLog_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_Container",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerNo_GW = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PlannedDelivery_GW = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_Container", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_Container_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_FinishedGood",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SKU = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_FinishedGood", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FinishedGood_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_RawMaterial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_RawMaterial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RawMaterial_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_WarehouseConfiguration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiresLotTracking = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresExpirationDates = table.Column<bool>(type: "boolean", nullable: false),
                    UsesSerialNumbers = table.Column<bool>(type: "boolean", nullable: false),
                    AutoAssignLocations = table.Column<bool>(type: "boolean", nullable: false),
                    InventoryStrategy = table.Column<int>(type: "integer", nullable: false),
                    DefaultMeasurementUnit = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DefaultDaysToExpiry = table.Column<int>(type: "integer", nullable: false),
                    BarcodeFormat = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CompanyLogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ThemePrimaryColor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ThemeSecondaryColor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_WarehouseConfiguration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_WarehouseConfiguration_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_Zone",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_Zone", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_Zone_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_ClientConfiguration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiresQualityCheck = table.Column<bool>(type: "boolean", nullable: false),
                    AutoGenerateReceivingLabels = table.Column<bool>(type: "boolean", nullable: false),
                    AutoGenerateShippingLabels = table.Column<bool>(type: "boolean", nullable: false),
                    HandlingFeePercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    StorageFeePerCubicMeter = table.Column<decimal>(type: "numeric", nullable: false),
                    DefaultLeadTimeDays = table.Column<int>(type: "integer", nullable: false),
                    LowStockThreshold = table.Column<int>(type: "integer", nullable: false),
                    SendLowStockAlerts = table.Column<bool>(type: "boolean", nullable: false),
                    AllowPartialShipments = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresAppointmentForReceiving = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_ClientConfiguration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_ClientConfiguration_TB_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "TB_Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            */
            migrationBuilder.CreateTable(
                name: "TB_Product",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    SKU = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Barcode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Length = table.Column<decimal>(type: "numeric", nullable: false),
                    Width = table.Column<decimal>(type: "numeric", nullable: false),
                    Height = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RequiresLotTracking = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresExpirationDate = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresSerialNumber = table.Column<bool>(type: "boolean", nullable: false),
                    MinStockLevel = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxStockLevel = table.Column<decimal>(type: "numeric", nullable: false),
                    ReorderPoint = table.Column<decimal>(type: "numeric", nullable: false),
                    ReorderQuantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SubCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsHazardous = table.Column<bool>(type: "boolean", nullable: false),
                    IsFragile = table.Column<bool>(type: "boolean", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_Product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_Product_TB_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "TB_Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_Product_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            /*
            migrationBuilder.CreateTable(
                name: "TB_User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SecurityStamp = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProfileImagePath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_User_TB_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "TB_Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TB_User_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_FG_Receive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeID = table.Column<int>(type: "integer", nullable: false),
                    FinishedGoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchNo = table.Column<string>(type: "text", nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_FG_Receive", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_Receive_TB_GIV_FinishedGood_FinishedGoodId",
                        column: x => x.FinishedGoodId,
                        principalTable: "TB_GIV_FinishedGood",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_Receive_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_RM_Receive",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TypeID = table.Column<int>(type: "integer", nullable: false),
                    RawMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_RM_Receive", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_Receive_TB_GIV_Container_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "TB_GIV_Container",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_Receive_TB_GIV_RawMaterial_RawMaterialId",
                        column: x => x.RawMaterialId,
                        principalTable: "TB_GIV_RawMaterial",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_Receive_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_Location",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsEmpty = table.Column<bool>(type: "boolean", nullable: false),
                    MaxWeight = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxVolume = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxItems = table.Column<int>(type: "integer", nullable: false),
                    Barcode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Length = table.Column<decimal>(type: "numeric", nullable: false),
                    Width = table.Column<decimal>(type: "numeric", nullable: false),
                    Height = table.Column<decimal>(type: "numeric", nullable: false),
                    Row = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Bay = table.Column<int>(type: "integer", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: true),
                    Aisle = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Side = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Bin = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    PickingPriority = table.Column<int>(type: "integer", nullable: true),
                    TemperatureZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AccessType = table.Column<int>(type: "integer", nullable: false),
                    FullLocationCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    XCoordinate = table.Column<decimal>(type: "numeric", nullable: true),
                    YCoordinate = table.Column<decimal>(type: "numeric", nullable: true),
                    ZCoordinate = table.Column<decimal>(type: "numeric", nullable: true),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_Location", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_Location_TB_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "TB_Client",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_Location_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_Location_TB_Zone_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "TB_Zone",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_RefreshToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "text", nullable: true),
                    ReasonRevoked = table.Column<string>(type: "text", nullable: true),
                    CreatedByIp = table.Column<string>(type: "text", nullable: true),
                    RevokedByIp = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_RefreshToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_RefreshToken_TB_User_UserId",
                        column: x => x.UserId,
                        principalTable: "TB_User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_UserClaim",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ClaimValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_UserClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_UserClaim_TB_User_UserId",
                        column: x => x.UserId,
                        principalTable: "TB_User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_UserPermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_UserPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_UserPermission_TB_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "TB_Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_UserPermission_TB_User_UserId",
                        column: x => x.UserId,
                        principalTable: "TB_User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_UserRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_UserRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_UserRole_TB_Role_RoleId",
                        column: x => x.RoleId,
                        principalTable: "TB_Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_UserRole_TB_User_UserId",
                        column: x => x.UserId,
                        principalTable: "TB_User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_FG_ReceivePallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FinishedGoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    PalletCode = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    HandledBy = table.Column<string>(type: "text", nullable: true),
                    StoredBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_FG_ReceivePallet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceivePallet_TB_GIV_FinishedGood_FinishedGoodId",
                        column: x => x.FinishedGoodId,
                        principalTable: "TB_GIV_FinishedGood",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceivePallet_TB_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceivePallet_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_InventoryMovement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FG_ID = table.Column<Guid>(type: "uuid", nullable: false),
                    FinishedGoodId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PerformedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MovementDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_InventoryMovement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_InventoryMovement_TB_GIV_FinishedGood_FinishedGoodId",
                        column: x => x.FinishedGoodId,
                        principalTable: "TB_GIV_FinishedGood",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_InventoryMovement_TB_Location_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_GIV_InventoryMovement_TB_Location_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_GIV_InventoryMovement_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_RM_ReceivePallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_RM_ReceiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    PalletCode = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    HandledBy = table.Column<string>(type: "text", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoredBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_RM_ReceivePallet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceivePallet_TB_GIV_RM_Receive_GIV_RM_ReceiveId",
                        column: x => x.GIV_RM_ReceiveId,
                        principalTable: "TB_GIV_RM_Receive",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceivePallet_TB_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceivePallet_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            */
            migrationBuilder.CreateTable(
                name: "TB_Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PONumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CostPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_Inventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_Inventory_TB_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "TB_Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_Inventory_TB_Location_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_Inventory_TB_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "TB_Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_Inventory_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            /*
            migrationBuilder.CreateTable(
                name: "TB_GIV_FG_ReceivePalletPhoto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivePalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhotoPath = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_FG_ReceivePalletPhoto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceivePalletPhoto_TB_GIV_FG_ReceivePallet_Receiv~",
                        column: x => x.ReceivePalletId,
                        principalTable: "TB_GIV_FG_ReceivePallet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_FG_ReceivePalletPhoto_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_RM_ReceivePalletItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_RM_ReceivePalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_RM_ReceivePalletItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceivePalletItem_TB_GIV_RM_ReceivePallet_GIV_RM_~",
                        column: x => x.GIV_RM_ReceivePalletId,
                        principalTable: "TB_GIV_RM_ReceivePallet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceivePalletItem_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_GIV_RM_ReceivePalletPhoto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GIV_RM_ReceivePalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhotoName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhotoPath = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_GIV_RM_ReceivePalletPhoto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceivePalletPhoto_TB_GIV_RM_ReceivePallet_GIV_RM~",
                        column: x => x.GIV_RM_ReceivePalletId,
                        principalTable: "TB_GIV_RM_ReceivePallet",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_GIV_RM_ReceivePalletPhoto_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            */
            migrationBuilder.CreateTable(
                name: "TB_InventoryMovement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PerformedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MovementDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Remarks = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_InventoryMovement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TB_InventoryMovement_TB_Inventory_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "TB_Inventory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TB_InventoryMovement_TB_Location_FromLocationId",
                        column: x => x.FromLocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_InventoryMovement_TB_Location_ToLocationId",
                        column: x => x.ToLocationId,
                        principalTable: "TB_Location",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TB_InventoryMovement_TB_Warehouse_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "TB_Warehouse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
            /*
            migrationBuilder.CreateIndex(
                name: "IX_TB_AuditLog_WarehouseId",
                table: "TB_AuditLog",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_Client_WarehouseId",
                table: "TB_Client",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_ClientConfiguration_ClientId",
                table: "TB_ClientConfiguration",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_AuditLog_WarehouseId",
                table: "TB_GIV_AuditLog",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_Container_WarehouseId",
                table: "TB_GIV_Container",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_Receive_FinishedGoodId",
                table: "TB_GIV_FG_Receive",
                column: "FinishedGoodId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_Receive_WarehouseId",
                table: "TB_GIV_FG_Receive",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceivePallet_FinishedGoodId",
                table: "TB_GIV_FG_ReceivePallet",
                column: "FinishedGoodId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceivePallet_LocationId",
                table: "TB_GIV_FG_ReceivePallet",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceivePallet_WarehouseId",
                table: "TB_GIV_FG_ReceivePallet",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceivePalletPhoto_ReceivePalletId",
                table: "TB_GIV_FG_ReceivePalletPhoto",
                column: "ReceivePalletId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FG_ReceivePalletPhoto_WarehouseId",
                table: "TB_GIV_FG_ReceivePalletPhoto",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_FinishedGood_WarehouseId",
                table: "TB_GIV_FinishedGood",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_InventoryMovement_FinishedGoodId",
                table: "TB_GIV_InventoryMovement",
                column: "FinishedGoodId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_InventoryMovement_FromLocationId",
                table: "TB_GIV_InventoryMovement",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_InventoryMovement_ToLocationId",
                table: "TB_GIV_InventoryMovement",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_InventoryMovement_WarehouseId",
                table: "TB_GIV_InventoryMovement",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RawMaterial_WarehouseId",
                table: "TB_GIV_RawMaterial",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_Receive_ContainerId",
                table: "TB_GIV_RM_Receive",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_Receive_RawMaterialId",
                table: "TB_GIV_RM_Receive",
                column: "RawMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_Receive_WarehouseId",
                table: "TB_GIV_RM_Receive",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceivePallet_GIV_RM_ReceiveId",
                table: "TB_GIV_RM_ReceivePallet",
                column: "GIV_RM_ReceiveId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceivePallet_LocationId",
                table: "TB_GIV_RM_ReceivePallet",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceivePallet_WarehouseId",
                table: "TB_GIV_RM_ReceivePallet",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceivePalletItem_GIV_RM_ReceivePalletId",
                table: "TB_GIV_RM_ReceivePalletItem",
                column: "GIV_RM_ReceivePalletId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceivePalletItem_WarehouseId",
                table: "TB_GIV_RM_ReceivePalletItem",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceivePalletPhoto_GIV_RM_ReceivePalletId",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                column: "GIV_RM_ReceivePalletId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_GIV_RM_ReceivePalletPhoto_WarehouseId",
                table: "TB_GIV_RM_ReceivePalletPhoto",
                column: "WarehouseId");
            */
            migrationBuilder.CreateIndex(
                name: "IX_TB_Inventory_ClientId",
                table: "TB_Inventory",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_Inventory_LocationId",
                table: "TB_Inventory",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_Inventory_ProductId",
                table: "TB_Inventory",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_Inventory_WarehouseId",
                table: "TB_Inventory",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_InventoryMovement_FromLocationId",
                table: "TB_InventoryMovement",
                column: "FromLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_InventoryMovement_InventoryId",
                table: "TB_InventoryMovement",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_InventoryMovement_ToLocationId",
                table: "TB_InventoryMovement",
                column: "ToLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_InventoryMovement_WarehouseId",
                table: "TB_InventoryMovement",
                column: "WarehouseId");
            /*
            migrationBuilder.CreateIndex(
                name: "IX_TB_Location_ClientId",
                table: "TB_Location",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_Location_WarehouseId",
                table: "TB_Location",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_Location_ZoneId",
                table: "TB_Location",
                column: "ZoneId");
            */
            migrationBuilder.CreateIndex(
                name: "IX_TB_Product_ClientId",
                table: "TB_Product",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_Product_WarehouseId",
                table: "TB_Product",
                column: "WarehouseId");
            /*
            migrationBuilder.CreateIndex(
                name: "IX_TB_RefreshToken_Token",
                table: "TB_RefreshToken",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_RefreshToken_UserId",
                table: "TB_RefreshToken",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_RefreshToken_UserId_ExpiresAt",
                table: "TB_RefreshToken",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TB_RolePermission_PermissionId",
                table: "TB_RolePermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_RolePermission_RoleId",
                table: "TB_RolePermission",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_User_ClientId",
                table: "TB_User",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_User_Email",
                table: "TB_User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_User_Username",
                table: "TB_User",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_User_WarehouseId_IsActive",
                table: "TB_User",
                columns: new[] { "WarehouseId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TB_UserClaim_UserId",
                table: "TB_UserClaim",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_UserClaim_UserId_ClaimType",
                table: "TB_UserClaim",
                columns: new[] { "UserId", "ClaimType" });

            migrationBuilder.CreateIndex(
                name: "IX_TB_UserPermission_PermissionId",
                table: "TB_UserPermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_UserPermission_UserId",
                table: "TB_UserPermission",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_UserRole_RoleId",
                table: "TB_UserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_UserRole_UserId",
                table: "TB_UserRole",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TB_WarehouseConfiguration_WarehouseId",
                table: "TB_WarehouseConfiguration",
                column: "WarehouseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TB_Zone_WarehouseId",
                table: "TB_Zone",
                column: "WarehouseId");
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.DropTable(
                name: "TB_AuditLog");

            migrationBuilder.DropTable(
                name: "TB_ClientConfiguration");

            migrationBuilder.DropTable(
                name: "TB_GIV_AuditLog");

            migrationBuilder.DropTable(
                name: "TB_GIV_FG_Receive");

            migrationBuilder.DropTable(
                name: "TB_GIV_FG_ReceivePalletPhoto");

            migrationBuilder.DropTable(
                name: "TB_GIV_InventoryMovement");

            migrationBuilder.DropTable(
                name: "TB_GIV_RM_ReceivePalletItem");

            migrationBuilder.DropTable(
                name: "TB_GIV_RM_ReceivePalletPhoto");
            */
            migrationBuilder.DropTable(
                name: "TB_InventoryMovement");
            /*
            migrationBuilder.DropTable(
                name: "TB_RefreshToken");

            migrationBuilder.DropTable(
                name: "TB_RolePermission");

            migrationBuilder.DropTable(
                name: "TB_UserClaim");

            migrationBuilder.DropTable(
                name: "TB_UserPermission");

            migrationBuilder.DropTable(
                name: "TB_UserRole");

            migrationBuilder.DropTable(
                name: "TB_WarehouseConfiguration");

            migrationBuilder.DropTable(
                name: "TB_GIV_FG_ReceivePallet");

            migrationBuilder.DropTable(
                name: "TB_GIV_RM_ReceivePallet");
            */
            migrationBuilder.DropTable(
                name: "TB_Inventory");
            /*
            migrationBuilder.DropTable(
                name: "TB_Permission");

            migrationBuilder.DropTable(
                name: "TB_Role");

            migrationBuilder.DropTable(
                name: "TB_User");

            migrationBuilder.DropTable(
                name: "TB_GIV_FinishedGood");

            migrationBuilder.DropTable(
                name: "TB_GIV_RM_Receive");

            migrationBuilder.DropTable(
                name: "TB_Location");
            */
            migrationBuilder.DropTable(
                name: "TB_Product");
            /*
            migrationBuilder.DropTable(
                name: "TB_GIV_Container");

            migrationBuilder.DropTable(
                name: "TB_GIV_RawMaterial");

            migrationBuilder.DropTable(
                name: "TB_Zone");

            migrationBuilder.DropTable(
                name: "TB_Client");

            migrationBuilder.DropTable(
                name: "TB_Warehouse");
            */
        }
    }
}
