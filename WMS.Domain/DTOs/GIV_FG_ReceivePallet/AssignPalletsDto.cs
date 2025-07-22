using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_FG_ReceivePallet
{
    public class AssignPalletsDto
    {
        [JsonPropertyName("skuId")]
        public Guid SkuId { get; set; } = default!;
         
        [JsonPropertyName("palletIds")]
        public List<Guid> PalletIds { get; set; } = new();
        public List<Guid> UnassignedReceiveIds { get; set; } = new();
    }


}
