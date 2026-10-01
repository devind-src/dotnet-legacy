# 11 — Migrasi dari SDK Lama (`IAppProcessor`) ke `SyncNetInterface`

Interface lama **tidak wajib** dimigrasi: `SyncNetSdk` dan `ApiInterfaces/*` tetap berjalan apa adanya, dan interface
baru memakai `SyncNetPro.*`. Dokumen ini untuk interface lama yang akan ditulis ulang (mis. saat ada perubahan besar
atau pindah ke Linux).

Pemetaan API lengkap: [dok. 03 §2–3](03-inventaris-dan-pemetaan-api.md#2-iappprocessor-callback--handler-baru).

## 1. Langkah

1. **Buat proyek dari template** yang setara (dok. 06 §1): `ApiBillerIso` → `syncnet-outbound-iso`,
   `ApiBillerJson` → `syncnet-outbound-http`, `ApiChannel` → `syncnet-inbound-http --with-routing`.
   Pakai `--app-name` dan `--node-name` yang **sama** dengan baris `sw_app` / `sw_nodes` interface lama.
2. **Pindahkan spesifikasi pesan**: kelas `IsoTemplate : FieldFormatter` → `IsoSpec.Legacy.ToBuilder().Field(...)`;
   DTO JSON dapat disalin (Newtonsoft tetap dipakai di 1.x).
3. **Pindahkan mapping**: isi `MyApp.ProcessMsgFromSinkNode` / `ProcessMsgFromRemoteWsServer` dipecah menjadi
   `Mapping/ToRemote.cs` + `Mapping/ToCore.cs` (atau `ToChannel.cs`). Logika cache, korelasi, timer, dan service
   wrapper **dibuang** — sudah ditangani SDK.
4. **Rekam perilaku lama sebagai skenario SimCore**: transaksi yang sama dikirim ke interface lama dan baru,
   bandingkan pesan ke Core (Web UI SimCore / `ContractWarnings`).
5. **Uji paralel** di lingkungan uji dengan database Core yang sama, lalu **cut-over** (§4).

## 2. Callback → override

| Lama (`IAppProcessor`) | Baru (`SyncNetInterface`) |
|------------------------|---------------------------|
| `ProcessMsgFromSinkNode(node, Request)` + `ReplyToSink` | `OnCoreRequestAsync(ctx)` → `return CoreResponse` (`null` = tidak membalas) |
| `SendToSource(...)` + `ProcessMsgFromSourceNode(...)` + cache | `await ctx.SendToCoreAsync(request, ct)` → `CoreResponse` |
| `SendToTcp(...)` + `ProcessMsgFromRemoteTcp(...)` + cache korelasi | `await ctx.Remote.SendAndReceiveAsync(payload, key, ct)` + `GetRemoteCorrelationKey` |
| `ProcessMsgFromRemoteTcp` untuk request masuk / 0800 | `OnRemoteMessageAsync(ctx)` + `ctx.ReplyAsync(...)` |
| `SendToHttpClient(...)` + `ProcessMsgFromRemoteWsClient(...)` | `await ctx.Remote.Http.SendAsync(RemoteHttpRequest, ct)` |
| `ProcessMsgFromRemoteWsServer(..., HttpContext)` + `ReplyToHttpServer` | `OnHttpRequestAsync(ctx)` → `return HttpReply` |
| `TimerAutoSignon` / `TimerEcho` / `TimerKeyExchange` | `OnRemoteConnectedAsync` (atau `OnAutoSignOnAsync`) / `OnEchoTimerAsync` / `OnKeyExchangeTimerAsync` |
| `NetworkManagement(EnumNtwrkMgmt, ...)` | `OnNetworkCommandAsync(ctx)` |
| `Resync()` | `OnConfigurationReloadedAsync` atau `ISyncNetModule.ReloadAsync` |
| `OnError(...)` | Exception di `await` (`TimeoutException`, `RemoteUnavailableException`, `CoreUnavailableException`, …) |
| `AppProcessor.WriteTrace` / `WriteLog` / `Logger` | `ctx.Trace.Message/Info`, `ctx.Logger` (`ILogger`) |
| `throw new NotImplementedException()` | Tidak perlu: override hanya yang dipakai |

## 3. Sebelum → sesudah (outbound ISO)

Lama (diringkas dari pola `ApiBillerIso`):

```csharp
public async Task ProcessMsgFromSinkNode(string NodeName, Request MsgRequest)
{
    var iso = Mapper.ToIso(MsgRequest);
    string key = iso.GetField(11) + iso.GetField(41);
    _cache.TryAdd(key, MsgRequest);                     // korelasi manual
    AppProcessor.WriteTrace(NodeName, "REQ", iso.GetFormattedMessage(), EnumFromTo.To, null);
    _app.SendToTcp(NodeName, iso.Pack());
    // ... balasan datang di ProcessMsgFromRemoteTcp, cari di _cache, ReplyToSink; timeout via timer sendiri
}
```

Baru:

```csharp
public override async Task<CoreResponse?> OnCoreRequestAsync(CoreRequestContext context, CancellationToken ct)
{
    IsoMessage iso = toRemote.Map(context.Request);
    context.Trace.Message(context.Node.Name, TraceDirection.Outgoing, iso.Mti, iso.FormatTrace());
    try
    {
        byte[] reply = await context.Remote.SendAndReceiveAsync(iso.Pack(), $"{iso[11]}|{iso[41]}", cancellationToken: ct);
        return toCore.From(IsoMessage.Parse(Spec, reply), context.Request);
    }
    catch (RemoteUnavailableException) { return context.Request.ToResponse(ResponseCodes.LinkDown, "Link down", AuthorizedBy.Internal); }
    catch (TimeoutException) { return null; }
}
```

## 4. Cut-over di server

Tidak ada konfigurasi baru: SDK baru membaca konfigurasi Core lama (`{SYNCNET_HOME}/Core/Bin/appsettings.json`,
`Resources.bin`, `Keys/PrivateKey.pem`) dan tabel `sw_app`/`sw_nodes`/`sw_connections` yang sama.

1. Publish interface baru ke folder terpisah (`/opt/syncnet/<service>` atau folder service Windows).
2. Hentikan service lama (satu node Core hanya dilayani satu proses).
3. Jalankan service baru; periksa status node di Web UI Core dan trace di Log Services.
4. Rollback = hentikan service baru, jalankan kembali service lama (tidak ada perubahan skema/data).

## 5. Perbedaan perilaku yang perlu diketahui

Semua perbedaan berikut adalah perbaikan bug atau keputusan tim (dok. 01 §5, dok. 08); format pesan ke Core tidak berubah.

| Area | SDK lama | SDK baru |
|------|----------|----------|
| `msgtype` & `pos_entry_mode` di response | Tidak diisi / tidak disalin | `ToResponse` mengisi MTI response (`0200`→`0210`) dan menyalin `pos_entry_mode` (Q2) |
| Encoding pesan ke Core | 1 char → 1 byte (B4) | UTF-8 dua arah |
| TLS ke eksternal | Sertifikat apa pun diterima (B6) | Divalidasi; `SyncNet:Remote:AllowUntrustedCertificates` hanya untuk dev |
| Header TCP non-persistent / None / Custom | Selalu 2 byte (B1, B2) | Sesuai `sw_connections` |
| Command `TRACE CLEAR` | Ada di daftar, tidak berfungsi (B10) | Dihapus — trace adalah jejak audit |
| Trace saat antrean penuh | Dibuang (B13) | Ditulis ke file fallback |
| Nama file log | Berbeda per OS | Sama di semua OS (Q8); `Logging:LegacyWindowsFileNames` untuk nama lama |
| Log Services host | Selalu `127.0.0.1` (B5) | Host dari `sw_app` |
| ISO 8583 BCD | Beberapa kombinasi salah (B14–B20) | Simetris; nilai tidak valid ditolak saat pack dengan nomor field |
| `appSettings.json` (huruf besar) | Tidak tersalin di Linux (B9) | Template memakai huruf kecil dan build gagal bila salah nama |
