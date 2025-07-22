
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_ClientConfiguration")]
    public class ClientConfiguration : BaseEntity
    {
        public Guid ClientId { get; set; }
        public virtual Client Client { get; set; } = null!;
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
