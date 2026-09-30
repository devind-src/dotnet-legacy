using Newtonsoft.Json.Linq;

namespace SyncNetPro.Iso8583.Tests;

/// <summary>Golden dari SDK lama (tools/SyncNetPro.GoldenGenerator/IsoGolden.cs).</summary>
internal static class GoldenCases
{
    private static readonly Lazy<Dictionary<string, JObject>> Cases = new(() =>
        JArray.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Golden", "iso8583.json")))
            .Cast<JObject>()
            .ToDictionary(c => (string)c["name"]!));

    /// <summary>Kasus yang di SDK lama "berhasil" di-unpack tetapi nilainya rusak (bug), bukan acuan parse.</summary>
    public static readonly HashSet<string> LegacyCorruptUnpack = ["bcd-track2-icc", "bcd-track2-odd"];

    public static TheoryData<string> Names(Func<JObject, bool>? filter = null)
    {
        var data = new TheoryData<string>();
        foreach ((string name, JObject c) in Cases.Value)
        {
            if (filter is null || filter(c)) data.Add(name);
        }

        return data;
    }

    public static JObject Get(string name) => Cases.Value[name];

    public static IsoSpec Spec(JObject c)
    {
        JObject def = (JObject)c["spec"]!;
        IsoSpecBuilder b = IsoSpec.Legacy.ToBuilder();
        b.MtiEncoding = Encoding((string)def["mti"]!);
        b.BitmapEncoding = Encoding((string)def["bitmap"]!);
        b.LengthEncoding = Encoding((string)def["lenvar"]!);
        b.TpduLength = (int)def["tpdu"]!;
        foreach (JObject f in def["fields"]!.Cast<JObject>())
        {
            b.Field(
                (int)f["nr"]!,
                Enum.Parse<IsoLengthType>((string)f["format"]!, ignoreCase: true),
                Enum.Parse<IsoFieldContent>((string)f["attr"]!, ignoreCase: true),
                (int)f["len"]!,
                "F" + (int)f["nr"]!,
                (string)f["type"]! switch { "BCD" => IsoFieldEncoding.Bcd, "EBCDIC" => IsoFieldEncoding.Ebcdic, _ => IsoFieldEncoding.Ascii });
        }

        return b.Build();
    }

    public static Dictionary<int, string> Fields(JToken token) =>
        ((JObject)token).Properties().ToDictionary(p => int.Parse(p.Name, System.Globalization.CultureInfo.InvariantCulture), p => (string)p.Value!);

    public static IsoMessage Message(JObject c)
    {
        var message = new IsoMessage(Spec(c), (string)c["mti"]!) { Tpdu = (string)c["tpdu"]! };
        foreach ((int field, string value) in Fields(c["fields"]!)) message[field] = value;
        return message;
    }

    private static IsoEncoding Encoding(string value) => value == "BCD" ? IsoEncoding.Bcd : IsoEncoding.Ascii;
}
