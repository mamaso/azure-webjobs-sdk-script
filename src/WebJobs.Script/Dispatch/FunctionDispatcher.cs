// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Azure.WebJobs.Script.Abstractions.Rpc;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Azure.WebJobs.Script.Eventing;

namespace Microsoft.Azure.WebJobs.Script.Dispatch
{
    internal class FunctionDispatcher : IFunctionDispatcher
    {
        private IScriptEventManager _eventManager;
        private IRpcServer _server;
        private Func<WorkerConfig, ILanguageWorkerChannel> _channelFactory;
        private List<WorkerConfig> _workerConfigs;

        private IDictionary<string, WorkerConfig> _workerMap = new Dictionary<string, WorkerConfig>();
        private ConcurrentDictionary<WorkerConfig, ILanguageWorkerChannel> _channelMap = new ConcurrentDictionary<WorkerConfig, ILanguageWorkerChannel>();
        private ConcurrentDictionary<WorkerConfig, List<FunctionRegistrationContext>> _registeredFunctions = new ConcurrentDictionary<WorkerConfig, List<FunctionRegistrationContext>>();

        private IDisposable _workerReadySubscription;
        private bool disposedValue = false;

        public FunctionDispatcher(
            IScriptEventManager manager,
            IRpcServer server,
            Func<WorkerConfig, ILanguageWorkerChannel> channelFactory,
            List<WorkerConfig> workers)
        {
            _eventManager = manager;
            _server = server;
            _channelFactory = channelFactory;
            _workerConfigs = workers ?? new List<WorkerConfig>();
        }

        public bool IsSupported(FunctionMetadata functionMetadata)
        {
            return _workerConfigs.Any(config => config.Extension == Path.GetExtension(functionMetadata.ScriptFile));
        }

        public void Register(FunctionRegistrationContext context)
        {
            WorkerConfig workerConfig = _workerConfigs.First(config => config.Extension == Path.GetExtension(context.Metadata.ScriptFile));
            _workerMap.Add(context.Metadata.FunctionId, workerConfig);
            var channel = _channelMap.GetOrAdd(workerConfig, _channelFactory);
            channel.Register(context);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _workerReadySubscription.Dispose();
                    foreach (var pair in _channelMap)
                    {
                        var channel = pair.Value;
                        channel.Dispose();
                        _server.ShutdownAsync().GetAwaiter().GetResult();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
