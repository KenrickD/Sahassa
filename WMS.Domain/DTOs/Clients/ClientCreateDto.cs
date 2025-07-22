using System;
using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.DTOs.Clients
{
    public class ClientCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? BillingAddress { get; set; }
        public string? ShippingAddress { get; set; }
        public bool IsActive { get; set; } = true;
        public Guid WarehouseId { get; set; }

        // Configuration properties
        public bool RequiresQualityCheck { get; set; }
        public bool AutoGenerateReceivingLabels { get; set; }
        public bool AutoGenerateShippingLabels { get; set; }
        public decimal HandlingFeePercentage { get; set; }
        public decimal StorageFeePerCubicMeter { get; set; }
        public int DefaultLeadTimeDays { get; set; } = 7;
        public int LowStockThreshold { get; set; }
        public bool SendLowStockAlerts { get; set; }
        public bool AllowPartialShipments { get; set; }
        public bool RequiresAppointmentForReceiving { get; set; }
    }
}