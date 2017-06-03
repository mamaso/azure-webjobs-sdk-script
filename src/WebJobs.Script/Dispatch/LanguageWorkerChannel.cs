﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Azure.WebJobs.Script.Rpc;
using Microsoft.Azure.WebJobs.Script.Rpc.Messages;

namespace Microsoft.Azure.WebJobs.Script.Dispatch
{
    // TODO: move to RPC project?
    internal class LanguageWorkerChannel : ILanguageWorkerChannel
    {
        private readonly LanguageWorkerConfig _workerConfig;
        private readonly ScriptHostConfiguration _scriptConfig;
        private readonly TraceWriter _logger;
        private Process _process;
        private IObservable<ChannelContext> _connections;
        private ChannelContext _context;

        public LanguageWorkerChannel(ScriptHostConfiguration scriptConfig, LanguageWorkerConfig workerConfig, TraceWriter logger, IObservable<ChannelContext> connections)
        {
            _workerConfig = workerConfig;
            _scriptConfig = scriptConfig;
            _logger = logger;
            _connections = connections;
        }

        public async Task<object> InvokeAsync(Dictionary<string, object> scriptExecutionContext)
        {
            InvocationRequest invocationRequest = scriptExecutionContext.ToRpcInvocationRequest();
            object result = null;
            InvocationResponse invocationResponse = await _context.SendAsync<InvocationRequest, InvocationResponse>(invocationRequest);
            Dictionary<string, object> itemsDictionary = new Dictionary<string, object>();
            if (invocationResponse.OutputData?.Count > 0)
            {
                foreach (ParameterBinding outputParameterBinding in invocationResponse.OutputData)
                {
                    object objValue = Utilities.ConvertTypedDataToObject(outputParameterBinding.Data);
                    if (outputParameterBinding.Name == "$return")
                    {
                        result = objValue;
                    }
                    else
                    {
                        itemsDictionary.Add(outputParameterBinding.Name, objValue);
                    }
                }
            }
            Dictionary<string, object> bindingsDictionary = (Dictionary<string, object>)scriptExecutionContext["bindings"];
            bindingsDictionary.AddRange(itemsDictionary);
            scriptExecutionContext["bindings"] = bindingsDictionary;
            return result;
        }

        public async Task HandleFileEventAsync(FileSystemEventArgs fileEvent)
        {
            FileChangeEventRequest request = new FileChangeEventRequest();
            FileChangeEventResponse response = await _context.SendAsync<FileChangeEventRequest, FileChangeEventResponse>(request);
        }

        public async Task<string> LoadAsync(FunctionMetadata functionMetadata)
        {
            FunctionLoadRequest request = new FunctionLoadRequest()
            {
                FunctionId = Guid.NewGuid().ToString(),
                Metadata = functionMetadata.ToRpcFunctionMetadata()
            };
            FunctionLoadResponse response = await _context.SendAsync<FunctionLoadRequest, FunctionLoadResponse>(request);

            // TODO handle load response and store loaded FunctionIds
            return response.FunctionId;
        }

        public async Task StartAsync()
        {
            await StopAsync();

            string requestId = "requestId";

            TaskCompletionSource<bool> connectionSource = new TaskCompletionSource<bool>();
            IDisposable subscription = null;
            subscription = _connections
                .Where(msg => msg.RequestId == requestId)

                // .Timeout(TimeSpan.FromSeconds(100))
                .Subscribe(msg =>
                {
                    _context = msg;
                    connectionSource.SetResult(true);
                    subscription?.Dispose();
                }, exc =>
                {
                    connectionSource.SetException(exc);
                    subscription?.Dispose();
                });
            Utilities.IsTcpEndpointAvailable("127.0.0.1", 50051, _logger);
            Task startWorkerTask = StartWorkerAsync(_workerConfig, requestId);

            await Task.WhenAll(startWorkerTask, connectionSource.Task);
        }

        public Task StopAsync()
        {
            // TODO: send cancellation warning
            // TODO: Close request stream for each worker pool
            _process?.Kill();
            _process = null;
            return Task.CompletedTask;
        }

        internal Task StartWorkerAsync(LanguageWorkerConfig config, string requestId)
        {
            var tcs = new TaskCompletionSource<bool>();

           // try
           // {
                // List<string> output = new List<string>();

            // var startInfo = new ProcessStartInfo
            //    {
            //        FileName = config.ExecutablePath,
            //        RedirectStandardOutput = true,
            //        RedirectStandardError = true,
            //        CreateNoWindow = true,
            //        UseShellExecute = false,
            //        ErrorDialog = false,
            //        WorkingDirectory = _scriptConfig.RootScriptPath,
            //        Arguments = config.ToArgumentString(requestId)
            //    };

            // _process = new Process { StartInfo = startInfo };
            //    _process.ErrorDataReceived += (sender, e) =>
            //    {
            //        _logger.Error(e?.Data);
            //    };
            //    _process.OutputDataReceived += (sender, e) =>
            //    {
            //        _logger.Info(e?.Data);
            //    };
            //    _process.EnableRaisingEvents = true;
            //    _process.Exited += (s, e) =>
            //    {
            //        _process.WaitForExit();
            //        _process.Close();
            //        tcs.SetResult(true);
            //    };

            // _process.Start();

            // _process.BeginErrorReadLine();
            //    _process.BeginOutputReadLine();
            // }
            // catch (Exception exc)
            // {
            //    _logger.Error("Error starting LanguageWorkerChannel", exc);
            //    tcs.SetException(exc);
            // }
            tcs.SetResult(true);
            return tcs.Task;
        }
    }
}
