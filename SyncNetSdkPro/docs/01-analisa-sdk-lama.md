# 01 — Analisa SDK Lama (`SyncNetSdk`)

Sumber: `SyncNetSdk/` (dibaca saja, tidak diubah) dan tiga pemakainya:
`ApiInterfaces/ApiBillerIso`, `ApiInterfaces/ApiBillerJson`, `ApiInterfaces/ApiChannel`.
Pembanding sisi core: `SyncNetCore/Nodes`, `SyncNetCore/Message`.

## 1. Profil Proyek

| Item | Nilai |
|------|-------|
| Target framework | `net10.0` |
| Jenis proyek | `Microsoft.NET.Sdk.Web` dengan `OutputType=Library` |
| Assembly / root namespace | `SyncNetSdk` / `SyncNet` |
| Ukuran | ±120 file `.cs`, ±17.250 baris |
| Dependensi | Dapper, Npgsql, Newtonsoft.Json, RabbitMQ.Client, Microsoft.Extensions.Hosting.Systemd + WindowsServices, Obfuscar |
| Distribusi | DLL hasil publish di path lokal Windows (`C:\Projects\SYNCNET\SDK\SyncNetSdk\bin\Publish`), direferensikan interface via `HintPath ..\..\..\..\SDK\...` (di luar repo) |
| Versi | `VERSION.txt` = `v1.0 Initial release`; tidak ada versioning paket |
| Test | Tidak ada proyek test |
| Dokumentasi XML | `GenerateDocumentationFile=False` |

## 2. Arsitektur Saat Ini

```
                 ┌──────────────────────── Proses Interface (mis. "API Biller") ───────────────────────┐
                 │                                                                                      │
 SyncNet Core    │  SinkNode (XTcpClientSdk → 127.0.0.1:port_out) ─┐                                    │   Sistem
 ┌──────────┐    │                                                 │   AppProcessor (static)           │   Eksternal
 │ SinkNode │◄──►│  SourceNode (XTcpClientSdk → 127.0.0.1:port_in) ├──►  + IAppProcessor (MyApp)  ◄────►│◄──► (bank/biller/
 │SourceNode│    │                                                 │                                    │     channel)
 │(listener)│    │  NodeRemote per sw_connections:                 │   NodeRemotes / NodeInternal       │
 └──────────┘    │   - XTcpListenerSdk (server) / XTcpClientSdk    │                                    │
      ▲          │   - XKestrel (HTTP server) / XHttpClientSdk     │                                    │
      │          │  XTcpListenerSdk command port (sw_app.command_port) ◄── SyncNetWebApi (VERSION, RESYNC…) │
      │          │  LogService → TcpLogWorker / RabbitLogWorker ──► "Log Services" (sw_app)            │
      │          └──────────────────────────────────────────────────────────────────────────────────────┘
      │                        ▲ baca/tulis konfigurasi & status
      └──── PostgreSQL (sw_app, sw_nodes, sw_connections, sw_routes_*, sw_fees, sw_product*) ◄─┘
```

### 2.1 Komponen inti

| Komponen | Peran |
|----------|-------|
| `AppProcessor` | Titik masuk tunggal. Konstruktor: `SdkConfig.Initialize()`, buat logger, `DbMgr`, `NodeRemotes`, `NodeInternal`, command listener, `LogService`. `AppStart()` → update status DB, load node, buka command port, `Task.Delay(-1)`. Juga menyediakan method kirim/balas, trace, dan logger **statis**. |
| `IAppProcessor` | Callback yang wajib diimplementasikan interface: 3 grup (internal, external, timer, command) = 11 method. |
| `NodeInternal` | Membaca `sw_nodes` untuk `app_name`; per node membuat `SourceNode` (kategori 1 = merchant/acquirer), `SinkNode` (kategori 2 = biller/issuer) atau keduanya (kategori 0). |
| `SinkNode` / `SourceNode` | Klien TCP ke Core di `127.0.0.1` (`port_out` / `port_in`), payload JSON. |
| `NodeRemotes` / `NodeRemote` | Membaca `sw_connections` JOIN `sw_nodes`; per koneksi membuat server/klien TCP atau HTTP; timer echo, key-exchange, auto sign-on, update status koneksi tiap menit. |
| `NodeClient` | TCP non-persistent (connect → kirim → tunggu → putus dengan timer). |
| `XTcpClient/XTcpListener` (+`*Sdk`) | Transport TCP dengan header panjang (2/4 byte, BCD/ASCII/biner, include/exclude, hi-lo). |
| `XKestrel` | HTTP server berbasis Kestrel yang “memarkir” request sampai interface memanggil `Reply`. |
| `XHttpClientSdk` / `XHttpClient` | HTTP client (event-based / static helper). |
| `LogService` + `TcpLogWorker`/`RabbitLogWorker` | Mengirim trace/info ke “Log Services” via TCP atau RabbitMQ; fallback tulis file. |
| `NbLogger` | Log error ke file `LogDir`. |
| `DbMgr` / `DbService` / `DbPgSql` | Akses PostgreSQL (Dapper); query konfigurasi node, status, routing, fee, produk. |
| `SdkConfig` / `Resources` / `CredenHelper` | Baca `appsettings.json` lokal → `AppDir` → `AppDir/Core/Bin/appsettings.json`; dekripsi `Resources.bin` dengan `Keys/PrivateKey.pem` (RSA-OAEP + AES-GCM); dekripsi password DB (AES-CBC). |
| `IsoMessage/*` | Packer/unpacker ISO 8583 (`Iso8583`, `FieldFormatter`, `IsoConverter`). |
| `Routing/*`, `Fees/*` | Domain bisnis: resolver routing (static/priority/best price/load balance), failover, jadwal, commitment volume, kalkulasi fee & margin. Dipakai `ApiChannel`. |
| `HSM/HsmService` | Klien HTTP ke SyncNetHsm (generate key, translate PIN). |
| `Library/Nb*`, `Helpers/*`, `Cryptography/*` | Utilitas: konversi, format, TLV EMV/QRIS, kartu, tanggal, cache, DES/AES/hash. |

