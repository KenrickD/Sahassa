using System.ComponentModel.DataAnnotations;

namespace WMS.WebApp.Models.Clients
{
    public class ClientViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Client name is required")]
        [StringLength(250, ErrorMessage = "Client name cannot exceed 250 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Client code cannot exceed 100 characters")]
        public string? Code { get; set; }

        [StringLength(100, ErrorMessage = "Contact person cannot exceed 100 characters")]
        public string? ContactPerson { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Contact email cannot exceed 100 characters")]
        public string? ContactEmail { get; set; }

        [StringLength(100, ErrorMessage = "Contact phone cannot exceed 100 characters")]
        public string? ContactPhone { get; set; }

        [StringLength(500, ErrorMessage = "Billing address cannot exceed 500 characters")]
        public string? BillingAddress { get; set; }

        [StringLength(500, ErrorMessage = "Shipping address cannot exceed 500 characters")]
        public string? ShippingAddress { get; set; }

        public bool IsActive { get; set; } = true;

        public Guid WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        // Configuration properties
        public bool RequiresQualityCheck { get; set; }
        public bool AutoGenerateReceivingLabels { get; set; }
        public bool AutoGenerateShippingLabels { get; set; }

        [Range(0, 100, ErrorMessage = "Handling fee percentage must be between 0 and 100")]
        public decimal HandlingFeePercentage { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Storage fee must be a positive value")]
        public decimal StorageFeePerCubicMeter { get; set; }

        [Range(1, 365, ErrorMessage = "Default lead time must be between 1 and 365 days")]
        public int DefaultLeadTimeDays { get; set; } = 7;

        [Range(0, int.MaxValue, ErrorMessage = "Low stock threshold must be a positive value")]
        public int LowStockThreshold { get; set; }

        public bool SendLowStockAlerts { get; set; }
        public bool AllowPartialShipments { get; set; }
        public bool RequiresAppointmentForReceiving { get; set; }
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }

        // Display properties
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
