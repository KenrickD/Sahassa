using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet.Web;

namespace WMS.Domain.DTOs.GIV_FinishedGood
{
    public class FinishedGoodReleaseSubmitDto
    {
        public Guid FinishedGoodId { get; set; }
        // For individual item releases
        public List<Guid> ItemIds { get; set; } = new List<Guid>();
        public Dictionary<string, string> ItemReleaseDates { get; set; } = new Dictionary<string, string>();

        // For pallet releases
        public List<PalletReleaseInfo> PalletReleases { get; set; } = new List<PalletReleaseInfo>();
    }
}
