namespace SyncNet.Networking
{
    internal class XTcpListenerSdk : XTcpListener
    {
        // Property config interface
        public SdkTcpHeaderLengthMode HeaderLengthMode { get; set; } = SdkTcpHeaderLengthMode.ExcludeHeader;
        public SdkTcpHeaderFormat HeaderFormat { get; set; } = SdkTcpHeaderFormat.ASC;
        public int HeaderLength { get; set; } = 2;
        public bool HeaderHiLo { get; set; } = true;

        public void SetProtocol()
        {
            // Set default length mode based on TCP header length mode
            if (HeaderLengthMode == SdkTcpHeaderLengthMode.ExcludeHeader)
            {
                DefaultLengthMode = TcpLengthMode.Exclude;
            }
            else
            {
                DefaultLengthMode = TcpLengthMode.Include;
            }

            // Set default endian mode based on TCP header Hi-Lo configuration
            if (HeaderHiLo == true)
            {
                DefaultEndianMode = TcpEndianMode.BigEndian;
            }
            else
            {
                DefaultEndianMode = TcpEndianMode.LittleEndian;
            }

            // Set default TCP header type based on TCP header length and format
            if (HeaderLength == 4 && HeaderFormat == SdkTcpHeaderFormat.ASC)
            {
                DefaultHeaderType = TcpHeaderType.Ascii4Digit;
            }
            else if (HeaderLength == 4 && HeaderFormat == SdkTcpHeaderFormat.BCD)
            {
                DefaultHeaderType = TcpHeaderType.Binary4Byte;
            }
            else if (HeaderLength == 2 && HeaderFormat == SdkTcpHeaderFormat.BCD)
            {
                DefaultHeaderType = TcpHeaderType.Bcd2Byte;
            }
            else
            {
                DefaultHeaderType = TcpHeaderType.Binary2Byte;
            }
        }
    }
}
