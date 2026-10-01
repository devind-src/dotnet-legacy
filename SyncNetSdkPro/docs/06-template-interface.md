# 06 — Template Interface

Tujuan: developer baru membuat interface siap jalan dalam hitungan menit, dengan pola
yang seragam, lalu hanya menulis **mapping pesan**.

> Status: **selesai (fase 6)** — paket `SyncNetPro.Templates` (`SyncNetSdkPro/templates`), diuji di CI
> (`scripts/test-templates.sh`: setiap template dibuat, di-build, dan test end-to-end-nya dijalankan terhadap paket
> yang baru di-pack, di Ubuntu dan Windows). Panduan pemakaian: [dok. 09](09-getting-started.md).

## 1. Distribusi

Paket NuGet `SyncNetPro.Templates` (template engine `dotnet new`), dirilis bersama SDK (tag `sdkpro-v*`):

```bash
# sekali per mesin: daftarkan feed GitHub Packages (PAT dengan scope read:packages)
dotnet nuget add source https://nuget.pkg.github.com/devind-src/index.json \
  --name github-devind --username <github-user> --password <PAT> --store-password-in-clear-text

dotnet new install SyncNetPro.Templates
dotnet new list syncnet
```

Proyek hasil template menyertakan `nuget.config` (nuget.org + feed `github-devind` dengan *package source mapping*
`SyncNetPro.*`); kredensial dibaca dari env `GITHUB_USER`/`GITHUB_TOKEN`, tidak disimpan di repo.

| Short name | Skenario | Padanan interface lama | Test bawaan |
|------------|----------|------------------------|-------------|
| `syncnet-outbound-iso` | Core → biller via **TCP ISO 8583** (klien persistent), sign-on & echo | `ApiBillerIso` | mapping + skenario SimCore dengan stub ISO |
| `syncnet-outbound-http` | Core → biller via **HTTP/JSON** | `ApiBillerJson` | mapping + skenario SimCore dengan stub HTTP |
| `syncnet-inbound-http` | Channel → Core via **REST API** (server), API key; `--with-routing` menambah routing & fee | `ApiChannel` | mapping + HTTP ke interface, SimCore sebagai Core |
| `syncnet-inbound-iso` | Bank/EDC/switch → Core via **TCP ISO 8583 server**, 0800/0810 | (baru) | mapping + `SimTcpClient` ke interface |
| `syncnet-blank` | Handler kosong, semua override sebagai komentar | — | skenario SimCore (`A1` default) |

Parameter:

| Parameter | Default | Keterangan |
|-----------|---------|------------|
| `-n` / `--name` | nama folder | Nama solusi, proyek, dan namespace (mis. `Api.BillerAbc`); nama service systemd = huruf kecil dengan `-` |
| `--app-name` | `API Template` | `sw_app.app_name` |
| `--node-name` | `TEMPLATE_NODE` | `sw_nodes.node_name` |
| `--sdk-version` | versi paket template | Versi paket `SyncNetPro.*` (`Directory.Build.props` → `SyncNetProVersion`) |
| `--with-routing` | `false` | Hanya `syncnet-inbound-http`: paket `SyncNetPro.Routing` + `Routing/RoutingStep.cs` |
| `--routing-version` | rilis `routing-v*` terakhir | Hanya `syncnet-inbound-http` |

Header TCP, peran klien/server, persistent/non-persistent, dan timeout **bukan** parameter template: semuanya
konfigurasi koneksi (`sw_connections` / `appsettings.Development.json`) sehingga tidak perlu kode berbeda.

```bash
dotnet new syncnet-outbound-iso -n Api.BillerAbc --app-name "API Biller ABC" --node-name BILLER_ABC
dotnet new syncnet-inbound-http -n Api.ChannelXyz --app-name "API Channel XYZ" --node-name CHANNEL_XYZ --with-routing
```

## 2. Struktur Proyek yang Dihasilkan

```
Api.BillerAbc/
├── Api.BillerAbc.slnx
├── Directory.Build.props             ← net10.0, nullable, warning = error, versi SyncNetPro.*, cek nama appsettings
├── global.json  nuget.config  .editorconfig  .gitignore
├── README.md                         ← tabel TODO(n), cara run dengan SimCore, test, deploy
├── src/Api.BillerAbc/
│   ├── Api.BillerAbc.csproj          ← PackageReference SyncNetPro.Sdk (+ Iso8583 / Routing)
│   ├── Program.cs                    ← ±5 baris: CreateBuilder → AddSyncNetInterface<T>() → Add…Services()
│   ├── ServiceRegistration.cs        ← DI mapping + options (dipakai Program.cs dan test)
│   ├── BillerInterface.cs            ← handler (ChannelInterface / AcquirerInterface / MyInterface)
│   ├── Mapping/                      ← ToRemote/ToCore (outbound), ToCore/ToChannel/ToIso (inbound)
│   ├── Iso/                          ← spesifikasi ISO + pesan network (template ISO)
│   ├── Models/                       ← DTO JSON (template HTTP)
│   ├── Routing/RoutingStep.cs        ← hanya --with-routing
│   ├── appsettings.json              ← produksi: NodeSource=Database
│   └── appsettings.Development.json  ← NodeSource=Json, node & koneksi ke SimCore
├── tests/Api.BillerAbc.Tests/
│   ├── MappingTests.cs               ← unit test mapping murni
│   └── InterfaceFlowTests.cs         ← SimCore in-process + SimInterfaceHost, port acak
├── simcore/
│   ├── simcore.json                  ← node, Responder Core, stub eksternal (ISO/HTTP)
│   └── scenarios/*.json              ← inquiry, payment, reversal, … (dipakai CLI dan test)
└── deploy/
    ├── api-billerabc.service         ← systemd (Type=notify, SYNCNET_HOME=/opt/syncnet)
    └── install-windows-service.ps1
```

