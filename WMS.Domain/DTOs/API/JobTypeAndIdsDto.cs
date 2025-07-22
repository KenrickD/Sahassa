
namespace WMS.Domain.DTOs.API
{
    public class JobTypeAndIdsDto
    {
        public string JobType { get; set; } = string.Empty;
        public List<int> JobIds { get; set; } = new List<int>();
    }
}
