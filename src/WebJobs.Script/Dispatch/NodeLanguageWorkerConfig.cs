// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.WebJobs.Script.Abstractions.Rpc;
using Microsoft.Azure.WebJobs.Script.Config;

namespace Microsoft.Azure.WebJobs.Script.Dispatch
{
    internal class NodeLanguageWorkerConfig : WorkerConfig
    {
        public NodeLanguageWorkerConfig()
        {
            ExecutablePath = "node";
            Language = "Node";
            ExecutableArguments = new Dictionary<string, string>();

            var nodeSection = ScriptSettingsManager.Instance.Configuration
                .GetSection("workers")
                .GetSection(Language);

            var inspectSection = nodeSection.GetSection("inspect");

            if (inspectSection.Value != null)
            {
                int port = 5858;
                try
                {
                    port = Convert.ToInt32(inspectSection.Value);
                }
                catch
                {
                }
                ExecutableArguments.Add($"--inspect={port}", string.Empty);
            }

            WorkerPath = Environment.GetEnvironmentVariable("NodeJSWorkerPath");
            if (string.IsNullOrEmpty(WorkerPath))
            {
                WorkerPath = Path.Combine(Location, "workers", "node", "dist", "src", "nodejsWorker.js");
            }
            WorkerPath = "\"" + WorkerPath + "\"";
            Extension = ".js";
        }
    }
}
