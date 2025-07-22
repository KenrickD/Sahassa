namespace WMS.WebApp.Models.Clients
{
    public class ClientItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? ContactPerson { get; set; }
        public string? ContactEmail { get; set; }
        public bool IsActive { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
