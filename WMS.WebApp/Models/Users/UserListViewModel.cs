namespace WMS.WebApp.Models.Users
{
    // Main view model for user listing page
    public class UserListViewModel
    {
        public List<UserItemViewModel> Users { get; set; } = new();
        public UserFilterViewModel Filters { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
        public string Search { get; set; }
        public Guid? WarehouseId { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}