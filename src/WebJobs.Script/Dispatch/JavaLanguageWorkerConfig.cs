// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Azure.WebJobs.Script.Abstractions.Rpc;
using Microsoft.Azure.WebJobs.Script.Config;

namespace Microsoft.Azure.WebJobs.Script.Dispatch
{
    public class JavaLanguageWorkerConfig : WorkerConfig
    {
        public JavaLanguageWorkerConfig()
        {
            Extension = ".jar";
            Language = "Java";
            var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME") ?? string.Empty;
            if (ScriptSettingsManager.Instance.IsAzureEnvironment)
            {
                // on azure, force latest jdk
                javaHome = Path.Combine(javaHome, "..", "jdk1.8.0_111");
            }
            var javaPath = Path.Combine(javaHome, "bin", "java");
            ExecutablePath = Path.GetFullPath(javaPath);

            var workerJar = Environment.GetEnvironmentVariable("AzureWebJobsJavaWorkerPath");
            if (string.IsNullOrEmpty(workerJar))
            {
                workerJar = Path.Combine(Location, "workers", "java", "azure-functions-java-worker.jar");
            }

            var settingsManager = ScriptSettingsManager.Instance;
            var javaSection = settingsManager.Configuration
                .GetSection("workers")
                .GetSection(Language);

            var javaOpts = settingsManager.GetSetting("JAVA_OPTS") ?? string.Empty;

            var debugSection = javaSection.GetSection("debug");
            if (debugSection.Value != null)
            {
                int port = 5005;
                try
                {
                    port = Convert.ToInt32(debugSection.Value);
                }
                catch
                {
                }
                javaOpts = $"-agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address={port}";
            }

            // Load the JVM starting parameters to support attach to debugging.
            WorkerPath = $"-jar {javaOpts} \"{workerJar}\"";
        }
    }
}
