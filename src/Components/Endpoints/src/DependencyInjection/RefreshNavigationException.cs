// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class RefreshNavigationException : NavigationException
{
    public RefreshNavigationException(string uri) : base(uri)
    {
    }
}
