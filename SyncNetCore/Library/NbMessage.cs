using System;
using System.Text;

namespace SyncNet.Library
{
    public class NbMessage
    {
        //all protocol
        public enum EnumProtocol
        {
            TCP2ByteExcludeHeader = 0,
            TCP2ByteIncludeHeader = 1,
            TCP4ByteExcludeHeader = 2,
            TCP4ByteIncludeHeader = 3,
            TCPHeaderCustom = 4,
            TCPHeaderNone = 5,
            MessageQueue = 6,
            WebService = 7,
            Custom = 8
        }

        // protocol tcp only
        public enum EnumTCPHeader
        {
            TCP_2_Bytes_BCD_ExcludeHeader = 0,
            TCP_2_Bytes_BCD_IncludeHeader = 1,
            TCP_2_Bytes_ASC_ExcludeHeader = 2, //default
            TCP_2_Bytes_ASC_IncludeHeader = 3,
            TCP_4_Bytes_ExcludeHeader = 4,
            TCP_4_Bytes_IncludeHeader = 5,
            None = 6,
            Custom = 7
        }

        //only for tcp header 2 bytes
        public static void Flip2BytesTcpHeader(ref byte[] bytes)
        {
            //get header
            byte b0 = bytes[0];
            byte b1 = bytes[1];

            //flip 2 bytes tcp header
            bytes[0] = b1;
            bytes[1] = b0;
        }

        public static int GetTcpHeaderLength(int Protocol)
        {
            int tcp_len = 2; //default

            switch (Protocol)
            {
                case 0:
                case 1:
                    tcp_len = 2;
                    break;
                case 2:
                case 3:
                    tcp_len = 4;
                    break;
                case 4:
                    tcp_len = 2; //custom
                    break;
                case 5:
                    tcp_len = 0; //none
                    break;
                default:
                    tcp_len = 2; //default
                    break;
            }

            return tcp_len;
        }

        public static int GetTcpHeaderLength(NbMessage.EnumTCPHeader TCPHeader)
        {
            int tcp_len = 2; //default

            switch (TCPHeader)
            {
                case NbMessage.EnumTCPHeader.TCP_2_Bytes_ASC_ExcludeHeader:
                case NbMessage.EnumTCPHeader.TCP_2_Bytes_BCD_ExcludeHeader:
                case NbMessage.EnumTCPHeader.TCP_2_Bytes_ASC_IncludeHeader:
                case NbMessage.EnumTCPHeader.TCP_2_Bytes_BCD_IncludeHeader:
                    tcp_len = 2;
                    break;
                case NbMessage.EnumTCPHeader.TCP_4_Bytes_ExcludeHeader:
                case NbMessage.EnumTCPHeader.TCP_4_Bytes_IncludeHeader:
                    tcp_len = 4;
                    break;
                case NbMessage.EnumTCPHeader.None:
                    tcp_len = 0;
                    break;
                default:
                    tcp_len = 2;
                    break;
            }

            return tcp_len;
        }

        public static NbMessage.EnumTCPHeader GetTcpHeader(int Protocol, int TCPHeaderFormat)
        {
            NbMessage.EnumTCPHeader tcp_header_format = NbMessage.EnumTCPHeader.TCP_2_Bytes_ASC_ExcludeHeader;

            //protocol tcp 1 until 5
            switch (Protocol)
            {
                case 0:
                    if (TCPHeaderFormat == 0)
                        tcp_header_format = NbMessage.EnumTCPHeader.TCP_2_Bytes_ASC_ExcludeHeader;
                    else
                        tcp_header_format = NbMessage.EnumTCPHeader.TCP_2_Bytes_BCD_ExcludeHeader;
                    break;
                case 1:
                    if (TCPHeaderFormat == 0)
                        tcp_header_format = NbMessage.EnumTCPHeader.TCP_2_Bytes_ASC_IncludeHeader;
                    else
                        tcp_header_format = NbMessage.EnumTCPHeader.TCP_2_Bytes_BCD_IncludeHeader;
                    break;
                case 2:
                    tcp_header_format = NbMessage.EnumTCPHeader.TCP_4_Bytes_ExcludeHeader;
                    break;
                case 3:
                    tcp_header_format = NbMessage.EnumTCPHeader.TCP_4_Bytes_IncludeHeader;
                    break;
                case 4:
                    tcp_header_format = NbMessage.EnumTCPHeader.Custom;
                    break;
                case 5:
                    tcp_header_format = NbMessage.EnumTCPHeader.None;
                    break;
            }

            return tcp_header_format;
        }

