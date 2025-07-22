
namespace WMS.Domain.DTOs.GeneralCodes
{
    public class GeneralCodeTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int CodesCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }

        // Permission flags for UI
        public bool HasWriteAccess { get; set; }
        public bool HasDeleteAccess { get; set; }
    }
}
