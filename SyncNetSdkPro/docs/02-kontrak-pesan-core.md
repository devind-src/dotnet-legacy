# 02 — Kontrak Pesan ke Core (WAJIB TIDAK BERUBAH)

Dokumen ini membekukan semua hal yang “terlihat” oleh SyncNet Core dan komponen
operasional lain. SDK baru boleh mengubah API publik untuk developer, tetapi
**semua yang ada di dokumen ini harus identik di level wire/DB** dan diverifikasi
dengan *golden test* (lihat dok. 08).

Sumber kebenaran: `SyncNetSdk/Nodes/SinkNode.cs`, `SourceNode.cs`,
`Networking/XTcpClient.cs`, `TcpHeader.cs`, `Message/*.cs`, `AppProcessor.cs`,
`Services/*LogWorker.cs`, `DbRepository/DbMgr.cs`, serta sisi Core
`SyncNetCore/Nodes/SinkNode.cs`, `SourceNode.cs`, `Helpers/DataHelper.cs`.

## 1. Kanal Interface ↔ Core

| Kanal | Pihak server | Pihak klien | Port | Arah pesan |
|-------|--------------|-------------|------|------------|
| **Sink** (outbound) | Core `SinkNode` (`XTcpListener`) | Interface `SinkNode` | `sw_nodes.port_out` | Core → Interface: `Request`; Interface → Core: `Response` |
| **Source** (inbound) | Core `SourceNode` (`XTcpListener`) | Interface `SourceNode` | `sw_nodes.port_in` | Interface → Core: `Request`; Core → Interface: `Response` |

- Host tujuan: **`127.0.0.1`** (hard-coded di SDK lama). SDK baru: default tetap `127.0.0.1`, boleh dikonfigurasi (untuk SimCore/container) tanpa mengubah perilaku default.
- Satu koneksi TCP persistent per node per arah; interface melakukan reconnect **setiap 5 detik** (`RetryDelaySeconds = 5`).
- Pembentukan node berdasarkan `sw_nodes.category`: `"1"` MERCHANT → hanya Source; `"2"` BILLER_ISSUER → hanya Sink; lainnya (`"0"`) → Source **dan** Sink.

### 1.1 Framing TCP

Kanal Sink/Source memakai default `XTcpClient` (tanpa `SetProtocol`):

| Atribut | Nilai |
|---------|-------|
| Header | 2 byte **biner**, **big-endian** (Hi-Lo) |
| Nilai header | panjang **payload saja** (exclude header) |
| Maks. payload | 65.535 byte (batas `ushort` dan `MaxPacketSize`) |
| Payload | Teks JSON satu objek, tanpa BOM, tanpa newline penutup |
| Timeout baca payload | 10 detik setelah header diterima (`ReadTimeoutMs`) |

Contoh: payload 50 byte → header `0x00 0x32`.

### 1.2 Encoding payload

| Arah | SDK lama | Decoder penerima | Keputusan SDK baru |
|------|----------|------------------|--------------------|
| Interface → Core (Source, `Request`) | `NbConvert.StringToBytes` (1 char = 1 byte, `Convert.ToByte`) | Core: UTF-8 | **UTF-8** |
| Interface → Core (Sink, `Response`) | UTF-8 | Core: UTF-8 | UTF-8 |
| Core → Interface | UTF-8 | UTF-8 | UTF-8 |

Untuk karakter ASCII (0x00–0x7F) kedua cara menghasilkan byte identik, sehingga
beralih ke UTF-8 **tidak mengubah format** untuk semua pesan valid saat ini; justru
memperbaiki bug B4 (karakter non-ASCII rusak/exception). Golden test wajib
mencakup kasus ASCII (identik) dan non-ASCII (UTF-8 valid).

## 2. Skema JSON

Serializer lama: **Newtonsoft.Json 13, setting default** (`JsonConvert.SerializeObject(obj)`):

- Nama properti **persis** seperti nama properti C# (snake_case), urutan = urutan deklarasi.
- Properti bernilai `null` **tetap ditulis** (`"pan":null`).
- `decimal` ditulis dengan titik desimal minimal satu digit bila bilangan bulat
  (`10000m` → `10000.0`, `10000.50m` → `10000.50`). ⚠️ `System.Text.Json` menulis `10000` — **berbeda**.
- `enum` ditulis sebagai **angka** (`"hsm_cmd":0`).
- `bool` → `true/false`; `int` → angka.
- `object` / `Dictionary<string, object>` → JSON apa adanya (round-trip via `JToken`).
- Tidak ada indentasi, tidak ada `$type`.

