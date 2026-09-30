# SyncNetSdkPro — Dokumen Analisa

Folder ini berisi analisa untuk membangun **SyncNetSdkPro**, pengganti modern dari
`SyncNetSdk` (SDK lama) yang dipakai untuk membangun interface inbound/outbound
antara **SyncNet Core** dan sistem eksternal (bank, biller, channel, dsb).

> Status: keputusan tim final (lihat *Keputusan Tim*); **Fase 1 (Fondasi & Kontrak), Fase 2 (Inti SDK), Fase 3 (Transport Remote), dan Fase 4 (SimCore) selesai** — lihat [dok. 08 §1.1](08-roadmap-testing-risiko.md#11-status). SDK lama (`SyncNetSdk`)
> dan interface lama (`ApiInterfaces/*`) hanya dibaca dan **tidak diubah**.

## Daftar Dokumen

| No | Dokumen | Isi |
|----|---------|-----|
| 01 | [Analisa SDK Lama](01-analisa-sdk-lama.md) | Arsitektur, alur pesan, inventaris komponen, dan temuan masalah (bug, teknis, desain) pada `SyncNetSdk`. |
| 02 | [Kontrak Pesan ke Core](02-kontrak-pesan-core.md) | Spesifikasi *wire format* yang **tidak boleh berubah**: framing TCP, skema JSON `Request`/`Response`, kunci korelasi, protokol command & log. |
| 03 | [Inventaris & Pemetaan API](03-inventaris-dan-pemetaan-api.md) | Keputusan per komponen/method: dipertahankan, diganti nama, digabung, atau dihapus (usang). Standar penamaan. |
| 04 | [Arsitektur SDK Baru](04-arsitektur-sdk-baru.md) | Teknologi, struktur solusi & paket, model pemrograman (handler), contoh API publik. |
| 05 | [Cross-Platform (Windows → Linux)](05-cross-platform.md) | Sumber masalah portabilitas di SDK lama dan aturan desain agar pindah OS tanpa konfigurasi ulang. |
| 06 | [Template Interface](06-template-interface.md) | Desain `dotnet new` template untuk interface baru (ISO/TCP, JSON/HTTP client, HTTP server/channel). |
| 07 | [SimCore](07-simcore.md) | Desain simulator core untuk mengembangkan & menguji interface tanpa menginstal SyncNet Core. |
| 08 | [Roadmap, Pengujian & Risiko](08-roadmap-testing-risiko.md) | Tahapan implementasi, strategi pengujian kompatibilitas, risiko, dan pertanyaan terbuka. |

## Ringkasan Eksekutif

**Kondisi saat ini.** `SyncNetSdk` (~17.000 baris, .NET 10, ~120 file) sudah berjalan
di produksi dan dipakai oleh `ApiBillerIso`, `ApiBillerJson`, `ApiChannel`. Namun:

- Seluruh interaksi melalui satu kelas statis `AppProcessor` + satu interface besar
  `IAppProcessor` (11 method) — developer wajib mengimplementasikan semua method,
  sehingga interface lama penuh `throw new NotImplementedException()`.
- Konfigurasi sangat terikat pada instalasi Core: SDK membaca
  `{AppDir}/Core/Bin/appsettings.json`, `Resources.bin`, `Keys/PrivateKey.pem`, dan
  database PostgreSQL Core sejak konstruktor. **Tanpa Core terinstal, interface
  tidak bisa dijalankan sama sekali.**
- Korelasi request↔response (cache, key, timeout), non-persistent TCP, dan mapping
  dilakukan manual di tiap interface → kode berulang dan rawan salah.
- Ada campuran domain bisnis (routing biller, fee, margin, commitment) di dalam SDK
  transport; banyak utilitas tidak terpakai; beberapa bug nyata (lihat dok. 01 §5).
- Path, nama file, dan konfigurasi dipisah per OS (`Windows`/`Linux`) di beberapa
  tempat, sehingga pindah platform butuh penyesuaian.

**Arah SDK baru (SyncNetSdkPro).**

1. **.NET 10 (LTS) + Generic Host**: DI, `IOptions`, `ILogger`, health check,
   OpenTelemetry, `System.IO.Pipelines`, `IHttpClientFactory`, resiliency standar.
2. **Model handler berbasis peran** (outbound / inbound, TCP / HTTP) dengan
   base class *override-what-you-need* — tidak ada lagi `NotImplementedException`.
3. **Request/response ter-korelasi otomatis** (`await SendAndReceiveAsync(...)`)
   untuk TCP persistent, TCP non-persistent, dan HTTP.
4. **Kontrak pesan ke Core dibekukan** dalam paket terpisah
   `SyncNetPro.Contracts` + *golden test* byte-per-byte terhadap SDK lama.
5. **Cross-platform by default**: satu set konfigurasi, path relatif terhadap
   `SYNCNET_HOME`, penamaan file konsisten, tanpa percabangan `if Windows`.
6. **Template `dotnet new syncnet-*`** dan **SimCore** (CLI + Web UI + library
   in-process untuk test) agar developer baru bisa produktif tanpa Core.
7. SDK lama tetap dipakai interface lama; SDK baru memakai namespace dan paket
   berbeda (`SyncNetPro.*`) sehingga keduanya dapat berjalan berdampingan.

## Keputusan Tim

Detail di [08 §5](08-roadmap-testing-risiko.md#5-keputusan-sebelumnya-pertanyaan-terbuka).

| # | Topik | Keputusan |
|---|-------|-----------|
| Q1 | Field JSON tambahan ke Core | Tidak boleh; data tambahan lewat `additional_data` (`Dictionary<string, object>`) |
| Q2 | `msgtype` pada response yang dibuat dari request | Diisi MTI response (`0200`→`0210`, dst.); `pos_entry_mode` disalin dari request |
| Q3 | Serializer | Newtonsoft.Json di 1.x; System.Text.Json di 2.x setelah golden test stabil |
| Q4 | Feed NuGet | GitHub Packages |
| Q5 | Obfuscation | Tidak perlu |
| Q6 | Modul Routing/Fee | .dll/paket terpisah dengan versi independen |
| Q7 | Target Linux | Ubuntu 22.04 LTS+ / 26.04 LTS; container opsional |
| Q8 | Nama file log | Dinormalisasi sama di semua OS; opsi legacy tersedia |
