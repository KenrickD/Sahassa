namespace WMS.WebApp.Models.Clients
{
    public class ClientListViewModel
    {
        public List<ClientItemViewModel> Clients { get; set; } = new List<ClientItemViewModel>();
        public int TotalCount { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
    }
}
