
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_Order")]
    public class Order : TenantEntity
    {
        [Required]
        [MaxLength(100)]
        public string OrderNumber { get; set; } = default!;
        public Guid ClientId { get; set; }
        public virtual Client Client { get; set; } = null!;
        public virtual ICollection<OrderLine> OrderLines { get; set; } = null!;
        public OrderType Type { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        [MaxLength(100)]
        public string CustomerName { get; set; } = default!;
        [MaxLength(100)]
        public string? CustomerEmail { get; set; }
        [MaxLength(100)]
        public string? CustomerPhone { get; set; }
        [MaxLength(500)]
        public string? ShippingAddress { get; set; }
        [MaxLength(100)]
        public string? ShippingCity { get; set; }
        [MaxLength(100)]
        public string? ShippingState { get; set; }
        [MaxLength(20)]
        public string? ShippingZipCode { get; set; }
        [MaxLength(100)]
        public string? ShippingCountry { get; set; }
        [MaxLength(20)]
        public string? ShippingMethod { get; set; }
        [MaxLength(100)]
        public string? TrackingNumber { get; set; }
        [MaxLength(500)]
        public string? Remarks { get; set; }
        public decimal TotalWeight { get; set; }
        public bool IsPriority { get; set; }
    }

    public enum OrderType
    {
        Inbound,  // Receiving order
        Outbound  // Shipping order
    }

    public enum OrderStatus
    {
        Draft,
        Confirmed,
        Processing,
        Picking,
        Packing,
        ReadyForShipment,
        Shipped,
        Delivered,
        Cancelled,
        OnHold,
        PartiallyFulfilled
    }
}
