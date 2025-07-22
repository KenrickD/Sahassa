namespace WMS.WebApp.Models.LocationGrids
{
    public class InventoryItemViewModel
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSKU { get; set; } = string.Empty;
        public string LotNumber { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string MainHU { get; set; } = string.Empty;
    }
}
