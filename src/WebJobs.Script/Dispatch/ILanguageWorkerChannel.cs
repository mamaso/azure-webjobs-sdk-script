// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Script.Dispatch
{
    internal interface ILanguageWorkerChannel : IDisposable
    {
        string Id { get; }

        void Register(FunctionRegistrationContext context);
    }
}
