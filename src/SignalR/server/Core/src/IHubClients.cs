// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SignalR;

/// <summary>
/// An abstraction that provides access to client connections.
/// </summary>
public interface IHubClients : IHubClients<IClientProxy>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="connectionId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    new ISingleClientProxy Single(string connectionId) => throw new NotImplementedException();
}
