# 04 — Arsitektur SDK Baru (SyncNetSdkPro)

## 1. Prinsip Desain

1. **Kontrak pesan dibekukan, API bebas didesain ulang** — hanya dok. 02 yang wajib kompatibel.
2. **Pakai fondasi .NET standar** (Generic Host, DI, Options, Logging, HttpClientFactory, Resilience, Pipelines) — developer .NET mana pun langsung familiar; tidak ada infrastruktur buatan sendiri bila BCL sudah menyediakan.
3. **Override-what-you-need** — handler berbasis base class dengan default yang aman.
4. **Request/response adalah `await`**, bukan pasangan callback + cache manual.
5. **Tidak ada state statis** — semua lewat DI, sehingga bisa diuji dan di-host lebih dari satu.
6. **Dapat berjalan tanpa Core** — sumber konfigurasi & kanal Core bisa diganti SimCore/JSON.
7. **Cross-platform by default** — tidak ada percabangan `if (Windows)` di kode SDK (dok. 05).
8. **Inti kecil, modul opsional** — ISO 8583, HSM, routing/fee, toolkit pembayaran dipisah paket.
9. **Skema pesan tetap** — tidak ada properti JSON baru ke Core; data tambahan melalui `additional_data` (Q1).

## 2. Teknologi

| Area | Pilihan | Alasan |
|------|---------|--------|
| Runtime | **.NET 10 (LTS)**, C# 14, `<Nullable>enable</Nullable>` | Sama dengan SDK lama (tanpa lompatan runtime), dukungan LTS, nullable mencegah NRE |
| Hosting | `Host.CreateApplicationBuilder` / `WebApplication.CreateSlimBuilder` + `UseSystemd()` + `UseWindowsService()` | Satu host untuk TCP, HTTP, timer, log; service di Linux (systemd) & Windows |
| Konfigurasi | `Microsoft.Extensions.Configuration` + `IOptions<T>` + validasi `ValidateOnStart` / `[OptionsValidator]` | Sumber berlapis: appsettings, env var, config Core, DB |
| Logging | `ILogger<T>` + provider SDK (file bergulir, Log Services, RabbitMQ) | Structured logging, satu API |
| Observabilitas | OpenTelemetry (`ActivitySource`, `Meter`): latency per node, jumlah pending, koneksi, timeout | Siap Prometheus/OTLP; opsional |
| Health | `Microsoft.Extensions.Diagnostics.HealthChecks` (koneksi Core, koneksi remote, DB) | Untuk systemd/container/monitoring |
| TCP | `System.IO.Pipelines` + `Socket` | Framing efisien tanpa alokasi berulang, backpressure |
| HTTP server | ASP.NET Core Minimal API / Kestrel di host yang sama | Menggantikan `XKestrel` |
| HTTP client | `IHttpClientFactory` + `Microsoft.Extensions.Http.Resilience` | Pooling, proxy, timeout, retry terkontrol |
| Serializer kontrak Core | Newtonsoft.Json di balik `ICoreMessageSerializer` (1.x), `System.Text.Json` source-gen direncanakan di 2.x setelah golden test stabil (Q3) | Kompatibilitas wire (dok. 02 §7) |
| DB | Npgsql `NpgsqlDataSource` + Dapper, async-only | Konfigurasi node/koneksi & status |
| Antrean internal | `System.Threading.Channels` | Dispatch pesan per node dengan batas konkurensi |
| Test | xUnit v3, `Microsoft.Extensions.TimeProvider.Testing`, Verify (snapshot/golden), Testcontainers (PostgreSQL, opsional) | Unit, golden, integrasi |
| Build & paket | SDK-style, Central Package Management (`Directory.Packages.props`), `global.json`, SourceLink, NuGet + `.snupkg` dipublikasikan ke **GitHub Packages** (Q4) oleh GitHub Actions, versi SemVer via MinVer; **tanpa obfuscation** (Q5) | Reproducible, dapat di-debug |
| CI | GitHub Actions matriks `ubuntu-latest` + `windows-latest` | Bukti cross-platform di setiap PR |

## 3. Struktur Solusi

