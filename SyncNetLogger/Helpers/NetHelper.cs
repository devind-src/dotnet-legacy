using System;
using System.Net;
using System.Text;

namespace SyncNet.Helpers
{
    public class NetHelper
    {
        public enum EnumTCPHeader
        {
            TCP_2_Bytes_ExcludeHeader, //default
            TCP_2_Bytes_IncludeHeader,
            TCP_4_Bytes_ExcludeHeader,
            TCP_4_Bytes_IncludeHeader
        }

        public enum EnumTCPHeaderFormat
        {
            BCD,
            ASC
        }

        public static string GetIPAddress(EndPoint ep)
        {
            string ip = "";

            try
            {
                if (ep == null) return "";
                ip = IPAddress.Parse(((IPEndPoint)ep).Address.ToString()).ToString();
            }
            catch (Exception)
            {

            }

            return ip;
        }

        public static string GetPort(EndPoint ep)
        {
            string port = "";

            try
            {
                if (ep == null) return "";
                port = ((IPEndPoint)ep).Port.ToString();
            }
            catch (Exception)
            {

            }

            return port;
        }

        public static string GetRemoteEP(EndPoint ep)
        {
            if (ep == null) return "";

            string ipaddress = GetIPAddress(ep);
            string port = GetPort(ep);
            string remote_ep = ipaddress + "," + port;

            if (string.IsNullOrEmpty(ipaddress) == true && string.IsNullOrEmpty(port) == true)
                remote_ep = string.Empty;

            return remote_ep;
        }

        public static EndPoint GetEndPoint(string ip, int port)
        {
            var ipAddress = IPAddress.Parse(ip);
            var ep = new IPEndPoint(ipAddress, port);

            return ep;
        }

        public static EndPoint GetEndPoint(string RemoteAddress)
        {
            string[] arr = RemoteAddress.Split(',');
            var ip = IPAddress.Parse(arr[0]);

            int port = Convert.ToInt32(arr[1]);
            var ep = new IPEndPoint(ip, port);

            return ep;
        }

        public static byte[] ReplaceTCPHeader(byte[] bytes, int OldTcpHeaderLength, EnumTCPHeader NewTCPHeader, EnumTCPHeaderFormat NewTCPHeaderFormat)
        {
            byte[] NewBytes = RemoveTCPHeader(bytes, OldTcpHeaderLength);

            return SetTCPHeader(NewBytes, NewTCPHeader, NewTCPHeaderFormat);
        }

        public static byte[] SetTCPHeader(byte[] bytes, EnumTCPHeader TCPHeader, EnumTCPHeaderFormat TCPHeaderFormat)
        {
            string strData = Encoding.ASCII.GetString(bytes);

            return SetTCPHeader(strData, TCPHeader, TCPHeaderFormat);
        }

