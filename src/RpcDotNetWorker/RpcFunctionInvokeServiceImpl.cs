using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Azure.WebJobs.Script.Rpc.Messages;

namespace RpcDotNetWorker
{
    class RpcFunctionInvokeServiceImpl : RpcFunction.RpcFunctionBase
    {
        public override async Task RpcInvokeFunction(IAsyncStreamReader<RpcFunctionInvokeMetadata> requestStream, IServerStreamWriter<RpcFunctionInvokeMetadata> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                var funcMetadata = requestStream.Current;
                funcMetadata.FunctionName = " Queue Trigger Out - Dotnet";
                //funcMetadata.Output.Add(funcMetadata.InvocationId, ByteString.CopyFromUtf8($"response to invocationId:{funcMetadata.InvocationId}"));
                await responseStream.WriteAsync(funcMetadata);
            }
        }
    }
}