Deserializer: Newtonsoft default — properti tak dikenal **diabaikan**, properti hilang → nilai default, pencocokan nama **case-insensitive**.

### 2.1 `Request` (Core → Interface di Sink; Interface → Core di Source)

Urutan properti (sesuai `SyncNetSdk/Message/Request.cs`):

| # | Properti | Tipe | Keterangan |
|---|----------|------|------------|
| 1 | `pan` | string | Primary account number |
| 2 | `msgtype` | string | MTI (mis. `0200`) |
| 3 | `tran_type` | string | Kode transaksi SyncNet (`INQ`, `PAY`, `ADV`, `REV`, … — lihat §5) |
| 4 | `tran_type_ext` | string | Kode transaksi eksternal / produk |
| 5 | `from_acc_type` | string | |
| 6 | `to_acc_type` | string | |
| 7 | `currency` | string | |
| 8 | `amount_tran` | decimal | |
| 9 | `trace_number` | string | STAN |
| 10 | `datetime_tran` | string | Tanggal/jam transaksi (format dari sumber, mis. `MMddHHmmss`) |
| 11 | `date_settle` | string | |
| 12 | `merchant_type` | string | |
| 13 | `merchant_id` | string | |
| 14 | `terminal_id` | string | |
| 15 | `acq_inst_id` | string | |
| 16 | `fwd_inst_id` | string | |
| 17 | `refnum` | string | RRN |
| 18 | `pos_entry_mode` | string | |
| 19 | `receiving_inst_id` | string | |
| 20 | `from_acc_number` | string | |
| 21 | `to_acc_number` | string | |
| 22 | `echo_data` | object | Data yang dikembalikan apa adanya pada response |
| 23 | `temp_data` | object | Data titipan, tidak disimpan ke DB |
| 24 | `original_data` | string | Kunci transaksi asal untuk reversal/advice |
| 25 | `additional_data` | object (map) | Default `{}` |
| 26 | `fee_data` | `Fees` | Default objek kosong |
| 27 | `security` | `Security` | Default objek kosong |
| 28 | `private_data` | `PrivateData` | Default objek kosong |
| 29 | `virtual_account` | `VirtualAccount` | Default objek kosong |

### 2.2 `Response`

Sama dengan `Request` #1–21 (dengan urutan `pos_entry_mode` di posisi 18), lalu:

| # | Properti | Tipe | Keterangan |
|---|----------|------|------------|
| 22 | `additional_amount` | string | |
| 23 | `resp_code` | string | Kode respons |
| 24 | `resp_message` | string | |
| 25 | `authorized_by` | string | `"0"` INTERNAL, `"1"` EXTERNAL (default konstruktor = `"1"`) |
| 26–33 | `echo_data`, `temp_data`, `original_data`, `additional_data`, `fee_data`, `security`, `private_data`, `virtual_account` | | Sama dengan Request |

### 2.3 Sub-objek

**`Fees`** (`fee_data`): `total_fee`, `acquirer_fee`, `merchant_fee`, `submerchant_fee`, `switch_fee`, `biller_fee`, `issuer_fee` — semua `decimal`.

**`Security`** (`security`), urutan: `track2data`, `iccdata`, `pindata`, `miscdata` (string), `hsm_cmd` (enum → int), `is_pin_change` (bool), `is_debet_tran` (bool, **default `true`**).
Nilai `hsm_cmd` di SDK: `TranslatePinblock = 3`, `TranslatePinblockTerminal = 4` (nilai 0–2 dimiliki Core: `GenerateKey`, `GenerateKeyTerminal`, `TranslateKey`). Default saat tidak diisi = `0`.

**`PrivateData`** (`private_data`), subset milik SDK: `sink_node`, `ip_external`, `connection_name` (string), `retry_send` (int).
Core memiliki properti tambahan (`source_node`, `ip_source`, `mode_timeout`, `auto_reply_rev`, `save_repeat_reversal`, `is_req_internal`, `max_retry_send`, `enable_closing`, `closing_time_start`, `closing_time_end`, `calendar_name`) yang **tidak ikut** diserialisasi SDK lama — dibuang saat interface mendeserialisasi, dan Core mengambilnya dari buffernya sendiri.

**`VirtualAccount`** (`virtual_account`): `enable` (bool), `acc_number` (string), `amount` (decimal), `balance` (decimal).

### 2.4 Contoh (dihasilkan SDK lama untuk `Response` kosong)

