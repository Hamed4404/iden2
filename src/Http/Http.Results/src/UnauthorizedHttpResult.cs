// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// produce an HTTP response with the No Unauthorized (401) status code.
/// </summary>
public sealed class UnauthorizedHttpResult : IResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedHttpResult"/> class.
    /// </summary>
    private UnauthorizedHttpResult()
    {
    }

    /// <summary>
    /// Gets an instance of <see cref="UnauthorizedHttpResult"/>.
    /// </summary>
    public static UnauthorizedHttpResult Instance { get; } = new();

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode => StatusCodes.Status401Unauthorized;

    /// <inheritdoc />
    public Task ExecuteAsync(HttpContext httpContext)
    {
        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.UnauthorizedResult");
        HttpResultsHelper.Log.WritingResultAsStatusCode(logger, StatusCode);

        httpContext.Response.StatusCode = StatusCode;

        return Task.CompletedTask;
    }

}
