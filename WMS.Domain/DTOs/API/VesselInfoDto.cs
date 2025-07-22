
using System.Globalization;

namespace WMS.Domain.DTOs.API
{
    public class VesselInfoDto
    {
        public int VesselID { get; set; }
        public string? VesselFullName { get; set; }
        public string? VesselName { get; set; }
        public string? InVoy { get; set; }
        public string? OutVoy { get; set; }
        public string? ETA { get; set; }
        public string? ETD { get; set; }
        public string? Berth { get; set; }
        public string? COD { get; set; }

        public DateTime? ETADate
        {
            get
            {
                if (ETA == null || ETA == string.Empty) return null;
                else
                {
                    return DateTime.ParseExact(ETA, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                }
            }
        }
        public DateTime? ETDDate
        {
            get
            {
                if (ETD == null || ETD == string.Empty) return null;
                else
                {
                    return DateTime.ParseExact(ETD, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                }
            }
        }
        public DateTime? CODDate
        {
            get
            {
                if (COD == null || COD == string.Empty) return null;
                else
                {
                    return DateTime.ParseExact(COD, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
                }
            }
        }
        public string? VesselCombined
        {
            get
            {
                return $"{this.VesselFullName} | {this.ETA} | {this.InVoy} | {this.OutVoy} | {this.Berth}";
            }
        }
    }
}