```json
{"pan":null,"msgtype":null,"tran_type":null,"tran_type_ext":null,"from_acc_type":null,"to_acc_type":null,"currency":null,"amount_tran":0.0,"trace_number":null,"datetime_tran":null,"date_settle":null,"merchant_type":null,"merchant_id":null,"terminal_id":null,"acq_inst_id":null,"fwd_inst_id":null,"refnum":null,"pos_entry_mode":null,"receiving_inst_id":null,"from_acc_number":null,"to_acc_number":null,"additional_amount":null,"resp_code":null,"resp_message":null,"authorized_by":"1","echo_data":null,"temp_data":null,"original_data":null,"additional_data":{},"fee_data":{"total_fee":0.0,"acquirer_fee":0.0,"merchant_fee":0.0,"submerchant_fee":0.0,"switch_fee":0.0,"biller_fee":0.0,"issuer_fee":0.0},"security":{"track2data":null,"iccdata":null,"pindata":null,"miscdata":null,"hsm_cmd":0,"is_pin_change":false,"is_debet_tran":true},"private_data":{"sink_node":null,"ip_external":null,"connection_name":null,"retry_send":0},"virtual_account":{"enable":false,"acc_number":null,"amount":0.0,"balance":0.0}}
```

> Contoh ini diturunkan dari definisi kelas; saat implementasi, golden file dibuat
> dengan **menjalankan** serializer SDK lama (dok. 08 §2), bukan diketik manual.

## 3. Aturan Semantik yang Diandalkan Core

1. **Kunci korelasi Sink (Core)**: `NodeName + UPPER(tran_type + datetime_tran + trace_number + terminal_id)`.
   Response dari interface **wajib** membawa keempat field tersebut sama persis dengan Request,
   jika tidak Core menganggap respons tidak dikenal dan transaksi akan timeout (auto reversal/advice).
2. Request dengan kunci yang sama saat masih pending → Core membalas sendiri `94 Duplicate transaction`.
3. Sink node tidak terkoneksi → Core membalas sendiri `91 Link down` (`authorized_by = "0"`).
4. Timeout ditentukan Core (`request_timeout` / `advice_timeout` untuk `ADV`/`REV`); interface **tidak**
   perlu (dan tidak boleh) membalas timeout ke Core, tetapi boleh membalas kode lain lebih cepat.
5. **Inbound (Source)**: SDK mengisi `private_data.connection_name` (nama koneksi eksternal asal) dan
   `private_data.ip_external` (IP klien eksternal) sebelum mengirim `Request`; Core mengembalikan
   `private_data.connection_name` pada `Response` agar interface tahu koneksi mana yang harus dibalas.
6. `Response(Request)` versi **SDK**: menyalin #1–21 kecuali `msgtype` dan `pos_entry_mode`, serta
   `echo_data`, `original_data`, `additional_data`, `fee_data`, `security`, `private_data`,
   `virtual_account`; `authorized_by = "1"`. (Versi Core berbeda: mengisi `msgtype` respons dan
   menyalin `pos_entry_mode`.) **Keputusan (Q2): SDK baru mengisi `msgtype` response dan menyalin `pos_entry_mode`** (sama dengan Core) —
   lihat §7.
7. `authorized_by = "0"` dipakai interface saat interface sendiri yang memutuskan respons
   (mis. `A1 Transaction is not supported`, `89 Link down`) — konvensi interface yang ada.

## 4. Protokol Command Port

| Atribut | Nilai |
|---------|-------|
| Server | Interface, `IPAddress.Any : sw_app.command_port` (per `app_name`) |
| Klien | SyncNetWebApi (`MonitoringCommandService`) |
| Framing | Default `XTcpListener`: header 2 byte biner big-endian, exclude |
| Payload request | Teks UTF-8: `VERSION`, `RESYNC`, `ECHO <node>`, `SIGNON <node>`, `SIGNOFF <node>`, `KEYCHANGE <node>`, `OTHER <node> <param…>`, `TRACE ON`, `TRACE OFF`, `TRACE CLEAR` |
| Payload response | Teks UTF-8: string versi (untuk `VERSION`), `OK`, atau `Unknown command` |

SDK baru mempertahankan sintaks dan balasan; perbaikan B10 (case-insensitive, `TRACE CLEAR` dibalas `OK`) tidak mengubah format.

## 5. Protokol Log Services

