using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyncNet.Library
{
    public class NbTranMgr
    {
        #region Event
        public delegate void onDataArrivalEventHandler(object obj);
        private onDataArrivalEventHandler onDataArrivalEvent;

        public event onDataArrivalEventHandler OnDataArrival
        {
            add
            {
                onDataArrivalEvent = (onDataArrivalEventHandler)System.Delegate.Combine(onDataArrivalEvent, value);
            }
            remove
            {
                onDataArrivalEvent = (onDataArrivalEventHandler)System.Delegate.Remove(onDataArrivalEvent, value);
            }
        }
        #endregion Event

        private readonly ConcurrentQueue<object> _transactionQueue;
        private readonly SemaphoreSlim _semaphore;

        private int _maxBatchSize = 50; //default 
        private int _maxWorkers = 4; //default

        public NbTranMgr()
        {
            _transactionQueue = new ConcurrentQueue<object>();
            _semaphore = new SemaphoreSlim(_maxWorkers);
        }

        public NbTranMgr(int MaxWorkers, int MaxBatchSize)
        {
            //max worker disesuaikan dengan jumlah core CPU & memory
            _maxWorkers = MaxWorkers;
            _maxBatchSize = MaxBatchSize;

            _transactionQueue = new ConcurrentQueue<object>();
            _semaphore = new SemaphoreSlim(MaxWorkers);
        }

        public async Task EnqueueTransactionAsync(object obj)
        {
            _transactionQueue.Enqueue(obj);

            await Task.Run(() => ProcessTransactionsAsync());
        }

        private async Task ProcessTransactionsAsync()
        {
            // Tunggu sampai slot worker tersedia
            await _semaphore.WaitAsync();

            try
            {
                List<object> batch = new List<object>();

                // ambil transaksi dari antrian & kumpulkan dalam batch 
                while (batch.Count < _maxBatchSize && _transactionQueue.TryDequeue(out var obj))
                {
                    batch.Add(obj);
                }

                if (batch.Count > 0)
                {
                    // Proses batch transaksi
                    await ProcessBatchAsync(batch);
                }
            }
            finally
            {
                _semaphore.Release(); // release worker
            }
        }

        // Proses batch transaksi secara paralel
        private Task ProcessBatchAsync(List<object> batch)
        {
            return Task.Run(() =>
            {
                foreach (var obj in batch)
                {
                    //process transaction
                    onDataArrivalEvent?.Invoke(obj);
                }
            });
        }
    }
}
