using Microsoft.AspNetCore.Mvc;
using WMS.Domain.DTOs.GIV_FinishedGood;
using WMS.Domain.DTOs.GIV_RawMaterial;

namespace WMS.WebApp.ViewComponents
{
    public class FinishedGoodDataTableViewComponent:ViewComponent
    {
        public FinishedGoodDataTableViewComponent()
        {

            }


        public async Task<IViewComponentResult> InvokeAsync(List<FinishedGoodDetailsDto> FinishedGoodDetails)
        {
            if (FinishedGoodDetails == null)
            {
                FinishedGoodDetails = new List<FinishedGoodDetailsDto>();
            }

            return await Task.FromResult((IViewComponentResult)View("FinishedGoodDataTable", FinishedGoodDetails));
        }
    }
}
