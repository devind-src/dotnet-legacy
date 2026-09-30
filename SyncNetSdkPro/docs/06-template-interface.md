# 06 — Template Interface

Tujuan: developer baru membuat interface siap jalan dalam hitungan menit, dengan pola
yang seragam, lalu hanya menulis **mapping pesan**.

## 1. Distribusi

Paket NuGet `SyncNetPro.Templates` (template engine `dotnet new`):

```bash
dotnet new install SyncNetPro.Templates
dotnet new list syncnet
```

| Short name | Skenario | Padanan interface lama |
|------------|----------|------------------------|
| `syncnet-outbound-iso` | Core → eksternal via **TCP ISO 8583** (persistent / non-persistent), sign-on & echo | `ApiBillerIso` |
| `syncnet-outbound-http` | Core → eksternal via **HTTP/JSON** (REST biller) | `ApiBillerJson` |
| `syncnet-inbound-http` | Eksternal (channel) → Core via **HTTP server** | `ApiChannel` (tanpa modul routing) |
| `syncnet-inbound-iso` | Eksternal (bank/EDC) → Core via **TCP ISO 8583 server** | (baru) |
| `syncnet-blank` | Handler kosong + semua override terkomentar | — |

Parameter umum:

```bash
dotnet new syncnet-outbound-iso -n Api.BillerAbc \
  --app-name "API Biller ABC" \
  --node-name BILLER_ABC \
  --tcp-header Binary2Byte \
  --with-routing false \
  --with-tests true
```

## 2. Struktur Proyek yang Dihasilkan

```
Api.BillerAbc/
├── Api.BillerAbc.sln(x)
├── src/Api.BillerAbc/
│   ├── Api.BillerAbc.csproj          ← PackageReference SyncNetPro.Sdk (+ Iso8583)
│   ├── Program.cs                    ← ±10 baris (dok. 04 §4.2)
│   ├── BillerAbcInterface.cs         ← handler: override On…Async, TODO jelas
│   ├── Mapping/
│   │   ├── ToRemote.cs               ← CoreRequest → pesan eksternal
│   │   └── ToCore.cs                 ← pesan eksternal → CoreResponse
│   ├── Iso/BillerAbcIsoSpec.cs       ← definisi field ISO (template ISO saja)
│   ├── Models/                       ← DTO eksternal (template HTTP)
│   ├── appsettings.json              ← konfigurasi produksi (NodeSource=Database)
│   ├── appsettings.Development.json  ← NodeSource=Json + koneksi ke SimCore
│   └── VERSION.txt / Directory.Build.props (versi)
├── tests/Api.BillerAbc.Tests/
│   ├── MappingTests.cs               ← unit test mapping murni
│   └── InterfaceFlowTests.cs         ← flow end-to-end memakai SyncNetPro.Sdk.Testing (SimCore in-process + fake remote)
├── simcore/
│   └── scenarios/*.json              ← skenario siap pakai untuk SimCore (inquiry, payment, advice, reversal)
├── deploy/
│   ├── syncnet-api-billerabc.service
│   └── install-windows-service.ps1
├── .editorconfig  .gitignore
└── README.md                         ← langkah run, debug, test, deploy
```

## 3. Isi Handler Template (contoh `syncnet-outbound-iso`)

```csharp
/// <summary>
/// Interface outbound: menerima request dari Core (kanal Sink / port_out),
/// meneruskan ke BILLER_ABC via ISO 8583, dan mengembalikan respons ke Core.
/// </summary>
public sealed class BillerAbcInterface(ToRemote toRemote, ToCore toCore) : SyncNetInterface
{
    public override async Task<CoreResponse> OnCoreRequestAsync(CoreRequestContext ctx, CancellationToken ct)
    {
        // TODO(1): tambahkan/hapus tran_type yang didukung biller ini
        IsoMessage? request = ctx.Request.TranType switch
        {
            TranType.Inquiry  => toRemote.Inquiry(ctx.Request),
            TranType.Payment  => toRemote.Payment(ctx.Request),
            TranType.Advice   => toRemote.Advice(ctx.Request),
            TranType.Reversal => toRemote.Reversal(ctx.Request),
            _ => null
        };

        if (request is null)
            return ctx.Request.ToResponse(ResponseCodes.NotSupported, "Transaction is not supported", AuthorizedBy.Internal);

        if (!ctx.Remote.IsConnected)
            return ctx.Request.ToResponse(ResponseCodes.LinkDown, "Link down", AuthorizedBy.Internal);

        IsoMessage response = await ctx.Remote.SendAndReceiveAsync(request, ct);

        // TODO(2): lengkapi mapping respons di Mapping/ToCore.cs
        return toCore.From(response, ctx.Request);
    }

    // TODO(3): hapus bila biller tidak memerlukan sign-on
    public override Task OnRemoteConnectedAsync(ConnectionContext ctx, CancellationToken ct)
        => ctx.Remote.SendAsync(IsoNetwork.SignOn(), ct);

    public override Task OnEchoTimerAsync(NodeContext ctx, CancellationToken ct)
        => ctx.Remote.SendAsync(IsoNetwork.Echo(), ct);
}
```

Prinsip isi template:

- Semua titik yang wajib diisi developer ditandai `TODO(n)` bernomor dan dijelaskan di README.
- Tidak ada kode infrastruktur (cache, korelasi, trace manual, service wrapper) di proyek hasil template.
- Contoh mapping lengkap untuk 4 transaksi dasar (INQ, PAY, ADV, REV) + network management.
- `appsettings.Development.json` langsung terhubung ke SimCore sehingga `dotnet run` bekerja tanpa Core.

## 4. Alur Kerja Developer Baru

```bash
dotnet new syncnet-outbound-iso -n Api.BillerAbc --app-name "API Biller ABC" --node-name BILLER_ABC
cd Api.BillerAbc

# 1. jalankan simulator core + simulator biller (dok. 07)
dotnet tool run syncnet-simcore -- up --scenario simcore/scenarios

# 2. jalankan interface (mode Development)
dotnet run --project src/Api.BillerAbc

# 3. kirim transaksi dari SimCore (CLI atau Web UI http://localhost:5080)
dotnet tool run syncnet-simcore -- send inquiry --node BILLER_ABC

# 4. test otomatis
dotnet test
```

## 5. Dokumentasi Pendamping

Di `SyncNetSdkPro/docs/` (fase implementasi) ditambahkan:

- *Getting Started* (15 menit pertama).
- *Cookbook*: sign-on/echo, non-persistent TCP, header BCD, HTTP dengan signature/header khusus,
  inbound HTTP dengan autentikasi, penanganan timeout & late response, masking.
- *Referensi API* hasil XML doc (DocFX).
- *Migrasi* dari pola `IAppProcessor` ke `SyncNetInterface` (tabel dok. 03 §2–3).
