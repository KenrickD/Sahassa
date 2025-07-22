using System;
using System.ComponentModel.DataAnnotations;
using WMS.Domain.Models;

namespace WMS.Domain.DTOs.Warehouses
{
    public class WarehouseConfigurationDto
    {
        public Guid Id { get; set; }
        public Guid WarehouseId { get; set; }

        public bool RequiresLotTracking { get; set; }
        public bool RequiresExpirationDates { get; set; }
        public bool UsesSerialNumbers { get; set; }
        public bool AutoAssignLocations { get; set; }
        public InventoryStrategy InventoryStrategy { get; set; }

        [MaxLength(100)]
        public string? DefaultMeasurementUnit { get; set; }

        public int DefaultDaysToExpiry { get; set; }

        [MaxLength(100)]
        public string? BarcodeFormat { get; set; }

        [MaxLength(500)]
        public string? CompanyLogoUrl { get; set; }

        [MaxLength(100)]
        public string? ThemePrimaryColor { get; set; }

        [MaxLength(100)]
        public string? ThemeSecondaryColor { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = default!;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}