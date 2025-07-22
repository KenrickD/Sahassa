using WMS.Domain.Models;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.WebApp.Models.Locations
{
    public class LocationItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public LocationType Type { get; set; }
        public AccessType AccessType { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmpty { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string? FullLocationCode { get; set; }
        public int InventoryCount { get; set; }
        public decimal CurrentUtilization { get; set; }
        public DateTime CreatedAt { get; set; }

        // Display properties
        public string TypeDisplay => Type.ToString();
        public string AccessTypeDisplay => AccessType.ToString();
        public string StatusDisplay => IsActive ? "Active" : "Inactive";
        public string AvailabilityDisplay => IsEmpty ? "Empty" : "Occupied";
        public string PositionDisplay => GetPositionDisplay();

        private string GetPositionDisplay()
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(Row)) parts.Add($"Row {Row}");
            if (Bay.HasValue) parts.Add($"Bay {Bay:00}");
            if (Level.HasValue) parts.Add($"Level {Level}");

            return parts.Any() ? string.Join(", ", parts) : "-";
        }

        public string? Row { get; set; }
        public int? Bay { get; set; }
        public int? Level { get; set; }
    }
}
