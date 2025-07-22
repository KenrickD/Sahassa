using System;

namespace WMS.Domain.DTOs.Clients
{
    public class ClientConfigurationUpdateDto
    {
        public bool RequiresQualityCheck { get; set; }
        public bool AutoGenerateReceivingLabels { get; set; }
        public bool AutoGenerateShippingLabels { get; set; }
        public decimal HandlingFeePercentage { get; set; }
        public decimal StorageFeePerCubicMeter { get; set; }
        public int DefaultLeadTimeDays { get; set; }
        public int LowStockThreshold { get; set; }
        public bool SendLowStockAlerts { get; set; }
        public bool AllowPartialShipments { get; set; }
        public bool RequiresAppointmentForReceiving { get; set; }
    }
}