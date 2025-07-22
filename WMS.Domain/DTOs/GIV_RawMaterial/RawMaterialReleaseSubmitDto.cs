using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet.Web;

namespace WMS.Domain.DTOs.GIV_RawMaterial
{
    public class RawMaterialReleaseSubmitDto
    {
        public Guid RawMaterialId { get; set; }
        public List<Guid> ItemIds { get; set; } = new();
        public List<Guid> ReceiveIds { get; set; } = new();

        public Dictionary<string, string> ItemReleaseDates { get; set; } = new Dictionary<string, string>();

        public List<PalletReleaseInfo> PalletReleases { get; set; } = new List<PalletReleaseInfo>();
    }

}
