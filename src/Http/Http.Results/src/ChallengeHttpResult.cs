// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An <see cref="IResult"/> that on execution invokes <see cref="M:HttpContext.ChallengeAsync"/>.
/// </summary>
public sealed partial class ChallengeHttpResult : IResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="ChallengeHttpResult"/> with the default sign out scheme.
    /// </summary>
    public ChallengeHttpResult()
        : this(authenticationSchemes: Array.Empty<string>(), properties: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ChallengeHttpResult"/> with the
    /// specified <paramref name="properties"/>.
    /// </summary>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
    /// challenge.</param>
    public ChallengeHttpResult(AuthenticationProperties? properties)
        : this(Array.Empty<string>(), properties)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ChallengeHttpResult"/> with the
    /// specified authentication scheme and <paramref name="properties"/>.
    /// </summary>
    /// <param name="authenticationScheme">The authentication schemes to challenge.</param>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
    /// challenge.</param>
    public ChallengeHttpResult(string authenticationScheme, AuthenticationProperties? properties)
        : this(new[] { authenticationScheme }, properties)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ChallengeHttpResult"/> with the
    /// specified authentication schemes and <paramref name="properties"/>.
    /// </summary>
    /// <param name="authenticationSchemes">The authentication scheme to challenge.</param>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
    /// challenge.</param>
    public ChallengeHttpResult(IList<string> authenticationSchemes, AuthenticationProperties? properties)
    {
        AuthenticationSchemes = authenticationSchemes.AsReadOnly();
        Properties = properties;
    }

    /// <summary>
    /// Gets the authentication schemes that are challenged.
    /// </summary>
    public IReadOnlyList<string> AuthenticationSchemes { get; init; }

    /// <summary>
    /// Gets the <see cref="AuthenticationProperties"/> used to perform the sign-out operation.
    /// </summary>
    public AuthenticationProperties? Properties { get; }

    /// <inheritdoc/>
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.ChallengeResult");

        Log.ChallengeResultExecuting(logger, AuthenticationSchemes);

        if (AuthenticationSchemes != null && AuthenticationSchemes.Count > 0)
        {
            foreach (var scheme in AuthenticationSchemes)
            {
                await httpContext.ChallengeAsync(scheme, Properties);
            }
        }
        else
        {
            await httpContext.ChallengeAsync(Properties);
        }
    }

    private static partial class Log
    {
        public static void ChallengeResultExecuting(ILogger logger, IReadOnlyList<string> authenticationSchemes)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                ChallengeResultExecuting(logger, authenticationSchemes.ToArray());
            }
        }

        [LoggerMessage(1, LogLevel.Information, "Executing ChallengeResult with authentication schemes ({Schemes}).", EventName = "ChallengeResultExecuting", SkipEnabledCheck = true)]
        private static partial void ChallengeResultExecuting(ILogger logger, string[] schemes);
    }
}
