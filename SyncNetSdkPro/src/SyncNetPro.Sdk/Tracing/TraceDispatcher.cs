using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SyncNetPro.Sdk.Tracing;

/// <summary>Mengirim antrean trace ke sink utama; bila gagal, ke file (fallback, seperti SDK lama).</summary>
public sealed class TraceDispatcher(
    TraceWriter writer,
    TraceSinkFactory sinkFactory,
    Hosting.StartupSignal startup,
    ILogger<TraceDispatcher> logger) : BackgroundService
{
    private static readonly JsonSerializerSettings Settings = new() { Formatting = Formatting.None };

    /// <summary>Serialisasi <see cref="TraceRecord"/> (Newtonsoft default, sama dengan SDK lama).</summary>
    public static string Serialize(TraceRecord record) => JsonConvert.SerializeObject(record, Settings);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Alamat Log Services berasal dari konfigurasi node (sw_app); tunggu sampai dimuat.
            await startup.ConfigurationLoaded.WaitAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Konfigurasi gagal dimuat; trace hanya ke file: {Error}", ex.Message);
        }

        (ITraceSink primary, ITraceSink fallback) = sinkFactory.Create();
        await using (primary.ConfigureAwait(false))
        await using (fallback.ConfigureAwait(false))
        {
            await primary.StartAsync(stoppingToken).ConfigureAwait(false);
            logger.LogInformation("Trace dikirim ke {Sink}", primary.Name);

            try
            {
                await foreach (TraceRecord record in writer.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
                {
                    string json = Serialize(record);
                    bool sent = await primary.TrySendAsync(record, json, stoppingToken).ConfigureAwait(false);
                    if (!sent && !ReferenceEquals(primary, fallback))
                    {
                        await fallback.TrySendAsync(record, json, stoppingToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Sisa antrean ditulis ke file agar tidak hilang saat shutdown.
                while (writer.Reader.TryRead(out TraceRecord? record))
                {
                    await fallback.TrySendAsync(record, Serialize(record), CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
    }
}
