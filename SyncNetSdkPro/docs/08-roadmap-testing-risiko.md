# 08 — Roadmap, Strategi Pengujian & Risiko

## 1. Tahapan

| Fase | Lingkup | Keluaran | Kriteria selesai |
|------|---------|----------|------------------|
| **0. Analisa** (dokumen ini) | Studi SDK lama, kontrak, desain | `SyncNetSdkPro/docs/*` | Disetujui tim (lihat §5 pertanyaan terbuka) |
| **1. Fondasi & Kontrak** | Struktur solusi, CI Windows+Linux, `SyncNetPro.Contracts`, serializer, **golden test** vs SDK lama | Paket Contracts 1.0.0-preview | 100% golden test lulus di kedua OS |
| **2. Inti SDK** | Host builder, `SyncNetInterface`, `CoreChannel` (Sink/Source), `NodeRegistry` (Postgres & JSON), command server, trace/log sinks, status reporter, health check | `SyncNetPro.Sdk` preview | Interface contoh berjalan terhadap Core UAT & SimCore |
| **3. Transport Remote** | TCP server/client (semua codec, persistent & non-persistent), HTTP server/client, korelasi, timer echo/key-exchange/sign-on | `SyncNetPro.Sdk` beta | Matriks test framing lulus; B1/B2/B6/B8 terbukti diperbaiki |
| **4. SimCore & Testing** | SimCore CLI/Web/library/container, skenario, record/replay | `SyncNetPro.SimCore`, `SyncNetPro.Sdk.Testing` | `ApiBillerJson` lama berjalan normal terhadap SimCore |
| **5. Modul** | `Iso8583`, `Hsm`, `Toolkit`, `Routing` (port) | Paket modul | Test parity dengan implementasi lama (input sama → output sama) |
| **6. Template & Dokumentasi** | `dotnet new` template, sample (3 padanan interface lama), getting started, cookbook, API reference | `SyncNetPro.Templates` | Developer baru (tanpa pengalaman SyncNet) membuat interface jalan di SimCore < 1 hari |
| **7. Pilot** | 1 interface baru di produksi (disarankan outbound HTTP) berdampingan dengan interface lama | Rilis 1.0.0 | 2 minggu produksi tanpa insiden terkait SDK |

Interface lama **tidak** dimigrasi dalam roadmap ini (sesuai kebutuhan); migrasi opsional dapat
direncanakan terpisah menggunakan tabel pemetaan dok. 03.

## 2. Strategi Pengujian

### 2.1 Golden test kontrak (paling kritis)

1. Buat proyek generator kecil yang mereferensikan **`SyncNetSdk` lama** (read-only, via `ProjectReference`
   ke folder `SyncNetSdk/` tanpa mengubahnya) dan menserialisasi ±50 kasus `Request`/`Response`
   (kosong, penuh, decimal bulat/pecahan/negatif/besar, `additional_data` bersarang, `echo_data` objek/array/string,
   karakter ASCII & non-ASCII, `hsm_cmd` berbagai nilai) → simpan sebagai file `.json` + `.bin` (dengan header TCP).
2. Test SDK baru: serialisasi objek setara → **byte-identik** dengan file `.bin`
   (pengecualian terdokumentasi: kasus non-ASCII yang di SDK lama rusak/exception, dibandingkan dengan UTF-8 yang benar).
   Pengecualian kedua (keputusan Q2): `CoreResponse.From(request)` menghasilkan `msgtype` response; golden file untuk kasus ini
   dibuat dari SDK lama lalu `msgtype` dibandingkan dengan output `NbMessage.GetMsgTypeResp(request.msgtype)` SDK lama.
5. Test skema: serialisasi `CoreRequest`/`CoreResponse` tidak boleh menghasilkan properti di luar daftar dok. 02 §2 (keputusan Q1).
3. Deserialisasi silang: pesan yang dihasilkan Core (`SyncNetCore/Message`, termasuk properti `private_data` tambahan)
   dapat dibaca SDK baru tanpa kehilangan field yang dipakai.
4. Golden test juga untuk: frame command port, `LogModel`, dan setiap varian `TcpHeader` (termasuk batas 9.999 / 65.535).

### 2.2 Level test lainnya

| Level | Isi |
|-------|-----|
| Unit | Codec framing, korelasi & timeout (`FakeTimeProvider`), resolusi path per OS, parser command, masking |
| Integrasi | SDK ↔ SimCore via socket nyata; SDK ↔ PostgreSQL (Testcontainers) untuk `NodeConfigurationSource` & status |
| Kesetaraan | Interface lama vs interface sampel baru menerima skenario SimCore yang sama → `Response` ke Core identik |
| Ketahanan | Putus-sambung Core/remote, burst 1.000 TPS, respons terlambat, pesan rusak/panjang header salah |
| Cross-platform | Seluruh suite di `ubuntu-latest` & `windows-latest` |
| UAT | Terhadap Core sebenarnya + biller sandbox sebelum pilot |

