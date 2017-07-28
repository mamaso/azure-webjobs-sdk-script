// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;

namespace Microsoft.Azure.WebJobs.Script
{
    internal class FunctionRpcImpl : FunctionRpc.FunctionRpcBase, IDisposable
    {
        private Subject<ChannelContext> _connections = new Subject<ChannelContext>();

        public IObservable<ChannelContext> Connections => _connections;

        public void Dispose()
        {
            _connections.Dispose();
        }

        public override async Task EventStream(IAsyncStreamReader<StreamingMessage> requestStream, IServerStreamWriter<StreamingMessage> responseStream, ServerCallContext context)
        {
            var input = new Subject<StreamingMessage>();
            var output = new Subject<StreamingMessage>();

            while (await requestStream.MoveNext(CancellationToken.None))
            {
                if (requestStream.Current.ContentCase == StreamingMessage.ContentOneofCase.StartStream)
                {
                    // TODO Worker start Async needs to wait for the startStream message before it completes the task
                    var startStream = requestStream.Current.StartStream;

                    output.Subscribe(msg => responseStream.WriteAsync(msg));

                    _connections.OnNext(new ChannelContext
                    {
                        WorkerId = startStream.WorkerId,
                        RequestId = requestStream.Current.RequestId,
                        InputStream = input,
                        OutputStream = output
                    });
                }
                input.OnNext(requestStream.Current);
            }
        }
    }
}
