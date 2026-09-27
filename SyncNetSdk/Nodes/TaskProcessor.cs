using SyncNet.Library;
using SyncNet.Message;
using System;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SyncNet.Nodes
{
    internal class TaskProcessor
    {
        //CreateUnbounded => no limit
        private readonly Channel<DataTran> _channel = Channel.CreateUnbounded<DataTran>();
        private readonly CancellationTokenSource _cts = new();
        private Task _processingTask;
        private string _taskName;

        public event Func<DataTran, Task> OnProcessMessageAsync;

        public Task Start(string taskName)
        {
            _taskName = taskName;

            _processingTask = Task.Run(() => ProcessTransaction(_cts.Token));

            return Task.CompletedTask;
        }

        public async Task Stop()
        {
            _cts.Cancel();

            if (_processingTask != null)
                await _processingTask;
        }

        private async Task ProcessTransaction(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await foreach (var data in _channel.Reader.ReadAllAsync(token))
                    {
                        try
                        {
                            if (OnProcessMessageAsync != null)
                            {
                                //flexible: async or sync
                                //ConfigureAwait false => pindahkan task ke thread lain
                                await OnProcessMessageAsync(data);
                            }
                        }
                        catch (Exception ex)
                        {
                            await AppProcessor.Logger(ex.Message,
                                NbFormat.FormatBinary(data.Data), data.NodeName);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    await AppProcessor.Logger($"Task {_taskName}: cancelled");
                }
                catch (Exception ex)
                {
                    await AppProcessor.Logger($"Task {_taskName}: {ex.Message}");

                    //delay before retry
                    await Task.Delay(1000, token);
                }
            }
        }

        public async Task EnqueueMessage(DataTran dataTran)
        {
            try
            {
                _channel.Writer.TryWrite(dataTran);
            }
            catch (Exception ex)
            {
                await AppProcessor.Logger($"Task {_taskName}: {ex.Message}",
                    NbFormat.FormatBinary(dataTran.Data), dataTran.NodeName);
            }
        }

        internal class DataTran
        {
            public byte[] Data { get; set; }
            public int TotalBytes { get; set; }
            public string NodeName { get; set; }
            public string ConnectionName { get; set; }
            public string WSMessage { get; set; }
            public Request MsgOriginal { get; set; }
            public EndPoint RemoteEP { get; set; }
            public HttpListenerContext Ctx { get; set; }
            public HttpStatusCode StatusCode { get; set; }
        }
    }
}
