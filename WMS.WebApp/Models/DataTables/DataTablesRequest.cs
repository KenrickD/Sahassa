namespace WMS.WebApp.Models.DataTables
{
    public class DataTablesRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public DataTablesSearch Search { get; set; }
        public DataTablesOrder[] Order { get; set; }
        public DataTablesColumn[] Columns { get; set; }
    }
}
