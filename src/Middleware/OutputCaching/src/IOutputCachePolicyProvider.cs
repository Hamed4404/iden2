// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A type which can provide an <see cref="IOutputCachePolicy"/> for a particular name.
/// </summary>
public interface IOutputCachePolicyProvider
{
    /// <summary>
    /// Gets a <see cref="IOutputCachePolicy"/> from the given <paramref name="policyName"/>.
    /// </summary>
    /// <param name="policyName">The policy name to retrieve.</param>
    /// <returns>The named <see cref="IOutputCachePolicy"/>.</returns>
    ValueTask<IOutputCachePolicy?> GetPolicyAsync(string policyName);
}