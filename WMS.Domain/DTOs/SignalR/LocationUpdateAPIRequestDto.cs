namespace WMS.Domain.DTOs.SignalR
{
    public class LocationUpdateAPIRequestDto
    {
        public Guid ZoneId { get; set; }
        public List<string> LocationCodes { get; set; } = new List<string>();
    }
}
