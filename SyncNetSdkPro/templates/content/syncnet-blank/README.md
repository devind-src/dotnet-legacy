# API Template

Interface SyncNet **kosong**: host, konfigurasi, SimCore, dan test sudah siap; logika ditulis di
`src/SyncNet.Template/MyInterface.cs` (semua event tersedia sebagai komentar). Dibuat dari template `syncnet-blank`
(SyncNetSdkPro). Untuk contoh lengkap lihat `syncnet-outbound-iso`, `syncnet-outbound-http`, `syncnet-inbound-http`,
`syncnet-inbound-iso`.

## Langkah

1. Atur node & koneksi pengembangan di `src/SyncNet.Template/appsettings.Development.json` (`Category`:
   `Merchant` = inbound, `BillerIssuer` = outbound, `Both`).
2. Override event di `MyInterface` (`OnCoreRequestAsync` untuk outbound, `OnHttpRequestAsync` /
   `OnRemoteMessageAsync` + `SendToCoreAsync` untuk inbound).
3. Tambahkan skenario di `simcore/scenarios` (dan stub eksternal di `simcore/simcore.json` bagian `RemoteStubs`).
   Skenario bawaan `inquiry-not-supported` mengharapkan `A1` (default SDK) — ubah setelah transaksi diimplementasikan.

## Menjalankan tanpa Core

```bash
dotnet tool install -g SyncNetPro.SimCore            # sekali (feed GitHub Packages, lihat nuget.config)
syncnet-simcore up -c simcore/simcore.json -s simcore/scenarios          # terminal 1 (Web UI http://127.0.0.1:5080)
DOTNET_ENVIRONMENT=Development dotnet run --project src/SyncNet.Template # terminal 2
syncnet-simcore run -c simcore/simcore.json -s simcore/scenarios         # terminal 3 / CI
```

Windows PowerShell: `$env:DOTNET_ENVIRONMENT="Development"; dotnet run --project src/SyncNet.Template`.

## Test

```bash
dotnet test
```

## Produksi

- `appsettings.json`: `NodeSource=Database` — node & koneksi dibaca dari database Core (`sw_app`, `sw_nodes`,
  `sw_connections`), connection string dari config Core di `SYNCNET_HOME`.
- Linux: `dotnet publish -c Release -o /opt/syncnet/syncnet-template-service` lalu `deploy/syncnet-template-service.service`.
- Windows: `deploy/install-windows-service.ps1`.
- Paket `SyncNetPro.*` diambil dari GitHub Packages: set `GITHUB_USER` dan `GITHUB_TOKEN` (PAT `read:packages`).
