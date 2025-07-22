namespace WMS.Domain.Enumerations
{
    public class Enumerations
    {
        public enum LocationStatus
        {
            Available,
            Partial,
            Occupied,
            Reserved,
            Maintenance,
            Blocked
        }
        public enum InventoryStatus
        {
            Available,
            Reserved,
            Damaged,
            Expired,
            InTransit,
            Quarantine,
            Allocated,
            Returned
        }
        public enum MovementType
        {
            Receiving,
            Putaway,
            Picking,
            Packing,
            Shipping,
            Return,
            Adjustment,
            Transfer,
            CycleCount,
            Quarantine,
            Release
        }

        public enum LocationType
        {
            Floor,
            Rack,
            Bin,
            Bulk,
            Staging,
            Dock,
            Overflow
        }

        public enum AccessType
        {
            Manual,      // Manual picking
            Forklift,    // Forklift access required
            Crane,       // Crane access required
            Automated,   // Automated system
            Restricted   // Special access required
        }

        public enum FileType
        {
            Photo = 1,
            Document = 2,
            Archive = 3
        }

        public enum ContainerProcessType
        {
            Import = 0,
            Export = 1
        }
    }
}
