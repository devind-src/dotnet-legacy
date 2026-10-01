# 10 — Cookbook

Resep singkat untuk kebutuhan yang sering muncul saat membangun interface dengan SyncNetSdkPro. Semua contoh
memakai API publik SDK (lihat [dok. 04 §6](04-arsitektur-sdk-baru.md#6-status-implementasi-fase-23)) dan dapat
langsung diuji dengan SimCore ([dok. 07](07-simcore.md)).

## 1. Sign-on, echo, dan key exchange (TCP)

```csharp
// Saat socket ke biller terhubung (pengganti TimerAutoSignon).
public override Task OnRemoteConnectedAsync(RemoteConnectionContext context, CancellationToken ct) =>
    context.Remote.SendAsync(NetworkMessages.SignOn(acquirerId).Pack(), ct);

// Interval dari sw_nodes (echo timer / key exchange timer, menit).
public override Task OnEchoTimerAsync(NodeContext context, CancellationToken ct) =>
    context.Remote.IsConnected ? context.Remote.SendAsync(NetworkMessages.Echo(acquirerId).Pack(), ct) : Task.CompletedTask;

public override Task OnKeyExchangeTimerAsync(NodeContext context, CancellationToken ct) => ...;
```

Balasan 0810 tidak perlu dikorelasikan: kembalikan `null` dari `GetRemoteCorrelationKey` untuk MTI 0810 sehingga
pesan masuk ke `OnRemoteMessageAsync` (lihat template `syncnet-outbound-iso`). Perintah `ECHO`/`SIGNON`/`KEYCHANGE`
dari command port masuk ke `OnNetworkCommandAsync`.

## 2. Request–response TCP dengan korelasi

```csharp
byte[] reply = await context.Remote.SendAndReceiveAsync(iso.Pack(), $"{iso[11]}|{iso[41]}", cancellationToken: ct);

public override string? GetRemoteCorrelationKey(RemoteConnectionInfo connection, ReadOnlySpan<byte> message) =>
    IsoMessage.TryParse(Spec, message, out IsoMessage? m, out _) ? $"{m![11]}|{m[41]}" : null;
```

- Kunci request dan kunci yang dihitung dari balasan **harus sama**; balasan tanpa pasangan → `OnRemoteMessageAsync`.
- `TimeoutException` setelah `request_timeout` node; kembalikan `null` dari `OnCoreRequestAsync` agar Core
  menandai timeout (dan mengirim reversal bila `auto_reversal`), atau balas kode sendiri.
- `RemoteUnavailableException` saat tidak ada koneksi → umumnya balas `ResponseCodes.LinkDown` (`89`).

## 3. TCP non-persistent (connect per transaksi)

Cukup konfigurasi: `sw_connections.always_connected = 0` (JSON: `"AlwaysConnected": false`). Kode handler sama
persis — `SendAndReceiveAsync` membuka socket, mengirim, mengambil balasan pertama, lalu menutup.

## 4. Header TCP: 2/4 byte, BCD, Lo-Hi, tanpa header

| Kebutuhan | Konfigurasi koneksi |
|-----------|---------------------|
| 2 byte biner Hi-Lo exclude (default) | `Protocol: Tcp2ByteExcludeHeader` |
| Include header | `Tcp2ByteIncludeHeader` / `Tcp4ByteIncludeHeader` |
| 4 byte | `Tcp4ByteExcludeHeader` |
| BCD | `HeaderFormat: Bcd` |
| Lo-Hi | `HeaderHighLow: false` |
| Framing khusus (delimiter, STX/ETX, panjang ASCII 6 digit, …) | `Protocol: TcpHeaderCustom` + override `CreateTcpCodec` |

```csharp
public override ITcpFrameCodec? CreateTcpCodec(RemoteConnectionInfo connection) => new StxEtxCodec();

sealed class StxEtxCodec : ITcpFrameCodec
{
    public byte[] Encode(ReadOnlySpan<byte> payload) => [0x02, .. payload, 0x03];

    public bool TryDecode(ref ReadOnlySequence<byte> buffer, out byte[] payload)
    {
        SequencePosition? end = buffer.PositionOf((byte)0x03);
        if (end is null) { payload = []; return false; }
        payload = buffer.Slice(1, buffer.GetOffset(end.Value) - buffer.GetOffset(buffer.Start) - 1).ToArray();
        buffer = buffer.Slice(buffer.GetPosition(1, end.Value));
        return true;
    }
}
```

SimCore: stub TCP dan perintah `syncnet-simcore tcp --header` menerima `Binary2Byte`, `Bcd2Byte`, `Binary4Byte`,
`Ascii4Digit`.

## 5. Spesifikasi ISO 8583 khusus

```csharp
public static IsoSpec Spec { get; } = IsoSpec.Legacy.ToBuilder()
    .Field(4, IsoLengthType.Fixed, IsoFieldContent.N, 12, "Amount", IsoFieldEncoding.Bcd)   // BCD
    .Field(48, IsoLengthType.LLLVar, IsoFieldContent.Ans, 999, "Additional Data")
    .Build();
```

`IsoSpec.Legacy` = tabel default SDK lama; cukup timpa field yang berbeda. Gunakan `IsoMessage.FormatTrace()` untuk
trace (PAN/PIN/track/ICC disamarkan). Salin spesifikasi yang sama ke `simcore/simcore.json` → `RemoteStubs[].Iso`
agar stub biller memakai format yang sama.

## 6. HTTP keluar: header, signature, method lain

```csharp
var request = new RemoteHttpRequest
{
    Method = "POST",
    Path = "/v2/payment?channel=syncnet",
    Body = json,
    ContentType = "application/json",
    Headers = new Dictionary<string, string>
    {
        ["X-Api-Key"] = context.Remote.Http.Info.WsKey ?? "",
        ["X-Signature"] = Convert.ToHexString(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(json))),
    },
    Timeout = TimeSpan.FromSeconds(20),
};
RemoteHttpResponse response = await context.Remote.Http.SendAsync(request, ct);
if (!response.IsSuccess) ...;
var body = response.ReadJson<BillerResponse>();
```

- Base URL = `ws_url`; `Path` boleh absolut. Tidak ada retry otomatis (aman untuk transaksi keuangan).
- Proxy dan sertifikat diatur dari `sw_connections` / `SyncNet:Remote:AllowUntrustedCertificates` (default validasi aktif).
- Lebih dari satu koneksi HTTP pada node: `context.Remote.GetConnection("NAMA_KONEKSI")`.

## 7. HTTP masuk: otentikasi dan balasan

```csharp
public override async Task<HttpReply> OnHttpRequestAsync(HttpRequestContext context, CancellationToken ct)
{
    if (!context.Headers.TryGetValue("X-Api-Key", out string? key) || key != context.Connection.Info.WsKey)
        return HttpReply.Json(new { rc = "X8" }, HttpStatusCode.Unauthorized);

    CoreResponse response = await context.SendToCoreAsync(Map(context.Body), ct);  // connection_name & ip_external otomatis
    return HttpReply.Json(new { rc = response.ResponseCode });
}
```

`HttpReply.Text(...)`, `HttpReply.Status(HttpStatusCode.RequestTimeout)` untuk balasan lain. Contoh lengkap
(path → tran_type, validasi, timeout `68`, duplikat `X2`, Core tidak tersedia `89`): template `syncnet-inbound-http`.

## 8. Timeout & respons terlambat dari Core

- `SendToCoreAsync` melempar `TimeoutException` setelah `request_timeout` node (+ margin `SyncNet:Core:ResponseTimeoutMargin`).
- Respons Core yang tiba setelah timeout masuk ke `OnUnmatchedCoreResponseAsync` — tempat mencatat atau
  mengirim notifikasi ke channel.
- Kunci korelasi default ke Core = `tran_type + datetime_tran + trace_number + terminal_id` (+ `connection_name`);
  ganti dengan `ICoreCorrelationKeyProvider` bila channel memakai kunci lain.

## 9. Data tambahan ke/dari Core

Format pesan ke Core tidak boleh berubah; data di luar skema dikirim lewat `additional_data`:

```csharp
response.SetAdditionalData("bill_info", text);
if (request.TryGetAdditionalData("customer_name", out string? name)) ...
```

`request.ToResponse(rc, message)` menyalin field request, mengisi `msgtype` response (`0200`→`0210`) dan
`pos_entry_mode`.

## 10. Trace, log, dan masking

```csharp
context.Trace.Message(context.Node.Name, TraceDirection.Outgoing, "REQ /payment", json, remoteAddress);
context.Trace.Info(context.Node.Name, "Sign-on", "OK");
context.Logger.LogWarning("Biller menolak {Trace}", request.TraceNumber);
```

- Trace hanya diantre (tidak memblok) dan diabaikan saat `TRACE OFF`; bila antrean penuh ditulis ke file fallback.
- Masking: `SensitiveData.Mask(value)`, `IsoMessage.FormatTrace()`; node dengan `sensitive_data = 1` disamarkan otomatis.
- Log memakai `ILogger<T>` standar → file harian di `SYNCNET_HOME` + diteruskan ke Log Services.

## 11. HSM

```csharp
builder.AddSyncNetHsm();                               // URL dari config Core / SyncNet:Hsm:Url
...
HsmResult result = await hsm.TranslatePinBlockAsync(...);
if (!result.IsSuccess) return request.ToResponse(result.ResponseCode, "HSM error", AuthorizedBy.Internal);
```

Saat pengembangan, SimCore menyediakan HSM tiruan (`SimCore.HsmUrl`, `HsmRequests`).

## 12. Routing biller & fee (channel)

`dotnet new syncnet-inbound-http --with-routing` atau manual:

```csharp
builder.AddSyncNetRouting();   // tabel sw_routes_* & Product > Fees; dimuat saat start dan RESYNC
```

`RoutingResolver.ResolveProductAsync` / `ResolveMarginAsync` memilih supplier (`private_data.sink_node`),
`ProductFeeCalculator.Calculate` / `MarginCalculator.Calculate` mengisi `fee_data`, dan `RecordResultAsync` mencatat
hasil untuk failover serta Volume & Tiering. Contoh lengkap: `Routing/RoutingStep.cs` di template.

## 13. Layanan saat start / RESYNC

```csharp
builder.Services.AddSingleton<ISyncNetModule, ProductCacheModule>();

sealed class ProductCacheModule(ProductCache cache) : ISyncNetModule
{
    public Task StartAsync(CancellationToken ct) => cache.LoadAsync(ct);   // sebelum kanal Core dibuka
    public Task ReloadAsync(CancellationToken ct) => cache.LoadAsync(ct);  // command RESYNC
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;      // setelah kanal ditutup
}
```

## 14. Test

- Mapping: unit test biasa terhadap kelas `Mapping/*`.
- End-to-end: `SimCore.StartAsync(options)` + `SimInterfaceHost.StartAsync<THandler>(core, connections, services: ...)`
  lalu `ScenarioRunner` (outbound), `HttpClient` (inbound HTTP), atau `SimTcpClient.SendAsync` (inbound TCP).
- Port acak: set `LogServicesPort = 0`, `Interface = null`, port node & stub `0` (lihat `InterfaceFlowTests` template).
- CI tanpa .NET test: `syncnet-simcore run -c simcore/simcore.json -s simcore/scenarios --report junit.xml`.
