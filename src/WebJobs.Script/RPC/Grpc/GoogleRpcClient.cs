﻿// <auto-generated>
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Grpc.Core;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Azure.WebJobs.Script.Rpc.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script.Rpc
{
#pragma warning disable SA1200 // Using directives must be placed correctly
    using RpcDataType = TypedData.Types.Type;
#pragma warning restore SA1200 // Using directives must be placed correctly

    // [CLSCompliant(false)]
    public class GoogleRpcClient
    {
        /// <summary>
        /// Sample client code that makes gRPC calls to the server.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        private readonly FunctionRpc.FunctionRpcClient functionRpcClient;

        // private static TraceWriter systemTraceWriter;

        public GoogleRpcClient(FunctionRpc.FunctionRpcClient client)
        {
            this.functionRpcClient = client;
        }

        // public static event UnhandledExceptionEventHandler UnhandledException;

        protected static void UnhandledExceptionInWorkerHandler(object sender, UnhandledExceptionEventArgs e)
        {
            // systemTraceWriter.Error(e.ExceptionObject.ToString());
        }

        /// <summary>
        /// Bi-directional streaming.
        /// </summary>
        /**
        public async Task<object> InvokeFunction(string scriptFilePath, object[] parameters, FunctionInvocationContext context, Dictionary<string, object> scriptExecutionContext)
        {
            UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionInWorkerHandler);
            TraceWriter systemTraceWriter = (TraceWriter)scriptExecutionContext["systemTraceWriter"];
            GoogleRpcClient.systemTraceWriter = systemTraceWriter;
            object result = null;
            if (parameters != null && parameters.Length > 0)
            {
                object input = parameters[0];
            }
            Log("*** Invoke Function");
            FunctionRpcInvokeMetadata funcMetadata = new FunctionRpcInvokeMetadata();
            funcMetadata.ScriptFile = scriptFilePath;
            funcMetadata.InvocationId = context.ExecutionContext.InvocationId.ToString();
            if (scriptExecutionContext != null)
            {
                if ((Dictionary<string, object>)scriptExecutionContext["executionContext"] != null)
                {
                    Dictionary<string, object> executionContext = (Dictionary<string, object>)scriptExecutionContext["executionContext"];
                    funcMetadata.FunctionName = (string)executionContext["functionName"];
                }

                if ((Dictionary<string, object>)scriptExecutionContext["bindingData"] != null)
                {
                    TriggerMetadata bindingsdata = new TriggerMetadata();
                    bindingsdata.MessageBindingData.Add(GetMetadataFromDictionary((Dictionary<string, object>)scriptExecutionContext["bindingData"], string.Empty));
                    funcMetadata.TriggerMetadata = bindingsdata;
                }

                if ((Dictionary<string, KeyValuePair<object, DataType>>)scriptExecutionContext["inputBindings"] != null)
                {
                    var inputBindings = (Dictionary<string, KeyValuePair<object, DataType>>)scriptExecutionContext["inputBindings"];
                    foreach (var inputBinding in inputBindings)
                    {
                        DataType dataType = DataType.String;
                        var item = inputBindings[inputBinding.Key];
                        FunctionBindings inputBindingsMessage = new FunctionBindings()
                        {
                            Name = inputBinding.Key,
                            DataValue = new DataValue()
                        };

                        ByteString convertedToByteString = null;

                        if (inputBinding.Key == "req" || inputBinding.Key == "request" || inputBinding.Key == "webhookReq")
                        {
                            HttpMessage httpRequest = BuildRpcHttpMessage((Dictionary<string, object>)item.Key);
                            inputBindingsMessage.DataType = RpcDataType.Http;
                            inputBindingsMessage.DataValue.HttpMessageValue = httpRequest;
                        }
                        else if (item.Key != null)
                        {
                            if (item.Key.GetType().FullName.Contains("Byte"))
                            {
                                inputBindingsMessage.DataType = RpcDataType.Bytes;
                            }
                            convertedToByteString = ConvertObjectToByteString(item.Key, out dataType, item.Value);

                            switch (dataType)
                            {
                                case DataType.Binary:
                                case DataType.Stream:
                                    inputBindingsMessage.DataType = RpcDataType.Bytes;
                                    inputBindingsMessage.DataValue.BytesValue = convertedToByteString;
                                    break;

                                case DataType.String:
                                default:
                                    inputBindingsMessage.DataType = RpcDataType.String;
                                    inputBindingsMessage.DataValue.StringValue = ConvertObjectToString(item.Key);
                                    break;
                            }
                        }
                        funcMetadata.FunctionInputBindings.Add(inputBindingsMessage);
                    }

                    funcMetadata.TriggerType = (string)scriptExecutionContext["_triggerType"];
                    string entryPoint;
                    if (scriptExecutionContext.TryGetValue("_entryPoint", out entryPoint))
                    {
                        funcMetadata.EntryPoint = (string)scriptExecutionContext["_entryPoint"];
                    }
                }

                using (var call = FunctionRpcClient.RpcInvokeFunction())
                {
                    var responseReaderTask = Task.Run(async () =>
                    {
                        while (await call.ResponseStream.MoveNext())
                        {
                            var funcMetadataOut = call.ResponseStream.Current;
                            foreach (var item in funcMetadataOut.MessageOutputs)
                            {
                                if (item.Key == "result")
                                {
                                    result = item.Value.ToStringUtf8();
                                }
                            }

                            if (funcMetadataOut.FunctionOutputBindings != null && funcMetadataOut.FunctionOutputBindings.Count > 0)
                            {
                                Dictionary<string, object> itemsDictionary = new Dictionary<string, object>();
                                foreach (var functionOutputBindingsItem in funcMetadataOut.FunctionOutputBindings)
                                {
                                    object objValue = null;
                                    if (functionOutputBindingsItem.DataValue != null)
                                    {
                                        switch (functionOutputBindingsItem.DataType)
                                        {
                                            case RpcDataType.Bytes:
                                                objValue = ConvertBytesStringToObject(functionOutputBindingsItem.DataValue.BytesValue, "Buffer");
                                                break;
                                            case RpcDataType.Http:
                                                objValue = ConvertFromHttpMessageToExpando(functionOutputBindingsItem.DataValue.HttpMessageValue, functionOutputBindingsItem.Name);
                                                break;
                                            case RpcDataType.String:
                                            default:
                                                objValue = functionOutputBindingsItem.DataValue.StringValue;
                                                break;
                                        }
                                    }

                                    itemsDictionary.Add(functionOutputBindingsItem.Name, objValue);
                                }

                                Dictionary<string, object> bindingsDictionary = (Dictionary<string, object>)scriptExecutionContext["bindings"];
                                bindingsDictionary.AddRange(itemsDictionary);
                                scriptExecutionContext["bindings"] = bindingsDictionary;
                            }

                            TraceWriter traceWriter = (TraceWriter)scriptExecutionContext["traceWriter"];
                            foreach (var itemLog in funcMetadataOut.Logs)
                            {
                                JObject logData = JObject.Parse(itemLog);
                                string message = (string)logData["msg"];
                                if (message != null)
                                {
                                    try
                                    {
                                        TraceLevel level = (TraceLevel)Enum.Parse(typeof(TraceLevel), logData["lvl"].ToString());
                                        var evt = new TraceEvent(level, message);
                                        traceWriter.Trace(evt);
                                    }
                                    catch (ObjectDisposedException)
                                    {
                                        // if a function attempts to write to a disposed
                                        // TraceWriter. Might happen if a function tries to
                                        // log after calling done()
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(funcMetadataOut.UnhandledExceptionError))
                            {
                                // if (UnhandledException != null)
                                {
                                    // raise the event to allow subscribers to handle
                                    var ex = new InvalidOperationException((string)funcMetadataOut.UnhandledExceptionError);
                                    UnhandledException(null, new UnhandledExceptionEventArgs(ex, true));

                                    // Ensure that we allow the unhandled exception to kill the process.
                                    // unhandled Node global exceptions should never be swallowed.
                                    throw ex;
                                }
                            }
                        }
                    });

                    Log("invoking invocationId: \"{0}\" ", funcMetadata.InvocationId);

                    await call.RequestStream.WriteAsync(funcMetadata);
                    await call.RequestStream.CompleteAsync();
                    await responseReaderTask;

                    Log("Finished func invoke");
                    return result;
                }
            }
            return null;
        }
        **/
        private static RpcHttp BuildRpcHttpMessage(Dictionary<string, object> inputHttpMessage)
        {
            RpcHttp requestMessage = new RpcHttp();
            TypedData messageBody = new TypedData();
            object bodyValue = null;
            DataType inputDatatype = DataType.String;
            foreach (var item in inputHttpMessage)
            {
                if (item.Key == "headers")
                {
                    var headers = item.Value as IDictionary<string, string>;
                    requestMessage.Headers.Add(headers);
                }
                else if (item.Key == "query")
                {
                    var query = item.Value as IDictionary<string, string>;
                    requestMessage.Query.Add(query);
                }
                else if (item.Key == "method")
                {
                    requestMessage.Method = (string)item.Value;
                }
                else if (item.Key == "originalUrl")
                {
                    requestMessage.Url = (string)item.Value;
                }
                else if (item.Key == "body")
                {
                    bodyValue = item.Value;
                }
                else if (item.Key == "rawBody")
                {
                    requestMessage.RawBody = item.Value.ToString();
                }
                else if (item.Key == "params")
                {
                    requestMessage.Params.Add(GetMetadataFromDictionary((Dictionary<string, object>)item.Value, string.Empty));
                }
                else
                {
                    throw new InvalidOperationException("Did not find req key");
                }
            }
            string rawHeader = null;

            if (requestMessage.Headers.TryGetValue("raw", out rawHeader))
            {
                if (bool.Parse(rawHeader))
                {
                    inputDatatype = DataType.Binary;
                }
            }

            if (bodyValue != null && bodyValue.GetType().FullName.Contains("Byte"))
            {
                inputDatatype = DataType.Binary;
            }

            messageBody.TypeVal = RpcDataType.String;
            DataType dataType = DataType.String;
            if (bodyValue != null)
            {
                messageBody = new TypedData();
                switch (inputDatatype)
                {
                    case DataType.Binary:
                    case DataType.Stream:
                        messageBody.TypeVal = RpcDataType.Bytes;
                        messageBody.BytesVal = ConvertObjectToByteString(bodyValue, out dataType, inputDatatype);
                        break;

                    case DataType.String:
                    default:
                        messageBody.TypeVal = RpcDataType.String;
                        messageBody.StringVal = ConvertObjectToString(bodyValue);
                        break;
                }
            }
            if (bodyValue != null)
            {
                requestMessage.Body = messageBody;
            }

            return requestMessage;
        }

        private static MapField<string, ByteString> GetMetadataFromDictionary(Dictionary<string, object> scriptExecutionContextDictionary, string triggerType)
        {
            MapField<string, ByteString> itemsDictionary = new MapField<string, ByteString>();
            foreach (var item in scriptExecutionContextDictionary)
            {
                if (item.Value != null)
                {
                    if (item.Value.GetType() == typeof(string) || (item.Value.GetType() == typeof(int)))
                    {
                        if (triggerType == "blobTrigger")
                        {
                            itemsDictionary.Add(item.Key, ByteString.CopyFrom((byte[])Encoding.ASCII.GetBytes(item.Value.ToString())));
                        }
                        else
                        {
                            itemsDictionary.Add(item.Key, ByteString.CopyFromUtf8(item.Value.ToString()));
                        }
                    }
                    else if (item.Value.GetType().FullName.Contains("Generic.Dictionary"))
                    {
                        JObject jobject = JObject.FromObject(item.Value);
                        itemsDictionary.Add(item.Key, ByteString.CopyFromUtf8(jobject.ToString()));
                    }
                    else if (item.Value.GetType().FullName.Contains("ExpandoObject"))
                    {
                        string jsonOfTest = Newtonsoft.Json.JsonConvert.SerializeObject(item.Value);
                        itemsDictionary.Add(item.Key, ByteString.CopyFromUtf8(jsonOfTest));
                    }
                    else if (item.Value.GetType().FullName.Contains("Byte"))
                    {
                        itemsDictionary.Add(item.Key, ByteString.CopyFrom((byte[])item.Value));
                    }
                    else if (item.Value.GetType().FullName.Contains("Newtonsoft.Json.Linq.JObject"))
                    {
                        itemsDictionary.Add(item.Key, ByteString.CopyFromUtf8(item.Value.ToString()));
                    }
                    else if (item.Value.GetType().FullName.Contains("Newtonsoft.Json.Linq.JArray"))
                    {
                        itemsDictionary.Add(item.Key, ByteString.CopyFromUtf8(item.Value.ToString()));
                    }
                    else
                    {
                        throw new InvalidOperationException("did not find item type: " + item.Value.GetType());
                    }
                }
            }
            return itemsDictionary;
        }

        private static object ConvertBytesStringToObject(ByteString byteStringData, string dataType)
        {
            if (dataType == "Buffer")
            {
                return byteStringData.ToByteArray();
            }
            if (dataType == "int")
            {
                return int.Parse(byteStringData.ToStringUtf8());
            }
            return byteStringData.ToStringUtf8();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private static object ConvertFromHttpMessageToExpando(RpcHttp inputMessage, string key = "")
        {
            if (inputMessage == null)
            {
                return null;
            }
            if (inputMessage.RawResponse != null)
            {
                object rawResponseData = null;
                switch (inputMessage.RawResponse.TypeVal)
                {
                    case RpcDataType.Bytes:
                        rawResponseData = ConvertBytesStringToObject(inputMessage.RawResponse.BytesVal, "Buffer");
                        break;
                    case RpcDataType.Http:
                        rawResponseData = ConvertFromHttpMessageToExpando(inputMessage.RawResponse.HttpVal);
                        break;
                    case RpcDataType.String:
                    default:
                        rawResponseData = inputMessage.RawResponse.StringVal;
                        break;
                }

                try
                {
                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.None
                    };
                    dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(rawResponseData.ToString(), settings);
                    return obj;
                }
                catch (System.Exception)
                {
                    return rawResponseData;
                }
            }
            dynamic expando = new ExpandoObject();
            expando.method = inputMessage.Method;
            expando.query = inputMessage.Query as IDictionary<string, string>;
            expando.statusCode = inputMessage.StatusCode;
            IDictionary<string, string> inputMessageHeaders = inputMessage.Headers as IDictionary<string, string>;
            IDictionary<string, object> headers = new Dictionary<string, object>();
            foreach (var item in inputMessageHeaders)
            {
                headers.Add(item.Key, item.Value);
            }
            expando.headers = headers;
            if (inputMessage.Body != null)
            {
                if (inputMessage.IsRaw && inputMessage.Body.TypeVal != RpcDataType.Bytes)
                {
                    expando.body = inputMessage.Body.StringVal;
                }
                else
                {
                    object bodyConverted = null;
                    switch (inputMessage.Body.TypeVal)
                    {
                        case RpcDataType.Bytes:
                            bodyConverted = ConvertBytesStringToObject(inputMessage.Body.BytesVal, "Buffer");
                            break;
                        case RpcDataType.String:
                        default:
                            bodyConverted = inputMessage.Body.StringVal;
                            break;
                    }
                    try
                    {
                        dynamic d = JsonConvert.DeserializeObject<ExpandoObject>(bodyConverted.ToString());
                        expando.body = d;
                    }
                    catch (System.Exception)
                    {
                        expando.body = bodyConverted;
                    }
                }
                if (key == "res" && inputMessage.IsRaw)
                {
                    expando.isRaw = true;
                }
            }
            else
            {
                expando.body = null;
            }
            return expando;
        }

        private static ByteString ConvertObjectToByteString(object scriptExecutionContextValue, out DataType dataType, DataType inputDataType)
        {
            dataType = DataType.String;
            if (scriptExecutionContextValue == null)
            {
                return null;
            }
            if (scriptExecutionContextValue.GetType() == typeof(string) || scriptExecutionContextValue.GetType() == typeof(int))
            {
                return ByteString.CopyFromUtf8(scriptExecutionContextValue.ToString());
            }
            else if (scriptExecutionContextValue.GetType().FullName.Contains("Generic.Dictionary"))
            {
                JObject jobject = JObject.FromObject(scriptExecutionContextValue);
                return ByteString.CopyFromUtf8(jobject.ToString());
            }
            else if (scriptExecutionContextValue.GetType().FullName.Contains("ExpandoObject"))
            {
                string jsonOfTest = Newtonsoft.Json.JsonConvert.SerializeObject(scriptExecutionContextValue);
                return ByteString.CopyFromUtf8(jsonOfTest);
            }
            else if (scriptExecutionContextValue.GetType().FullName.Contains("Byte") || inputDataType == DataType.Binary)
            {
                dataType = DataType.Binary;
                return ByteString.CopyFrom((byte[])scriptExecutionContextValue);
            }
            else if (scriptExecutionContextValue.GetType().FullName.Contains("Newtonsoft.Json.Linq.JObject"))
            {
                return ByteString.CopyFromUtf8(scriptExecutionContextValue.ToString());
            }
            else if (scriptExecutionContextValue.GetType().FullName.Contains("Newtonsoft.Json.Linq.JArray"))
            {
                return ByteString.CopyFromUtf8(scriptExecutionContextValue.ToString());
            }
            else
            {
                throw new InvalidOperationException("did not find item type: " + scriptExecutionContextValue.GetType().FullName);
            }
        }

        private static string ConvertObjectToString(object scriptExecutionContextValue)
        {
            if (scriptExecutionContextValue == null)
            {
                return null;
            }
            if (scriptExecutionContextValue.GetType() == typeof(string) || scriptExecutionContextValue.GetType() == typeof(int))
            {
                return scriptExecutionContextValue.ToString();
            }
            else if (scriptExecutionContextValue.GetType().FullName.Contains("Generic.Dictionary"))
            {
                JObject jobject = JObject.FromObject(scriptExecutionContextValue);
                return jobject.ToString();
            }
            else if (scriptExecutionContextValue.GetType().FullName.Contains("ExpandoObject"))
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(scriptExecutionContextValue);
            }
            else if (scriptExecutionContextValue.GetType().FullName.Contains("Newtonsoft.Json.Linq.JObject"))
            {
                return scriptExecutionContextValue.ToString();
            }
            else if (scriptExecutionContextValue.GetType().FullName.Contains("Newtonsoft.Json.Linq.JArray"))
            {
                return scriptExecutionContextValue.ToString();
            }
            else
            {
                throw new InvalidOperationException("did not find item type: " + scriptExecutionContextValue.GetType().FullName);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "s")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "args")]
        private static void Log(string s, params object[] args)
        {
            // systemTraceWriter.Verbose(string.Format(s, args));
        }
    }
}