### 2.2 Alur pesan

**Outbound (Core → eksternal → Core)** — contoh `ApiBillerIso` / `ApiBillerJson`:

1. Core `SinkNode` (listener `port_out`) mengirim `Request` JSON.
2. SDK `SinkNode.OnDataArrival` → deserialisasi → `IAppProcessor.ProcessMsgFromSinkNode(node, req)`.
3. Interface mapping `Request` → ISO/JSON, **menyimpan `req` ke cache manual** dengan key buatan sendiri, kirim via `SendToTcp` / `SendToHttpClient` / handler non-persistent buatan sendiri.
4. Balasan eksternal masuk ke `ProcessMsgFromRemoteTcp` / `ProcessMsgFromRemoteWsClient`; interface mencari `req` di cache, mapping ke `Response`, `ReplyToSink(node, rsp)`.
5. Timeout ditangani Core (`SinkNode.CheckMsgTimeout` → auto reversal/advice).

**Inbound (eksternal → Core → eksternal)** — contoh `ApiChannel`:

1. `XKestrel` menerima HTTP → `ProcessMsgFromRemoteWsServer(node, conn, body, HttpContext)`.
2. Interface validasi, mapping ke `Request`, menyimpan `HttpContext` ke cache dengan key `tran_type+datetime_tran+merchant_id+trace_number`, `SendToSource(...)` (SDK mengisi `private_data.connection_name` & `ip_external`).
3. Core membalas lewat `SourceNode` → `ProcessMsgFromSourceNode(node, conn, rsp)`.
4. Interface ambil `HttpContext` dari cache → `ReplyToHttpServer(...)`. Jika lewat `MaxWait`, `XKestrel` membalas 408.

**Manajemen jaringan**: timer echo/key-exchange per node, auto sign-on saat connect, serta perintah dari SyncNetWebApi lewat command port (`VERSION`, `RESYNC`, `ECHO`, `SIGNON`, `SIGNOFF`, `KEYCHANGE`, `OTHER <node> <param>`, `TRACE ON|OFF|CLEAR`).

## 3. Cara Pakai Saat Ini (pola interface lama)

```csharp
class MyApp : IAppProcessor
{
    private readonly AppProcessor _app;
    public MyApp(INbCache cache) { _app = new AppProcessor(APPNAME, VERSION, this); }  // langsung butuh Core+DB
    public Task ProcessMsgFromSinkNode(string NodeName, Request MsgRequest) { ... }
    public Task ProcessMsgFromSourceNode(...) => throw new NotImplementedException();
    public Task ProcessMsgFromRemoteWsServer(...) => throw new NotImplementedException();
    public Task ProcessMsgFromRemoteWsClient(...) => throw new NotImplementedException();
    public Task TimerKeyExchange(string NodeName) => throw new NotImplementedException();
    // ... total 11 method wajib
}
```

Setiap interface juga menulis ulang: `Program.cs` + `AppService` (BackgroundService), cache korelasi,
`ReplyTransactionUnsupported`, `ReplyTransactionLinkDown`, handler TCP non-persistent,
helper `Logger(...)`, pengecekan `IsTraceOn()` sebelum `WriteTrace`.

## 4. Kekuatan yang Perlu Dipertahankan

