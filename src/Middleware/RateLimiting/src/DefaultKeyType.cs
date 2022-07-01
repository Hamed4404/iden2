// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.RateLimiting;

internal abstract class DefaultKeyType
{
    public abstract object? GetKey();

    public abstract string PolicyName { get; }
}