```
SyncNetSdkPro/
├── docs/                              ← dokumen analisa (folder ini)
├── SyncNetSdkPro.slnx
├── global.json                         ← pin .NET SDK 10.0.x
├── Directory.Build.props               ← Nullable, LangVersion, TreatWarningsAsErrors, SourceLink
├── Directory.Packages.props            ← versi paket terpusat
├── src/
│   ├── SyncNetPro.Contracts/           ← CoreRequest/CoreResponse & sub-objek, TranType, AuthorizedBy, serializer
│   ├── SyncNetPro.Sdk/                 ← inti: host builder, handler, kanal Core, koneksi remote, command, trace
│   ├── SyncNetPro.Sdk.Testing/         ← SimCore in-process + fake remote untuk unit test interface
│   ├── SyncNetPro.Iso8583/             ← packer/unpacker ISO 8583 (port dari IsoMessage/*)
│   ├── SyncNetPro.Hsm/                 ← klien SyncNetHsm
│   ├── SyncNetPro.Routing/             ← port Routing/* & Fees/* — .dll terpisah, versi independen (Q6)
│   └── SyncNetPro.Toolkit/             ← masking, hex/BCD, TLV EMV/QRIS, crypto DES/AES/hash
├── tools/
│   └── SyncNetPro.SimCore/             ← simulator core (CLI + Web UI) — dok. 07
├── templates/
│   └── SyncNetPro.Templates/           ← paket `dotnet new` — dok. 06
├── samples/
│   ├── Sample.BillerIso/               ← padanan ApiBillerIso dengan SDK baru
│   ├── Sample.BillerJson/              ← padanan ApiBillerJson
│   └── Sample.Channel/                 ← padanan ApiChannel
└── tests/
    ├── SyncNetPro.Contracts.GoldenTests/  ← byte-per-byte vs SDK lama
    ├── SyncNetPro.Sdk.Tests/
    └── SyncNetPro.Iso8583.Tests/
```

Namespace root **`SyncNetPro`** (bukan `SyncNet`) sehingga interface lama dan baru tidak pernah
bentrok nama, dan satu mesin dapat menjalankan keduanya berdampingan.

## 4. Model Pemrograman

### 4.1 Tipe publik yang perlu dipelajari developer baru

| Tipe | Fungsi |
|------|--------|
| `SyncNetInterface` | Base class handler; override method `On…Async` yang dibutuhkan |
| `CoreRequest` / `CoreResponse` | Kontrak pesan Core (dok. 02) |
| `CoreRequestContext` | Request dari Core (outbound) + `Remote`, `Node`, `Trace`, `ReplyAsync` |
| `HttpRequestContext` / `HttpReply` | Request HTTP masuk (inbound) & balasannya |
| `RemoteMessageContext` | Pesan TCP masuk yang tidak dikorelasikan (mis. request dari bank, 0800) |
| `ICoreClient` | Kirim request ke Core (inbound) → `Task<CoreResponse>` |
| `IRemoteConnection` | Kirim ke eksternal via TCP: `SendAsync`, `SendAndReceiveAsync` |
| `IRemoteHttpClient` | Kirim ke eksternal via HTTP → `Task<RemoteHttpResponse>` |
| `ITraceWriter` | Trace ke Log Services (otomatis cek TRACE ON/OFF, masking PAN) |
| `ResponseCodes` | Kode respons umum (`NotSupported = "A1"`, `LinkDown = "89"`, …) |

### 4.2 `Program.cs` (seluruh isi)

```csharp
var builder = SyncNetApplication.CreateBuilder(args);   // host + systemd/windows service + config berlapis

builder.AddSyncNetInterface<BillerIsoInterface>(options =>
{
    options.AppName = "API Biller";                      // = sw_app.app_name
});

builder.Services.AddIso8583<BillerIsoSpec>();           // modul opsional

await builder.Build().RunAsync();
```

### 4.3 Outbound TCP/ISO (padanan `ApiBillerIso`)