        public static string GetMsgTypeResp(string MsgTypeReq)
        {
            string MsgTypeResp;

            if (MsgTypeReq.Length == 4)
            {
                switch (MsgTypeReq.Substring(2, 1))
                {
                    case "0": //Request
                        MsgTypeResp = MsgTypeReq.Substring(0, 2) + "1" + MsgTypeReq.Substring(MsgTypeReq.Length - 1, 1);

                        if (MsgTypeResp.Substring(MsgTypeResp.Length - 1, 1) == "1")
                        {
                            MsgTypeResp = MsgTypeResp.Substring(0, 3) + "0";
                        }
                        break;
                    case "2": //Advice
                        MsgTypeResp = MsgTypeReq.Substring(0, 2) + "3" + MsgTypeReq.Substring(MsgTypeReq.Length - 1, 1);

                        if (MsgTypeResp.Substring(MsgTypeResp.Length - 1, 1) == "1")
                        {
                            MsgTypeResp = MsgTypeResp.Substring(0, 3) + "0";
                        }
                        break;
                    default:
                        MsgTypeResp = MsgTypeReq;
                        break;
                }
            }
            else
            {
                MsgTypeResp = MsgTypeReq;
            }

            return MsgTypeResp;
        }

        public static bool IsRequestMessage(string MsgType)
        {
            bool bval = false;

            if (string.IsNullOrEmpty(MsgType) == false)
            {
                if (MsgType.Substring(2, 1) == "0" ||
                    MsgType.Substring(2, 1) == "2")
                {
                    bval = true;
                }
            }

            return bval;
        }

        public static bool IsResponseMessage(string MsgType)
        {
            bool bval = false;

            if (string.IsNullOrEmpty(MsgType) == false)
            {
                if (MsgType.Substring(2, 1) == "1" ||
                    MsgType.Substring(2, 1) == "3")
                {
                    bval = true;
                }
            }

            return bval;
        }

        public static byte[] RemoveTCPHeader(byte[] src, byte len)
        {
            //byte[] dst = new byte[src.Length - len + 1];
            byte[] dst = new byte[src.Length - len];

            Array.Copy(src, len, dst, 0, src.Length - len);

            return dst;
        }

        public static byte[] AddTCPHeader(string strData, EnumTCPHeader TCPHeader)
        {
            byte[] bytes = NbConvert.StringToBytes(strData);

            return AddTCPHeader(bytes, TCPHeader, true);
        }

        public static byte[] AddTCPHeader(string strData, EnumTCPHeader TCPHeader, bool HiLo)
        {
            byte[] bytes = NbConvert.StringToBytes(strData);

            return AddTCPHeader(bytes, TCPHeader, HiLo);
        }

        public static byte[] AddTCPHeader(byte[] src, EnumTCPHeader TCPHeader)
        {
            return AddTCPHeader(src, TCPHeader, true);
        }

