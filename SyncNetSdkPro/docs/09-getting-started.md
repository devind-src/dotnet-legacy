# 09 — Getting Started (15 Menit Pertama)

Panduan untuk developer baru: dari mesin kosong sampai interface pertama berjalan dan lulus test, **tanpa
menginstal SyncNet Core**. Berlaku sama di Linux (Ubuntu 22.04+/26.04) dan Windows.

## 1. Prasyarat

| Kebutuhan | Keterangan |
|-----------|------------|
| .NET SDK 10.0.100+ | `dotnet --version` |
| Akses GitHub Packages | PAT GitHub dengan scope `read:packages` (paket `SyncNetPro.*` ada di feed `devind-src`) |
| Editor | VS Code (C# Dev Kit), Visual Studio 2026, atau Rider |

Set kredensial feed sebagai variabel lingkungan (dipakai `nuget.config` proyek hasil template):

```bash
export GITHUB_USER=<github-user>
export GITHUB_TOKEN=<PAT read:packages>          # PowerShell: $env:GITHUB_TOKEN="..."
```

## 2. Pasang template & SimCore

```bash
dotnet nuget add source https://nuget.pkg.github.com/devind-src/index.json \
  --name github-devind --username "$GITHUB_USER" --password "$GITHUB_TOKEN" --store-password-in-clear-text

dotnet new install SyncNetPro.Templates
dotnet tool install -g SyncNetPro.SimCore
dotnet new list syncnet
```

## 3. Pilih template

| Kebutuhan | Template |
|-----------|----------|
| Core mengirim transaksi ke biller/bank lewat TCP ISO 8583 | `syncnet-outbound-iso` |
| Core mengirim transaksi ke biller lewat REST/JSON | `syncnet-outbound-http` |
| Aplikasi channel/mitra memanggil REST API kita → Core | `syncnet-inbound-http` (`--with-routing` untuk routing & fee) |
| Bank/EDC/switch mengirim ISO 8583 ke port kita → Core | `syncnet-inbound-iso` |
| Kasus lain | `syncnet-blank` |

Rincian isi setiap template: [dok. 06](06-template-interface.md).

## 4. Buat proyek

```bash
dotnet new syncnet-outbound-iso -n Api.BillerAbc --app-name "API Biller ABC" --node-name BILLER_ABC
cd Api.BillerAbc
dotnet test                       # mapping test + end-to-end dengan SimCore in-process: harus hijau sejak awal
```

`--app-name` = `sw_app.app_name`, `--node-name` = `sw_nodes.node_name` di database Core produksi.

## 5. Jalankan seperti di produksi, tetapi dengan SimCore

```bash
# terminal 1 — Core tiruan + sistem eksternal tiruan + Web UI http://127.0.0.1:5080
syncnet-simcore up -c simcore/simcore.json -s simcore/scenarios

# terminal 2 — interface (Development: node & koneksi dari appsettings.Development.json)
DOTNET_ENVIRONMENT=Development dotnet run --project src/Api.BillerAbc

# terminal 3 — kirim transaksi
syncnet-simcore send --scenario inquiry
syncnet-simcore cmd -p 17000 VERSION          # command port interface (appsettings.Development.json)
```

Di Web UI terlihat pesan yang dikirim/diterima Core (JSON persis seperti di kabel), trace interface, dan peringatan
kontrak bila ada field yang tidak sesuai format Core.

## 6. Kerjakan TODO

Setiap template menandai bagian yang wajib diisi dengan `TODO(n)` dan menjelaskannya di `README.md` proyek.
Urutan yang disarankan:

1. Spesifikasi pesan eksternal (ISO spec / model JSON).
2. Mapping `CoreRequest` ↔ pesan eksternal (`Mapping/`), sambil menambah kasus di `MappingTests`.
3. Balasan sistem eksternal tiruan di `simcore/simcore.json` (`RemoteStubs`) dan skenario di `simcore/scenarios`.
4. `dotnet test` dan `syncnet-simcore run ...` (exit code ≠ 0 bila ada skenario gagal; cocok untuk CI).

Resep untuk kebutuhan umum (sign-on, header BCD, TCP non-persistent, signature HTTP, HSM, masking): [dok. 10](10-cookbook.md).

## 7. Ke produksi

- `appsettings.json` memakai `NodeSource=Database`: node & koneksi dibaca dari tabel Core, connection string dari
  konfigurasi Core di `SYNCNET_HOME` — **tidak ada konfigurasi baru di server**.
- Linux: `dotnet publish -c Release -o /opt/syncnet/<service>` lalu pasang `deploy/<service>.service` (systemd).
- Windows: `deploy/install-windows-service.ps1`.
- Interface baru memakai SDK baru; interface lama tetap memakai `SyncNetSdk` dan dapat berjalan berdampingan
  ([dok. 04 §7](04-arsitektur-sdk-baru.md#7-koeksistensi-dengan-sdk-lama)).

Memindahkan interface lama ke SDK baru: [dok. 11](11-migrasi-dari-sdk-lama.md).
