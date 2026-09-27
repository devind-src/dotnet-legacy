using System;
using System.Linq;
using System.Numerics;

namespace SyncNet.Services
{
    public class DukptLib
    {
        // Konstanta untuk modifikasi BDK dan derivasi key sesuai standar ANSI X9.24
        private static readonly byte[] KeyMask = [
            0xC0, 0xC0, 0xC0, 0xC0, 0x00, 0x00, 0x00, 0x00,
            0xC0, 0xC0, 0xC0, 0xC0, 0x00, 0x00, 0x00, 0x00
        ];
        private static readonly byte[] Ms16Mask = [
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        ];
        private static readonly byte[] Ls16Mask = [
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        ];
        private static readonly byte[] Reg8Mask = [
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xE0, 0x00, 0x00
        ];
        private static readonly byte[] PekMask = [
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        ];
        private static readonly byte[] PinMask = [
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF
        ];

        private static readonly byte[] Reg3Mask = [0x1F, 0xFF, 0xFF];
        private static readonly byte[] ShiftRegMask = [0x10, 0x00, 0x00];

        public static byte[] GenerateIPEK(byte[] bdk, byte[] ksn)
        {
            if (bdk == null || bdk.Length != 16)
                throw new ArgumentException("BDK harus 16 byte (2-key 3DES).", nameof(bdk));
            if (ksn == null || ksn.Length != 10)
                throw new ArgumentException("KSN harus 10 byte.", nameof(ksn));

            byte[] ksnForIpek = new byte[8];
            Array.Copy(ksn, 0, ksnForIpek, 0, 8);
            ksnForIpek[7] &= 0xE0;

            byte[] ipekLeft = DukptUtils.TripleDESEncrypt(bdk, ksnForIpek);
            byte[] modifiedBdk = DukptUtils.XorArrays(bdk, KeyMask);
            byte[] ipekRight = DukptUtils.TripleDESEncrypt(modifiedBdk, ksnForIpek);

            return ipekLeft.Concat(ipekRight).ToArray();
        }

        public static byte[] GeneratePEK(byte[] ipek, byte[] ksn)
        {
            BigInteger ipekInt = DukptUtils.BytesToBigInteger(ipek);
            BigInteger ksnInt = DukptUtils.BytesToBigInteger(ksn);
            BigInteger pekMask = new BigInteger(PekMask, isUnsigned: true, isBigEndian: true);
            BigInteger sessionKey = DeriveKey(ipekInt, ksnInt) ^ pekMask;

            return DukptUtils.BigIntegerToBytes(sessionKey);
        }
        public static byte[] EncryptPinblock(byte[] pinblock, byte[] pek)
        {
            //xor pek with mask
            byte[] modifiedPek = DukptUtils.XorArrays(pek, PinMask);

            return DukptUtils.TripleDESEncrypt(modifiedPek, pinblock);
        }
        private static BigInteger DeriveKey(BigInteger ipek, BigInteger ksn)
        {
            BigInteger ksnReg = ksn
                & new BigInteger(Ls16Mask, isUnsigned: true, isBigEndian: true)
                & new BigInteger(Reg8Mask, isUnsigned: true, isBigEndian: true);

            BigInteger curKey = ipek;
            BigInteger reg3Mask = new BigInteger(Reg3Mask, isUnsigned: true, isBigEndian: true);
            BigInteger shiftReg = new BigInteger(ShiftRegMask, isUnsigned: true, isBigEndian: true);

            for (; shiftReg > 0; shiftReg >>= 1)
            {
                if ((shiftReg & ksn & reg3Mask) > 0)
                {
                    ksnReg |= shiftReg;
                    curKey = GenerateKey(curKey, ksnReg);
                }
            }
            return curKey;
        }

        private static BigInteger GenerateKey(BigInteger key, BigInteger ksnReg)
        {
            BigInteger keyMask = new BigInteger(KeyMask, isUnsigned: true, isBigEndian: true);

            BigInteger left = EncryptRegister(key ^ keyMask, ksnReg);
            BigInteger right = EncryptRegister(key, ksnReg);
            return (left << 64) | right;
        }

        private static BigInteger EncryptRegister(BigInteger key, BigInteger reg8)
        {
            BigInteger ls16 = new BigInteger(Ls16Mask, isUnsigned: true, isBigEndian: true);
            BigInteger ms16 = new BigInteger(Ms16Mask, isUnsigned: true, isBigEndian: true);

            BigInteger lower = key & ls16;
            BigInteger upper = (key & ms16) >> 64;
            BigInteger encrypted = DukptUtils.TransformDes(upper, lower ^ reg8, true);
            return lower ^ encrypted;
        }
    }
}
