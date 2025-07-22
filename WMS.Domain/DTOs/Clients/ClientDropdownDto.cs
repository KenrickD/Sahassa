namespace WMS.Domain.DTOs.Clients
{
    /// <summary>
    /// Client dropdown item for filter
    /// </summary>
    public class ClientDropdownDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
