using WMS.WebApp.Models.Users;

namespace WMS.WebApp.Models.Roles
{
    public class RoleListViewModel
    {
        public List<RoleItemViewModel> Roles { get; set; } = new();
        //public RoleFilterViewModel Filters { get; set; } = new();
        public PaginationViewModel Pagination { get; set; } = new();
        public string Search { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
