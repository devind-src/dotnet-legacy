# SyncNetSdkPro

SDK modern untuk membangun interface inbound/outbound antara **SyncNet Core** dan sistem
eksternal. Pengganti `SyncNetSdk` untuk interface **baru**; interface lama tetap memakai SDK lama.

- Analisa & desain: [`docs/`](docs/README.md)
- Status: **Fase 4 — SimCore selesai**, berikutnya fase 5 modul (lihat [dok. 08](docs/08-roadmap-testing-risiko.md))

## Isi

| Path | Keterangan |
|------|------------|
| `src/SyncNetPro.Contracts` | Kontrak pesan ke Core: `CoreRequest`, `CoreResponse`, serializer, framing TCP (`CoreFrame`), `MessageTypes`, `TranType`, `AuthorizedBy`, helper `additional_data` |
| `src/SyncNetPro.Sdk` | Inti SDK: host, `SyncNetInterface`, kanal Core, koneksi eksternal (TCP/HTTP klien & server), konfigurasi node, command port, trace & log, status |
| `src/SyncNetPro.Sdk.Testing` | SimCore in-process untuk unit/integration test interface (`SimCore`, `SimInterfaceHost`, skenario, remote stub) |
| `tools/SyncNetPro.SimCore` | .NET tool `syncnet-simcore` (CLI + Web UI) dan `Dockerfile` |
| `samples/Sample.Outbound` | Contoh interface outbound minimal (jalan tanpa Core/DB dengan `DOTNET_ENVIRONMENT=Development`) |
| `tests/SyncNetPro.Contracts.Tests` | Unit test + **golden test** byte-per-byte terhadap SDK lama |
| `tests/SyncNetPro.Sdk.Tests` | Test SDK: golden header TCP/LogModel/command, FakeCore via socket, integrasi PostgreSQL (bila `SYNCNET_TEST_PG` diisi) |
| `tests/SyncNetPro.SimCore.Tests` | Test SimCore: perilaku Core, skenario, Web API, CLI |
| `tools/SyncNetPro.GoldenGenerator` | Generator golden file; mereferensikan `../SyncNetSdk` (read-only) |
| `.github/workflows/syncnetsdkpro.yml` (root repo) | CI Ubuntu + Windows, cek golden file, publish ke GitHub Packages |

## Prasyarat

- .NET SDK 10.0.1xx (lihat `global.json`)

## Build & test

```bash
cd SyncNetSdkPro
dotnet build SyncNetSdkPro.slnx
dotnet test --solution SyncNetSdkPro.slnx
```

Test integrasi PostgreSQL (opsional):

```bash
export SYNCNET_TEST_PG="Host=127.0.0.1;Database=sdkpro_test;Username=sdkpro;Password=sdkpro"
dotnet test --solution SyncNetSdkPro.slnx
```

## SimCore — jalan tanpa Core

```bash
# 1. buat simcore/simcore.json + contoh skenario (port default: command 17000, in 17001, out 17002, log 17009)
dotnet run --project tools/SyncNetPro.SimCore -- init --node SAMPLE_BILLER

# 2. jalankan simulator + Web UI (http://127.0.0.1:5080)
dotnet run --project tools/SyncNetPro.SimCore -- up -c simcore/simcore.json -s simcore/scenarios

# 3. di terminal lain jalankan interface dalam mode Development
DOTNET_ENVIRONMENT=Development dotnet run --project samples/Sample.Outbound

# 4. regression test skenario (exit code 1 bila gagal, laporan JUnit untuk CI)
dotnet run --project tools/SyncNetPro.SimCore -- run -c simcore/simcore.json -s simcore/scenarios --report junit.xml
```

`samples/Sample.Outbound/simcore/` berisi konfigurasi & skenario siap pakai untuk sample. Setelah
dipublikasikan, tool dapat dipasang dengan `dotnet tool install -g SyncNetPro.SimCore` (perintah `syncnet-simcore`).
Unit test in-process: lihat [dok. 07 §4](docs/07-simcore.md#4-contoh-pemakaian).

## Membuat interface (ringkas)

```csharp
// Program.cs
var builder = SyncNetApplication.CreateBuilder(args);
builder.AddSyncNetInterface<MyBillerInterface>();
await builder.Build().RunAsync();

// MyBillerInterface.cs — override hanya yang dibutuhkan
public sealed class MyBillerInterface : SyncNetInterface
{
    public override Task<CoreResponse?> OnCoreRequestAsync(CoreRequestContext ctx, CancellationToken ct) =>
        Task.FromResult<CoreResponse?>(ctx.Request.ToResponse("00", "Approved"));
}
```

Meneruskan ke sistem eksternal (koneksi dari `sw_connections`):

```csharp
public override async Task<CoreResponse?> OnCoreRequestAsync(CoreRequestContext ctx, CancellationToken ct)
{
    // TCP: korelasi otomatis lewat GetRemoteCorrelationKey
    byte[] reply = await ctx.Remote.SendAndReceiveAsync(BuildIso(ctx.Request), key: Key(ctx.Request), cancellationToken: ct);

    // HTTP: base URL = ws_url
    RemoteHttpResponse rsp = await ctx.Remote.Http.SendAsync(RemoteHttpRequest.Json("/bill/inquiry", dto), ct);
    ...
}
```

Mode pengembangan tanpa Core/DB: `SyncNet:NodeSource=Json` + `SyncNet:Nodes` (lihat `samples/Sample.Outbound/appsettings.Development.json`).
Produksi: cukup `SyncNet:AppName`; konfigurasi DB, path log/trace, RabbitMQ dibaca dari instalasi Core
(`SYNCNET_HOME` atau default `C:\SyncNet` / `/opt/SyncNet`).

## Golden file

Golden file di `tests/*/Golden` **dihasilkan oleh SDK lama**, tidak diedit manual.
Regenerasi (mis. setelah menambah kasus di generator):

```bash
dotnet run --project tools/SyncNetPro.GoldenGenerator
```

CI gagal bila golden file tidak sinkron dengan output SDK lama.

## Contoh pemakaian kontrak

```csharp
using SyncNetPro.Contracts;

CoreRequest request = CoreMessageCodec.Default.DecodePayload<CoreRequest>(payload);

CoreResponse response = request.ToResponse("00", "Approved");   // msgtype 0200 → 0210, pos_entry_mode disalin
response.SetAdditionalData("customer_name", "BUDI");               // data tambahan hanya lewat additional_data

byte[] frame = CoreMessageCodec.Default.EncodeFrame(response);      // header 2 byte + JSON UTF-8
```

## Rilis

Tag `sdkpro-vX.Y.Z` → CI mem-publish paket ke GitHub Packages
(`https://nuget.pkg.github.com/devind-src/index.json`). Versi dihitung MinVer dari tag.
