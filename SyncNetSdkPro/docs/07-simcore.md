# 07 — SimCore (Simulator Core)

## 1. Tujuan

Developer dapat mengembangkan, menjalankan, dan menguji interface **tanpa menginstal
SyncNet Core, database, Log Services, atau HSM**. SimCore meniru semua perilaku Core yang
terlihat oleh interface (dok. 02) — tidak lebih, tidak kurang.

Non-tujuan: SimCore **bukan** pengganti Core (tidak ada routing, otorisasi, settlement,
penyimpanan transaksi).

## 2. Bentuk Distribusi

| Bentuk | Pemakaian |
|--------|-----------|
| **.NET tool** `syncnet-simcore` (CLI) | Menjalankan simulator dari terminal, kirim transaksi, lihat trace |
| **Web UI** (dijalankan oleh `syncnet-simcore up`, default `http://localhost:5080`) | Form kirim transaksi, daftar node & status koneksi, log pesan real-time, editor skenario |
| **Library** `SyncNetPro.Sdk.Testing` | SimCore in-process untuk unit/integration test (`await using var core = await SimCore.StartAsync(...)`) |
| **Container image** `syncnet/simcore` | CI/integrasi & tim non-.NET (Linux) |

## 3. Fitur

### 3.1 Kanal Core (inti)

| Fitur | Perilaku (meniru Core) |
|-------|------------------------|
| Listener **Sink** per node (`port_out`) | Interface connect sebagai klien; SimCore mengirim `Request`, menunggu `Response` |
| Listener **Source** per node (`port_in`) | Interface mengirim `Request`; SimCore membalas `Response` sesuai skenario |
| Framing | 2 byte biner big-endian, exclude header, UTF-8 JSON (dok. 02 §1) |
| Korelasi | Kunci `NodeName + UPPER(tran_type+datetime_tran+trace_number+terminal_id)` |
| Duplikat | Kunci pending sama → balas `94 Duplicate transaction` (`authorized_by=0`) |
| Link down | Sink tidak terkoneksi saat kirim → hasil `91 Link down` di UI/CLI |
| Timeout | `request_timeout` / `advice_timeout` per node; saat habis ditandai TIMEOUT dan (opsional) SimCore mengirim **reversal/advice otomatis** sesuai `auto_reversal`, dengan `original_data` diisi seperti Core |
| Late response | Response setelah timeout ditandai “late/unmatched” |
| Validasi kontrak | Setiap pesan dari interface divalidasi: JSON valid, nama properti dikenal, tipe sesuai, field kunci korelasi tidak berubah, `authorized_by` terisi, ukuran ≤ 65.535 byte. Pelanggaran tampil sebagai **warning** dengan penjelasan. |
| Mode respons Source | `echo` (salin request + `resp_code`), `fixed` (dari skenario), `script` (aturan per `tran_type`/`amount`), `delay` (uji timeout channel) |

### 3.2 Layanan pendukung

| Fitur | Keterangan |
|-------|------------|
| **Command client** | `simcore cmd VERSION`, `RESYNC`, `ECHO <node>`, `SIGNON <node>`, `SIGNOFF`, `KEYCHANGE`, `OTHER <node> <param>`, `TRACE ON/OFF` ke command port interface (framing dok. 02 §4) |
| **Log Services receiver** | Listener TCP yang menerima `LogModel` dan menampilkannya di UI/CLI (trace from/to, info) |
| **Penyedia konfigurasi node** | File `simcore.json` mendefinisikan app, node, koneksi (kolom sama dengan `sw_nodes`/`sw_connections`). Interface mode Development membaca konfigurasi ini (NodeSource=`SimCore` via HTTP atau file `Json`), jadi **tidak perlu PostgreSQL** |
| **Status node** | Menampilkan status yang dilaporkan interface (pengganti tabel `sw_app`/`sw_nodes`/`sw_connections`) |
| **Remote stub (opsional)** | Stub biller TCP ISO / HTTP JSON dan stub channel HTTP client, dikendalikan skenario — sehingga alur end-to-end bisa dijalankan di satu laptop |
| **HSM stub (opsional)** | Endpoint tiruan SyncNetHsm (translate PIN mengembalikan nilai deterministik) |

### 3.3 Skenario

File JSON/YAML di `simcore/scenarios/` proyek interface (dibuat oleh template):

```json
{
  "name": "inquiry-sukses",
  "node": "BILLER_ABC",
  "channel": "sink",
  "request": {
    "msgtype": "0200",
    "tran_type": "INQ",
    "tran_type_ext": "PLNPOST",
    "amount_tran": 0,
    "trace_number": "{{stan}}",
    "datetime_tran": "{{now:MMddHHmmss}}",
    "terminal_id": "TERM0001",
    "merchant_id": "MERCHANT01",
    "to_acc_number": "123456789012"
  },
  "expect": {
    "resp_code": "00",
    "within_ms": 5000
  },
  "remote_stub": { "reply": "fixtures/inquiry-0210.hex" }
}
```

- Placeholder: `{{stan}}` (auto increment 6 digit), `{{now:format}}`, `{{rrn}}`, `{{random:n}}`, `{{env:VAR}}`.
- `expect` menjadikan skenario sebagai test otomatis (`simcore run --scenario …` → exit code ≠ 0 bila gagal; output JUnit untuk CI).
- **Record & replay**: `simcore record` menyimpan pesan nyata (dari lingkungan UAT, dengan masking PAN) menjadi skenario/fixture.

## 4. Contoh Pemakaian

