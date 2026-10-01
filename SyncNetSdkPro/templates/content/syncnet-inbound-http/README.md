# API Template

Interface **inbound REST API (channel)**: aplikasi channel memanggil REST API interface → interface memetakan ke
pesan Core dan mengirim lewat kanal source (`port_in`) → respons Core dikembalikan sebagai JSON. Dibuat dari template
`syncnet-inbound-http` (SyncNetSdkPro).

Alamat listen = `sw_connections.ws_url` (peran server, protokol web service), API key = `sw_connections.ws_key`
(header `X-Api-Key`, kosong = tanpa otentikasi).

## Endpoint

| Method | Path | tran_type | msgtype |
|--------|------|-----------|---------|
| POST | `/inquiry` | INQ | 0200 |
| POST | `/payment` | PAY | 0200 |
| POST | `/advice` | ADV | 0220 |
| POST | `/reversal` | REV | 0400 |
| GET/POST | `/echo` | — (tidak ke Core) | — |

Kode yang dibuat interface sendiri: `X8` API key salah (401), `X6` path tidak dikenal (404/405), `30` body tidak
valid (400), `X2` duplikat, `X15` biller cut-off (routing), `68` Core timeout, `89` Core tidak tersedia.

## Yang harus diisi (cari `TODO(n)`)

| TODO | File | Isi |
|------|------|-----|
| 1 | `src/SyncNet.Template/Models/ChannelMessages.cs` | Bentuk JSON API channel |
| 2 | `src/SyncNet.Template/Mapping/ToCore.cs` | Path → tran_type |
| 3 | `src/SyncNet.Template/Mapping/ToCore.cs` | Mapping request channel → CoreRequest |
| 4 | `src/SyncNet.Template/Mapping/ToChannel.cs` | Mapping CoreResponse → response channel |
| 5 | `src/SyncNet.Template/ChannelInterface.cs` | Validasi tambahan (terminal, limit, duplikat) |
| 6 | `src/SyncNet.Template/Routing/RoutingStep.cs` | (hanya `--with-routing`) margin routing produk topup |

## Routing & fee (`--with-routing`)

Dibuat dengan `dotnet new syncnet-inbound-http --with-routing`: paket `SyncNetPro.Routing` ditambahkan,
`builder.AddSyncNetRouting()` memuat tabel routing/fee dari database Core saat start dan RESYNC, dan `RoutingStep`
mengisi `private_data.sink_node` + `fee_data` sebelum request dikirim ke Core serta mencatat hasil supplier
(failover & Volume & Tiering). Saat pengembangan dengan `NodeSource=Json`, isi
`SyncNet:Routing:ConnectionString` dengan database Core pengembangan. Test end-to-end berjalan tanpa database
(`RoutingStep` tidak didaftarkan di test).

## Menjalankan tanpa Core

```bash
dotnet tool install -g SyncNetPro.SimCore            # sekali (feed GitHub Packages, lihat nuget.config)

# terminal 1: simulator Core (membalas sesuai Responder di simcore.json) + Web UI http://127.0.0.1:5080
syncnet-simcore up -c simcore/simcore.json -s simcore/scenarios

# terminal 2: interface (appsettings.Development.json: node & koneksi dari file, tanpa database)
DOTNET_ENVIRONMENT=Development dotnet run --project src/SyncNet.Template

# terminal 3: bertindak sebagai aplikasi channel
curl -s -H "X-Api-Key: dev-key" -H "Content-Type: application/json" http://127.0.0.1:19080/inquiry \
  -d '{"reference":"260930000001","trace_number":"000001","terminal_id":"TERM0001","merchant_id":"MERCHANT01","product_code":"PLNPOST","customer_id":"532110000001","amount":0}'
```

Windows PowerShell: `$env:DOTNET_ENVIRONMENT="Development"; dotnet run --project src/SyncNet.Template`.

Balasan Core tiruan diatur di `simcore/simcore.json` → `Nodes[].Responder` (`Mode`: `Fixed`/`Rules`/`None`;
`Rules` dicocokkan berdasarkan `TranType`, `TranTypeExt`, `MinAmount`/`MaxAmount`).

## Test

```bash
dotnet test
```

`MappingTests` menguji mapping murni; `InterfaceFlowTests` menjalankan SimCore in-process, mengirim HTTP ke interface,
dan memeriksa pesan yang diterima Core serta jawaban ke channel.

## Produksi

- `appsettings.json`: `NodeSource=Database` — node & koneksi dibaca dari database Core (`sw_app`, `sw_nodes`,
  `sw_connections`), connection string dari config Core di `SYNCNET_HOME`.
- Linux: `dotnet publish -c Release -o /opt/syncnet/syncnet-template-service` lalu `deploy/syncnet-template-service.service`.
- Windows: `deploy/install-windows-service.ps1`.
- Paket `SyncNetPro.*` diambil dari GitHub Packages: set `GITHUB_USER` dan `GITHUB_TOKEN` (PAT `read:packages`).
