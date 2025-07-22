using Microsoft.AspNetCore.Mvc;
using WMS.Domain.DTOs.GIV_Container;
using WMS.Domain.DTOs.Receiving;
using WMS.Domain.Interfaces;

namespace WMS.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GIV_ContainerController : Controller
    {
        private IContainerService ContainerService { get; }
        private readonly ILogger<GIV_ContainerController> _logger;
        public GIV_ContainerController(ILogger<GIV_ContainerController> logger, IContainerService ContainerService)
        {
            this._logger = logger;
            this.ContainerService = ContainerService;
        }
        [HttpPost]
        [Route("DeltaUpdate")]
        public async Task<IActionResult> DeltaUpdateContainer(ContainerCreateDto containerDto, string Username, Guid warehouseid, bool WhFlag)
        {
            var id = await ContainerService.DeltaUpdateContainerAsync(containerDto, Username, warehouseid,WhFlag);

            return Ok(id);
        }
    }
}