```bash
# buat simcore/simcore.json + contoh skenario
syncnet-simcore init --node BILLER_ABC

# jalankan simcore + Web UI (http://127.0.0.1:5080)
syncnet-simcore up -c simcore/simcore.json -s simcore/scenarios

# jalankan satu skenario pada instance 'up' yang sedang berjalan
syncnet-simcore send --scenario inquiry-sukses

# perintah ke command port interface
syncnet-simcore cmd -p 17000 VERSION

# jalankan seluruh skenario sebagai regression test (exit code 1 bila gagal)
syncnet-simcore run -c simcore/simcore.json -s simcore/scenarios --report junit.xml
```

Unit test interface (in-process, paket `SyncNetPro.Sdk.Testing`):

```csharp
[Fact]
public async Task Inquiry_is_forwarded_and_mapped()
{
    await using SimCore core = await SimCore.StartAsync(SimCoreOptions.Load("simcore/simcore.json"));
    await using var app = await SimInterfaceHost.StartAsync<BillerAbcInterface>(core);

    SimResult result = await core.SendAsync("BILLER_ABC", new CoreRequest { /* ... */ });

    Assert.Equal(SimOutcome.Responded, result.Outcome);
    Assert.Equal("00", result.Response!.ResponseCode);
    Assert.Empty(core.ContractWarnings);
}
```

`SimInterfaceHost` menjalankan host interface sungguhan (DI, kanal Core, command port, trace ke
Log Services SimCore) dengan port dari `SimCore` dan baru kembali setelah kedua sisi terkoneksi.

## 5. Arsitektur SimCore

```
┌──────────────────────── SyncNetPro.SimCore ────────────────────────┐
│  SimNodeRegistry ◄── simcore.json                                   │
│  SinkListener (port_out) ─┐                                         │
│  SourceListener (port_in) ├─ PendingStore (switch key, timeout)     │
│                           └─ ContractValidator (dok. 02)            │
│  CommandClient    LogServicesReceiver    ConfigEndpoint (HTTP)      │
│  RemoteStubs (TCP ISO / HTTP)            HsmStub                    │
│  ScenarioEngine (placeholder, expect, record/replay)                │
│  Web UI (Blazor SSR / Minimal API + SignalR)   CLI (System.CommandLine) │
└─────────────────────────────────────────────────────────────────────┘
```

- Memakai **paket kontrak dan codec TCP yang sama** dengan SDK (`SyncNetPro.Contracts`,
  `LengthPrefixCodec`) — sehingga perilaku wire SimCore otomatis ikut terverifikasi oleh golden test.
- Perilaku yang ditiru diambil dari `SyncNetCore/Nodes/SinkNode.cs` & `SourceNode.cs`; setiap
  perubahan perilaku Core harus diikuti pembaruan SimCore (dicatat di checklist rilis Core).
- Uji kesetaraan: interface lama (`ApiBillerJson`, memakai SDK lama) juga harus dapat
  dijalankan terhadap SimCore → bukti SimCore meniru Core dengan benar.

## 6. Batasan yang Perlu Diketahui Developer

- Kode respons, routing, fee, dan otorisasi Core tidak disimulasikan (hanya dari skenario).
- Kolom `private_data` milik Core (mis. `mode_timeout`) diisi nilai default yang dapat diatur di skenario.
- Uji performa/ketahanan final tetap harus dilakukan terhadap Core sebenarnya di UAT.

## 7. Status Implementasi (Fase 4)

| Fitur desain | Status |
|--------------|--------|
| Listener Sink/Source, framing, korelasi, duplikat 94, link down 91 | ✅ `SimCore` |
| Timeout + auto reversal (`msgtype 0400`, `original_data`), late/unmatched response | ✅ |
| Validasi kontrak (properti tidak dikenal → saran `additional_data`, tipe, field kunci, `resp_code`/`authorized_by`) | ✅ `ContractValidator` |
| Mode respons Source | ✅ `Fixed` (termasuk delay), `Rules` (per `tran_type`/`tran_type_ext`/nominal), `None` (uji timeout). Mode `echo` tercakup oleh `Fixed` (response dibuat dari request via `CoreResponse.From`) |
| Command client, Log Services receiver | ✅ |
| Konfigurasi node | ✅ file `Json` (`NodeSource=Json`, lihat `samples/Sample.Outbound/appsettings.Development.json`). NodeSource via HTTP SimCore: backlog |
| Remote stub TCP / HTTP | ✅ `RemoteStub` (balasan tetap/per pola) |
| Skenario JSON, placeholder `{{stan}}`, `{{now:…}}`, `{{rrn}}`, `{{random:n}}`, `{{env:…}}`, `expect`, JUnit | ✅ `ScenarioRunner` |
| Web UI | ✅ Minimal API + halaman statis + Server-Sent Events (lebih ringan daripada Blazor/SignalR; tanpa dependensi tambahan) |
| Container | ✅ `tools/SyncNetPro.SimCore/Dockerfile` (publikasi image di CI: backlog) |
| HSM stub | ✅ fase 5: `SimCoreOptions.Hsm` (balasan deterministik, `ResponseCode`, `TranslatedPinBlock`, `DelayMs`), `SimCore.HsmRequests`; `SimInterfaceHost` mengisi `SyncNet:Hsm:Url` otomatis |
| Skenario YAML, record & replay, status node dari interface | ⏳ backlog |
| Uji kesetaraan `ApiBillerJson` lama terhadap SimCore | ⏳ butuh direktori `Core/Bin` lama (SDK lama membaca config Core sejak konstruktor); dijalankan di lingkungan UAT |