```csharp
public sealed class BillerIsoInterface(IsoMapper mapper) : SyncNetInterface
{
    public override async Task<CoreResponse> OnCoreRequestAsync(CoreRequestContext ctx, CancellationToken ct)
    {
        IsoMessage? isoRequest = ctx.Request.TranType switch
        {
            TranType.Inquiry  => mapper.ToInquiry(ctx.Request),
            TranType.Payment  => mapper.ToPayment(ctx.Request),
            TranType.Advice   => mapper.ToAdvice(ctx.Request),
            TranType.Reversal => mapper.ToReversal(ctx.Request),
            _ => null
        };

        if (isoRequest is null)
            return ctx.Request.ToResponse(ResponseCodes.NotSupported, "Transaction is not supported", AuthorizedBy.Internal);

        // korelasi, timeout, trace keluar-masuk, persistent / non-persistent: ditangani SDK
        IsoMessage isoResponse = await ctx.Remote.SendAndReceiveAsync(isoRequest, ct);

        return mapper.ToCoreResponse(isoResponse, ctx.Request);
    }

    public override Task OnRemoteConnectedAsync(ConnectionContext ctx, CancellationToken ct)
        => ctx.Remote.SendAsync(IsoNetwork.SignOn(), ct);          // auto sign-on

    public override Task OnEchoTimerAsync(NodeContext ctx, CancellationToken ct)
        => ctx.Remote.SendAsync(IsoNetwork.Echo(), ct);
}
```

### 4.4 Outbound HTTP/JSON (padanan `ApiBillerJson`)

```csharp
public override async Task<CoreResponse> OnCoreRequestAsync(CoreRequestContext ctx, CancellationToken ct)
{
    var (path, body) = BillerMapper.ToRemote(ctx.Request);        // "/bill/inquiry", DTO

    RemoteHttpResponse rsp = await ctx.Remote.Http.PostJsonAsync(path, body, ct);   // base URL = sw_connections.ws_url

    return BillerMapper.ToCore(rsp.ReadJson<BillerResponse>(), ctx.Request);
}
```

### 4.5 Inbound HTTP (padanan `ApiChannel`)

```csharp
public override async Task<HttpReply> OnHttpRequestAsync(HttpRequestContext ctx, CancellationToken ct)
{
    if (!auth.IsValid(ctx)) return HttpReply.Json(ChannelReply.AuthFailed());

    CoreRequest? request = ctx.Path switch
    {
        "/inquiry" => ChannelMapper.Inquiry(ctx.Body),
        "/payment" => ChannelMapper.Payment(ctx.Body),
        _ => null
    };
    if (request is null) return HttpReply.Json(ChannelReply.InvalidUrl());

    CoreResponse response = await ctx.Core.SendAsync(request, ct);   // korelasi & timeout otomatis
    return HttpReply.Json(ChannelMapper.ToChannel(ctx.Body, response));
}
```

## 5. Komponen Runtime

```
┌──────────────────────────── SyncNetPro Host (1 proses) ─────────────────────────────┐
│                                                                                       │
│  NodeConfigurationSource ──► NodeRegistry ──► (per node)                              │
│   (Postgres | JSON | SimCore)        │                                               │
│                                      ├─ CoreChannel[Outbound] ◄─TCP─► Core port_out    │
│                                      ├─ CoreChannel[Inbound]  ◄─TCP─► Core port_in     │
│                                      └─ RemoteConnection(s)  ◄─TCP/HTTP─► Eksternal    │
│                                                                                       │
│  MessageDispatcher (Channels, konkurensi per node) ──► SyncNetInterface (kode dev)    │
│  PendingRequestStore (korelasi + timeout, TimeProvider)                               │
│  NodeTimerService (echo, key exchange, status 1 mnt)   CommandServer (command_port)   │
│  TraceWriter ─► ITraceSink (Log Services TCP | RabbitMQ | File)   ILogger providers   │
│  NodeStatusReporter (sw_app, sw_nodes, sw_connections)   HealthChecks   OpenTelemetry │
└───────────────────────────────────────────────────────────────────────────────────────┘
```

### 5.1 Korelasi request/response

- **Ke Core (inbound)**: kunci default = `tran_type + datetime_tran + trace_number + terminal_id`
  (sama dengan kunci Core, dok. 02 §3) + `connection_name`. Dapat diganti via
  `ICoreCorrelationKeyProvider` (mis. `ApiChannel` memakai `merchant_id`).
  Timeout = `request_timeout`/`advice_timeout` node + margin, lalu `TimeoutException` → handler
  memutuskan balasan ke channel.
