using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RM_Receive
{
    public class RM_ReceiveTableRowDto
    {
        public bool HasEditAccess { get; set; }
        public Guid? GroupId { get; set; }
        public string? BatchNumbers { get; set; }
        public Guid Id { get; set; }
        public DateTime ReceivedDate { get; set; }
        public int PackSize { get; set; }
        public int Quantity { get; set; }
        public int TotalPallets { get; set; }
        public int BalanceQuantity { get; set; }
        public int BalancePallets { get; set; }
        public string? ContainerUrl { get; set; }
        public bool ShowGrouped { get; set; }

        public string? BatchNo { get; set; }
        public string PalletCodes { get; set; } = string.Empty;
        public string ItemCodes { get; set; } = string.Empty;

        public int ReceivesInGroup { get; set; } = 1; // Default to 1 if not grouped
        public bool IsGrouped { get; set; }
    }

}
