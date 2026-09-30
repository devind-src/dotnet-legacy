# 03 — Inventaris & Pemetaan API (Lama → Baru)

Keterangan kolom **Keputusan**:

- **PERTAHANKAN** — fungsi tetap ada, boleh dirapikan.
- **GANTI NAMA** — fungsi sama, nama/signature distandarkan.
- **GABUNG** — beberapa overload/kelas digabung menjadi satu API.
- **GANTI** — diganti mekanisme modern (framework .NET) dengan fungsi setara.
- **MODUL** — dipindah ke paket opsional terpisah (bukan inti SDK).
- **HAPUS** — usang/tidak dipakai; tidak dibuat di SDK baru.

## 1. Standar Penamaan SDK Baru

| Aturan | Contoh |
|--------|--------|
| Bahasa Inggris, istilah domain konsisten: **Core**, **Remote** (sistem eksternal), **Node**, **Connection** | `ReplyToCoreAsync`, `RemoteConnection` |
| Method async wajib sufiks `Async` dan menerima `CancellationToken` terakhir | `SendAsync(msg, ct)` |
| Parameter & variabel lokal `camelCase`; properti/method `PascalCase` | `nodeName` bukan `NodeName` untuk parameter |
| Tanpa prefiks tipe/Hungarian (`Nb*`, `X*`, `Ws*`, `b*`, `_Tmr*`) | `NbConvert` → `Hex`, `XTcpClient` → `TcpClientConnection` |
| Handler/callback diawali `On…Async` dan menerima **satu objek konteks** | `OnCoreRequestAsync(CoreRequestContext ctx)` |
| Flag konfigurasi `bool`/`enum`, bukan string `"0"/"1"` atau `byte` | `AlwaysConnected: true`, `TcpHeaderType.Binary2Byte` |
| Arah pesan dinamai dari sudut pandang interface: *Inbound* = eksternal → Core, *Outbound* = Core → eksternal | `InboundHandler`, `OutboundHandler` |
| Interface diawali `I`, opsi diakhiri `Options`, konteks diakhiri `Context` | `ISyncNetInterface`, `RemoteTcpOptions`, `CoreRequestContext` |
| Istilah Core lama dipetakan jelas di XML doc: `Sink` = kanal outbound, `Source` = kanal inbound | `/// <remarks>Core: SinkNode (port_out)</remarks>` |
| Satu nama untuk satu konsep (tidak ada `Logger`/`WriteLog`/`LogAsync` bersamaan) | `ILogger<T>` + `ITraceWriter` |

## 2. `IAppProcessor` (callback) → handler baru

| Lama | Keputusan | Baru |
|------|-----------|------|
| `ProcessMsgFromSinkNode(NodeName, Request)` | GANTI NAMA | `OutboundHandler.OnCoreRequestAsync(CoreRequestContext ctx)` — balasan cukup `return CoreResponse` atau `ctx.ReplyAsync(...)` |
| `ProcessMsgFromSourceNode(NodeName, ConnectionName, Response)` | GANTI | Tidak perlu callback: `var rsp = await ctx.Core.SendAsync(request, ct)` pada inbound handler (korelasi otomatis). Tetap tersedia `InboundHandler.OnUnmatchedCoreResponseAsync` untuk respons terlambat. |
| `ProcessMsgFromRemoteTcp(NodeName, ConnectionName, byte[], int TotalBytes, EndPoint)` | GANTI NAMA + GABUNG | `OnRemoteMessageAsync(RemoteMessageContext ctx)` untuk pesan yang **bukan** balasan dari `SendAndReceiveAsync` (mis. request masuk dari bank, 0800). `TotalBytes` dihapus (sama dengan `Length`). |
| `ProcessMsgFromRemoteWsServer(NodeName, ConnectionName, string, HttpContext)` | GANTI NAMA | `InboundHandler.OnHttpRequestAsync(HttpRequestContext ctx)` → mengembalikan `HttpReply` (status, body, header). Tidak ada lagi cache `HttpContext`. |
| `ProcessMsgFromRemoteWsClient(NodeName, ConnectionName, Request, string, HttpStatusCode)` | GANTI | `var reply = await ctx.Remote.Http.SendAsync(httpRequest, ct)` — hasil langsung di-`await`. |
| `OnError(NodeName, ConnectionName, Request, string)` | GANTI | Exception dilempar ke pemanggil `await`; hook global `OnErrorAsync(ErrorContext)` opsional. |
| `TimerAutoSignon(NodeName)` | GANTI NAMA | `OnRemoteConnectedAsync(ConnectionContext ctx)` (auto sign-on = logika di sini bila `AutoSignOn` aktif) |
| `TimerEcho(NodeName)` | GANTI NAMA | `OnEchoTimerAsync(NodeContext ctx)` |
| `TimerKeyExchange(NodeName)` | GANTI NAMA | `OnKeyExchangeTimerAsync(NodeContext ctx)` |
| `Resync()` | GANTI NAMA | `OnConfigurationReloadedAsync(ReloadContext ctx)` |
| `NetworkManagement(EnumNtwrkMgmt, NodeName, Param)` | GANTI NAMA | `OnNetworkCommandAsync(NetworkCommandContext ctx)` dengan `ctx.Command` bertipe `NetworkCommand { Echo, SignOn, SignOff, KeyChange, Other }` |

