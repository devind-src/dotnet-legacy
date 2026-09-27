using SyncNet.Common;
using SyncNet.Library;
using SyncNet.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SyncNet.Services
{
    internal class TaskProcessor
    {
        //CreateUnbounded => no limit
        private readonly Channel<DataTran> _channel = Channel.CreateUnbounded<DataTran>();
        private readonly CancellationTokenSource _cts = new();
        private List<Task> _processingTask = [];
        private string _taskName;

        public event Func<DataTran, Task> OnProcessMessageAsync;

        public void Start(string taskName)
        {
            _taskName = taskName;

            int maxTask = Math.Clamp(AppConfig.MaxWorker, 1, 2);
            for (int i = 0; i < maxTask; i++)
            {
                //Create a new task for each processor core
                _processingTask.Add(Task.Run(() => ProcessTransaction(_cts.Token)));
            }
        }

        public async Task Stop()
        {
            _cts.Cancel();
            await Task.WhenAll(_processingTask);
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
                            await MyApp.Logger(ex.Message, NbMessage.FormatBinary(data.Data));
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    await MyApp.Logger($"Task {_taskName}: cancelled");
                }
                catch (Exception ex)
                {
                    await MyApp.Logger($"Task {_taskName}: {ex.Message}");

                    //delay before retry
                    await Task.Delay(1000, token);
                }
            }
        }

        public async Task EnqueueMessage(DataTran data)
        {
            try
            {
                _channel.Writer.TryWrite(data);
            }
            catch (Exception ex)
            {
                await MyApp.Logger($"Task {_taskName}: {ex.Message}", NbMessage.FormatBinary(data.Data));
            }
        }
    }

    internal class DataTran
    {
        public string NodeName { get; set; }
        public byte[] Data { get; set; }
        public EndPoint RemoteEP { get; set; }
    }
}