- **Ke remote TCP**: `IRemoteCorrelationKeyProvider<TMessage>` (default ISO: MTI 2 digit pertama + DE11 + DE41,
  seperti `ApiBillerIso`). Pesan masuk yang cocok dengan pending → menyelesaikan `await`;
  yang tidak cocok → `OnRemoteMessageAsync`.
- **Ke remote HTTP**: korelasi alami per request.
- Pending disimpan di `ConcurrentDictionary` + expiry berbasis `TimeProvider` (dapat diuji
  dengan waktu palsu); duplikat kunci → `DuplicateRequestException`.

### 5.2 Framing TCP

`ITcpFrameCodec` dengan implementasi bawaan:

| Codec | Setara lama |
|-------|-------------|
| `LengthPrefixCodec(Binary2Byte, BigEndian/LittleEndian, Include/Exclude)` | `TCP2Byte*` biner |
| `LengthPrefixCodec(Bcd2Byte, …)` | header BCD 2 byte |
| `LengthPrefixCodec(Ascii4Digit / Binary4Byte, …)` | `TCP4Byte*` |
| `NoHeaderCodec` (idle-timeout / delimiter / fixed length) | `TCPHeaderNone` (sekarang benar-benar didukung) |
| Kustom (developer implement `ITcpFrameCodec`) | `TCPHeaderCustom` |

Nilai kolom DB (`protocol`, `tcp_header_format`, `tcp_hi_lo`) dipetakan ke opsi codec secara
eksplisit dan diuji per kombinasi, termasuk mode non-persistent (perbaikan B1, B2).

### 5.3 Konkurensi & ketahanan

- Setiap pesan dari Core didispatch ke handler secara paralel dengan batas
  `MaxConcurrentRequestsPerNode` (default 100); backpressure lewat bounded `Channel`.
- Exception di handler ditangkap per pesan, dicatat dengan konteks (node, trace number),
  dan **tidak** menjatuhkan koneksi.
- Reconnect memakai backoff + jitter; status koneksi dilaporkan ke DB hanya saat berubah
  (+ heartbeat 1 menit, seperti perilaku lama).
- Graceful shutdown: berhenti menerima pesan baru, tunggu pending hingga batas waktu, update status `DOWN`.

### 5.4 Konfigurasi (contoh `appsettings.json` interface)

```json
{
  "SyncNet": {
    "AppName": "API Biller",
    "Home": null,
    "NodeSource": "Database",
    "Core": { "Host": "127.0.0.1" },
    "Trace": { "Enabled": true, "Sink": "LogServices", "MaskPan": true },
    "Remote": { "AllowUntrustedCertificate": false }
  }
}
```

- `Home` null → `SYNCNET_HOME` env → default per OS (dok. 05). Konfigurasi Core
  (`{Home}/Core/Bin/appsettings.json`, `Resources.bin`, `Keys/PrivateKey.pem`) tetap dibaca
  dengan format lama oleh `AddSyncNetCoreConfiguration()`, sehingga **tidak ada konfigurasi
  baru di server produksi**.
- `NodeSource: "Json"` + bagian `Nodes` → untuk dev/SimCore tanpa database.
- Semua nilai dapat dioverride env var (`SyncNet__AppName=...`).

### 5.5 Keamanan

- Validasi sertifikat TLS aktif secara default (perbaikan B6).
- Masking PAN/track2 di trace secara default bila `sensitive_data = 1` pada node.
- Rahasia (password DB, key) tidak pernah ditulis ke log; dekripsi memakai `IConfigProtector`.
- Command port dapat dibatasi ke alamat bind tertentu (`CommandServer:BindAddress`), default perilaku lama (`Any`).

## 6. Status Implementasi (Fase 2–3)

API yang **sudah tersedia** di `SyncNetPro.Sdk`. Catatan terhadap contoh §4.3–4.5: pada fase 3 `SendAndReceiveAsync`
bekerja dengan `byte[]` + kunci korelasi (varian bertipe `IsoMessage` menyusul bersama modul ISO di fase 5), dan
`OnHttpRequestAsync` memakai `ctx.SendToCoreAsync(request)` yang mengisi `connection_name`/`ip_external` otomatis.

