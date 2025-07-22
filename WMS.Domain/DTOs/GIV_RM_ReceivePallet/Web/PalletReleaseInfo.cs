using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RM_ReceivePallet.Web
{
    public class PalletReleaseInfo
    {
        public Guid PalletId { get; set; }
        public string ReleaseDate { get; set; } = string.Empty;
        public bool ReleaseEntirePallet { get; set; } = true;
    }
}
