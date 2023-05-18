// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Encapsulates all HTTP-specific information about an individual HTTP request.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
[DebuggerTypeProxy(typeof(HttpContextDebugView))]
public abstract class HttpContext
{
    /// <summary>
    /// Gets the collection of HTTP features provided by the server and middleware available on this request.
    /// </summary>
    public abstract IFeatureCollection Features { get; }

    /// <summary>
    /// Gets the <see cref="HttpRequest"/> object for this request.
    /// </summary>
    public abstract HttpRequest Request { get; }

    /// <summary>
    /// Gets the <see cref="HttpResponse"/> object for this request.
    /// </summary>
    public abstract HttpResponse Response { get; }

    /// <summary>
    /// Gets information about the underlying connection for this request.
    /// </summary>
    public abstract ConnectionInfo Connection { get; }

    /// <summary>
    /// Gets an object that manages the establishment of WebSocket connections for this request.
    /// </summary>
    public abstract WebSocketManager WebSockets { get; }

    /// <summary>
    /// Gets or sets the user for this request.
    /// </summary>
    public abstract ClaimsPrincipal User { get; set; }

    /// <summary>
    /// Gets or sets a key/value collection that can be used to share data within the scope of this request.
    /// </summary>
    public abstract IDictionary<object, object?> Items { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/> that provides access to the request's service container.
    /// </summary>
    public abstract IServiceProvider RequestServices { get; set; }

    /// <summary>
    /// Notifies when the connection underlying this request is aborted and thus request operations should be
    /// cancelled.
    /// </summary>
    public abstract CancellationToken RequestAborted { get; set; }

    /// <summary>
    /// Gets or sets a unique identifier to represent this request in trace logs.
    /// </summary>
    public abstract string TraceIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the object used to manage user session data for this request.
    /// </summary>
    public abstract ISession Session { get; set; }

    /// <summary>
    /// Aborts the connection underlying this request.
    /// </summary>
    public abstract void Abort();

    private string DebuggerToString()
    {
        return $"{Request.Method} {Request.Path.Value} {Request.ContentType}"
            + $" Status: {Response.StatusCode} {Response.ContentType}";
    }

    private sealed class HttpContextDebugView(HttpContext context)
    {
        private readonly HttpContext _context = context;

        public HttpRequest Request => _context.Request;
        public HttpResponse Response => _context.Response;
        public ConnectionInfo Connection => _context.Connection;
        public WebSocketManager WebSockets => _context.WebSockets;
        public ClaimsPrincipal User => _context.User;
        public IDictionary<object, object?> Items => _context.Items;
        public CancellationToken RequestAborted => _context.RequestAborted;
        public string TraceIdentifier => _context.TraceIdentifier;
        // The normal session property throws if accessed before/without the session middleware.
        public ISession? Session => _context.Features.Get<ISessionFeature>()?.Session;
    }
}