| Atribut | Nilai |
|---------|-------|
| Tujuan (TCP) | Port = `sw_app.command_port` untuk `app_name = 'Log Services'` (host: lihat bug B5) |
| Framing | Default `XTcpClient`: 2 byte biner big-endian, exclude |
| Tujuan (RabbitMQ) | Jika `RabbitMQ.Enable` di config Core: publish ke default exchange, routing key = `QueueName`, `ContentType=application/json`, persistent |
| Payload | JSON (Newtonsoft default) `LogModel`: `Datetime` (DateTime lokal), `AppName`, `FileName` (nama node / app), `LogType` (`"info"` / `"transaction"`), `Title`, `Detail` |
| Title trace | `"<{title}> Message from {node} {remote}"` atau `"<{title}> Message to {node} {remote}"` |
| Fallback | Bila tidak terkirim: file `{TraceDir}/{appName}/{logName}_{yyyyMMdd_HH}.log` |

## 6. Kontrak Database (ditulis oleh SDK)

| Tabel | Kolom yang di-update | Kapan |
|-------|----------------------|-------|
| `sw_app` | `status` (0/1), `last_update` | Start (UP) / Stop (DOWN) |
| `sw_nodes` (by `app_name`) | `status` | Start / Stop |
| `sw_nodes` (by `node_name`) | `remote`, `last_connected` / `last_disconnected` | Start / Close `NodeRemote` |
| `sw_nodes` | `conn_in` | Source connect / disconnect |
| `sw_nodes` | `conn_out` | Sink connect / disconnect |
| `sw_connections` (by `conn_name`) | `remote`, `last_connected` / `last_disconnected` | Connect/disconnect & timer 1 menit |

Dibaca: `sw_app` (`command_port`, host Log Services), `sw_nodes`, `sw_connections` (kolom lengkap di `DbMgr.GetConnection`), serta tabel domain routing/fee/produk (hanya modul bisnis).

## 7. Keputusan Kompatibilitas untuk SDK Baru

Keputusan tim atas pertanyaan terbuka tercatat di [dok. 08 §5](08-roadmap-testing-risiko.md#5-keputusan-sebelumnya-pertanyaan-terbuka).

| Topik | Keputusan |
|-------|-----------|
| Serializer | Wire serializer dikunci oleh golden test. **Keputusan (Q3): versi 1.x memakai** Newtonsoft.Json di balik abstraksi `ICoreMessageSerializer` — risiko nol. **Versi 2.x (rencana):** `System.Text.Json` + source generator + converter `decimal` gaya Newtonsoft + `JsonIgnoreCondition.Never` + urutan properti eksplisit — hanya diaktifkan setelah lulus 100% golden test. |
| Properti tambahan (keputusan Q1) | **Skema JSON tidak boleh bertambah.** Model kontrak tidak memiliki `[JsonExtensionData]`; properti tak dikenal dari Core diabaikan saat deserialisasi (perilaku lama). Setiap informasi tambahan dari interface **wajib** ditaruh di `additional_data` (`Dictionary<string, object>`). SDK menyediakan helper typed `request.AdditionalData.Set("key", value)` / `TryGet<T>("key", out value)`, dan analyzer/SimCore memperingatkan bila ada properti di luar skema §2. |
| `CoreResponse.From(request)` | Menyalin field seperti SDK lama (§3.6) **dan mengisi `msgtype` response** dari MTI request (keputusan Q2) dengan aturan `NbMessage.GetMsgTypeResp` (SDK lama, sama dengan Core): digit ke-3 `0`→`1` (request) atau `2`→`3` (advice), lalu digit terakhir `1` (repeat) → `0`; mis. `0200`→`0210`, `0201`→`0210`, `0220`→`0230`, `0221`→`0230`, `0400`→`0410`, `0800`→`0810`; digit ke-3 lain atau panjang ≠ 4 → dikembalikan apa adanya; `null`/kosong → `""`. `pos_entry_mode` **disalin** dari request (keputusan Q2, sama dengan Core). Developer masih boleh menimpa kedua nilai. Catatan: bug Core `to_acc_type = req.from_acc_type` **tidak** ditiru — SDK baru tetap menyalin `to_acc_type` dari `to_acc_type`. |
| Encoding | UTF-8 dua arah (§1.2). |
| Nama properti C# baru | Boleh PascalCase di model developer (mis. `TraceNumber`) **selama** nama JSON dikunci via atribut ke nama lama (`trace_number`). |
| Namespace kontrak | `SyncNetPro.Contracts` (paket terpisah, versi mayor dibekukan). |