        public static byte[] AddTCPHeader(byte[] src, EnumTCPHeader TCPHeader, bool HiLo)
        {
            byte[] dst;
            string strLen;
            int lenHdr = 2;
            int lenMsg = src.Length;
            int maxSend = 256;

            switch (TCPHeader)
            {
                case EnumTCPHeader.TCP_2_Bytes_ASC_ExcludeHeader:
                case EnumTCPHeader.TCP_2_Bytes_BCD_ExcludeHeader:
                    lenMsg = src.Length;
                    lenHdr = 2;
                    break;
                case EnumTCPHeader.TCP_2_Bytes_ASC_IncludeHeader:
                case EnumTCPHeader.TCP_2_Bytes_BCD_IncludeHeader:
                    lenMsg = src.Length + 2;
                    lenHdr = 2;
                    break;
                case EnumTCPHeader.TCP_4_Bytes_ExcludeHeader:
                    lenMsg = src.Length;
                    lenHdr = 4;
                    break;
                case EnumTCPHeader.TCP_4_Bytes_IncludeHeader:
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
            if (TCPHeader == EnumTCPHeader.TCP_2_Bytes_ASC_ExcludeHeader ||
                TCPHeader == EnumTCPHeader.TCP_2_Bytes_ASC_IncludeHeader)
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
            else if (TCPHeader == EnumTCPHeader.TCP_2_Bytes_BCD_ExcludeHeader ||
                TCPHeader == EnumTCPHeader.TCP_2_Bytes_BCD_IncludeHeader)
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
            else
            {
                strLen = lenMsg.ToString().PadLeft(4, '0');
                byte[] hdr = NbConvert.StringToBytes(strLen);
                hdr.CopyTo(dst, 0);

                //tcp header 4 bytes
                //dst[0] = Convert.ToByte(strLen.Substring(0, 1));
                //dst[1] = Convert.ToByte(strLen.Substring(1, 1));
                //dst[2] = Convert.ToByte(strLen.Substring(2, 1));
                //dst[3] = Convert.ToByte(strLen.Substring(3, 1));
            }

            return dst;
        }

        public static string ContructXmlMessage(string[] de)
        {
            string ret = "<Iso8583Xml>";

            for (int i = 0; i < de.Length; i++)
            {
                if (string.IsNullOrEmpty(de[i]) == false)
                {
                    ret = ret +
                        "<F" + i.ToString() + ">" +
                        de[i] +
                        "</F" + i.ToString() + ">";
                }
            }

            ret = ret + "</Iso8583Xml>";

            return ret;
        }

        public static string[] ExtractXmlMessage(string xmlmessage)
        {
            string[] de = new string[129];
            string tagStart;
            string tagEnd;
            int idxStart;
            int idxEnd;

            try
            {
                for (int i = 0; i < de.Length; i++)
                {
                    tagStart = "<F" + i.ToString() + ">";
                    tagEnd = "</F" + i.ToString() + ">";

                    idxStart = xmlmessage.IndexOf(tagStart);
                    idxEnd = xmlmessage.IndexOf(tagEnd);

                    if (idxStart > -1 && idxEnd > -1)
                    {
                        idxStart = idxStart + tagStart.Length;
                        idxEnd = idxEnd - idxStart;

                        de[i] = xmlmessage.Substring(idxStart, idxEnd);
                    }
                    else
                        de[i] = "";
                }
            }
            catch { }

            return de;
        }

        public static string FormatBinary(byte[] bytes)
        {
            string strdata = Encoding.ASCII.GetString(bytes);

            return FormatBinary(strdata);
        }

        public static string FormatBinary(string strdata)
        {
            const short LENGTH_ROW = 16;

            int lengthRow = strdata.Length;
            int maxLoop = lengthRow / LENGTH_ROW;

            string ret = "";
            string tmp = "";

            string sbin;
            string shex;

            for (int i = 0; i <= maxLoop; i++)
            {
                if (i == maxLoop)
                    sbin = strdata.Substring(i * LENGTH_ROW, lengthRow - (i * LENGTH_ROW));
                else
                    sbin = strdata.Substring(i * LENGTH_ROW, LENGTH_ROW);

                shex = "";

                for (int j = 0; j < sbin.Length; j++)
                {
                    tmp = sbin.Substring(j, 1);
                    tmp = NbString.AscInHex(tmp);

                    shex = shex + " " + tmp;
                }

                //display binary + hex
                if (sbin.Length < 16)
                    sbin = sbin + "".PadLeft(16 - sbin.Length, ' ');

                ret = ret + "[" + (i * LENGTH_ROW).ToString().PadLeft(5, '0') + "]  " + FormatString(sbin) + "  " + shex + "\r\n";
            }

            return ret;
        }

        public static string FormatString(string strdata)
        {
            string ret = "";

            for (int i = 0; i < strdata.Length; i++)
            {
                if (NbString.Asc(strdata.Substring(i, 1)) < 32)
                    ret = ret + ".";
                else if (NbString.Asc(strdata.Substring(i, 1)) > 126)
                    ret = ret + ".";
                else
                    ret = ret + strdata.Substring(i, 1);
            }

            return ret;
        }
    }
}
