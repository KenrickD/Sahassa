using Microsoft.AspNetCore.Mvc;
using WMS.Domain.DTOs.Receiving;
using WMS.Domain.Interfaces;

namespace WMS.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GIV_FinishedGoodController : Controller
    {
        private IFinishedGoodService FinishedGoodService { get; }
        private readonly ILogger<GIV_FinishedGoodController> _logger;
        public GIV_FinishedGoodController(ILogger<GIV_FinishedGoodController> logger, IFinishedGoodService FinishedGoodService)
        {
            this._logger = logger;
            this.FinishedGoodService = FinishedGoodService;
        }
        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> Create(FinishedGoodReceivingDto receivingdto, string Username, Guid warehouseid)
        {
            var id = await FinishedGoodService.CreateFinishedGoodAsync(receivingdto, Username, warehouseid);

            return Ok(id);
        }
    }
}
