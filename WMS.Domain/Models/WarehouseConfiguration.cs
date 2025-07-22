
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_WarehouseConfiguration")]
    public class WarehouseConfiguration : BaseEntity
    {
        public Guid WarehouseId { get; set; }
        public virtual Warehouse Warehouse { get; set; } = null!;
        public bool RequiresLotTracking { get; set; }
        public bool RequiresExpirationDates { get; set; }
        public bool UsesSerialNumbers { get; set; }
        public bool AutoAssignLocations { get; set; }
        public InventoryStrategy InventoryStrategy { get; set; } // FIFO, LIFO, FEFO
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
    }
    public enum InventoryStrategy
    {
        FIFO, // First In First Out
        LIFO, // Last In First Out
        FEFO  // First Expired First Out
    }

}
