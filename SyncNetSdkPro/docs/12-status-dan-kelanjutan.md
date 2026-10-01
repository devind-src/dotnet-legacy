# 12 — Status Proyek & Panduan Melanjutkan

Dokumen serah-terima untuk melanjutkan pekerjaan SyncNetSdkPro di sesi/chat baru (oleh developer atau asisten AI).
Baca dokumen ini lebih dulu, lalu dokumen yang dirujuk sesuai kebutuhan.

> Terakhir diperbarui: setelah merge fase 6 (PR #4, commit `52dd15d` di `main`). Fase berikutnya: **fase 7 — pilot**,
> belum dimulai, menunggu jawaban tim (§6).

## 1. Ringkasan Permintaan Awal

Membangun SDK baru **SyncNetSdkPro** (folder `SyncNetSdkPro/`) sebagai pengganti `SyncNetSdk` untuk interface
inbound/outbound baru yang menghubungkan SyncNet Core dengan sistem eksternal. Kriteria:

- Dimodernisasi (.NET 10, Generic Host, DI); method usang tidak dibuat ulang; nama method distandarkan.
- Pindah Windows → Linux tanpa konfigurasi ulang.
- Mudah dipakai developer baru; **template interface** dan **SimCore** (simulasi Core tanpa instalasi Core).
- Interface baru memakai SDK baru; interface lama tetap memakai SDK lama.
- **Yang dipertahankan: format pesan ke Core tidak boleh berubah.**
- Semua dokumen di `SyncNetSdkPro/docs`.

## 2. Aturan yang Wajib Dipatuhi

| Aturan | Sumber |
|--------|--------|
| `SyncNetSdk/`, `ApiInterfaces/*`, `SyncNetCore/` dan proyek lama lain **read-only** — jangan ubah kode apa pun | Permintaan awal |
| Format pesan JSON/TCP ke Core tidak berubah; dijaga golden test byte-per-byte terhadap SDK lama | Permintaan awal, dok. 02 |
| Semua bug SDK lama wajib diperbaiki di SDK baru (tidak ditiru demi kompatibilitas) | Keputusan tim, dok. 01 §5.1.1 |
| Keputusan Q1–Q8 mengikat (tabel di [dok. 08 §5](08-roadmap-testing-risiko.md#5-keputusan-sebelumnya-pertanyaan-terbuka)) | Keputusan tim |
| Command `TRACE CLEAR` dihapus (trace = jejak audit); trace yang tidak muat antrean ditulis ke file fallback, tidak boleh hilang | Keputusan tim |
| Alur kerja per fase: kerjakan → commit → push ke branch kerja → buka PR → tunggu CI hijau → **merge hanya setelah user menyetujui** (squash) → lanjut fase berikutnya | Kebiasaan proyek |
| Dokumen dan komunikasi dalam Bahasa Indonesia; identifier kode dalam Bahasa Inggris | Kebiasaan proyek |

Ringkasan keputusan Q1–Q8: `additional_data` untuk data tambahan (Q1); response mengisi `msgtype` response dan
menyalin `pos_entry_mode` (Q2); Newtonsoft di 1.x (Q3); GitHub Packages (Q4); tanpa obfuscation (Q5); Routing/Fee
paket terpisah dengan versi independen (Q6); Ubuntu 22.04+/26.04 (Q7); nama file log dinormalisasi (Q8).

## 3. Status per Fase

| Fase | PR | Isi utama |
|------|----|-----------|
| 0 Analisa | — | Dok. 01–08 + keputusan tim |
| 1 Fondasi & kontrak | #1 | Solusi, CPM, MinVer, `SyncNetPro.Contracts`, golden generator, CI |
| 2 Inti SDK | #1 | Host, `SyncNetInterface`, kanal Core, node registry, command server, trace/log, status DB |
| 3 Transport remote | #1 | TCP/HTTP klien & server, korelasi, timer, RESYNC |
| 4 SimCore | #2 | `SyncNetPro.Sdk.Testing`, tool `syncnet-simcore` (CLI + Web UI) |
| 5 Modul | #3 | `Iso8583`, `Toolkit`, `Hsm`, `Routing` (versi independen) + `ISyncNetModule` |
| 6 Template & dokumen | #4 | `SyncNetPro.Templates` (5 template), stub ISO SimCore, `SimTcpClient`, dok. 09–11 |
| **7 Pilot** | — | **Belum dimulai** (§6) |

Angka saat ini: 515 test SDK lulus (dengan PostgreSQL), 35 test proyek hasil template (6 varian). Bug B1–B35
(dok. 01 §5) semuanya ✅. Detail per fase: [dok. 08 §1.1](08-roadmap-testing-risiko.md#11-status).

## 4. Peta Repositori

```
dotnet-legacy/
├── .github/workflows/syncnetsdkpro.yml   CI: build-test (Ubuntu+Windows, PG di Linux), golden-check, templates, publish
├── SyncNetSdk/ ApiInterfaces/ SyncNetCore/ …   SDK & aplikasi lama (READ-ONLY)
└── SyncNetSdkPro/
    ├── SyncNetSdkPro.slnx  global.json  Directory.Build.props  Directory.Packages.props (CPM)
    ├── src/   Contracts · Sdk · Sdk.Testing · Iso8583 · Toolkit · Hsm · Routing
    ├── tests/ satu proyek test per paket + SimCore.Tests (xUnit v3, Microsoft.Testing.Platform)
    ├── tools/ SyncNetPro.SimCore (dotnet tool `syncnet-simcore`), SyncNetPro.GoldenGenerator (pakai SDK lama)
    ├── templates/ SyncNetPro.Templates.csproj + content/_shared + content/syncnet-*
    ├── scripts/test-templates.sh   smoke test template
    ├── samples/Sample.Outbound
    └── docs/  01–12
```

Dokumen kunci: kontrak Core [02](02-kontrak-pesan-core.md), pemetaan API lama→baru [03](03-inventaris-dan-pemetaan-api.md),
API publik [04 §6](04-arsitektur-sdk-baru.md#6-status-implementasi-fase-23), template [06](06-template-interface.md),
SimCore [07](07-simcore.md), roadmap/keputusan [08](08-roadmap-testing-risiko.md).

## 5. Cara Kerja Teknis

### 5.1 Build & test

```bash
cd SyncNetSdkPro
dotnet build SyncNetSdkPro.slnx -c Release
dotnet test --solution SyncNetSdkPro.slnx -c Release --no-build      # xUnit v3 lewat MTP: pakai --solution
```

Test PostgreSQL (resolver routing, integrasi node/status) dilewati bila `SYNCNET_TEST_PG` kosong. Di container dev:

```bash
service postgresql start
SYNCNET_TEST_PG="Host=127.0.0.1;Database=sdkpro_test;Username=sdkpro;Password=sdkpro" dotnet test --solution SyncNetSdkPro.slnx -c Release
```

(Bila user/database belum ada: `sudo -u postgres psql -c "CREATE USER sdkpro WITH PASSWORD 'sdkpro' SUPERUSER;" -c "CREATE DATABASE sdkpro_test OWNER sdkpro;"`.)

### 5.2 Golden file

`dotnet run --project tools/SyncNetPro.GoldenGenerator -c Release` menjalankan **kode SDK lama** dan menulis ulang
`tests/*/Golden`. Golden resolver routing butuh `SYNCNET_TEST_PG`. CI (job `golden-check`) gagal bila hasil
generator berbeda dengan file di repo — setiap perubahan golden harus berasal dari generator, tidak diedit manual.

### 5.3 Template

```bash
scripts/test-templates.sh [folder-kerja]                 # pack semua paket → dotnet new → build → test, 6 varian
TEMPLATES="syncnet-blank syncnet-inbound-http:--with-routing" scripts/test-templates.sh /tmp/tt
dotnet build templates && dotnet new install templates/obj/templates/<nama>   # coba satu template tanpa pack
```

### 5.4 Versi & rilis

- MinVer: tag `sdkpro-v*` → semua paket kecuali Routing (termasuk Templates & SimCore); tag `routing-v*` → hanya
  `SyncNetPro.Routing` (dependensi SDK dipatok ke tag `sdkpro-v*` terakhir via `SdkReleaseVersion`).
- Job `publish` CI mendorong paket ke GitHub Packages saat tag dibuat. Default `--routing-version` template diisi
  dari tag `routing-v*` terakhir (`RoutingTemplateVersion`).
- Belum ada tag rilis; target pertama `sdkpro-v1.0.0` + `routing-v1.0.0` setelah pilot (fase 7).

### 5.5 Git & PR

- Branch kerja yang ditetapkan untuk sesi; bila PR sebelumnya sudah di-merge, mulai ulang branch dari `main`
  (`git fetch origin main && git checkout -B <branch> origin/main`).
- Force push dapat ditolak oleh izin sesi; bila branch remote masih berisi riwayat yang sudah di-squash ke `main`,
  merge branch remote itu (isi identik, konflik diselesaikan dengan versi sendiri) lalu push biasa.
- Commit diakhiri baris atribusi yang diminta lingkungan sesi; PR diakhiri tautan Claude Code + sesi.
- Setelah membuka PR: pantau CI; perbaiki bila merah; jangan merge tanpa persetujuan user.

### 5.6 Pelajaran teknis (jebakan yang sudah ditemui)

| Area | Jebakan | Solusi yang dipakai |
|------|---------|---------------------|
| Golden JSON | Newtonsoft mengubah teks tanggal menjadi `DateTime` saat membaca golden | `DateParseHandling.None` |
| Golden DB | NULL vs `""` berbeda antara SDK lama dan PostgreSQL | Generator menormalkan sesuai perilaku lama |
| 3DES | Kunci 16 byte (double-length) harus diperluas K1K2K1 untuk OpenSSL | Ekspansi eksplisit di `DesEcb` |
| Waktu di test | `FakeTimeProvider` tidak bisa mundur | `TestClock` sendiri di test routing |
| Analyzer | `TreatWarningsAsErrors` + `AnalysisLevel latest-recommended` (CA*) | Perbaiki kode, bukan menurunkan level |
| Template engine | Engine mengevaluasi atribut MSBuild `Condition` sendiri dan bisa menghapusnya | Bungkus dengan `<!--/-:cnd:noEmit -->` … `<!--/+:cnd:noEmit -->` |
| Template engine | `WriteLinesToFile` mengubah `\\.` di regex `forms` | Pola `[.]` |
| csproj | Komentar XML tidak boleh berisi `--` | Ubah kalimat komentar |
| Pack template | NU5110/NU5111 untuk `.ps1` di konten template | `NoWarn` di `SyncNetPro.Templates.csproj` |
| Linux | Nama file case-sensitive (`appSettings.json`) | Template memakai huruf kecil + cek build (B9) |
| Line ending | Skrip `.sh` di Windows CI | `SyncNetSdkPro/.gitattributes` `eol=lf` |

## 6. Fase 7 — Pilot (Langkah Berikutnya)

Tujuan (dok. 08): satu interface baru berjalan di produksi berdampingan dengan interface lama, 2 minggu tanpa
insiden terkait SDK → rilis 1.0.0. Rencana yang sudah disampaikan ke user:

**A. Dikerjakan di repo (via PR):**

1. Interface pilot dari template yang sesuai (usulan: **outbound HTTP**, risiko paling rendah) — mapping sesuai
   spesifikasi biller, skenario SimCore, test end-to-end.
2. Alat **uji kesetaraan** interface lama vs baru: transaksi sama → bandingkan pesan ke Core per field (dijalankan di
   UAT karena SDK lama butuh `Core/Bin`; tertunda sejak fase 4).
3. **Runbook operasional** (dok. baru): checklist pra-deploy (config Core, `sw_nodes`/`sw_connections`, port,
   `SYNCNET_HOME`), cut-over & rollback (service lama dan baru tidak aktif bersamaan untuk satu node), pemantauan
   2 minggu (status node, trace Log Services, log, `OverflowCount`/`LostCount`, health check, response code, latency),
   definisi "insiden terkait SDK".
4. **Persiapan rilis 1.0.0**: changelog, review API publik (hindari breaking change setelah 1.0), alur tag
   `sdkpro-v1.0.0`/`routing-v1.0.0`, opsional publikasi image Docker SimCore di CI.
5. Perbaikan temuan pilot.

**B. Disiapkan tim:** pilihan interface pilot + spesifikasi biller, `node_name`/`app_name`, daftar response code;
lingkungan UAT dengan Core & `Core/Bin`; deploy & pemantauan di server; keputusan go/no-go dan pembuatan tag rilis.

**Pertanyaan terbuka ke user (belum dijawab):**

- Interface pilot yang mana (usulan outbound HTTP)? Biller apa dan apakah dokumen spesifikasinya tersedia?
- Bila spesifikasi belum siap: setujui mulai dari A2–A4 lebih dulu, A1 menyusul.

## 7. Backlog (di luar fase 7, opsional)

| Item | Catatan |
|------|---------|
| Skenario SimCore format YAML | Saat ini JSON |
| Record & replay transaksi SimCore | — |
| NodeSource via HTTP SimCore | Saat ini `NodeSource=Json` dari file |
| Status node dari interface di Web UI SimCore | — |
| Publikasi image Docker SimCore di CI | `Dockerfile` sudah ada |
| System.Text.Json | Rencana 2.x (Q3), setelah golden test stabil |
