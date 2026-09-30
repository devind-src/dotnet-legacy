// Generator golden file: seluruh output dihasilkan oleh kode SDK LAMA (SyncNetSdk) apa adanya.
// SDK baru (SyncNetPro.Contracts) diuji terhadap file ini di tests/SyncNetPro.Contracts.Tests.
using System.Text;
using Newtonsoft.Json;
using SyncNet.Library;
using SyncNet.Message;
using SyncNet.Networking;

string outDir = args.Length > 0
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "SyncNetPro.Contracts.Tests", "Golden"));

Directory.CreateDirectory(outDir);
foreach (string f in Directory.GetFiles(outDir)) File.Delete(f);

var utf8 = new UTF8Encoding(false);
var manifest = new List<object>();

void Write(string name, string kind, object message, string note)
{
    // persis seperti SinkNode.Reply / SourceNode.Send SDK lama
    string json = JsonConvert.SerializeObject(message);
    byte[] payload = utf8.GetBytes(json);
    byte[] frame = TcpHeader.AddTcpHeader(payload); // default Binary2Byte, Exclude, BigEndian

    File.WriteAllText(Path.Combine(outDir, name + ".json"), json, utf8);
    File.WriteAllBytes(Path.Combine(outDir, name + ".frame.bin"), frame);

    bool ascii = json.All(c => c < 0x80);
    if (ascii)
    {
        // Bukti bahwa untuk pesan ASCII, NbConvert.StringToBytes (SourceNode lama) == UTF-8
        byte[] legacy = NbConvert.StringToBytes(json);
        if (!legacy.AsSpan().SequenceEqual(payload)) throw new InvalidOperationException($"{name}: NbConvert != UTF-8");
    }

    manifest.Add(new { name, kind, ascii, note });
}

void WriteFrom(string name, Request req, string note)
{
    // Keputusan Q2: Response(Request) lama + msgtype response + pos_entry_mode dari request.
    var rsp = new Response(req)
    {
        msgtype = NbMessage.GetMsgTypeResp(req.msgtype),
        pos_entry_mode = req.pos_entry_mode,
    };

    File.WriteAllText(Path.Combine(outDir, name + ".request.json"), JsonConvert.SerializeObject(req), utf8);
    File.WriteAllText(Path.Combine(outDir, name + ".expected.json"), JsonConvert.SerializeObject(rsp), utf8);
    manifest.Add(new { name, kind = "response-from", ascii = true, note });
}

Request FullRequest()
{
    var r = new Request
    {
        pan = "6019001234567890",
        msgtype = "0200",
        tran_type = "INQ",
        tran_type_ext = "PLNPOST",
        from_acc_type = "10",
        to_acc_type = "20",
        currency = "360",
        amount_tran = 150000m,
        trace_number = "000123",
        datetime_tran = "0930101530",
        date_settle = "0930",
        merchant_type = "6012",
        merchant_id = "MERCHANT01",
        terminal_id = "TERM0001",
        acq_inst_id = "441",
        fwd_inst_id = "441",
        refnum = "627310000123",
        pos_entry_mode = "021",
        receiving_inst_id = "999",
        from_acc_number = "1234567890",
        to_acc_number = "532110000001",
        echo_data = new Dictionary<string, object> { ["channel"] = "MOBILE", ["session"] = 42 },
        temp_data = "tmp",
        original_data = "INQ0930101530000123",
    };
    r.additional_data["customer_id"] = "532110000001";
    r.additional_data["bill_count"] = 2;
    r.fee_data.total_fee = 2500m;
    r.fee_data.switch_fee = 500m;
    r.fee_data.biller_fee = 2000m;
    r.security.track2data = "6019001234567890=2912";
    r.security.pindata = "A1B2C3D4E5F60718";
    r.security.hsm_cmd = Security.EnumHsmCommand.TranslatePinblock;
    r.private_data.sink_node = "BILLER_ABC";
    r.private_data.retry_send = 1;
    return r;
}

// ---------------- Request ----------------
Write("request-empty", "request", new Request(), "Request kosong (default konstruktor)");
Write("request-full-inquiry", "request", FullRequest(), "Semua field terisi, ASCII");

var dec = FullRequest();
dec.amount_tran = 10000.50m;
dec.fee_data.total_fee = 0.01m;
dec.fee_data.acquirer_fee = -500m;
dec.fee_data.merchant_fee = 999999999999.99m;
dec.fee_data.issuer_fee = 1.000m;
dec.virtual_account.amount = 1m;
dec.virtual_account.balance = 79228162514264337593543950335m;
Write("request-decimals", "request", dec, "Variasi decimal: pecahan, negatif, besar, skala, decimal.MaxValue");

