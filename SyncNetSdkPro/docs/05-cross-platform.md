# 05 — Cross-Platform: Pindah Windows → Linux Tanpa Konfigurasi Ulang

Target: binary interface yang sama (framework-dependent `net10.0`, `AnyCPU`) dapat
dipindah dari server Windows ke Linux (atau sebaliknya) **tanpa mengubah file
konfigurasi interface** dan tanpa rebuild.

## 1. Temuan di SDK Lama & Interface

| # | Lokasi | Masalah | Dampak saat pindah ke Linux |
|---|--------|---------|------------------------------|
| P1 | `SdkConfig.Initialize` | `AppDir` dibaca dari `AppDir.Windows` / `AppDir.Linux` di appsettings interface, lalu path Core dibentuk dengan string literal `\` (Windows) atau `/` (Linux). | Wajib mengisi dua nilai; salah satu kosong → fallback hard-coded (`C:\SyncNet` / `/opt/SyncNet`). |
| P2 | `ConfigModel.Paths.Windows/Linux` (config Core) | `App`, `Logs`, `Traces` diduplikasi per OS. | Dua set path harus dijaga sinkron. |
| P3 | `NbLogger.WriteLog` | `$@"{_logdir}\{FileName}_{dt}.log"` di Windows; di Linux nama file dilower-case & spasi → `-` (`AdjustFileLinux`). | Nama file log berbeda antar OS; skrip/monitoring log harus dibedakan. |
| P4 | `TcpLogWorker`/`RabbitLogWorker`/`NbTrace` | `AdjustFileName` hanya mengubah nama di non-Windows. | Sama dengan P3 untuk trace fallback. |
| P5 | `ApiBillerIso.csproj` | `appSettings.json` vs file `appsettings.json` (case). | Di Linux file tidak tersalin → `SdkConfig` gagal membaca `AppDir`. |
| P6 | `*.csproj` interface | `HintPath ..\..\..\..\SDK\SyncNetSdk\bin\Publish\SyncNetSdk.dll`, publish profile `C:\Projects\...`. | Tidak bisa build di Linux/CI; bergantung struktur folder mesin developer. |
| P7 | `Program.cs` interface | `UseWindowsService()` + `UseSystemd()` sudah benar, tetapi instalasi service didokumentasikan hanya `sc create` (README Core). | Tidak ada unit file systemd standar. |
| P8 | `NbSystem` | `Environment.SpecialFolder.ProgramFiles`. | Kosong di Linux (tidak terpakai, tetapi jebakan). |
| P9 | Encoding | `Encoding.RegisterProvider(CodePagesEncodingProvider)` untuk EBCDIC. | Aman lintas OS (tetap dipertahankan di modul ISO). |
| P10 | `DateTime.Now` di log & DB | Bergantung zona waktu OS; container Linux sering UTC. | Timestamp `last_update`/`last_connected` bergeser bila TZ server berbeda. |
| P11 | Bind TCP `IPAddress.Any` | Di Linux, port < 1024 butuh hak root/capability. | Port command/remote rendah gagal bind sebagai user service. |

## 2. Aturan Desain SyncNetSdkPro

1. **Satu konfigurasi, tanpa bagian per-OS.** Opsi `SyncNet:Home` (opsional). Urutan resolusi:
   1. `SyncNet:Home` di appsettings / env `SyncNet__Home`
   2. env `SYNCNET_HOME`
   3. default OS: `C:\SyncNet` (Windows) atau `/opt/SyncNet` (Linux/macOS) — **satu-satunya**
      percabangan OS, terpusat di `SyncNetPaths`.
2. **Semua path relatif terhadap `Home`** dan dibangun dengan `Path.Combine` / `Path.Join`
   (`Core/Bin/appsettings.json`, `Core/Bin/Resources.bin`, `Keys/PrivateKey.pem`, `Logs`, `Traces`).
3. **Kompatibel config Core lama**: bila config Core masih berisi `Paths.Windows` dan
   `Paths.Linux`, SDK memilih sesuai OS berjalan (sama dengan perilaku lama); bila salah
   satu kosong, diturunkan dari `Home`. Tidak perlu mengedit config Core.
4. **Nama file log/trace dinormalisasi sama di semua OS**: huruf kecil, spasi → `-`,
   karakter tak valid dihapus (`api-biller_2026-09-30.log`). Opsi
   `Logging:File:LegacyWindowsNames=true` untuk mempertahankan nama lama di Windows bila
   ada tooling yang bergantung.
5. **Case-sensitive safe**: semua referensi file memakai huruf kecil (`appsettings.json`);
   template dan analyzer build memeriksa `None Update`/`Content Include` yang tidak cocok case.
6. **Referensi SDK via NuGet dari GitHub Packages** (`nuget.config` di template menambahkan source `https://nuget.pkg.github.com/devind-src/index.json`), bukan `HintPath`.
   Pengembangan lokal: `ProjectReference` relatif di dalam repo.
7. **Waktu**: `TimeProvider` di seluruh SDK; timestamp log memakai offset lokal eksplisit
   (`DateTimeOffset`), nilai ke DB tetap “local time” seperti perilaku lama (kolom tanpa TZ) —
   dicatat di health check bila TZ proses ≠ TZ yang dikonfigurasi (`SyncNet:TimeZone`, opsional).
8. **Line ending & encoding file**: UTF-8 tanpa BOM, `\n` (log) — dapat dibaca di kedua OS.
9. **Service**: template menyertakan `deploy/syncnet-<nama>.service` (systemd,
   `Type=notify`, `User=syncnet`, `AmbientCapabilities=CAP_NET_BIND_SERVICE`,
   `Environment=SYNCNET_HOME=/opt/SyncNet`) dan `deploy/install-windows-service.ps1`
   (`New-Service`/`sc.exe`). Kode yang sama; `UseSystemd()`/`UseWindowsService()` aktif otomatis.
10. **Tanpa API khusus Windows** (registry, EventLog, WMI, `System.Drawing`). Analyzer
    `CA1416` (platform compatibility) diset sebagai error.

## 3. Checklist Migrasi Server (Interface Baru)

| Langkah | Windows | Linux |
|---------|---------|-------|
| 1. Salin folder publish | `C:\SyncNet\Interfaces\api-biller\` | `/opt/SyncNet/Interfaces/api-biller/` |
| 2. Konfigurasi interface | `appsettings.json` **sama persis** | `appsettings.json` **sama persis** |
| 3. Home | default `C:\SyncNet` | default `/opt/SyncNet` (atau `SYNCNET_HOME`) |
| 4. Service | `deploy/install-windows-service.ps1` | `sudo cp deploy/*.service /etc/systemd/system && systemctl enable --now …` |
| 5. Verifikasi | `GET /health` atau command `VERSION` | sama |

Tidak ada nilai konfigurasi yang harus diubah karena perbedaan OS.

## 4. Verifikasi Otomatis

- CI menjalankan seluruh test pada **`ubuntu-latest` dan `windows-latest`**. Target Linux resmi: **Ubuntu 22.04 LTS+ / 26.04 LTS** (Q7); container opsional.
- Test khusus: resolusi `Home`, pembentukan path Core, normalisasi nama log, framing TCP
  (endian), dan golden test kontrak pesan harus menghasilkan output identik di kedua OS.
- SimCore dijalankan sebagai container Linux di pipeline integrasi (dok. 07).
