using Microsoft.AspNetCore.Mvc;
using WMS.Domain.DTOs.GIV_Container;
using WMS.Domain.DTOs.GIV_FG_ReceiveItem;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;
using WMS.Domain.Interfaces;

namespace WMS.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GIV_RM_ReceivePalletController : Controller
    {
        private IReceivePalletService ReceivePalletService { get; }
        private readonly ILogger<GIV_RM_ReceivePalletController> _logger;
        public GIV_RM_ReceivePalletController(ILogger<GIV_RM_ReceivePalletController> logger, IReceivePalletService ReceivePalletService)
        {
            this._logger = logger;
            this.ReceivePalletService = ReceivePalletService;
        }
        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> Create(ReceivePalletCreateDto ReceivePalletDto, string Username, Guid warehouseid)
        {
            var id = await ReceivePalletService.CreateReceivePalletAsync(ReceivePalletDto, Username, warehouseid);

            return Ok(id);
        }
        [HttpPost]
        [Route("UpdateLocation")]
        public async Task<IActionResult> UpdateLocation(UpdatePalletLocationDto UpdatePalletLocationDto, string Username, Guid warehouseid)
        {
            var id = await ReceivePalletService.UpdateLocationPalletAsync(UpdatePalletLocationDto, Username,warehouseid);

            return Ok(id);
        }
    }
}