var types = FullRequest();
types.additional_data.Clear();
types.additional_data["s"] = "text";
types.additional_data["i"] = 7;
types.additional_data["l"] = 9007199254740993L;
types.additional_data["b"] = true;
types.additional_data["n"] = null;
types.additional_data["d"] = 12.5m;
types.additional_data["dbl"] = 0.1d;
types.additional_data["arr"] = new object[] { 1, "two", false };
types.additional_data["obj"] = new Dictionary<string, object> { ["k"] = "v", ["nested"] = new Dictionary<string, object> { ["x"] = 1 } };
types.additional_data["date"] = "2026-09-30T10:15:30";
types.echo_data = new object[] { "a", 1 };
Write("request-additional-data-types", "request", types, "Tipe nilai additional_data & echo_data array");

var echoStr = FullRequest();
echoStr.echo_data = "ECHO-STRING";
echoStr.temp_data = null;
Write("request-echo-string", "request", echoStr, "echo_data string, temp_data null");

var special = FullRequest();
special.additional_data["text"] = "quote\" backslash\\ newline\n tab\t <html>&'";
special.merchant_id = "M&M's <01>";
Write("request-special-chars", "request", special, "Escape karakter khusus JSON");

var unicode = FullRequest();
unicode.additional_data["customer_name"] = "Budi Śantoso – 日本 ✓";
Write("request-non-ascii", "request", unicode, "Non-ASCII: SDK lama rusak/exception di SourceNode; SDK baru UTF-8 (bug B4 diperbaiki)");

var inbound = FullRequest();
inbound.private_data.connection_name = "CHANNEL_CONN_1";
inbound.private_data.ip_external = "10.20.30.40";
inbound.security.hsm_cmd = Security.EnumHsmCommand.TranslatePinblockTerminal;
inbound.security.is_pin_change = true;
inbound.security.is_debet_tran = false;
inbound.virtual_account.enable = true;
inbound.virtual_account.acc_number = "VA0001";
Write("request-inbound-private-data", "request", inbound, "private_data inbound, security & virtual_account terisi");

var nulls = new Request { additional_data = null, fee_data = null, security = null, private_data = null, virtual_account = null };
Write("request-null-objects", "request", nulls, "Sub-objek null");

// ---------------- Response ----------------
Write("response-empty", "response", new Response(), "Response kosong (authorized_by default \"1\")");

var ok = new Response(FullRequest())
{
    msgtype = "0210",
    additional_amount = "C000000150000",
    resp_code = "00",
    resp_message = "Approved",
};
ok.additional_data["customer_name"] = "BUDI";
Write("response-success", "response", ok, "Response sukses dari biller");

var internalRsp = new Response(FullRequest())
{
    resp_code = "A1",
    resp_message = "Transaction is not supported",
    authorized_by = "0",
};
Write("response-internal", "response", internalRsp, "Response yang diputuskan interface (authorized_by internal)");

// ---------------- CoreResponse.From (keputusan Q2) ----------------
foreach (var (name, mti) in new[]
{
    ("from-0200", "0200"), ("from-0201", "0201"), ("from-0220", "0220"), ("from-0221", "0221"),
    ("from-0400", "0400"), ("from-0401", "0401"), ("from-0800", "0800"), ("from-0210", "0210"),
    ("from-null-mti", null), ("from-short-mti", "02"),
})
{
    var req = FullRequest();
    req.msgtype = mti;
    WriteFrom(name, req, $"CoreResponse.From dengan msgtype {mti ?? "null"}");
}

// ---------------- MTI ----------------
var mtis = new[] { "0100", "0101", "0110", "0120", "0121", "0200", "0201", "0210", "0220", "0221", "0230", "0400", "0401", "0420", "0421", "0500", "0800", "0801", "0810", "0820", "9999", "02", "02000", "" };
var mtiMap = mtis.ToDictionary(m => m, NbMessage.GetMsgTypeResp);
File.WriteAllText(Path.Combine(outDir, "mti.json"), JsonConvert.SerializeObject(mtiMap, Formatting.Indented), utf8);

File.WriteAllText(Path.Combine(outDir, "manifest.json"), JsonConvert.SerializeObject(new
{
    generator = "tools/SyncNetPro.GoldenGenerator",
    source = "SyncNetSdk (SDK lama) — JsonConvert.SerializeObject + TcpHeader.AddTcpHeader default",
    cases = manifest,
}, Formatting.Indented), utf8);

Console.WriteLine($"{manifest.Count} kasus golden ditulis ke {outDir}");