- Kontrak pesan JSON yang sederhana dan stabil antara interface dan Core.
- Transport TCP dengan variasi header lengkap (2/4 byte, BCD/ASCII/biner, include/exclude, endian).
- Konfigurasi node/koneksi terpusat di database dan bisa di-**RESYNC** tanpa restart.
- Integrasi operasional: status node/koneksi di DB, command port, trace ke Log Services.
- Library ISO 8583 dan utilitas domain pembayaran (TLV EMV/QRIS, masking, fee, routing).

## 5. Temuan Masalah

### 5.1 Bug / perilaku salah (terverifikasi dari kode)

| # | Lokasi | Temuan | Dampak |
|---|--------|--------|--------|
| B1 | `Nodes/NodeClient.cs` `Send()` | Mengisi `HeaderLength`/`HeaderFormat` tetapi **tidak memanggil `SetProtocol()`**, sehingga header selalu default `Binary2Byte`. | TCP non-persistent salah framing bila node memakai header selain 2-byte biner. Kemungkinan inilah sebab `ApiBillerIso` menulis `TcpClientHandler` sendiri. |
| B2 | `XTcpClientSdk/XTcpListenerSdk.SetProtocol()` | `HeaderLength = 0` (`TCPHeaderNone`) dan `TCPHeaderCustom` jatuh ke `default` → `Binary2Byte`. `HeaderHiLo` hanya berlaku untuk tipe biner. | Protokol “tanpa header” / “custom” yang tampil di konfigurasi sebenarnya tidak didukung. |
| B3 | `Nodes/NodeInternal.cs` `Stop()` | Loop hanya atas `_snk.Keys`; node kategori MERCHANT yang hanya punya `SourceNode` **tidak pernah di-stop**. | Koneksi ke Core tidak ditutup rapi saat shutdown/resync. |
| B4 | `Nodes/SourceNode.cs` `Send()` | Serialisasi JSON lalu `NbConvert.StringToBytes` (1 char → 1 byte via `Convert.ToByte`); sementara penerima (Core) decode **UTF-8**. `SinkNode.Reply` justru memakai UTF-8. | Karakter ≥ U+0080 rusak; karakter > U+00FF melempar `OverflowException` → pesan ke Core gagal terkirim. |
| B5 | `Services/TcpLogWorker.cs` | Mengambil `host` Log Services dari DB tetapi selalu connect ke `127.0.0.1`. | Log Services di host lain tidak bisa dipakai. |
| B6 | `Networking/Certificate.cs` | `ValidateRemoteCertificate` selalu `true` (komentar: “agar bisa digunakan di serverdev”) dan dipakai di `XHttpClientSdk` produksi. | Validasi TLS dimatikan → rentan MITM ke biller/bank. |
| B7 | `Nodes/NodeRemotes.cs` `GetNode(name, out node)` | Saat tidak ditemukan tetap membuat `new NodeRemote()` (yang membuat listener & client TCP baru). | Alokasi sia-sia; pemanggil yang lupa cek return mendapat objek “kosong” bukan `null`. |
| B8 | `XTcpClient.ConnectAsync` | Saat `AutoReconnect=false` dan timeout, `throw` di dalam `Task.Run` fire-and-forget. | Exception tidak teramati; pemanggil tidak tahu koneksi gagal. |
| B9 | `ApiBillerIso.csproj` (pemakai) | `None Update="appSettings.json"` sedangkan file bernama `appsettings.json`. | Di Linux (case-sensitive) file konfigurasi **tidak tersalin** ke output. |
| B10 | `AppProcessor.RequestCommand` | `TRACE CLEAR` ada di daftar command tetapi `switch` tidak punya case-nya → dibalas “Unknown command”. Pencocokan daftar bersifat case-insensitive tetapi `switch` case-sensitive → `version` (huruf kecil) juga dibalas “Unknown command”. | Perilaku tidak konsisten bagi operator/WebApi. |

### 5.2 Masalah desain

