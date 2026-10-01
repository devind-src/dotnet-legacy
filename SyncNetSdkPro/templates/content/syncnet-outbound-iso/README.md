# API Template

Interface **outbound TCP ISO 8583**: Core mengirim request (kanal sink / `port_out`) → interface memetakan ke ISO 8583
→ biller → response dipetakan kembali ke Core. Dibuat dari template `syncnet-outbound-iso` (SyncNetSdkPro).

## Yang harus diisi (cari `TODO(n)`)

| TODO | File | Isi |
|------|------|-----|
| 1 | `src/SyncNet.Template/Iso/BillerIsoSpec.cs` | Spesifikasi field ISO biller (salin juga ke `simcore/simcore.json` bagian `Iso`) |
| 2 | `src/SyncNet.Template/Mapping/ToRemote.cs` | Mapping CoreRequest → ISO |
| 3 | `src/SyncNet.Template/Mapping/ToCore.cs` | Mapping ISO → CoreResponse (response code, data tagihan) |
| 4 | `src/SyncNet.Template/BillerInterface.cs` | Transaksi yang didukung |
| 5 | `src/SyncNet.Template/BillerInterface.cs` | Sign-on / echo (hapus bila tidak dipakai) |

## Menjalankan tanpa Core

```bash
dotnet tool install -g SyncNetPro.SimCore            # sekali (feed GitHub Packages, lihat nuget.config)

# terminal 1: simulator Core + biller tiruan (stub ISO port 18000) + Web UI http://127.0.0.1:5080
syncnet-simcore up -c simcore/simcore.json -s simcore/scenarios

# terminal 2: interface (appsettings.Development.json: node & koneksi dari file, tanpa database)
DOTNET_ENVIRONMENT=Development dotnet run --project src/SyncNet.Template

# terminal 3: kirim transaksi / jalankan semua skenario
syncnet-simcore send --scenario inquiry
syncnet-simcore run -c simcore/simcore.json -s simcore/scenarios     # dipakai juga di CI (exit code)
```

Windows PowerShell: `$env:DOTNET_ENVIRONMENT="Development"; dotnet run --project src/SyncNet.Template`.

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
