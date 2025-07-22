using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.Clients;

namespace WMS.Domain.DTOs.Locations
{
    /// <summary>
    /// Response DTO for available linkable items
    /// </summary>
    public class GetLinkableItemsResponseDto
    {
        public List<LinkableInventoryItemDto> AvailableItems { get; set; } = new();
        public int CurrentLocationItemCount { get; set; }
        public int MaxItems { get; set; }
        public int AvailableCapacity { get; set; }
        public List<ClientDropdownDto> AvailableClients { get; set; } = new();
    }
}
