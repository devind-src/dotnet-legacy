// Golden ISO 8583: pack/unpack/format dihasilkan oleh SyncNet.IsoMessage (SDK lama) apa adanya.
// Diuji di tests/SyncNetPro.Iso8583.Tests. Kasus yang memicu bug SDK lama tetap direkam (field "legacyUnpack")
// agar perbedaan perilaku SDK baru terdokumentasi, bukan disembunyikan.
using System.Text;
using Newtonsoft.Json;
using SyncNet.IsoMessage;
using SyncNet.Library;

internal static class IsoGolden
{
    private sealed record FieldDef(int nr, string type, string format, string attr, int len);

    // Seperti SDK lama, setiap spesifikasi = tabel default FieldFormatter + field yang ditimpa.
    private sealed record SpecDef(string name, string mti, string bitmap, string lenvar, int tpdu, FieldDef[] fields);

    private sealed record Case(string name, string note, SpecDef spec, string mti, string tpdu, Dictionary<int, string> fields);

    public static void Write(string outDir)
    {
        Directory.CreateDirectory(outDir);
        foreach (string f in Directory.GetFiles(outDir)) File.Delete(f);

        // SDK lama baru dapat memakai EBCDIC setelah konstruktor AppProcessor mendaftarkan provider ini.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var ascii = new SpecDef("legacy-default", "ASC", "ASC", "ASC", 0, []);
        var biller = new SpecDef("api-biller-iso", "ASC", "ASC", "ASC", 0,
        [
            F(2, "ASCII", "LLVAR", "n", 19), F(3, "ASCII", "Fixed", "n", 6), F(4, "ASCII", "Fixed", "n", 12),
            F(7, "ASCII", "Fixed", "n", 10), F(11, "ASCII", "Fixed", "n", 6), F(12, "ASCII", "Fixed", "n", 6),
            F(13, "ASCII", "Fixed", "n", 4), F(18, "ASCII", "Fixed", "n", 4), F(32, "ASCII", "LLVAR", "n", 11),
            F(33, "ASCII", "LLVAR", "n", 11), F(37, "ASCII", "Fixed", "ans", 12), F(39, "ASCII", "Fixed", "ans", 2),
            F(41, "ASCII", "Fixed", "ans", 8), F(42, "ASCII", "Fixed", "ans", 15), F(48, "ASCII", "LLLVAR", "ans", 999),
            F(49, "ASCII", "Fixed", "ans", 3), F(70, "ASCII", "Fixed", "n", 3), F(100, "ASCII", "LLVAR", "n", 11),
            F(103, "ASCII", "LLVAR", "ans", 28),
        ]);
        var bcd = new SpecDef("bcd", "BCD", "BCD", "BCD", 0,
        [
            F(2, "BCD", "LLVAR", "n", 19), F(3, "BCD", "Fixed", "n", 6), F(4, "BCD", "Fixed", "n", 12),
            F(11, "BCD", "Fixed", "n", 6), F(12, "BCD", "Fixed", "n", 6), F(22, "BCD", "Fixed", "n", 3),
            F(35, "BCD", "LLVAR", "an", 37), F(41, "ASCII", "Fixed", "an", 8), F(42, "ASCII", "Fixed", "an", 15),
            F(48, "ASCII", "LLLVAR", "ans", 999), F(55, "BCD", "LLLVAR", "ans", 999),
        ]);
        var bcdTpdu = bcd with { name = "bcd-tpdu", tpdu = 5 };
        var ebcdic = new SpecDef("ebcdic", "ASC", "ASC", "ASC", 0,
        [
            F(3, "ASCII", "Fixed", "n", 6), F(11, "ASCII", "Fixed", "n", 6), F(41, "EBCDIC", "Fixed", "ans", 8),
            F(43, "EBCDIC", "Fixed", "ans", 40), F(48, "EBCDIC", "LLLVAR", "ans", 999),
        ]);
        var bcdMtiAscBitmap = new SpecDef("bcd-mti-ascii-bitmap", "BCD", "ASC", "ASC", 0, [F(3, "ASCII", "Fixed", "n", 6), F(11, "ASCII", "Fixed", "n", 6)]);
        var bcdAscLen = new SpecDef("bcd-data-ascii-length", "ASC", "ASC", "ASC", 0, [F(2, "BCD", "LLVAR", "n", 19), F(3, "ASCII", "Fixed", "n", 6)]);
        var bcdSecondary = new SpecDef("bcd-secondary-bitmap", "BCD", "BCD", "BCD", 0, [F(3, "BCD", "Fixed", "n", 6), F(70, "BCD", "Fixed", "n", 3)]);

        string long127 = string.Concat(Enumerable.Range(0, 60).Select(i => $"<F{i}>v{i}</F{i}>"));
        var cases = new List<Case>
        {
            new("ascii-0200", "spesifikasi default, bitmap primer", ascii, "0200", "", new()
            {
                [2] = "4111111111111111", [3] = "380000", [4] = "000000150000", [7] = "20260930101530", [11] = "000000123456",
                [37] = "260930123456", [41] = "TERM0001        ", [42] = "MERCHANT0000001", [48] = "PLNPOST|532110000001|NAMA", [49] = "360",
            }),
            new("ascii-0200-secondary", "bitmap sekunder + field 127 panjang", ascii, "0210", "", new()
            {
                [3] = "500000", [4] = "000001234500", [11] = "000000000777", [39] = "0000", [100] = "12345", [102] = "0012345678", [103] = "532110000001", [127] = long127,
            }),
            new("ascii-0800", "network management", ascii, "0800", "", new() { [7] = "20260930101530", [11] = "000000000001", [70] = "301" }),
            new("api-biller-inquiry", "template ApiBillerIso", biller, "0100", "", new()
            {
                [3] = "380000", [4] = "000000000000", [7] = "0930031530", [11] = "123456", [12] = "101530", [13] = "0930", [18] = "6012",
                [32] = "008", [33] = "008", [37] = "260930123456", [41] = "TERM0001", [42] = "MERCHANT01     ", [48] = "", [49] = "360", [100] = "501", [103] = "532110000001",
            }),
            new("api-biller-response", "response dengan field 39 & 48", biller, "0110", "", new()
            {
                [3] = "380000", [4] = "000000250000", [7] = "0930031530", [11] = "123456", [37] = "260930123456", [39] = "00",
                [41] = "TERM0001", [48] = "NAMA PELANGGAN      TARIF R1 900VA", [70] = "301",
            }),
            new("bcd-even", "BCD MTI/bitmap/panjang, digit genap dan < 0x80", bcd, "0200", "", new()
            {
                [2] = "4111111111111111", [3] = "310000", [4] = "000000001000", [11] = "000123", [12] = "101530", [41] = "TERM0001", [48] = "DATA",
            }),
            new("bcd-odd", "BCD digit ganjil (PAN 19, POS entry 3)", bcd, "0200", "", new()
            {
                [2] = "4111111111111111123", [3] = "310000", [11] = "000123", [22] = "021",
            }),
            new("bcd-high-digits", "BCD dengan byte >= 0x80 (digit 80-99): unpack SDK lama salah (FromBCD via UTF-8)", bcd, "0200", "", new()
            {
                [3] = "380000", [4] = "000000009900", [11] = "998877",
            }),
            new("bcd-track2-icc", "track 2 BCD pad kanan + ICC field 55 (panjang byte)", bcd, "0200", "", new()
            {
                [3] = "000000", [11] = "000124", [35] = "4111111111111111D2512101", [55] = "9F2608A1B2C3D4E5F601029F2701805F340101",
            }),
            new("bcd-track2-odd", "track 2 BCD ganjil pad kanan: unpack SDK lama membuang digit pertama", bcd, "0200", "", new()
            {
                [3] = "000000", [11] = "000125", [35] = "4111111111111111D251210",
            }),
            new("bcd-tpdu", "TPDU 5 byte", bcdTpdu, "0800", "6000010000", new() { [3] = "990000", [11] = "000001" }),
            new("ebcdic", "field EBCDIC fixed & LLLVAR", ebcdic, "0200", "", new()
            {
                [3] = "380000", [11] = "000321", [41] = "TERM0001", [43] = "TOKO MAJU JAYA          JAKARTA       ID", [48] = "Data EBCDIC 123",
            }),
            new("ascii-fixed-short", "field fixed lebih pendek: SDK lama mem-pack tanpa validasi sehingga penerima salah offset", ascii, "0200", "", new() { [3] = "38", [11] = "000000123456", [41] = "TERM0001        " }),
            new("bcd-mti-ascii-bitmap", "MTI BCD + bitmap ASCII: unpack SDK lama salah offset", bcdMtiAscBitmap, "0200", "", new() { [3] = "380000", [11] = "000001" }),
            new("bcd-data-ascii-length", "data BCD + indikator panjang ASCII: unpack SDK lama tidak men-decode BCD", bcdAscLen, "0200", "", new() { [2] = "4111111111111111", [3] = "380000" }),
            new("bcd-secondary-bitmap", "bitmap BCD dengan bitmap sekunder: unpack SDK lama salah", bcdSecondary, "0800", "", new() { [3] = "990000", [70] = "301" }),
        };

        var output = new List<object>();
        foreach (Case c in cases)
        {
            FieldFormatter formatter = Build(c.spec);
            var iso = new Iso8583(formatter) { MsgType = c.mti };
            if (c.tpdu.Length > 0) iso.TPDU = c.tpdu;
            foreach ((int nr, string value) in c.fields) iso.PutField(nr, value);

            byte[] packed = iso.Pack();
            string format = Normalize(iso.GetFormattedMessage());
            string simple = Normalize(iso.GetFormattedSimple());
            string dump = Normalize(NbFormat.FormatBinary(packed));

            var back = new Iso8583(Build(c.spec));
            int rc = c.spec.tpdu > 0 ? back.Unpack(packed, 0, c.spec.tpdu) : back.Unpack(packed);
            string[] de = back.GetArray();
            var unpacked = new SortedDictionary<int, string>();
            for (int i = 2; i < de.Length; i++)
            {
                if (!string.IsNullOrEmpty(de[i])) unpacked[i] = Hexify(de[i]);
            }

            output.Add(new
            {
                c.name, c.note, c.spec, c.mti, c.tpdu,
                fields = c.fields.Where(kv => kv.Value.Length > 0).ToDictionary(kv => kv.Key, kv => kv.Value),
                packed = Convert.ToHexString(packed),
                format, simple, dump,
                legacyUnpack = new { rc, error = back.GetLastError(), mti = back.MsgType, tpdu = back.TPDU, fields = unpacked },
            });
        }

        var utf8 = new UTF8Encoding(false);
        File.WriteAllText(Path.Combine(outDir, "iso8583.json"), JsonConvert.SerializeObject(output, Formatting.Indented), utf8);

        // Pemetaan processing code ↔ tran type (IsoConverter SDK lama, via reflection karena private).
        var getPCode = typeof(IsoConverter).GetMethod("GetPCode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var pcodes = new List<object>();
        foreach (string mti in new[] { "0200", "0220", "0421", "0100" })
        {
            for (int code = 0; code <= 99; code++)
            {
                string pcode = code.ToString("D2") + "0000";
                pcodes.Add(new { mti, pcode, tranType = IsoConverter.GetTranTypeStr(mti, pcode) });
            }
        }

        var tranTypes = new[] { "BAL", "STM", "INQ", "WDL", "PAY", "PUR", "TRF", "RFD", "DEP", "ADV", "REV", "ADM", "PIN", "KEY", "VCR", "VDB", "VBA", "XXX" };
        var toPcode = new List<object>();
        foreach (string t in tranTypes)
        foreach (string ext in new[] { null, "", "57", "PLNPOST" })
        {
            toPcode.Add(new { tranType = t, ext, pcode = (string)getPCode.Invoke(null, [t, ext, "10", "20"])! });
        }

        File.WriteAllText(Path.Combine(outDir, "processing-code.json"), JsonConvert.SerializeObject(new { toTranType = pcodes, fromTranType = toPcode }, Formatting.Indented), utf8);
        Console.WriteLine($"{output.Count} kasus ISO 8583 + {pcodes.Count + toPcode.Count} kasus processing code ditulis ke {outDir}");
    }

    private static FieldDef F(int nr, string type, string format, string attr, int len) => new(nr, type, format, attr, len);

    private static FieldFormatter Build(SpecDef spec)
    {
        var f = new FieldFormatter
        {
            MsgTypeFormat = Enum.Parse<FieldFormatter.EnumMsgFormat>(spec.mti),
            BitmapFormat = Enum.Parse<FieldFormatter.EnumMsgFormat>(spec.bitmap),
            LenVarFormat = Enum.Parse<FieldFormatter.EnumMsgFormat>(spec.lenvar),
        };

        foreach (FieldDef d in spec.fields)
        {
            f.SetField(d.nr, Enum.Parse<Field.EnumFieldType>(d.type), Enum.Parse<Field.EnumFieldFormat>(d.format), Enum.Parse<Field.EnumFieldAtribute>(d.attr), d.len, "F" + d.nr);
        }

        return f;
    }

    // Nilai hasil unpack SDK lama dapat berisi byte biner (bug) — simpan printable apa adanya, selain itu hex.
    private static string Hexify(string value) => value.All(c => c >= 0x20 && c <= 0x7E) ? value : "hex:" + Convert.ToHexString(Encoding.Latin1.GetBytes(value));

    // SDK lama memakai Environment.NewLine dan "\r\n" campuran; golden dinormalisasi ke "\n".
    private static string Normalize(string text) => text.Replace("\r\n", "\n");
}
