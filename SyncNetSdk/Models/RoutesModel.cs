namespace SyncNet.Models
{
    public class RoutesModel
    {
        public class RoutesBySource
        {
            public int NodeIdSource { get; set; }
            public string NodeNameSource { get; set; }
            public int NodeIdDest { get; set; }
            public string NodeNameDest { get; set; }
            public string ProductId { get; set; }
            public string Notes { get; set; }
        }
        public class RoutesByBin
        {
            public int GroupId { get; set; }
            public string GroupName { get; set; }
            public int NodeId { get; set; }
            public string NodeName { get; set; }
        }
        public class RoutesByProduct
        {
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public int NodeId { get; set; }
            public string NodeName { get; set; }
            public string Notes { get; set; }
            public int? FeeSharing { get; set; }
            public int LbWeight { get; set; }
        }
        public class RoutesByProductAlt
        {
            public string ProductId { get; set; }
            public int NodeId { get; set; }
            public string NodeName { get; set; }
            public int Priority { get; set; }
            public int? FeeSharing { get; set; }
            public int LbWeight { get; set; }
        }
        public class RoutesMargin
        {
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public string Notes { get; set; }
            public string RoutingMode { get; set; }
            public string StaticNodeName { get; set; }
        }
    }
}
