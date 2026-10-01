# API Template

Interface **inbound TCP ISO 8583**: pengirim (bank/EDC/switch) terhubung ke port TCP interface dan mengirim ISO 8583
→ interface memetakan ke pesan Core dan mengirim lewat kanal source (`port_in`) → respons Core dibalas sebagai ISO
8583. Dibuat dari template `syncnet-inbound-iso` (SyncNetSdkPro).

Port listen & header TCP = `sw_connections` (peran server, mis. protokol `Tcp2ByteExcludeHeader`).

## Alur

| Pesan masuk | Penanganan |
|-------------|-----------|
| 0800 | Dibalas 0810 rc `00` oleh interface (tidak ke Core) |
| 0200 / 0220 / 0400 | Diteruskan ke Core; 0210/0230/0410 dengan rc dari Core |
| MTI / processing code lain | rc `12` |
| Core tidak membalas / tidak tersedia / duplikat | rc `68` / `89` / `94` |

## Yang harus diisi (cari `TODO(n)`)

| TODO | File | Isi |
|------|------|-----|
| 1 | `src/SyncNet.Template/Iso/AcquirerIsoSpec.cs` | Spesifikasi field ISO pengirim |
| 2 | `src/SyncNet.Template/Mapping/ToCore.cs` | MTI yang diteruskan ke Core |
| 3 | `src/SyncNet.Template/Mapping/ToCore.cs` | Mapping ISO → CoreRequest |
| 4 | `src/SyncNet.Template/Mapping/ToIso.cs` | Mapping CoreResponse → ISO response |
| 5 | `src/SyncNet.Template/AcquirerInterface.cs` | Sign-on / key exchange |

## Menjalankan tanpa Core

```bash
dotnet tool install -g SyncNetPro.SimCore            # sekali (feed GitHub Packages, lihat nuget.config)

# terminal 1: simulator Core (membalas sesuai Responder di simcore.json) + Web UI http://127.0.0.1:5080
syncnet-simcore up -c simcore/simcore.json -s simcore/scenarios

# terminal 2: interface (appsettings.Development.json: node & koneksi dari file, tanpa database)
DOTNET_ENVIRONMENT=Development dotnet run --project src/SyncNet.Template

# terminal 3: bertindak sebagai pengirim — kirim ISO 8583 (hex, tanpa header) ke port 19000, balasan ditampilkan hex dump
syncnet-simcore tcp -p 19000 --hex <pesan-iso-hex>
```

Windows PowerShell: `$env:DOTNET_ENVIRONMENT="Development"; dotnet run --project src/SyncNet.Template`.

Pesan contoh paling mudah dibuat lewat test (`IsoMessage.Pack()` lalu `Convert.ToHexString`). Balasan Core tiruan
diatur di `simcore/simcore.json` → `Nodes[].Responder`.

## Test

```bash
dotnet test
```

`MappingTests` menguji mapping murni; `InterfaceFlowTests` menjalankan SimCore in-process, mengirim ISO 8583 ke
interface dengan `SimTcpClient`, dan memeriksa pesan yang diterima Core serta balasan ISO.

## Produksi

- `appsettings.json`: `NodeSource=Database` — node & koneksi dibaca dari database Core (`sw_app`, `sw_nodes`,
  `sw_connections`), connection string dari config Core di `SYNCNET_HOME`.
- Linux: `dotnet publish -c Release -o /opt/syncnet/syncnet-template-service` lalu `deploy/syncnet-template-service.service`.
- Windows: `deploy/install-windows-service.ps1`.
- Paket `SyncNetPro.*` diambil dari GitHub Packages: set `GITHUB_USER` dan `GITHUB_TOKEN` (PAT `read:packages`).
