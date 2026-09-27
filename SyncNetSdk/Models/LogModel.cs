using System;

namespace SyncNet.Models
{
    public class LogModel
    {
        public DateTime Datetime { get; set; }
        public string AppName { get; set; }
        public string FileName { get; set; }
        public string LogType { get; set; }
        public string Title { get; set; }
        public object Detail { get; set; }
    }
}