## 3. Risiko & Mitigasi

| # | Risiko | Dampak | Mitigasi |
|---|--------|--------|----------|
| R1 | Perbedaan halus serialisasi (decimal `10000.0` vs `10000`, null, urutan, enum) | Core salah parse / nilai berubah | Fase 1 tetap Newtonsoft; STJ hanya setelah golden test 100%; test di CI setiap PR |
| R2 | Perilaku implisit Core yang tidak terdokumentasi (mis. field `private_data` yang ternyata dibaca Core dari interface) | Transaksi gagal di produksi | Review dok. 02 bersama tim Core; uji kesetaraan & UAT; SimCore memvalidasi kunci korelasi |
| R9 | Perubahan perilaku `msgtype` (Q2): SDK lama mengirim `msgtype` request apa adanya/`null` pada response | Core/laporan yang membaca `msgtype` response dari interface melihat nilai berbeda antara interface lama & baru | Golden test khusus; konfirmasi ke tim Core bahwa Core menerima MTI response; didokumentasikan di release notes |
| R3 | Perbaikan bug B1/B2/B4 mengubah perilaku yang (tanpa sadar) diandalkan interface/remote tertentu | Remote menolak pesan | Perbaikan hanya berlaku di SDK baru; interface lama tetap memakai SDK lama; didokumentasikan di release notes |
| R4 | SimCore tidak sinkron dengan Core seiring waktu | Developer lolos di SimCore, gagal di UAT | SimCore di repo yang sama; checklist rilis Core mewajibkan update SimCore; uji kesetaraan interface lama vs SimCore di CI |
| R5 | Modul Routing/Fee di-port dengan perubahan logika | Salah pilih biller / salah fee | Port 1:1 + test parity input/output terhadap implementasi lama sebelum refactor apa pun |
| R6 | Dua SDK dipelihara paralel | Beban tim | SDK lama dibekukan (hanya bugfix kritis); fitur baru hanya di SDK baru |
| R7 | Tanpa obfuscation, kode SDK terbaca | Kekhawatiran IP | Diterima (keputusan Q5): SDK hanya berisi infrastruktur integrasi; paket di GitHub Packages bersifat privat (akses terbatas org) |
| R8 | Adopsi developer rendah | Investasi tidak kembali | Template + SimCore + dokumentasi + sesi onboarding; ukur waktu “hello interface” |

## 4. Estimasi Kasar

| Fase | Perkiraan (1–2 developer senior) |
|------|----------------------------------|
| 1 | 1–2 minggu |
| 2 | 3–4 minggu |
| 3 | 3 minggu |
| 4 | 2–3 minggu |
| 5 | 2–3 minggu |
| 6 | 1–2 minggu |
| 7 | 2 minggu pilot |

Total ±3,5–5 bulan kalender hingga 1.0.0, dapat dipercepat dengan paralelisasi fase 4–6.

## 5. Keputusan (sebelumnya pertanyaan terbuka)

Semua pertanyaan telah dijawab tim; keputusan di bawah ini **mengikat** untuk implementasi
dan sudah diterapkan ke dokumen 02–07.

| # | Pertanyaan | Keputusan |
|---|------------|-----------|
| Q1 | Apakah Core boleh menerima properti JSON tambahan dari interface (untuk round-trip `private_data` penuh)? | **Tidak.** Skema JSON tetap. Informasi tambahan dari interface ditaruh di `additional_data` (`Dictionary<string, object>`). |
| Q2 | `CoreResponse.From(request)` mengikuti perilaku SDK (tanpa `msgtype`) atau Core (isi `msgtype` respons)? | **Isi `msgtype` response** (MTI request → MTI response, mis. `0200` → `0210`, aturan `NbMessage.GetMsgTypeResp`). |
| Q3 | Serializer target jangka panjang: Newtonsoft atau System.Text.Json? | Newtonsoft di 1.x; STJ di 2.x setelah golden test stabil. |
| Q4 | Distribusi paket: feed NuGet internal apa (Azure Artifacts, GitHub Packages, BaGet)? | **GitHub Packages.** |
| Q5 | Apakah SDK baru tetap perlu obfuscation? | **Tidak perlu.** SDK dirilis dengan symbol + SourceLink. |
| Q6 | Modul Routing/Fee: tetap di SDK atau dipindah ke layanan terpisah? | Dibuat .dll (paket) terpisah `SyncNetPro.Routing` dengan versi independen, agar update kebutuhan bisnis tidak berdampak ke SDK. |
| Q7 | Versi minimum OS Linux target (RHEL/Ubuntu) dan apakah container menjadi target deploy? | Ubuntu 22.04 LTS+ / 26.04 LTS; container opsional. |
| Q8 | Nama file log di Windows: ikut normalisasi baru atau tetap nama lama? | Normalisasi baru (sama di semua OS), opsi legacy tersedia. |
