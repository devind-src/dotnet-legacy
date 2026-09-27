namespace ApiBiller.Models
{
    internal class TerminalInfo
    {
        public string owner { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }

        public TerminalInfo()
        {
            state = "IDN";
            country = "IN";
        }
    }
}
