using Newtonsoft.Json;
using SyncNet.Library;
using SyncNet.Models;
using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SyncNet.Services
{
    /// <summary>
    /// Setiap item langsung diproses begitu tiba dari channel,
    /// satu per satu, secara sekuensial.
    ///
    /// Urutan tetap terjamin karena:
    /// - Channel dikonfigurasi SingleReader = true (hanya 1 consumer).
    /// - Consumer loop memproses item secara sekuensial (await per item,
    ///   tidak ada Task.Run/paralel).
    /// - Tidak ada windowing waktu (_maxBatchTime) yang menahan proses.
    ///
    /// Gunakan class ini untuk stress test dan dibandingkan throughput/latency
    /// dengan OrderedBatchService (versi batching + sort), untuk memastikan
    /// apakah batching+sort memang dibutuhkan atau justru sumber latency.
    /// </summary>
    public sealed class ChannelService : IAsyncDisposable
    {
        private readonly Channel<object> _transactionChannel;

        private readonly CancellationTokenSource _internalCts = new();
        private readonly Task _consumerTask;
        private readonly NbTrace _trace;

        public ChannelService()
        {
            _trace = new NbTrace();

            _transactionChannel = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
            {
                SingleReader = true,   // 1 consumer -> urutan FIFO terjamin
                SingleWriter = false,  // banyak producer boleh enqueue bersamaan
                AllowSynchronousContinuations = false
            });

            _consumerTask = Task.Run(() => ConsumerLoopAsync(_internalCts.Token));
        }

        /// <summary>
        /// Masukkan data mentah (mis. DataSocket) ke antrian pemrosesan.
        /// Thread-safe, boleh dipanggil dari banyak thread/producer sekaligus.
        /// </summary>
        public async ValueTask EnqueueTransactionAsync(object obj, CancellationToken ct = default)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            await _transactionChannel.Writer.WriteAsync(obj, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Hentikan service secara graceful: tutup writer, tunggu semua data
        /// tersisa di channel selesai diproses secara berurutan.
        /// </summary>
        public async Task StopAsync(CancellationToken ct = default)
        {
            _transactionChannel.Writer.TryComplete();

            using var reg = ct.Register(() => _internalCts.Cancel());
            await _consumerTask.ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_consumerTask.IsCompleted)
            {
                try
                {
                    await StopAsync().ConfigureAwait(false);
                }
                catch 
                {
                    await MyApp.Logger("ChannelService: error during dispose-time StopAsync");
                }
            }

            _internalCts.Dispose();
        }

        // ------------------------------------------------------------------
        // Consumer loop: baca 1 item, proses, lanjut ke item berikutnya.
        // Tidak ada pengumpulan batch, tidak ada sort, tidak ada window waktu.
        // ------------------------------------------------------------------
        private async Task ConsumerLoopAsync(CancellationToken shutdownToken)
        {
            var reader = _transactionChannel.Reader;

            try
            {
                await foreach (var obj in reader.ReadAllAsync(shutdownToken).ConfigureAwait(false))
                {
                    LogModel log = await TryParse(obj);
                    if (log == null)
                        continue; // item invalid/gagal parse, skip - urutan item lain tidak terganggu

                    try
                    {
                        await WriteLogToFileAsync(log, shutdownToken);
                    }
                    catch (Exception ex)
                    {
                        await MyApp.Logger($"ConsumerLoopAsync: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
            {
                // Shutdown dipaksa - keluar dengan tenang.
            }
            catch (Exception ex)
            {
                await MyApp.Logger($"ConsumerLoopAsync: consumer loop terminated unexpectedly. {ex.Message}");
                throw;
            }
        }

        private async Task<bool> WriteLogToFileAsync(LogModel log, CancellationToken cancellationToken)
        {
            try
            {
                string detail = log.Detail == null ? "" : detail = log.Detail.ToString();

                //write data
                if (log.LogType == LogType.Info)
                    await _trace.WriteTraceStatusAsync(log.Datetime, log.AppName, log.FileName, log.Title, detail);
                else
                    await _trace.WriteTraceMessageAsync(log.Datetime, log.AppName, log.FileName, log.Title, detail);

                return true;
            }
            catch (Exception ex)
            {
                await MyApp.Logger($"WriteLogToFileAsync error: {ex.Message}");
                return false;
            }
        }
       
        private async Task<LogModel> TryParse(object obj)
        {
            try
            {
                byte[] bytes = obj as byte[];
                string data = Encoding.UTF8.GetString(bytes);
                LogModel req = JsonConvert.DeserializeObject<LogModel>(data);

                if (req == null || req.Datetime == DateTime.MinValue)
                {
                    await MyApp.Logger("TryParse: skip invalid item");
                    return null;
                }

                return req;
            }
            catch (Exception ex)
            {
                await MyApp.Logger($"TryParse: {ex.Message}");
                return null;
            }
        }
    }
}
