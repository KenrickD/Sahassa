using Microsoft.AspNetCore.Mvc;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs.GIV_Container;
using WMS.Domain.DTOs.GIV_RM_Receive;
using WMS.Domain.DTOs.RawMaterial;
using WMS.Domain.DTOs.Receiving;
using WMS.Domain.DTOs.Users;
using WMS.Domain.Interfaces;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WMS.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GIV_RawMaterialController : Controller
    {
        private IRawMaterialService RawMaterialService { get; }
        private readonly ILogger<GIV_RawMaterialController> _logger;
        public GIV_RawMaterialController(ILogger<GIV_RawMaterialController> logger, IRawMaterialService RawMaterialService)
        {
            this._logger = logger;
            this.RawMaterialService = RawMaterialService;
        }
        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> Create(RawMaterialReceivingDto receivingdto,string Username,Guid warehouseid)
        {
            var id = await RawMaterialService.CreateRawMaterialAsync(receivingdto,Username,warehouseid);

            return Ok(id);
        }
    }
}