| # | Temuan | Dampak |
|---|--------|--------|
| D1 | `AppProcessor` menyimpan `_iapp`, `_logger`, `_logService`, `_dbMgr`, `_isTraceOn` sebagai **static**; method logging statis. | Tidak bisa unit test, tidak bisa >1 instance per proses, urutan inisialisasi rapuh (memanggil `AppProcessor.Logger` sebelum konstruktor → `NullReferenceException`). |
| D2 | `IAppProcessor` “god interface” 11 method; parameter PascalCase; tipe transport bocor (`HttpContext`, `EndPoint`, `WebHeaderCollection`). | Developer baru harus paham semua jalur walau hanya butuh satu; `NotImplementedException` di mana-mana. |
| D3 | Model event-callback: kirim di satu method, balasan datang di method lain. | Korelasi, timeout, dan cleanup cache ditulis ulang di tiap interface. |
| D4 | Konstruktor `AppProcessor` langsung membaca file Core, `Resources.bin`, private key, dan membuka DB. | Interface **tidak bisa dijalankan tanpa instalasi Core**; tidak ada mode dev/simulasi. |
| D5 | Konfigurasi status berbasis string `"0"/"1"` (`AlwaysConnected`, `AutoSignon`, `TcpHighLowByte`, …) dan `byte` konstanta. | Mudah salah, tidak type-safe. |
| D6 | Domain bisnis (Routing, Fees, Commitment, Product mapping, 40+ query di `DbMgr`) bercampur dengan SDK transport. | SDK besar, dependensi DB wajib untuk semua interface; perubahan routing memaksa rilis SDK. |
| D7 | Dua lapisan logging (`NbLogger` file + `LogService`) tidak terintegrasi dengan `ILogger`; `Console`/`ILogger` host terpisah. | Observabilitas tidak seragam; tidak ada structured logging / metrics / tracing. |
| D8 | `LogService` membangun `IHost` kedua di dalam SDK. | Dua host dalam satu proses; lifecycle & DI tidak menyatu. |
| D9 | Akses DB sinkron + asinkron duplikat (`Execute`/`ExecuteAsync`, dst.), `DataTable`/`DataRow` sebagai tipe publik. | API lebar, tidak type-safe, blocking I/O. |
| D10 | Newtonsoft.Json + `DataTable` + refleksi → tidak kompatibel AOT/trimming; Obfuscar mengaburkan SDK. | Debugging developer interface sulit (stack trace/nama private tersamarkan). |
| D11 | Tidak ada versioning paket/NuGet; referensi DLL via path relatif di luar repo. | Build tidak reproducible; “works on my machine”. |
| D12 | `Response(Request)` di SDK tidak mengisi `msgtype` dan tidak menyalin `pos_entry_mode` (berbeda dengan versi Core). | Perilaku berbeda antara SDK dan Core; developer harus tahu detail ini. (Harus dipertahankan/diputuskan — lihat dok. 02 §6.) |

### 5.3 Kode usang / tidak terpakai

Ditentukan dengan pencarian referensi di SDK dan ketiga interface:

- **Tidak dikompilasi**: `Common/Signature.cs` (di-`Compile Remove`).
- **Tidak direferensikan sama sekali**: `Networking/SocketClient.cs`, `Networking/SocketListener.cs`,
  `Networking/XHttpServer.cs` (digantikan `XKestrel`), `Nodes/TaskProcessor.cs` (memakai `HttpListenerContext`),
  `Common/CacheData.cs` (duplikat `NbCache`), `Helpers/IsoHelper.cs`, `Library/NbTrace.cs`, `NbTranMgr`,
  `NbStrUtil`, `NbXml`, `NbApp`, `NbDateTime`, `NbCard`, `NbTlvEmv`, `NbTlvQris` (tidak dipakai interface saat ini — kandidat modul opsional, bukan core),
  enum `SdkTcpHeader` (hanya dipakai overload `Iso8583.Pack`), `Constants/Network` (`MAX_LENGTH_MSG`, `MAX_WAIT_MSG`).
- **Overload/duplikasi**: `GetNode`/`GetNodeRemote` (4 varian), `IsConnected` (2), `SendToTcp` (2),
  `SendToHttpClient` (2), `ReplyToHttpServer` (2), `WriteLog` (3) + `Logger` (3), `DbMgr` sync+async,
  `NbConvert.StringToBytes` vs `ConvertHelper.StringToBytes` (encoding berbeda!).
- **Komentar kode mati**: timer lama `_TmrAutoSignon`, `Signature`, `GetEndPointLogServices` yang dikomentari di `AppStart`.
- **Artefak IDE**: `*.csproj.user`, `*.pubxml.user`, `launchSettings.json` (library tidak perlu), publish profile ke `C:\`.

Rincian keputusan per item ada di [03 — Inventaris & Pemetaan API](03-inventaris-dan-pemetaan-api.md).

## 6. Kesimpulan

SDK lama fungsional tetapi (a) terlalu terikat pada instalasi Core, (b) memaksa pola
callback yang berulang di setiap interface, (c) mencampur transport dan domain bisnis,
dan (d) punya beberapa bug framing/encoding. SDK baru sebaiknya **ditulis ulang di atas
Generic Host** dengan kontrak pesan dibekukan (dok. 02) sebagai satu-satunya bagian
yang wajib kompatibel byte-per-byte, sementara API publik didesain ulang untuk
kemudahan developer.
