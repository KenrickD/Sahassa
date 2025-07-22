
namespace WMS.Domain.DTOs.API
{
    public class AddressDto
    {
        public int AddressID { get; set; }
        public string? PostalCode { get; set; }
        public string? StreetNo { get; set; }
        public string? StreetName { get; set; }
        public string? PlaceName { get; set; }
        public string? LevelNo { get; set; }
        public string? UnitNo { get; set; }
        public string? BlockNo { get; set; }
        public string? BuildingName { get; set; }
        public string? TelephoneNo { get; set; }
        public string? Remarks { get; set; }
        public bool IsRoadSide { get; set; }
        public bool IsForkLift { get; set; }
        public bool IsTailGate { get; set; }
        public bool IsPalletJack { get; set; }
        public bool IsLoadingBay { get; set; }
        public bool IsSmallTruck { get; set; }
        public bool IsPermitEndorse { get; set; }
        public string? AddressType { get; set; }

        public string AddressCombination
        {
            get
            {
                string combineUnitLevel = string.Empty;
                if (this.LevelNo != string.Empty && this.UnitNo != string.Empty)
                    combineUnitLevel = string.Format("#{0}-{1}", this.LevelNo, this.UnitNo);
                else
                    combineUnitLevel = "#" + this.LevelNo != string.Empty ? this.LevelNo : this.UnitNo;

                string address = string.Format("({0}) {1} {2}, {3} {4}, SINGAPORE {5}",
                    this.PlaceName, this.StreetNo, this.StreetName, combineUnitLevel, this.BuildingName, this.PostalCode);

                return address;
            }
        }
    }
}
