
namespace WMS.Domain.DTOs.API
{
    public class PortDto
    {
        public string? Region { get; set; }
        public string? Country { get; set; }
        public string? PortName { get; set; }
        public string? PortCode { get; set; }
        public bool? AMS { get; set; }
        public string DisplayPort
        {
            get
            {
                if (this.PortCode == string.Empty && this.PortName == string.Empty)
                    return "-";
                else if (PortCode != string.Empty && this.PortName != string.Empty)
                    return this.PortName! + " - " + this.PortCode;
                else
                    return this.PortCode == null ? this.PortName! : this.PortCode!;
            }
        }
    }
}
