using SyncNet.Library;
using SyncNet.Networking;
using System;

namespace SyncNet.Helpers
{
    public static class TcpHelper
    {
        public static byte[] AddTcpHeader(string src, SdkTcpHeader TCPHeader)
        {
            return AddTcpHeader(NbConvert.StringToBytes(src), TCPHeader, true);
        }
        public static byte[] AddTcpHeader(byte[] src, SdkTcpHeader TCPHeader, bool HiLo = true)
        {
            string strLen;
            byte[] dst;

            int lenHdr = 2;
            int lenMsg = src.Length;
            int maxSend = 256;

            switch (TCPHeader)
            {
                case SdkTcpHeader.Tcp2ByteAscExcludeHeader:
                case SdkTcpHeader.Tcp2ByteBcdExcludeHeader:
                    lenMsg = src.Length;
                    lenHdr = 2;
                    break;
                case SdkTcpHeader.Tcp2ByteAscIncludeHeader:
                case SdkTcpHeader.Tcp2ByteBcdIncludeHeader:
                    lenMsg = src.Length + 2;
                    lenHdr = 2;
                    break;
                case SdkTcpHeader.Tcp4ByteExcludeHeader:
                    lenMsg = src.Length;
                    lenHdr = 4;
                    break;
                case SdkTcpHeader.Tcp4ByteIncludeHeader:
                    lenMsg = src.Length + 4;
                    lenHdr = 4;
                    break;
                default:
                    lenHdr = 2;
                    break;
            }

            dst = new byte[src.Length + lenHdr];

            //copy data
            Array.Copy(src, 0, dst, lenHdr, src.Length);

            //set tcp header
            if (TCPHeader == SdkTcpHeader.Tcp2ByteAscExcludeHeader ||
                TCPHeader == SdkTcpHeader.Tcp2ByteAscIncludeHeader)
            {
                //tcp header 2 bytes asc
                if (HiLo == true)
                {
                    if (lenMsg >= maxSend)
                    {
                        dst[0] = (byte)(lenMsg / maxSend);
                        dst[1] = (byte)(lenMsg % maxSend);
                    }
                    else
                    {
                        dst[0] = 0;
                        dst[1] = (byte)lenMsg;
                    }
                }
                else
                {
                    if (lenMsg >= maxSend)
                    {
                        dst[1] = (byte)(lenMsg / maxSend);
                        dst[0] = (byte)(lenMsg % maxSend);
                    }
                    else
                    {
                        dst[1] = 0;
                        dst[0] = (byte)lenMsg;
                    }
                }
            }
            else if (TCPHeader == SdkTcpHeader.Tcp2ByteBcdExcludeHeader ||
                TCPHeader == SdkTcpHeader.Tcp2ByteBcdIncludeHeader)
            {
                strLen = lenMsg.ToString().PadLeft(4, '0');

                //tcp header 2 bytes bcd
                if (HiLo == true)
                {
                    dst[0] = Convert.ToByte(strLen.Substring(0, 2), 16);
                    dst[1] = Convert.ToByte(strLen.Substring(2, 2), 16);
                }
                else
                {
                    dst[1] = Convert.ToByte(strLen.Substring(0, 2), 16);
                    dst[0] = Convert.ToByte(strLen.Substring(2, 2), 16);
                }
            }
            else if (TCPHeader == SdkTcpHeader.Tcp4ByteExcludeHeader ||
                TCPHeader == SdkTcpHeader.Tcp4ByteIncludeHeader)
            {
                strLen = lenMsg.ToString().PadLeft(4, '0');
                byte[] hdr = NbConvert.StringToBytes(strLen);

                //copy header to array dst
                Array.Copy(hdr, 0, dst, 0, hdr.Length);
            }
            else
            {
                return src;
            }

            return dst;
        }

        public static byte[] RemoveTCPHeader(byte[] payLoad, int lengthHeader)
        {
            byte[] dst = new byte[payLoad.Length - lengthHeader];

            Array.Copy(payLoad, lengthHeader, dst, 0, payLoad.Length - lengthHeader);

            return dst;
        }

    }
}