Semua method di atas adalah **virtual dengan implementasi default** (log “not supported”/no-op, atau
untuk `OnCoreRequestAsync` default balas `A1 Transaction is not supported`) — developer hanya
meng-override yang dibutuhkan.

## 3. `AppProcessor` (API yang dipanggil interface)

| Lama | Keputusan | Baru |
|------|-----------|------|
| `new AppProcessor(AppName, Version, this)` | GANTI | `builder.Services.AddSyncNetInterface<THandler>(o => o.AppName = …)`; versi dari `AssemblyInformationalVersion` |
| `AppStart(ct)` / `AppStop()` | GANTI | Lifecycle `IHostedService` (otomatis via Generic Host) |
| `GetNode(NodeName, out NodeRemote)` / `GetNode(NodeName, ConnName, out …)` / `GetNodeRemote(…)` ×2 | GABUNG | `INodeRegistry.TryGetNode(nodeName, out NodeInfo)`, `INodeRegistry.GetConnections(nodeName)` — mengembalikan objek **read-only** `NodeInfo`/`RemoteConnectionInfo` (tanpa membuat socket baru; memperbaiki B7) |
| `IsConnected(NodeName)` / `IsConnected(NodeName, ConnName)` | GABUNG | `IRemoteConnection.IsConnected` / `ctx.Remote.IsConnected` |
| `Resync()` | GANTI NAMA | `ISyncNetRuntime.ReloadAsync(ct)` (juga dipicu command `RESYNC`) |
| `ResetTcp(NodeName)` | GANTI NAMA | `IRemoteConnection.ResetAsync(ct)` |
| `SendToTcp(NodeName, bytes)` / `SendToTcp(NodeName, ConnName, bytes)` | GABUNG | `IRemoteConnection.SendAsync(ReadOnlyMemory<byte>, ct)` |
| *(tidak ada — ditulis manual di interface)* | BARU | `IRemoteConnection.SendAndReceiveAsync(payload, correlationKey, timeout, ct)` (persistent & non-persistent) |
| `ReplyToTcp(NodeName, ConnName, bytes, EndPoint)` | GANTI NAMA | `RemoteMessageContext.ReplyAsync(payload, ct)` (endpoint dibawa konteks) |
| `ReplyToHttpServer(…, string, HttpContext)` / `(…, HttpStatusCode, HttpContext)` | GANTI | Nilai kembali `HttpReply` dari `OnHttpRequestAsync` (`HttpReply.Json(body)`, `HttpReply.Status(408)`) |
| `ReplyToSink(NodeName, Response)` | GANTI NAMA | `CoreRequestContext.ReplyAsync(CoreResponse, ct)` atau nilai kembali handler |
| `SendToSource(NodeName, ConnName, HttpContext, Request)` / `(…, string IPEndPoint, Request)` | GABUNG | `ICoreClient.SendAsync(CoreRequest, ct)` → `Task<CoreResponse>`; `connection_name`/`ip_external` diisi otomatis dari konteks |
| `SendToHttpClient(NodeName, Request, string, WebHeaderCollection)` / `(…, Parameter, …)` | GABUNG + GANTI | `IRemoteHttpClient.SendAsync(RemoteHttpRequest, ct)` → `Task<RemoteHttpResponse>` (path/query, header `IDictionary`, method, body); berbasis `IHttpClientFactory` |
| `SetTrace(EnumStatus)` / `IsTraceOn()` (static) | GANTI | `ITraceWriter.IsEnabled` (instance), dikendalikan command `TRACE ON/OFF` |
| `WriteTrace(NodeName, Title, Binary, EnumFromTo, RemoteAddress)` | GANTI NAMA | `ITraceWriter.TraceAsync(TraceDirection.Outgoing, title, content, ct)` — node & remote address diambil dari konteks; otomatis dilewati bila trace off |
| `WriteTraceStatus(NodeName, Info, Detail)` | GANTI NAMA | `ITraceWriter.InfoAsync(title, detail, ct)` |
| `WriteLog(…)` ×3, `Logger(…)` ×3 (static) | GANTI | `ILogger<T>` standar (+ sink ke file/Log Services lewat provider SDK) |
| Konstanta `CMD_*`, `APPNAME` | HAPUS dari API publik | Internal di command server |
| `EnumStatus`, `EnumFromTo`, `EnumNtwrkMgmt` (nested di `AppProcessor`) | GANTI NAMA | `TraceDirection`, `NetworkCommand` (top-level enum) |

