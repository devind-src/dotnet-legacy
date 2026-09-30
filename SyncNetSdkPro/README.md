# SyncNetSdkPro

SDK modern untuk membangun interface inbound/outbound antara **SyncNet Core** dan sistem
eksternal. Pengganti `SyncNetSdk` untuk interface **baru**; interface lama tetap memakai SDK lama.

- Analisa & desain: [`docs/`](docs/README.md)
- Status: **Fase 1 — Fondasi & Kontrak** (lihat [dok. 08](docs/08-roadmap-testing-risiko.md))

## Isi

| Path | Keterangan |
|------|------------|
| `src/SyncNetPro.Contracts` | Kontrak pesan ke Core: `CoreRequest`, `CoreResponse`, serializer, framing TCP (`CoreFrame`), `MessageTypes`, `TranType`, `AuthorizedBy`, helper `additional_data` |
| `tests/SyncNetPro.Contracts.Tests` | Unit test + **golden test** byte-per-byte terhadap SDK lama |
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

## Golden file

Golden file di `tests/SyncNetPro.Contracts.Tests/Golden` **dihasilkan oleh SDK lama**, tidak diedit manual.
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
