namespace WMS.Domain.DTOs.Zones
{
    public class ZoneDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int LocationCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        // this two for decide whether show and hide edit and delete button
        public bool HasWriteAccess { get; set; }
        public bool HasDeleteAccess { get; set; }
    }
}
