using System.ComponentModel.DataAnnotations.Schema;


namespace WMS.Domain.Models
{
    [Table("TB_FileUpload")]
    public class FileUpload : BaseEntity
    {
        // Navigation property
        public virtual ICollection<FileUploadItem> FileUploadItems { get; set; } = new List<FileUploadItem>();

        // Reverse navigation - other entities can reference this
        //public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }
}
