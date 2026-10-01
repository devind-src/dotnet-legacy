# dotnet-legacy

Repo berisi SyncNet lama (read-only) dan **SyncNetSdkPro** (SDK baru yang sedang dikembangkan).

## Mulai di sini

Baca `SyncNetSdkPro/docs/12-status-dan-kelanjutan.md` sebelum mengerjakan apa pun: status per fase, aturan,
cara build/test/golden/template/rilis, jebakan teknis, dan langkah berikutnya (fase 7 — pilot).

## Aturan wajib

- **Jangan ubah** `SyncNetSdk/`, `ApiInterfaces/`, `SyncNetCore/`, `SyncNetHsm/`, `SyncNetLogger/`, `SyncNetWebApi/`,
  `SyncNetWebContract/`, `SyncNetWebUI/` — semuanya read-only. Semua pekerjaan baru di `SyncNetSdkPro/`.
- Format pesan ke Core tidak boleh berubah (golden test terhadap SDK lama; golden hanya dibuat ulang lewat
  `tools/SyncNetPro.GoldenGenerator`).
- Satu fase = satu PR; merge (squash) **hanya setelah user menyetujui**.
- Dokumen dan komunikasi dalam Bahasa Indonesia; dokumen di `SyncNetSdkPro/docs`.

## Perintah cepat

```bash
cd SyncNetSdkPro
dotnet build SyncNetSdkPro.slnx -c Release
service postgresql start   # opsional, untuk test PostgreSQL
SYNCNET_TEST_PG="Host=127.0.0.1;Database=sdkpro_test;Username=sdkpro;Password=sdkpro" \
  dotnet test --solution SyncNetSdkPro.slnx -c Release --no-build
scripts/test-templates.sh /tmp/tt                         # smoke test semua template dotnet new
```