## 3. Isi Handler Template (contoh `syncnet-outbound-iso`)

```csharp
public sealed class BillerInterface(ToRemote toRemote, ToCore toCore, IOptions<BillerOptions> options) : SyncNetInterface
{
    public override async Task<CoreResponse?> OnCoreRequestAsync(CoreRequestContext context, CancellationToken cancellationToken)
    {
        CoreRequest request = context.Request;

        // TODO(4): tambahkan/hapus tran_type yang didukung biller ini.
        IsoMessage? iso = request.TranType switch
        {
            TranType.Inquiry => toRemote.Inquiry(request),
            TranType.Payment => toRemote.Payment(request),
            TranType.Advice => toRemote.Advice(request),
            TranType.Reversal => toRemote.Reversal(request),
            _ => null,
        };

        if (iso is null) return request.ToResponse(ResponseCodes.NotSupported, "Transaction is not supported", AuthorizedBy.Internal);
        if (!context.Remote.IsConnected) return request.ToResponse(ResponseCodes.LinkDown, "Link down", AuthorizedBy.Internal);

        context.Trace.Message(context.Node.Name, TraceDirection.Outgoing, iso.Mti, iso.FormatTrace());
        try
        {
            byte[] reply = await context.Remote.SendAndReceiveAsync(iso.Pack(), BillerIsoSpec.CorrelationKey(iso), cancellationToken: cancellationToken);
            return toCore.From(IsoMessage.Parse(BillerIsoSpec.Instance, reply), request);
        }
        catch (RemoteUnavailableException) { return request.ToResponse(ResponseCodes.LinkDown, "Link down", AuthorizedBy.Internal); }
        catch (TimeoutException) { return null; }   // Core menandai timeout (dan auto reversal bila aktif)
    }

    // + GetRemoteCorrelationKey, OnRemoteMessageAsync (0800 → 0810), OnRemoteConnectedAsync (sign-on), OnEchoTimerAsync
}
```

Prinsip isi template:

- Semua titik yang wajib diisi developer ditandai `TODO(n)` bernomor dan dijelaskan di README proyek.
- Tidak ada kode infrastruktur (cache, korelasi, trace koneksi, service wrapper) di proyek hasil template.
- Contoh mapping lengkap untuk 4 transaksi dasar (INQ, PAY, ADV, REV) + network management.
- Kode respons buatan interface sendiri eksplisit (`A1`, `89`, `96`; channel: `X2`, `X6`, `X8`, `X15`, `30`, `68`).
- `appsettings.Development.json` langsung terhubung ke SimCore sehingga `dotnet run` bekerja tanpa Core.
- `dotnet test` hijau sejak proyek dibuat, termasuk test end-to-end.

## 4. Alur Kerja Developer Baru

```bash
dotnet new syncnet-outbound-iso -n Api.BillerAbc --app-name "API Biller ABC" --node-name BILLER_ABC
cd Api.BillerAbc

syncnet-simcore up -c simcore/simcore.json -s simcore/scenarios        # 1. Core + biller tiruan, Web UI :5080
DOTNET_ENVIRONMENT=Development dotnet run --project src/Api.BillerAbc  # 2. interface
syncnet-simcore send --scenario inquiry                                # 3. transaksi
dotnet test                                                            # 4. test otomatis
```

## 5. Pemeliharaan Template

- Sumber: `templates/content/<template>` + `templates/content/_shared` (file bersama), dirakit saat build ke
  `templates/obj/templates/<template>`; default `--sdk-version`/`--routing-version` diisi versi paket saat pack.
- Coba lokal tanpa pack: `dotnet build templates && dotnet new install templates/obj/templates/<template>`.
- Smoke test lengkap: `scripts/test-templates.sh [folder]` (opsional `TEMPLATES="syncnet-blank syncnet-inbound-http:--with-routing"`).

## 6. Dokumentasi Pendamping

- [09 — Getting Started](09-getting-started.md)
- [10 — Cookbook](10-cookbook.md): sign-on/echo, TCP non-persistent, header BCD/kustom, ISO spec, HTTP dengan
  signature, inbound HTTP dengan otentikasi, timeout & respons terlambat, `additional_data`, trace & masking, HSM,
  routing, modul RESYNC, test.
- [11 — Migrasi dari SDK lama](11-migrasi-dari-sdk-lama.md)
- Referensi API: XML doc di setiap paket (IntelliSense).
