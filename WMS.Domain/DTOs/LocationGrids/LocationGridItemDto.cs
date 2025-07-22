using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Domain.DTOs.LocationGrids
{
    public class LocationGridItemDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Row { get; set; } = string.Empty;
        public int Bay { get; set; }
        public int Level { get; set; }
        public bool IsEmpty { get; set; }
        public LocationStatus Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int InventoryCount { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal MaxWeight { get; set; }
        public decimal MaxVolume { get; set; }
        public int MaxItems { get; set; }
    }
}
