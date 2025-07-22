using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMS.Domain.Models
{
    [Table("TB_JobTask")]
    public class JobTask : TenantEntity
    {
        public Guid? OrderId { get; set; }
        public virtual Order? Order { get; set; }
        public Guid? LocationId { get; set; }
        public virtual Location? Location { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public virtual User? AssignedToUser { get; set; }
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = default!;
        [MaxLength(500)]
        public string? Description { get; set; }
        public TaskType Type { get; set; }
        public TaskPriority Priority { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime DueDate { get; set; }
    }

    public enum TaskType
    {
        Receiving,
        Putaway,
        Picking,
        Packing,
        Shipping,
        CycleCount,
        Replenishment,
        Cleanup,
        Maintenance
    }

    public enum TaskPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    public enum TaskStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled,
        OnHold
    }
}
