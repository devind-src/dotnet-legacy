// Golden SyncNetPro.Hsm: JSON request persis seperti kelas internal SyncNet.HSM.HsmService (SDK lama).
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using SyncNet.HSM;

internal static class HsmGolden
{
    public static void Write(string outDir)
    {
        Directory.CreateDirectory(outDir);
        foreach (string f in Directory.GetFiles(outDir)) File.Delete(f);

        object Make(string type, params (string Name, string Value)[] values)
        {
            Type t = typeof(HsmService).GetNestedType(type, BindingFlags.NonPublic)!;
            object o = Activator.CreateInstance(t, nonPublic: true)!;
            foreach ((string name, string value) in values) t.GetProperty(name)!.SetValue(o, value);
            return o;
        }

        // Nomor rekening dihitung sama dengan HsmService.TranslatePinblock lama.
        static string Account(string pan) => !string.IsNullOrEmpty(pan) && pan.Length > 12 ? pan.Substring(pan.Length - 13, 12) : "";

        var cases = new Dictionary<string, string>
        {
            ["generate-key"] = JsonConvert.SerializeObject(Make("HsmGenKeyRequest", ("node_name", "BILLER_ABC"))),
            ["generate-key-terminal"] = JsonConvert.SerializeObject(Make("HsmGenKeyRequest", ("terminal_id", "TERM0001"))),
            ["translate-key"] = JsonConvert.SerializeObject(Make("HsmUpdateKeyRequest", ("node_name", "BILLER_ABC"), ("zpk_under_zmk", "U0123456789ABCDEF0123456789ABCDEF"))),
            ["translate-pinblock"] = JsonConvert.SerializeObject(Make("HsmTranslatePinblock", ("node_source", "CHANNEL"), ("node_dest", "BANK"), ("source_pinblock", "1A2B3C4D5E6F7A8B"), ("account_number", Account("4111111111111111")))),
            ["translate-pinblock-long-pan"] = JsonConvert.SerializeObject(Make("HsmTranslatePinblock", ("node_source", "CHANNEL"), ("node_dest", "BANK"), ("source_pinblock", "1A2B3C4D5E6F7A8B"), ("account_number", Account("6220123456789012345")))),
            ["translate-pinblock-short-pan"] = JsonConvert.SerializeObject(Make("HsmTranslatePinblock", ("node_source", "CHANNEL"), ("node_dest", "BANK"), ("source_pinblock", "1A2B3C4D5E6F7A8B"), ("account_number", Account("123456789012")))),
            ["translate-pinblock-terminal"] = JsonConvert.SerializeObject(Make("HsmTranslatePinblockTerminal", ("terminal_id", "TERM0001"), ("node_dest", "BANK"), ("source_pinblock", "1A2B3C4D5E6F7A8B"), ("account_number", Account("4111111111111111")))),
        };

        File.WriteAllText(Path.Combine(outDir, "hsm-requests.json"), JsonConvert.SerializeObject(cases, Formatting.Indented), new UTF8Encoding(false));
        Console.WriteLine($"{cases.Count} request HSM ditulis ke {outDir}");
    }
}