| Area | API |
|------|-----|
| Host | `SyncNetApplication.CreateBuilder(args)` (systemd/Windows Service otomatis), `builder.AddSyncNetInterface<THandler>(o => ...)` |
| Handler | `SyncNetInterface`: `OnCoreRequestAsync` → `Task<CoreResponse?>` (`null` = tidak membalas), `OnUnmatchedCoreResponseAsync`, `OnNetworkCommandAsync`, `OnConfigurationReloadedAsync`, `OnStartedAsync`, `OnStoppingAsync` |
| Konteks | `CoreRequestContext` (`Node`, `Request`, `ReplyAsync`, `Core`, `Trace`, `Logger`, `Services`), `CoreResponseContext`, `NetworkCommandContext` |
| Core inbound | `ICoreClient.SendAsync(node, request, new CoreSendOptions { ConnectionName, RemoteAddress, Timeout })` → `CoreResponse`; exception `CoreUnavailableException`, `DuplicateCoreRequestException`, `TimeoutException`; kunci korelasi dapat diganti via `ICoreCorrelationKeyProvider` |
| Trace | `ITraceWriter.Message(node, TraceDirection, title, content, remote)`, `Info(...)` — sinkron (hanya antre), otomatis diabaikan saat TRACE OFF; `SensitiveData.Mask` |
| Log | `ILogger<T>` standar → file harian + diteruskan ke Log Services; `logger.BeginNodeScope(node)` |
| Node | `INodeRegistry` (`TryGetNode`, `GetConnections`, `Current`), `NodeInfo`, `RemoteConnectionInfo`, `ISyncNetRuntime.ReloadAsync` |
| Remote (fase 3) | `ctx.Remote` (`IRemoteNode`): `SendAsync`, `SendAndReceiveAsync(payload, key, timeout)`, `Tcp`, `Http`, `GetConnection(name)`; `IRemoteHttpClient.SendAsync(RemoteHttpRequest)` → `RemoteHttpResponse` (`RemoteHttpRequest.Json(path, body)`, `ReadJson<T>()`); `IRemoteRegistry` |
| Handler remote (fase 3) | `OnRemoteMessageAsync(RemoteMessageContext)` (+ `ReplyAsync`, `SendToCoreAsync`), `OnHttpRequestAsync(HttpRequestContext)` → `HttpReply` (+ `SendToCoreAsync`), `GetRemoteCorrelationKey`, `CreateTcpCodec`, `OnRemoteConnectedAsync`, `OnRemoteDisconnectedAsync`, `OnAutoSignOnAsync`, `OnEchoTimerAsync`, `OnKeyExchangeTimerAsync` |
| Transport | `LengthPrefixCodec`, `TcpFrameClient`, `TcpFrameServer`, `FramedConnection` (dipakai ulang oleh transport remote fase 3) |

Kunci konfigurasi (`appsettings.json`, bagian `SyncNet`): `AppName`, `Version`, `Home`, `NodeSource` (`Database`/`Json`),
`Nodes[]`, `Connections[]`, `Core:{Host,ReconnectDelay,ResponseTimeoutMargin}`, `Command:{Enabled,BindAddress,Port}`, `Remote:{AllowUntrustedCertificates,AutoSignOnDelay,ConnectTimeout,StatusInterval}`,
`Trace:{Enabled,Sink,LogServicesHost,LogServicesPort,QueueCapacity}`, `Logging:{Enabled,Directory,TraceDirectory,ForwardToTrace,LegacyWindowsFileNames}`,
`Database:{ConnectionString,ReportStatus}`, `MaxConcurrentRequestsPerNode`.

## 7. Koeksistensi dengan SDK Lama

| Aspek | SDK lama | SDK baru |
|-------|----------|----------|
| Paket / assembly | `SyncNetSdk.dll` | `SyncNetPro.*` (NuGet, GitHub Packages) |
| Namespace | `SyncNet.*` | `SyncNetPro.*` |
| Wire ke Core, command, log, DB status | — | **identik** |
| Interface pemakai | `ApiInterfaces/*` (tidak diubah) | Interface baru dari template |

Core tidak perlu tahu sebuah interface dibangun dengan SDK lama atau baru. Satu node Core
hanya dilayani satu proses interface pada satu waktu (sama seperti sekarang).
