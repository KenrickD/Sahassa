namespace WMS.Domain.DTOs
{
    public static class AppConsts
    {
        public static class Roles
        {
            public const string SYSTEM_ADMIN = "SystemAdmin";
            public const string WAREHOUSE_GM = "WarehouseGeneralManager";
            public const string WAREHOUSE_MANAGER = "WarehouseManager";
            public const string WAREHOUSE_USER = "WarehouseUser";
            public const string CLIENT_USER = "ClientUser";
            public const string MOBILE_APP_USER = "MobileAppUser";
        }

        public static class Permissions
        {
            // User
            public const string USER_ACCESS_ALL = "User.AccessAll";
            public const string USER_READ = "User.Read";
            public const string USER_WRITE = "User.Write";
            public const string USER_DELETE = "User.Delete";

            // Role
            public const string ROLE_ACCESS_ALL = "Role.AccessAll";
            public const string ROLE_READ = "Role.Read";
            public const string ROLE_WRITE = "Role.Write";
            public const string ROLE_DELETE = "Role.Delete";

            // Client
            public const string CLIENT_ACCESS_ALL = "Client.AccessAll";
            public const string CLIENT_READ = "Client.Read";
            public const string CLIENT_WRITE = "Client.Write";
            public const string CLIENT_DELETE = "Client.Delete";

            // Warehouse
            public const string WAREHOUSE_ACCESS_ALL = "Warehouse.AccessAll";
            public const string WAREHOUSE_READ = "Warehouse.Read";
            public const string WAREHOUSE_WRITE = "Warehouse.Write";
            public const string WAREHOUSE_DELETE = "Warehouse.Delete";

            // Zone
            public const string ZONE_ACCESS_ALL = "Zone.AccessAll";
            public const string ZONE_READ = "Zone.Read";
            public const string ZONE_WRITE = "Zone.Write";
            public const string ZONE_DELETE = "Zone.Delete";

            // Location
            public const string LOCATION_ACCESS_ALL = "Location.AccessAll";
            public const string LOCATION_READ = "Location.Read";
            public const string LOCATION_WRITE = "Location.Write";
            public const string LOCATION_DELETE = "Location.Delete";

            // Product
            public const string PRODUCT_ACCESS_ALL = "Product.AccessAll";
            public const string PRODUCT_READ = "Product.Read";
            public const string PRODUCT_WRITE = "Product.Write";
            public const string PRODUCT_DELETE = "Product.Delete";

            // GENERAL_CODE
            public const string GENERAL_TYPE_CODE_READ = "GeneralCodeType.Read";
            public const string GENERAL_TYPE_CODE_WRITE = "GeneralCodeType.Write";
            public const string GENERAL_TYPE_CODE_DELETE = "GeneralCodeType.Delete";

            public const string GENERAL_CODE_READ = "GeneralCode.Read";
            public const string GENERAL_CODE_WRITE = "GeneralCode.Write";
            public const string GENERAL_CODE_DELETE = "GeneralCode.Delete";

            //Raw Material
            public const string RAW_MATERIAL_ACCESS_ALL = "RawMaterial.AccessAll";
            public const string RAW_MATERIAL_READ = "RawMaterial.Read";
            public const string RAWMATERIAL_WRITE = "RawMaterial.Write";
            public const string RAW_MATERIAL_DELETE = "RawMaterial.Delete";

            //Finished Goods
            public const string FINISHED_GOODS_ACCESS_ALL = "FinishedGoods.AccessAll";
            public const string FINISHED_GOODS_READ = "FinishedGoods.Read";
            public const string FINISHED_GOODS_WRITE = "FinishedGoods.Write";
            public const string FINISHED_GOODS_DELETE = "FinishedGoods.Delete";

            // Container
            public const string CONTAINER_READ = "Container.Read";
            public const string CONTAINER_WRITE = "Container.Write";
            public const string CONTAINER_DELETE = "Container.Delete";

            // Location Grid Dashboard
            public const string LOCATION_GRID_READ = "LocationGridDashboard.Read";
        }

        public static class GeneralCodeType
        {
            public const string PRODUCT_TYPE = "PRODUCT_TYPE";
            public const string PRODUCT_CATEGORY = "PRODUCT_CATEGORY";
            public const string PRODUCT_UOM = "PRODUCT_UOM";
            public const string CONTAINER_STATUS = "CONTAINER_STATUS";

        }

        public static class MemoryCacheKey
        {
            public const string ADDRESS_LOOKUP = "ADDRESS_LOOKUP";
            public const string PORT_LOOKUP = "PORT_LOOKUP";
            public const string VESSEL_INFO = "VESSEL_INFO";
        }

        public static class LocationGridStatus
        {
            public const string AVAILABLE = "Available";
            public const string PARTIAL = "Partial";
            public const string OCCUPIED = "Occupied";
            public const string RESERVED = "Reserved";
            public const string MAINTENANCE = "Maintenance";
            public const string BLOCKED = "Blocked";
        }

        public static class ZoneName
        {
            public const string RACKING = "RACKING";
            public const string QUEUE = "QUEUE";
            public const string REFRIGERATED = "REFRIGERATED";
        }
        public static class ContainerStatusCode
        {
            public const string New = "NEW";
            public const string Unstuffed = "USTF";
            public const string Completed = "CMP";
            public const string Closed = "CLD";
            public const string Cancelled = "CAN";
        }

        public static class GivaudanJobTye
        {
            public const string IMPORT = "JobImport";
            public const string IMPORT_NONHSC = "JobImportNonHSC";
            public const string IMPORT_CEVA = "JobImportCEVA";
            public const string EXPORT_FCL = "JobFCL";
            public const string EXPORT_FCL_WL = "JobFCLWL";
            public const string EXPORT_FCL_RDC = "JobFCLRDC";
        }
    }
}
