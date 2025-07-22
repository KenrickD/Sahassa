namespace WMS.Domain.DTOs.Users
{
    public class UserListDto
    {
        public List<UserDto> Items { get; set; } = new List<UserDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
