// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed partial class KestrelTrace : ILogger
{
    public void ApplicationError([StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string traceIdentifier, Exception ex)
    {
        GeneralLog.ApplicationError(_generalLogger, connectionId, traceIdentifier, ex);
    }

    public void ConnectionHeadResponseBodyWrite([StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, long count)
    {
        GeneralLog.ConnectionHeadResponseBodyWrite(_generalLogger, connectionId, count);
    }

    public void HeartbeatSlow(TimeSpan heartbeatDuration, TimeSpan interval, DateTimeOffset now)
    {
        // while the heartbeat does loop over connections, this log is usually an indicator of threadpool starvation
        GeneralLog.HeartbeatSlow(_generalLogger, now, heartbeatDuration, interval);
    }

    public void ApplicationNeverCompleted([StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId)
    {
        GeneralLog.ApplicationNeverCompleted(_generalLogger, connectionId);
    }

    public void RequestBodyStart([StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string traceIdentifier)
    {
        GeneralLog.RequestBodyStart(_generalLogger, connectionId, traceIdentifier);
    }

    public void RequestBodyDone([StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string traceIdentifier)
    {
        GeneralLog.RequestBodyDone(_generalLogger, connectionId, traceIdentifier);
    }

    public void RequestBodyNotEntirelyRead([StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string traceIdentifier)
    {
        GeneralLog.RequestBodyNotEntirelyRead(_generalLogger, connectionId, traceIdentifier);
    }

    public void RequestBodyDrainTimedOut([StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string traceIdentifier)
    {
        GeneralLog.RequestBodyDrainTimedOut(_generalLogger, connectionId, traceIdentifier);
    }

    public void InvalidResponseHeaderRemoved()
    {
        GeneralLog.InvalidResponseHeaderRemoved(_generalLogger);
    }

    public void Http2DisabledWithHttp1AndNoTls(EndPoint endPoint)
    {
        GeneralLog.Http2DisabledWithHttp1AndNoTls(_generalLogger, endPoint);
    }

    public void Http3DisabledWithHttp1AndNoTls(EndPoint endPoint)
    {
        GeneralLog.Http3DisabledWithHttp1AndNoTls(_generalLogger, endPoint);
    }

    public void RequestAborted([StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string traceIdentifier)
    {
        GeneralLog.RequestAbortedException(_generalLogger, connectionId, traceIdentifier);
    }

    private static partial class GeneralLog
    {
        [LoggerMessage(13, LogLevel.Error, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": An unhandled exception was thrown by the application.", EventName = "ApplicationError")]
        public static partial void ApplicationError(ILogger logger, [StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string traceIdentifier, Exception ex);

        [LoggerMessage(18, LogLevel.Debug, @"Connection id ""{ConnectionId}"" write of ""{count}"" body bytes to non-body HEAD response.", EventName = "ConnectionHeadResponseBodyWrite")]
        public static partial void ConnectionHeadResponseBodyWrite(ILogger logger, [StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, long count);

        [LoggerMessage(22, LogLevel.Warning, @"As of ""{now}"", the heartbeat has been running for ""{heartbeatDuration}"" which is longer than ""{interval}"". This could be caused by thread pool starvation.", EventName = "HeartbeatSlow")]
        public static partial void HeartbeatSlow(ILogger logger, DateTimeOffset now, TimeSpan heartbeatDuration, TimeSpan interval);

        [LoggerMessage(23, LogLevel.Critical, @"Connection id ""{ConnectionId}"" application never completed.", EventName = "ApplicationNeverCompleted")]
        public static partial void ApplicationNeverCompleted(ILogger logger, [StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId);

        [LoggerMessage(25, LogLevel.Debug, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": started reading request body.", EventName = "RequestBodyStart", SkipEnabledCheck = true)]
        public static partial void RequestBodyStart(ILogger logger, [StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string traceIdentifier);

        [LoggerMessage(26, LogLevel.Debug, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": done reading request body.", EventName = "RequestBodyDone", SkipEnabledCheck = true)]
        public static partial void RequestBodyDone(ILogger logger, [StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string traceIdentifier);

        [LoggerMessage(32, LogLevel.Information, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": the application completed without reading the entire request body.", EventName = "RequestBodyNotEntirelyRead")]
        public static partial void RequestBodyNotEntirelyRead(ILogger logger, [StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string traceIdentifier);

        [LoggerMessage(33, LogLevel.Information, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": automatic draining of the request body timed out after taking over 5 seconds.", EventName = "RequestBodyDrainTimedOut")]
        public static partial void RequestBodyDrainTimedOut(ILogger logger, [StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string traceIdentifier);

        [LoggerMessage(41, LogLevel.Warning, "One or more of the following response headers have been removed because they are invalid for HTTP/2 and HTTP/3 responses: 'Connection', 'Transfer-Encoding', 'Keep-Alive', 'Upgrade' and 'Proxy-Connection'.", EventName = "InvalidResponseHeaderRemoved")]
        public static partial void InvalidResponseHeaderRemoved(ILogger logger);

        [LoggerMessage(64, LogLevel.Warning, "HTTP/2 is not enabled for {Endpoint}. The endpoint is configured to use HTTP/1.1 and HTTP/2, but TLS is not enabled. HTTP/2 requires TLS application protocol negotiation. Connections to this endpoint will use HTTP/1.1.", EventName = "Http2DisabledWithHttp1AndNoTls")]
        public static partial void Http2DisabledWithHttp1AndNoTls(ILogger logger, EndPoint endPoint);

        [LoggerMessage(65, LogLevel.Warning, "HTTP/3 is not enabled for {Endpoint}. HTTP/3 requires TLS. Connections to this endpoint will use HTTP/1.1.", EventName = "Http3DisabledWithHttp1AndNoTls")]
        public static partial void Http3DisabledWithHttp1AndNoTls(ILogger logger, EndPoint endPoint);

        [LoggerMessage(66, LogLevel.Debug, @"Connection id ""{ConnectionId}"", Request id ""{TraceIdentifier}"": The request was aborted by the client.", EventName = "RequestAborted")]
        public static partial void RequestAbortedException(ILogger logger, [StringSyntax(StringSyntaxAttribute.GuidFormat)] string connectionId, string traceIdentifier);

        // Highest shared ID is 66. New consecutive IDs start at 67
    }
}
