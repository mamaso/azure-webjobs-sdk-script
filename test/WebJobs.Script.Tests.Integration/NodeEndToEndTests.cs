// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Script.Tests
{
    public class NodeEndToEndTests : LanguageWorkerEndToEndTestsBase<NodeEndToEndTests.TestFixture>
    {
        public NodeEndToEndTests(TestFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Scenario_Logging()
        {
            TestHelpers.ClearFunctionLogs("Scenarios");

            string testData = Guid.NewGuid().ToString();
            JObject input = new JObject
            {
                { "scenario", "logging" },
                { "input", testData },
            };
            Dictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "input", input.ToString() }
            };
            await Fixture.Host.CallAsync("Scenarios", arguments);

            var logs = await TestHelpers.GetFunctionLogsAsync("Scenarios");

            // verify use of context.log to log complex objects
            TraceEvent scriptTrace = Fixture.TraceWriter.Traces.Single(p => p.Message.Contains(testData));
            Assert.Equal(TraceLevel.Info, scriptTrace.Level);
            JObject logEntry = JObject.Parse(scriptTrace.Message);
            Assert.Equal("This is a test", logEntry["message"]);
            Assert.Equal("v6.5.0", (string)logEntry["version"]);
            Assert.Equal(testData, logEntry["input"]);

            // verify log levels
            TraceEvent[] traces = Fixture.TraceWriter.Traces.Where(t => t.Message.Contains("loglevel")).ToArray();
            Assert.Equal(TraceLevel.Info, traces[0].Level);
            Assert.Equal("loglevel default", traces[0].Message);
            Assert.Equal(TraceLevel.Info, traces[1].Level);
            Assert.Equal("loglevel info", traces[1].Message);
            Assert.Equal(TraceLevel.Verbose, traces[2].Level);
            Assert.Equal("loglevel verbose", traces[2].Message);
            Assert.Equal(TraceLevel.Warning, traces[3].Level);
            Assert.Equal("loglevel warn", traces[3].Message);
            Assert.Equal(TraceLevel.Error, traces[4].Level);
            Assert.Equal("loglevel error", traces[4].Message);
        }

        private async Task<CloudBlobContainer> GetEmptyContainer(string containerName)
        {
            var container = Fixture.BlobClient.GetContainerReference(containerName);
            if (container.Exists())
            {
                foreach (CloudBlockBlob blob in container.ListBlobs())
                {
                    await blob.DeleteAsync();
                }
            }
            return container;
        }

        [Fact]
        public async Task Scenario_OutputBindingContainsFunctions()
        {
            var container = await GetEmptyContainer("scenarios-output");

            JObject input = new JObject
                {
                    { "scenario", "bindingContainsFunctions" },
                    { "container", "scenarios-output" },
                };
            Dictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "input", input.ToString() }
            };
            await Fixture.Host.CallAsync("Scenarios", arguments);

            var blobs = container.ListBlobs().Cast<CloudBlockBlob>().ToArray();
            Assert.Equal(1, blobs.Length);

            var blobString = await blobs[0].DownloadTextAsync();
            Assert.Equal("{\"nested\":{},\"array\":[{}],\"value\":\"value\"}", blobString);
        }

        [Fact]
        public async Task MultipleExports()
        {
            TestHelpers.ClearFunctionLogs("MultipleExports");

            Dictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "input", string.Empty }
            };
            await Fixture.Host.CallAsync("MultipleExports", arguments);

            var logs = await TestHelpers.GetFunctionLogsAsync("MultipleExports");

            Assert.Equal(3, logs.Count);
            Assert.True(logs[1].Contains("Exports: IsObject=true, Count=4"));
        }

        [Fact]
        public async Task SingleNamedExport()
        {
            TestHelpers.ClearFunctionLogs("SingleNamedExport");

            Dictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "input", string.Empty }
            };
            await Fixture.Host.CallAsync("SingleNamedExport", arguments);

            var logs = await TestHelpers.GetFunctionLogsAsync("SingleNamedExport");

            Assert.Equal(3, logs.Count);
            Assert.True(logs[1].Contains("Exports: IsObject=true, Count=1"));
        }

        [Fact]
        public async Task HttpTriggerExpressApi_Get()
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                RequestUri = new Uri(string.Format("http://localhost/api/httptrigger?name=Mathew%20Charles&location=Seattle")),
                Method = HttpMethod.Get,
            };
            request.SetConfiguration(new HttpConfiguration());
            request.Headers.Add("test-header", "Test Request Header");

            Dictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "request", request }
            };
            await Fixture.Host.CallAsync("HttpTriggerExpressApi", arguments);

            HttpResponseMessage response = (HttpResponseMessage)request.Properties[ScriptConstants.AzureFunctionsHttpResponseKey];
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            Assert.Equal("Test Response Header", response.Headers.GetValues("test-header").SingleOrDefault());
            Assert.Equal(MediaTypeHeaderValue.Parse("application/json; charset=utf-8"), response.Content.Headers.ContentType);

            string body = await response.Content.ReadAsStringAsync();
            JObject resultObject = JObject.Parse(body);
            Assert.Equal("undefined", (string)resultObject["reqBodyType"]);
            Assert.Null((string)resultObject["reqBody"]);
            Assert.Equal("undefined", (string)resultObject["reqRawBodyType"]);
            Assert.Null((string)resultObject["reqRawBody"]);

            // verify binding data was populated from query parameters
            Assert.Equal("Mathew Charles", (string)resultObject["bindingData"]["name"]);
            Assert.Equal("Seattle", (string)resultObject["bindingData"]["location"]);

            // validate input headers
            JObject reqHeaders = (JObject)resultObject["reqHeaders"];
            Assert.Equal("Test Request Header", reqHeaders["test-header"]);
        }

        [Fact]
        public async Task HttpTriggerExpressApi_SendStatus()
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                RequestUri = new Uri(string.Format("http://localhost/api/httptrigger")),
                Method = HttpMethod.Get
            };
            request.SetConfiguration(new HttpConfiguration());
            request.Headers.Add("scenario", "sendStatus");

            Dictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "request", request }
            };
            await Fixture.Host.CallAsync("HttpTriggerExpressApi", arguments);

            HttpResponseMessage response = (HttpResponseMessage)request.Properties[ScriptConstants.AzureFunctionsHttpResponseKey];
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task HttpTriggerPromise_TestBinding()
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                RequestUri = new Uri(string.Format("http://localhost/api/httptriggerpromise")),
                Method = HttpMethod.Get,
            };
            request.SetConfiguration(Fixture.RequestConfiguration);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

            Dictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "request", request }
            };
            await Fixture.Host.CallAsync("HttpTriggerPromise", arguments);

            HttpResponseMessage response = (HttpResponseMessage)request.Properties[ScriptConstants.AzureFunctionsHttpResponseKey];
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string body = await response.Content.ReadAsStringAsync();
            Assert.Equal("returned from promise", body);
        }

        [Fact]
        public async Task HttpTrigger_Scenarios_ResBinding()
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                RequestUri = new Uri(string.Format("http://localhost/api/httptrigger-scenarios")),
                Method = HttpMethod.Post,
            };
            request.SetConfiguration(new HttpConfiguration());
            request.Headers.Add("scenario", "resbinding");
            Dictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "req", request }
            };
            await Fixture.Host.CallAsync("HttpTrigger-Scenarios", arguments);

            HttpResponseMessage response = (HttpResponseMessage)request.Properties[ScriptConstants.AzureFunctionsHttpResponseKey];
            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("test", await response.Content.ReadAsAsync<string>());
        }

        [Fact]
        public async Task NextTick()
        {
            // See https://github.com/tjanczuk/edge/issues/325.
            // This ensures the workaround is working

            // we're not going to await this call as it may hang if there is
            // a regression, instead, monitor for IsCompleted below.
            JObject input = new JObject
            {
                { "scenario", "nextTick" }
            };
            Task t = Fixture.Host.CallAsync("Scenarios",
                new Dictionary<string, object>()
                {
                    { "input", input.ToString() }
                });

            Task result = await Task.WhenAny(t, Task.Delay(5000));
            Assert.Same(t, result);
            if (t.IsFaulted)
            {
                throw t.Exception;
            }
        }

        [Fact]
        public async Task PromiseResolve()
        {
            JObject input = new JObject
            {
                { "scenario", "promiseResolve" }
            };

            Task t = Fixture.Host.CallAsync("Scenarios",
                new Dictionary<string, object>()
                {
                    { "input", input.ToString() }
                });

            Task result = await Task.WhenAny(t, Task.Delay(5000));
            Assert.Same(t, result);
            if (t.IsFaulted)
            {
                throw t.Exception;
            }
        }

        [Fact]
        public async Task PromiseApi_Resolves()
        {
            JObject input = new JObject
            {
                { "scenario", "promiseApiResolves" }
            };

            Task t = Fixture.Host.CallAsync("Scenarios",
                new Dictionary<string, object>()
                {
                    { "input", input.ToString() }
                });

            Task result = await Task.WhenAny(t, Task.Delay(5000));
            Assert.Same(t, result);
            if (t.IsFaulted)
            {
                throw t.Exception;
            }
        }

        [Fact]
        public async Task PromiseApi_Rejects()
        {
            JObject input = new JObject
            {
                { "scenario", "promiseApiRejects" }
            };

            Task t = Fixture.Host.CallAsync("Scenarios",
                new Dictionary<string, object>()
                {
                    { "input", input.ToString() }
                });

            Task result = await Task.WhenAny(t, Task.Delay(5000));
            Assert.Same(t, result);
            Assert.Equal(true, t.IsFaulted);
            Assert.Contains("reject", t.Exception.InnerException.InnerException.Message);
        }

        public class TestFixture : EndToEndTestFixture
        {
            public TestFixture() : base(@"TestScripts\Node", "node")
            {
            }
        }
    }
}
