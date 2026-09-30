// Golden SyncNetPro.Toolkit: dihasilkan oleh SyncNet.Cryptography / SyncNet.Library (SDK lama) apa adanya.
using System.Text;
using Newtonsoft.Json;
using SyncNet.Cryptography;
using SyncNet.Library;

internal static class ToolkitGolden
{
    public static void Write(string outDir)
    {
        Directory.CreateDirectory(outDir);
        foreach (string f in Directory.GetFiles(outDir)) File.Delete(f);

        string[] keys = ["0123456789ABCDEF", "0123456789ABCDEFFEDCBA9876543210", "0123456789ABCDEFFEDCBA987654321089ABCDEF01234567"];
        string[] blocks = ["0000000000000000", "041234FFFFFFFFFF", "0123456789ABCDEF0123456789ABCDEF"];
        var des = new List<object>();
        foreach (string key in keys)
        foreach (string block in blocks)
        {
            string enc = DesAlgorithm.EncryptHex(block, key);
            des.Add(new { key, block, encrypted = enc, decrypted = DesAlgorithm.DecryptHex(enc, key), kcv = NbCard.GetKeyCheckValue(key) });
        }

        var pins = new List<object>();
        foreach (string key in keys.Skip(1))
        foreach (string pan in new[] { "4111111111111111", "5264220012345678" })
        foreach (string pin in new[] { "1234", "123456", "987654321" })
        foreach (NbCard.EnumPinblockFormat format in Enum.GetValues<NbCard.EnumPinblockFormat>())
        {
            if (format == NbCard.EnumPinblockFormat.FORMAT_02_DOCUTEL && pin.Length > 6) continue;
            string block = NbCard.CreatePinBlock(pan, pin, key, format, out string err);
            pins.Add(new { key, pan, pin, format = (int)format, block, clear = DesAlgorithm.DecryptHex(block, key), err });
        }

        var luhn = new List<object>();
        foreach (string pan in new[] { "411111111111111", "526422001234567", "601100000000000", "37828224631000", "49927398716", "123456789012345678" })
        {
            luhn.Add(new { digits = pan, check = NbCard.GetLuhnCheckDigit(pan), full = NbCard.SetCheckDigit(pan), valid = NbCard.IsCreditCardValid(NbCard.SetCheckDigit(pan)) });
        }

        string icc = "9F2608A1B2C3D4E5F60102" + "9F2701" + "80" + "9F100706010A03A0B800" + "5F340101" + "82025C00" + "9F3602001A" + "9F1A020360" + "5F2A020360" + "9A03260930" + "9C0100" + "9F370412345678";
        string longValue = "DF01" + "81" + "96" + string.Concat(Enumerable.Repeat("AB", 150));
        var emv = new List<object>();
        foreach (string hex in new[] { icc, longValue })
        {
            var parsed = NbTlvEmv.ParseTLV(hex);
            emv.Add(new
            {
                hex,
                items = parsed.Values.Select(t => new { tag = Convert.ToHexString(t.Tag), length = t.Length, value = Convert.ToHexString(t.Value) }),
                rebuilt = Convert.ToHexString(NbTlvEmv.ConstructTLV(parsed)),
                info = NbTlvEmv.GetInfo(hex).Replace("\r\n", "\n"),
            });
        }

        var tlvDefault = new NbTlvQris();
        var tlv2 = new NbTlvQris(2, 2);
        string[] de = new string[40];
        de[1] = "HELLO"; de[26] = "ID.CO.QRIS.WWW"; de[3] = ""; de[39] = "X";
        var numeric = new
        {
            build3 = tlvDefault.ConstructTlvMessage(de),
            build2 = tlv2.ConstructTlvMessage(de),
            element = tlvDefault.SetTagValue("007", "ABC"),
            parse3 = Pick(tlvDefault.ExtractTlvMessage(tlvDefault.ConstructTlvMessage(de))),
            parse2 = Pick(tlv2.ExtractTlvMessage(tlv2.ConstructTlvMessage(de))),
            truncated = Pick(tlvDefault.ExtractTlvMessage("001005HELLO002010ABC")),
        };

        var dumps = new List<object>();
        foreach (int length in new[] { 0, 1, 15, 16, 17, 40 })
        {
            byte[] bytes = Enumerable.Range(0, length).Select(i => (byte)(i * 7 + 20)).ToArray();
            dumps.Add(new { hex = Convert.ToHexString(bytes), dump = NbFormat.FormatBinary(bytes).Replace("\r\n", "\n") });
        }

        var bcd = new List<object>();
        foreach ((string digits, bool padRight) in new[] { ("0200", false), ("123", false), ("4111111111111111D251", true), ("4111D25", true), ("000000009900", false) })
        {
            bcd.Add(new { digits, padRight, hex = Convert.ToHexString(Encoding.Latin1.GetBytes(NbConvert.ToBCD(digits, padRight))) });
        }

        var ebcdic = new { text = "TERM0001 Toko Jaya-123", hex = Convert.ToHexString(Encoding.Latin1.GetBytes(NbConvert.AsciiToEbcdic("TERM0001 Toko Jaya-123"))) };

        File.WriteAllText(Path.Combine(outDir, "toolkit.json"),
            JsonConvert.SerializeObject(new { des, pins, luhn, emv, numeric, dumps, bcd, ebcdic }, Formatting.Indented), new UTF8Encoding(false));
        Console.WriteLine($"{des.Count} DES + {pins.Count} PIN block + {luhn.Count} Luhn + {emv.Count} EMV + TLV/dump/BCD/EBCDIC ditulis ke {outDir}");
    }

    private static Dictionary<int, string> Pick(string[] values) =>
        values.Select((v, i) => (v, i)).Where(x => !string.IsNullOrEmpty(x.v)).ToDictionary(x => x.i, x => x.v);
}
