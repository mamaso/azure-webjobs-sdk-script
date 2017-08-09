// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Script.Grpc.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RpcDataType = Microsoft.Azure.WebJobs.Script.Grpc.Messages.TypedData.DataOneofCase;

namespace Microsoft.Azure.WebJobs.Script
{
    public static class RpcMessageConversionExtensions
    {
        public static object ToObject(this TypedData typedData)
        {
            switch (typedData.DataCase)
            {
                case RpcDataType.Bytes:
                case RpcDataType.Stream:
                    return typedData.Bytes.ToByteArray();
                case RpcDataType.String:
                    return typedData.String;
                case RpcDataType.Json:
                    return JsonConvert.DeserializeObject(typedData.Json);
                case RpcDataType.Http:
                    return Utilities.ConvertFromHttpMessageToExpando(typedData.Http);
                case RpcDataType.Int:
                    return typedData.Int;
                case RpcDataType.Double:
                    return typedData.Double;
                case RpcDataType.None:
                    return null;
                default:
                    // TODO better exception
                    throw new InvalidOperationException("Unknown RpcDataType");
            }
        }

        public static async Task<TypedData> ToRpcAsync(this object value)
        {
            TypedData typedData = new TypedData();

            if (value == null)
            {
                return typedData;
            }

            if (value is byte[] arr)
            {
                typedData.Bytes = ByteString.CopyFrom(arr);
            }
            else if (value is JObject jobj)
            {
                typedData.Json = jobj.ToString();
            }
            else if (value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                typedData.Json = JObject.FromObject(value).ToString();
            }
            else if (value is HttpRequestMessage request)
            {
                var http = new RpcHttp()
                {
                    Url = request.RequestUri.ToString(),
                    Method = request.Method.ToString()
                };
                typedData.Http = http;

                http.Query.Add(request.GetQueryParameterDictionary());
                foreach (var pair in request.GetRawHeaders())
                {
                    http.Headers.Add(pair.Key.ToLowerInvariant(), pair.Value);
                }

                if (request.Properties.TryGetValue(HttpExtensionConstants.AzureWebJobsHttpRouteDataKey, out IDictionary<string, object> parameters))
                {
                    foreach (var pair in parameters)
                    {
                        http.Params.Add(pair.Key, pair.Value.ToString());
                    }
                }

                if (request.Content != null && request.Content.Headers.ContentLength > 0)
                {
                    MediaTypeHeaderValue contentType = request.Content.Headers.ContentType;
                    object body = null;
                    string rawBody = null;

                    switch (contentType?.MediaType)
                    {
                        case "application/json":
                            rawBody = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                            body = JsonConvert.DeserializeObject(rawBody);
                            break;

                        case "application/octet-stream":
                            body = await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                            break;

                        default:
                            body = rawBody = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                            break;
                    }

                    http.Body = await body.ToRpcAsync();
                }
            }
            else
            {
                typedData.String = value.ToString();
            }
            return typedData;
        }
    }
}