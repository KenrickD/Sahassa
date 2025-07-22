using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Domain.DTOs.GIV_RawMaterial.Web
{
    public class JobReleaseInventoryDto
    {
        public Guid Id { get; set; }
        public string MaterialNo { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TotalBalanceQty { get; set; }
        public int TotalBalancePallet { get; set; }
        public List<JobReleaseReceiveDto> Receives { get; set; } = new List<JobReleaseReceiveDto>();
    }

    public class JobReleaseReceiveDto
    {
        public Guid Id { get; set; }
        public string? BatchNo { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string? ReceivedBy { get; set; }
        public List<JobReleasePalletDto> Pallets { get; set; } = new List<JobReleasePalletDto>();
    }

    public class JobReleasePalletDto
    {
        public Guid Id { get; set; }
        public string PalletCode { get; set; } = string.Empty;
        public bool IsReleased { get; set; }
        public string? HandledBy { get; set; }
        public List<JobReleaseItemDto> Items { get; set; } = new List<JobReleaseItemDto>();
    }

    public class JobReleaseItemDto
    {
        public Guid Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public bool IsReleased { get; set; }
    }
}
