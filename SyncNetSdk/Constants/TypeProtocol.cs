namespace SyncNet.Constants
{
    public class TypeProtocol
    {
        public const byte TCP2ByteExcludeHeader = 0;
        public const byte TCP2ByteIncludeHeader = 1;
        public const byte TCP4ByteExcludeHeader = 2;
        public const byte TCP4ByteIncludeHeader = 3;
        public const byte TCPHeaderCustom = 4;
        public const byte TCPHeaderNone = 5;
        public const byte MessageQueue = 6;
        public const byte WebService = 7;
        public const byte Custom = 8;
    }
}
