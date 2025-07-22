namespace WMS.Domain.DTOs.Warehouses
{
    public class WarehouseListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Code { get; set; } = default!;
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactEmail { get; set; }
        public bool IsActive { get; set; }
        public int ClientCount { get; set; }
        public int ZoneCount { get; set; }
        public int LocationCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = default!;
        // this two for decide whether show and hide edit and delete button
        public bool HasWriteAccess { get; set; }
        public bool HasDeleteAccess { get; set; }
    }
}