        public static byte[] SetTCPHeader(string strData, EnumTCPHeader TCPHeader, EnumTCPHeaderFormat TCPHeaderFormat)
        {
            int len = 0;
            short maxSend = 256;

            switch (TCPHeader)
            {
                case EnumTCPHeader.TCP_2_Bytes_ExcludeHeader:
                    len = strData.Length;
                    strData = "00" + strData;
                    break;
                case EnumTCPHeader.TCP_2_Bytes_IncludeHeader:
                    len = strData.Length + 2;
                    strData = "00" + strData;
                    break;
                case EnumTCPHeader.TCP_4_Bytes_ExcludeHeader:
                    len = strData.Length;
                    strData = len.ToString().PadLeft(4, '0') + strData;
                    break;
                case EnumTCPHeader.TCP_4_Bytes_IncludeHeader:
                    len = strData.Length + 4;
                    strData = len.ToString().PadLeft(4, '0') + strData;
                    break;
                default: //default exclude header
                    len = strData.Length;
                    strData = "00" + strData;
                    break;
            }

            byte[] bytes = ConvertStringToBytes(strData);

            //additional function for 2 bytes tcp header
            if (TCPHeader == EnumTCPHeader.TCP_2_Bytes_ExcludeHeader || TCPHeader == EnumTCPHeader.TCP_2_Bytes_IncludeHeader)
            {
                if (TCPHeaderFormat == EnumTCPHeaderFormat.ASC)
                {
                    if (len >= maxSend)
                    {
                        bytes[0] = (byte)(len / maxSend);
                        bytes[1] = (byte)(len % maxSend);
                    }
                    else
                    {
                        bytes[0] = 0;
                        bytes[1] = (byte)len;
                    }
                }
                else
                {
                    //default is hex format
                    string strlen = len.ToString().PadLeft(4, '0');

                    //convert from hex to byte
                    bytes[0] = Byte.Parse(strlen.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    bytes[1] = Byte.Parse(strlen.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                }
            }

            return bytes;
        }

        public static byte[] AddTCPHeader(byte[] bytes, EnumTCPHeader TCPHeader, EnumTCPHeaderFormat TCPHeaderFormat)
        {
            return SetTCPHeader(bytes, TCPHeader, TCPHeaderFormat);
        }

        public static byte[] AddTCPHeader(string strData, EnumTCPHeader TCPHeader, EnumTCPHeaderFormat TCPHeaderFormat)
        {
            return SetTCPHeader(strData, TCPHeader, TCPHeaderFormat);
        }

        public static byte[] AddTCPHeader(string strData, EnumTCPHeader TCPHeader)
        {
            int LengthMessage;
            short MaxSend = 256;

            switch (TCPHeader)
            {
                case EnumTCPHeader.TCP_2_Bytes_ExcludeHeader:
                    strData = "00" + strData;
                    LengthMessage = strData.Length - 2;
                    break;
                case EnumTCPHeader.TCP_2_Bytes_IncludeHeader:
                    strData = "00" + strData;
                    LengthMessage = strData.Length;
                    break;
                default: //default exclude header
                    strData = "00" + strData;
                    LengthMessage = strData.Length - 2;
                    break;
            }

            byte[] ByteArray = Encoding.ASCII.GetBytes(strData);

            if (LengthMessage >= MaxSend)
            {
                ByteArray[0] = (byte)(LengthMessage / MaxSend);
                ByteArray[1] = (byte)(LengthMessage % MaxSend);
            }
            else
            {
                ByteArray[0] = 0;
                ByteArray[1] = (byte)LengthMessage;
            }

            return ByteArray;
        }

        public static byte[] RemoveTCPHeader(byte[] bytes, int len)
        {
            byte[] newbytes = new byte[bytes.Length - len];
            Array.Copy(bytes, len, newbytes, 0, newbytes.Length);

            return newbytes;
        }

        public static string FormatBinary(byte[] bytes)
        {
            string strData = Encoding.ASCII.GetString(bytes);

            return FormatBinary(strData);
        }

        public static string FormatBinary(string strData)
        {
            const short LENGTH_ROW = 16;

            int lengthRow = strData.Length;
            int maxLoop = lengthRow / LENGTH_ROW;

            string ret = "";

            string sbin;
            string shex;

            for (int i = 0; i <= maxLoop; i++)
            {
                if (i == maxLoop)
                    sbin = strData.Substring(i * LENGTH_ROW, lengthRow - (i * LENGTH_ROW));
                else
                    sbin = strData.Substring(i * LENGTH_ROW, LENGTH_ROW);

                shex = "";

                for (int j = 0; j < sbin.Length; j++)
                {
                    byte tmp = (byte)Convert.ToChar(sbin.Substring(j, 1));
                    string hex = tmp.ToString("X").PadLeft(2, '0');

                    shex = shex + " " + hex;
                }

                //display binary + hex
                if (sbin.Length < 16)
                    sbin = sbin + "".PadLeft(16 - sbin.Length, ' ');

                ret = ret + "[" + (i * LENGTH_ROW).ToString().PadLeft(5, '0') + "]  " + FormatString(sbin) + "  " + shex + "\r\n";
            }

            return ret;
        }

        private static string FormatString(string strdata)
        {
            string ret = "";

            for (int i = 0; i < strdata.Length; i++)
            {
                byte val = (byte)Convert.ToChar(strdata.Substring(i, 1));

                if (val < 32)
                    ret = ret + ".";
                else if (val > 126)
                    ret = ret + ".";
                else
                    ret = ret + strdata.Substring(i, 1);
            }

            return ret;
        }

        private static byte[] ConvertStringToBytes(string strdata)
        {
            byte[] bytes = new byte[strdata.Length];

            for (int i = 0; i < strdata.Length; i++)
            {
                bytes[i] = (byte)Convert.ToChar(strdata.Substring(i, 1));
            }

            return bytes;
        }
    }
}
