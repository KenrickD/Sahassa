namespace WMS.WebApp.Models.LocationGrids
{
    public class LocationDetailsViewModel
    {
        public Guid Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public string Row { get; set; } = string.Empty;
        public int Bay { get; set; }
        public int Level { get; set; }
        public bool IsEmpty { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public decimal MaxWeight { get; set; }
        public decimal MaxVolume { get; set; }
        public int MaxItems { get; set; }
        public decimal CurrentWeight { get; set; }
        public decimal CurrentVolume { get; set; }
        public int CurrentItems { get; set; }
        public List<InventoryItemViewModel> Inventories { get; set; } = new List<InventoryItemViewModel>();
    }
}
