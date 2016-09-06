// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Azure.WebJobs.Host;

namespace Microsoft.Azure.WebJobs.Script.WebHost.Diagnostics
{
    public class SystemTraceWriter : TraceWriter
    {
        private ISystemEventGenerator _eventGenerator;
        private string _appName;
        private string _subscriptionId;

        public SystemTraceWriter(ISystemEventGenerator eventGenerator, TraceLevel level) : base(level)
        {
            // we read this in ctor (not static ctor) since it can change on the fly
            _appName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");

            string ownerName = Environment.GetEnvironmentVariable("WEBSITE_OWNER_NAME") ?? string.Empty;
            if (!string.IsNullOrEmpty(ownerName))
            {
                int idx = ownerName.IndexOf('+');
                if (idx > 0)
                {
                    _subscriptionId = ownerName.Substring(0, idx);
                }
            }

            _eventGenerator = eventGenerator;
        }

        public SystemTraceWriter(TraceLevel level) : base(level)
        {
        }

        public override void Trace(TraceEvent traceEvent)
        {
            // Apply standard event properties
            string subscriptionId = _subscriptionId ?? string.Empty;
            string appName = _appName ?? string.Empty;
            string source = traceEvent.Source;
            string summary = traceEvent.Message;

            // Apply any additional extended event info from the Properties bag
            string functionName = string.Empty;
            string eventName = string.Empty;
            string details = string.Empty;
            if (traceEvent.Properties != null)
            {
                object value;
                if (traceEvent.Properties.TryGetValue("FunctionName", out value))
                {
                    functionName = value.ToString();
                }

                if (traceEvent.Properties.TryGetValue("EventName", out value))
                {
                    eventName = value.ToString();
                }

                if (traceEvent.Properties.TryGetValue("Details", out value))
                {
                    details = value.ToString();
                }
            }

            switch (traceEvent.Level)
            {
                case TraceLevel.Verbose:
                    _eventGenerator.LogFunctionsEventVerbose(subscriptionId, appName, functionName, eventName, source, details, summary);
                    break;
                case TraceLevel.Info:
                    _eventGenerator.LogFunctionsEventInfo(subscriptionId, appName, functionName, eventName, source, details, summary);
                    break;
                case TraceLevel.Warning:
                    _eventGenerator.LogFunctionsEventWarning(subscriptionId, appName, functionName, eventName, source, details, summary);
                    break;
                case TraceLevel.Error:
                    _eventGenerator.LogFunctionsEventError(subscriptionId, appName, functionName, eventName, source, details, summary);
                    break;
            }
        }
    }
}