namespace SyncNet.Networking
{
    public enum TcpHeaderType
    {
        Binary2Byte,
        Bcd2Byte,
        Binary4Byte,
        Ascii4Digit
    }

    public enum TcpLengthMode
    {
        Exclude, // panjang payload saja
        Include  // panjang payload + header
    }

    public enum TcpEndianMode
    {
        BigEndian,
        LittleEndian
    }


    #region Config Middleware
    public enum SdkTcpHeader
    {
        Tcp2ByteBcdExcludeHeader = 0,
        Tcp2ByteBcdIncludeHeader = 1,
        Tcp2ByteAscExcludeHeader = 2, //default
        Tcp2ByteAscIncludeHeader = 3,
        Tcp4ByteExcludeHeader = 4,
        Tcp4ByteIncludeHeader = 5,
        None = 6,
        Custom = 7
    }

    public enum SdkTcpHeaderLengthMode
    {
        ExcludeHeader,
        IncludeHeader
    }

    public enum SdkTcpHeaderFormat
    {
        BCD,
        ASC
    }
    #endregion
}
