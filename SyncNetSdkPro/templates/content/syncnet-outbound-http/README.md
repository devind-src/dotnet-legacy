# API Template

Interface **outbound HTTP/JSON**: Core mengirim request (kanal sink / `port_out`) → interface memanggil REST API biller
→ response JSON dipetakan kembali ke Core. Dibuat dari template `syncnet-outbound-http` (SyncNetSdkPro).

Base URL biller = `sw_connections.ws_url`, API key = `sw_connections.ws_key` (dikirim sebagai header `X-Api-Key`,
atau atur `Biller:Headers`). Timeout = `request_timeout` node. SDK tidak melakukan retry otomatis.

## Yang harus diisi (cari `TODO(n)`)

| TODO | File | Isi |
|------|------|-----|
| 1 | `src/SyncNet.Template/Models/BillerMessages.cs` | Bentuk JSON request/response biller |
| 2 | `src/SyncNet.Template/Mapping/ToRemote.cs` | Endpoint per transaksi + mapping CoreRequest → JSON |
| 3 | `src/SyncNet.Template/Mapping/ToCore.cs` | Mapping JSON → CoreResponse (response code, data tagihan) |
| 4 | `src/SyncNet.Template/BillerInterface.cs` | Penanganan HTTP non-2xx / timeout |

Hasil per kondisi (default template):

| Kondisi | Response ke Core |
|---------|------------------|
| HTTP 2xx + JSON | `rc` dari biller |
| HTTP non-2xx / JSON rusak | `96` (authorized_by internal) |
| Biller tidak dapat dihubungi / timeout transport | `89` link down |
| tran_type tidak didukung | `A1` |

## Menjalankan tanpa Core

```bash
dotnet tool install -g SyncNetPro.SimCore            # sekali (feed GitHub Packages, lihat nuget.config)

# terminal 1: simulator Core + biller tiruan (stub HTTP port 18080) + Web UI http://127.0.0.1:5080
syncnet-simcore up -c simcore/simcore.json -s simcore/scenarios

# terminal 2: interface (appsettings.Development.json: node & koneksi dari file, tanpa database)
DOTNET_ENVIRONMENT=Development dotnet run --project src/SyncNet.Template

# terminal 3: kirim transaksi / jalankan semua skenario
syncnet-simcore send --scenario inquiry
syncnet-simcore run -c simcore/simcore.json -s simcore/scenarios     # dipakai juga di CI (exit code)
```

Windows PowerShell: `$env:DOTNET_ENVIRONMENT="Development"; dotnet run --project src/SyncNet.Template`.

Balasan stub biller diatur di `simcore/simcore.json` (`RemoteStubs[].Replies`: `Path`, `Contains`, `Status`,
`Reply` dengan placeholder `{{json:nama_properti}}` dari body request).

## Test

```bash
dotnet test
```

`MappingTests` menguji mapping murni; `InterfaceFlowTests` menjalankan SimCore in-process dengan
`simcore/simcore.json` dan memastikan semua skenario lulus.

## Produksi

- `appsettings.json`: `NodeSource=Database` — node & koneksi dibaca dari database Core (`sw_app`, `sw_nodes`,
  `sw_connections`), connection string dari config Core di `SYNCNET_HOME`.
- Linux: `dotnet publish -c Release -o /opt/syncnet/syncnet-template-service` lalu `deploy/syncnet-template-service.service`.
- Windows: `deploy/install-windows-service.ps1`.
- Paket `SyncNetPro.*` diambil dari GitHub Packages: set `GITHUB_USER` dan `GITHUB_TOKEN` (PAT `read:packages`).
