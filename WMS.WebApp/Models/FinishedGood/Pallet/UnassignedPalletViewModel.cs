namespace WMS.WebApp.Models.FinishedGood.Pallet
{
    public class UnassignedPalletViewModel
    {
        public List<PalletDto> UnassignedPallets { get; set; } = new();
        public List<SkuDto> Skus { get; set; } = new();
    }

}
