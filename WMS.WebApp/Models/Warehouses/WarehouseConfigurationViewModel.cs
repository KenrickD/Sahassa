using System.ComponentModel.DataAnnotations;
using WMS.Domain.Models;

namespace WMS.WebApp.Models.Warehouses
{
    public class WarehouseConfigurationViewModel
    {
        public Guid WarehouseId { get; set; }

        public bool RequiresLotTracking { get; set; }
        public bool RequiresExpirationDates { get; set; }
        public bool UsesSerialNumbers { get; set; }
        public bool AutoAssignLocations { get; set; }
        public InventoryStrategy InventoryStrategy { get; set; } = InventoryStrategy.FIFO;

        [MaxLength(100)]
        public string? DefaultMeasurementUnit { get; set; }

        [Range(1, 365, ErrorMessage = "Default days to expiry must be between 1 and 365")]
        public int DefaultDaysToExpiry { get; set; } = 365;

        [MaxLength(100)]
        public string? BarcodeFormat { get; set; }

        [MaxLength(500)]
        public string? CompanyLogoUrl { get; set; }

        [MaxLength(100)]
        public string? ThemePrimaryColor { get; set; }

        [MaxLength(100)]
        public string? ThemeSecondaryColor { get; set; }

        // Display properties
        public List<InventoryStrategyOption> InventoryStrategyOptions { get; set; } = new()
        {
            new InventoryStrategyOption { Value = InventoryStrategy.FIFO, Text = "First In, First Out (FIFO)" },
            new InventoryStrategyOption { Value = InventoryStrategy.LIFO, Text = "Last In, First Out (LIFO)" },
            new InventoryStrategyOption { Value = InventoryStrategy.FEFO, Text = "First Expired, First Out (FEFO)" }
        };
        public class InventoryStrategyOption
        {
            public InventoryStrategy Value { get; set; }
            public string Text { get; set; } = default!;
        }
    }
}
