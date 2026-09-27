using System;
using System.Linq;
using System.Text;

namespace SyncNet.Networking
{
    public static class TcpHeader
    {
        /*         
            Contoh panjang message 50 bytes:
            - Binary2Byte BigEndian (Hi Lo) → 0x00 0x32 
            - Binary2Byte LittleEndian (Lo Hi) → 0x32 0x00
            - Ascii4Digit → 0x35 0x30 (karakter '5' '0')
            - Bcd2Byte → 0x00 0x50 

            Catatan:
            Binary 2 byte max message 64 KB (65.535 bytes), 
            Binary 4 byte max message 4 GB (4.294.967.295 bytes), 
            Ascii 4 digit max message 9999 bytes
         */

        // Default: Binary 2-byte, Exclude, BigEndian
        private const TcpHeaderType DefaultTcpHeaderType = TcpHeaderType.Binary2Byte;
        private const TcpLengthMode DefaultLengthMode = TcpLengthMode.Exclude;
        private const TcpEndianMode DefaultEndianMode = TcpEndianMode.BigEndian;

        public static byte[] AddTcpHeader(byte[] payload, TcpHeaderType type = DefaultTcpHeaderType,
            TcpLengthMode lengthMode = DefaultLengthMode, TcpEndianMode endian = DefaultEndianMode)
        {
            int length = payload.Length;

            switch (type)
            {
                case TcpHeaderType.Binary2Byte:
                    if (lengthMode == TcpLengthMode.Include) length += 2;

                    ushort len2 = (ushort)length;
                    byte[] header2 = BitConverter.GetBytes(len2);

                    if (endian == TcpEndianMode.BigEndian) Array.Reverse(header2);

                    return Combine(header2, payload);

                case TcpHeaderType.Bcd2Byte:
                    if (lengthMode == TcpLengthMode.Include) length += 2;

                    // Panjang ditulis sebagai 4 digit decimal (misalnya 0050)
                    string bcdLen = length.ToString("D4"); // selalu 4 digit

                    // Encode ke BCD: setiap 2 digit → 1 byte
                    byte[] headerBcd = new byte[2];
                    headerBcd[0] = (byte)(((bcdLen[0] - '0') << 4) | (bcdLen[1] - '0'));
                    headerBcd[1] = (byte)(((bcdLen[2] - '0') << 4) | (bcdLen[3] - '0'));

                    return Combine(headerBcd, payload);

                case TcpHeaderType.Binary4Byte:
                    if (lengthMode == TcpLengthMode.Include) length += 4;

                    int len4 = length;
                    byte[] header4 = BitConverter.GetBytes(len4);

                    if (endian == TcpEndianMode.BigEndian) Array.Reverse(header4);

                    return Combine(header4, payload);

                case TcpHeaderType.Ascii4Digit:
                    if (lengthMode == TcpLengthMode.Include) length += 4;

                    string asciiLen = length.ToString("D4"); // pad 4 digit
                    byte[] headerAscii = Encoding.ASCII.GetBytes(asciiLen);

                    return Combine(headerAscii, payload);

                default:
                    throw new NotSupportedException("Unsupported header type");
            }
        }
        public static int GetLengthHeader(TcpHeaderType type = DefaultTcpHeaderType)
        {
            return type switch
            {
                TcpHeaderType.Binary2Byte => 2,
                TcpHeaderType.Bcd2Byte => 2,
                TcpHeaderType.Binary4Byte => 4,
                TcpHeaderType.Ascii4Digit => 4,
                _ => throw new NotSupportedException("Unsupported header type"),
            };
        }
        public static int GetLengthMessage(byte[] bytes, TcpHeaderType type = DefaultTcpHeaderType,
            TcpLengthMode lengthMode = DefaultLengthMode, TcpEndianMode endian = DefaultEndianMode)
        {
            switch (type)
            {
                case TcpHeaderType.Binary2Byte:
                    byte[] header2 = bytes.Take(2).ToArray();

                    if (endian == TcpEndianMode.BigEndian) Array.Reverse(header2);
                    ushort len2 = BitConverter.ToUInt16(header2, 0);

                    return lengthMode == TcpLengthMode.Include ? len2 - 2 : len2;

                case TcpHeaderType.Bcd2Byte:
                    byte[] bcdHeader = bytes.Take(2).ToArray();

                    // Decode BCD: setiap nibble → 1 digit
                    int d1 = (bcdHeader[0] >> 4) & 0x0F;
                    int d2 = bcdHeader[0] & 0x0F;
                    int d3 = (bcdHeader[1] >> 4) & 0x0F;
                    int d4 = bcdHeader[1] & 0x0F;

                    int lenBcd = int.Parse($"{d1}{d2}{d3}{d4}");
                    return lengthMode == TcpLengthMode.Include ? lenBcd - 2 : lenBcd;

                case TcpHeaderType.Binary4Byte:
                    byte[] header4 = bytes.Take(4).ToArray();

                    if (endian == TcpEndianMode.BigEndian) Array.Reverse(header4);
                    int len4 = BitConverter.ToInt32(header4, 0);

                    return lengthMode == TcpLengthMode.Include ? len4 - 4 : len4;

                case TcpHeaderType.Ascii4Digit:
                    string asciiLen = Encoding.ASCII.GetString(bytes.Take(4).ToArray());
                    int lenAscii = int.Parse(asciiLen);

                    return lengthMode == TcpLengthMode.Include ? lenAscii - 4 : lenAscii;
                default:
                    throw new NotSupportedException("Unsupported header type");
            }
        }

        private static byte[] Combine(byte[] header, byte[] payload)
        {
            byte[] result = new byte[header.Length + payload.Length];

            Buffer.BlockCopy(header, 0, result, 0, header.Length);
            Buffer.BlockCopy(payload, 0, result, header.Length, payload.Length);

            return result;
        }
    }
}
