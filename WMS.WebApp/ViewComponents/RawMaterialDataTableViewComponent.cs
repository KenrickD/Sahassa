using Microsoft.AspNetCore.Mvc;
using WMS.Domain.DTOs.GIV_RawMaterial;

namespace WMS.WebApp.ViewComponents
{
    public class RawMaterialDataTableViewComponent : ViewComponent
    {
        public RawMaterialDataTableViewComponent()
        {
            // Constructor logic can be added here if needed
        }
        public async Task<IViewComponentResult> InvokeAsync(List<RawMaterialDetailsDto> RawMaterialDetails)
        {
            if (RawMaterialDetails == null)
            {
                RawMaterialDetails = new List<RawMaterialDetailsDto>();
            }

            return await Task.FromResult((IViewComponentResult)View("RawMaterialDataTable", RawMaterialDetails));
        }
    }
}
