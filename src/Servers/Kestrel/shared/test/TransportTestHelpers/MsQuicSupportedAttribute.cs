// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Quic;

namespace Microsoft.AspNetCore.Testing;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class MsQuicSupportedAttribute : Attribute, ITestCondition
{
    public bool IsMet => QuicConnection.IsSupported;

    public string SkipReason => "QUIC is not supported on the current test machine";
}
