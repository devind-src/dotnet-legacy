namespace SyncNet.Models.Common
{
    class BusinessDateModel
    {
        public string CalendarName { get; set; }
        public string TimeCutover { get; set; }
        public string CurrentBusinessDate { get; set; }
        public string PreviousBusinessDate { get; set; }

        public string Sun { get; set; }
        public string Mon { get; set; }
        public string Tue { get; set; }
        public string Wed { get; set; }
        public string Thu { get; set; }
        public string Fri { get; set; }
        public string Sat { get; set; }
    }
}
