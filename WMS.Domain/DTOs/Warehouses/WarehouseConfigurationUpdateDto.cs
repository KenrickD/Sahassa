using System.ComponentModel.DataAnnotations;
using WMS.Domain.Models;

namespace WMS.Domain.DTOs.Warehouses
{
    public class WarehouseConfigurationUpdateDto
    {
        public bool RequiresLotTracking { get; set; }
        public bool RequiresExpirationDates { get; set; }
        public bool UsesSerialNumbers { get; set; }
        public bool AutoAssignLocations { get; set; }
        public InventoryStrategy InventoryStrategy { get; set; }

        [MaxLength(100)]
        public string? DefaultMeasurementUnit { get; set; }

        [Range(1, 365, ErrorMessage = "Default days to expiry must be between 1 and 365")]
        public int DefaultDaysToExpiry { get; set; }

        [MaxLength(100)]
        public string? BarcodeFormat { get; set; }

        [MaxLength(500)]
        public string? CompanyLogoUrl { get; set; }

        [MaxLength(100)]
        public string? ThemePrimaryColor { get; set; }

        [MaxLength(100)]
        public string? ThemeSecondaryColor { get; set; }
    }
}