## 4. Komponen per folder

| Folder / Kelas | Keputusan | Catatan / Pengganti |
|----------------|-----------|---------------------|
| **Message/** `Request`, `Response`, `Fees`, `Security`, `PrivateData`, `VirtualAccount` | PERTAHANKAN (paket `SyncNetPro.Contracts`) | Nama JSON dikunci (dok. 02). Nama C# boleh `CoreRequest`/`CoreResponse` dengan properti PascalCase + `[JsonPropertyName]`. Tambah `CoreResponse.From(request)` (salin field seperti SDK lama **+ isi `msgtype` response + salin `pos_entry_mode`**) dan helper `WithResponseCode(rc, message)`. Tidak ada properti baru di luar skema; data tambahan lewat `additional_data` + helper `AdditionalData.Set/TryGet<T>`. |
| **Constants/** `TranType`, `AuthTran` | PERTAHANKAN | `TranType` tetap konstanta string (nilai wire). Duplikasi `ADJUSTMENT`=`ADVICE`=`"ADV"` didokumentasikan. `AuthTran` → `AuthorizedBy.Internal/External`. |
| `TypeProtocol`, `NodeCategory` | GANTI | `enum ConnectionProtocol`, `enum NodeCategory` (mapping dari nilai DB) |
| `HsmPath`, `LogType`, `RoutingMode` | PERTAHANKAN (internal / modul terkait) | |
| `Network` (`MAX_LENGTH_MSG`, `MAX_WAIT_MSG`) | HAPUS | Tidak dipakai |
| **Nodes/** `NodeInternal`, `SinkNode`, `SourceNode` | GANTI | `CoreChannel` (satu implementasi, dua peran) + `CoreChannelHostedService`; perbaikan B3, B4 |
| `NodeRemotes`, `NodeRemote` | GANTI | `NodeRegistry` + `RemoteConnection` (TCP server/client, HTTP server/client) + `NodeTimerService` (echo, key exchange, status 1 menit) |
| `NodeClient` | GANTI | Mode `NonPersistent` pada `RemoteTcpClientConnection` (perbaikan B1) |
| `TaskProcessor` | HAPUS | Tidak dipakai; antrean internal memakai `System.Threading.Channels` |
| **Networking/** `XTcpClient`, `XTcpListener`, `XTcpClientSdk`, `XTcpListenerSdk` | GANTI | `TcpClientConnection`, `TcpServerConnection` di atas `System.IO.Pipelines` + `ITcpFrameCodec` (perbaikan B2, B8) |
| `TcpHeader`, `EnumTcp` (`TcpHeaderType`, `TcpLengthMode`, `TcpEndianMode`) | PERTAHANKAN (dirapikan) | `LengthPrefixCodec` + opsi; tambah `None` (delimiter/fixed) dan `Custom` (implementasi `ITcpFrameCodec` sendiri) yang benar-benar berfungsi |
| `SdkTcpHeader`, `SdkTcpHeaderLengthMode`, `SdkTcpHeaderFormat` | GABUNG | Satu `TcpFramingOptions` |
| `RetryPolicyDelay` | GANTI | `Microsoft.Extensions.Resilience` / Polly (backoff + jitter) |
| `XKestrel` | GANTI | Minimal API di host yang sama (`WebApplication`), request→handler→`HttpReply` langsung (tanpa `TaskCompletionSource` + dictionary) |
| `XHttpServer` | HAPUS | Tidak dipakai (digantikan `XKestrel`) |
| `XHttpClientSdk`, `XHttpClient`, `WSResponse` | GANTI | `IRemoteHttpClient` via `IHttpClientFactory`, proxy/timeout per koneksi |
| `Certificate` | GANTI | Validasi TLS standar; pengecualian hanya via opsi eksplisit `AllowUntrustedCertificate` (default `false`, peringatan di log) — perbaikan B6 |
| `SocketClient`, `SocketListener` | HAPUS | Tidak dipakai |
| **Services/** `LogService`, `TcpLogWorker`, `RabbitLogWorker` | GANTI | `ILoggerProvider`/`ITraceSink` di host yang sama; implementasi `LogServicesTcpSink`, `RabbitMqTraceSink`, `FileTraceSink` (fallback) — perbaikan B5, D8 |
| **Library/** `NbLogger` | GANTI | `ILogger` + file sink |
| `NbCache` / `INbCache`, `Common/CacheData` | GANTI | Korelasi dibangun ke SDK (`PendingRequestStore`); bila perlu cache umum: `IMemoryCache`/`HybridCache` |
| `NbConvert`, `NbFormat`, `NbString`, `NbMath`, `NbRandom`, `NbMessage` (`GetMsgTypeResp` dipindah ke `SyncNetPro.Contracts` untuk `CoreResponse.From`) | PERTAHANKAN sebagian (paket `SyncNetPro.Toolkit`) | Hanya fungsi yang dipakai interface: hex/bytes, format biner untuk trace, padding, MTI response. Hapus duplikasi dengan BCL (`Convert.ToHexString`, `RandomNumberGenerator`). |
| `NbTlvEmv`, `NbTlvQris`, `NbCard` | MODUL | `SyncNetPro.Toolkit.Payments` (opsional) |
| `NbTrace`, `NbTranMgr`, `NbStrUtil`, `NbXml`, `NbApp`, `NbDateTime`, `NbSystem` | HAPUS | Tidak dipakai / digantikan BCL |
| **Helpers/** `NetHelper`, `ConvertHelper`, `DataHelper`, `TcpHelper` | GABUNG ke Toolkit | `DataHelper.GetMasking` → `Masking.Pan(...)` |
| `CredenHelper` | GANTI | `IConfigProtector` (dekripsi nilai konfigurasi terenkripsi Core, kompatibel format lama) |
| `IsoHelper` | HAPUS | Tidak dipakai |
| **IsoMessage/** `Iso8583`, `FieldFormatter`, `IFieldFormatter`, `Field`, `IsoConverter` | MODUL (`SyncNetPro.Iso8583`) | API dirapikan: `IsoMessage.Parse(bytes, spec)`, `msg[11]`, `msg.Pack()`; framing TCP dipisah dari packer (tidak ada lagi `Pack(SdkTcpHeader)`) |
| **HSM/** `HsmService`, `HsmErrCode` | MODUL (`SyncNetPro.Hsm`) | Klien typed `IHsmClient` via `IHttpClientFactory` |
| **Cryptography/** `AesAlgorithm`, `DesAlgorithm`, `HashProvider` | MODUL (Toolkit.Crypto) | Dipertahankan untuk kebutuhan PIN/MAC (3DES) & hash; berbasis API BCL modern |
| **DbRepository/** `DbMgr` (query node/koneksi/status) | GANTI | `INodeConfigurationSource` (implementasi: `PostgresNodeConfigurationSource`, `JsonNodeConfigurationSource` untuk dev/SimCore) + `INodeStatusReporter` |
| `DbMgr` (query umum `Execute/GetRecords/GetRow/...` sync+async) | GANTI | Tidak diekspos; interface yang butuh DB memakai `NpgsqlDataSource` + Dapper sendiri (didaftarkan via `AddSyncNetDatabase()`) |
| `DbMgr` (routing, fee, produk, volume, commitment) | MODUL | `SyncNetPro.Routing` (lihat bawah) |
| `DbService`, `DbPgSql`, `DbResult`, `QueryModel` | GANTI | `NpgsqlDataSource` + Dapper, async-only |
| **Routing/**, **Fees/**, `Models/*` terkait | MODUL (`SyncNetPro.Routing`) | Keputusan Q6: .dll/paket terpisah dengan **versi independen** dari SDK inti, agar perubahan kebutuhan bisnis cukup merilis `SyncNetPro.Routing` tanpa rilis SDK. Dipindah utuh (port) karena dipakai `ApiChannel`; logika bisnis tidak diubah di fase awal |
| **Common/** `SdkConfig`, `Resources`, `ConfigModel` | GANTI | `IOptions<SyncNetOptions>` + provider konfigurasi Core (`AddSyncNetCoreConfiguration()`) — dok. 05 |
| `Signature` | HAPUS | Tidak dikompilasi |
| `obfuscar.xml`, Obfuscar | HAPUS | Keputusan Q5: tidak ada obfuscation. SDK dirilis sebagai NuGet di GitHub Packages dengan symbol (`.snupkg`) + SourceLink agar mudah di-debug developer |
| `Properties/launchSettings.json`, `*.user`, `PublishProfiles` | HAPUS | Artefak IDE |

## 5. Ringkasan Jumlah

| Keputusan | Perkiraan jumlah kelas lama |
|-----------|-----------------------------|
| PERTAHANKAN / GANTI NAMA | ±20 (kontrak pesan, konstanta, framing, ISO) |
| GANTI (fungsi setara, implementasi baru) | ±30 (node, transport, log, config, DB) |
| MODUL opsional | ±30 (routing, fee, HSM, crypto, TLV) |
| HAPUS | ±25 (tidak dipakai / duplikat / artefak) |

Hasilnya: inti SDK (`SyncNetPro.Sdk`) diperkirakan < 40% ukuran SDK lama, dan developer
interface baru hanya perlu mempelajari ±10 tipe publik (dok. 04 §4).
