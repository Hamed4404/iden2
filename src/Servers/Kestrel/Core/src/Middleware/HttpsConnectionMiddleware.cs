// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Https.Internal
{
    internal delegate ValueTask<SslServerAuthenticationOptions> HttpsOptionsCallback(ConnectionContext connection, SslStream stream, SslClientHelloInfo clientHelloInfo, object state, CancellationToken cancellationToken);

    internal class HttpsConnectionMiddleware
    {
        private const string EnableWindows81Http2 = "Microsoft.AspNetCore.Server.Kestrel.EnableWindows81Http2";

        private static readonly bool _isWindowsVersionIncompatibleWithHttp2 = IsWindowsVersionIncompatibleWithHttp2();

        private readonly ConnectionDelegate _next;
        private readonly TimeSpan _handshakeTimeout;
        private readonly ILogger<HttpsConnectionMiddleware> _logger;
        private readonly Func<Stream, SslStream> _sslStreamFactory;

        // The following fields are only set by HttpsConnectionAdapterOptions ctor.
        private readonly HttpsConnectionAdapterOptions _options;
        private readonly SslStreamCertificateContext _serverCertificateContext;
        private readonly X509Certificate2 _serverCertificate;
        private readonly Func<ConnectionContext, string, X509Certificate2> _serverCertificateSelector;

        // The following fields are only set by ServerOptionsSelectionCallback ctor.
        private readonly HttpsOptionsCallback _httpsOptionsCallback;
        private readonly object _httpsOptionsCallbackState;

        public HttpsConnectionMiddleware(ConnectionDelegate next, HttpsConnectionAdapterOptions options)
          : this(next, options, loggerFactory: NullLoggerFactory.Instance)
        {
        }

        public HttpsConnectionMiddleware(ConnectionDelegate next, HttpsConnectionAdapterOptions options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.ServerCertificate == null && options.ServerCertificateSelector == null)
            {
                throw new ArgumentException(CoreStrings.ServerCertificateRequired, nameof(options));
            }

            _next = next;
            _handshakeTimeout = options.HandshakeTimeout;
            _logger = loggerFactory.CreateLogger<HttpsConnectionMiddleware>();

            // Something similar to the following could allow us to remove more duplicate logic, but we need https://github.com/dotnet/runtime/issues/40402 to be fixed first.
            //var sniOptionsSelector = new SniOptionsSelector("", new Dictionary<string, SniConfig> { { "*", new SniConfig() } }, new NoopCertificateConfigLoader(), options, options.HttpProtocols, _logger);
            //_httpsOptionsCallback = SniOptionsSelector.OptionsCallback;
            //_httpsOptionsCallbackState = sniOptionsSelector;
            //_sslStreamFactory = s => new SslStream(s);

            _options = options;
            _options.HttpProtocols = ValidateAndNormalizeHttpProtocols(_options.HttpProtocols, _logger);

            // capture the certificate now so it can't be switched after validation
            _serverCertificate = options.ServerCertificate;
            _serverCertificateSelector = options.ServerCertificateSelector;

            // If a selector is provided then ignore the cert, it may be a default cert.
            if (_serverCertificateSelector != null)
            {
                // SslStream doesn't allow both.
                _serverCertificate = null;
            }
            else
            {
                EnsureCertificateIsAllowedForServerAuth(_serverCertificate);

                // This might be do blocking IO but it'll resolve the certificate chain up front before any connections are
                // made to the server
                _serverCertificateContext = SslStreamCertificateContext.Create(_serverCertificate, additionalCertificates: null);
            }

            var remoteCertificateValidationCallback = _options.ClientCertificateMode == ClientCertificateMode.NoCertificate ?
                (RemoteCertificateValidationCallback)null : RemoteCertificateValidationCallback;

            _sslStreamFactory = s => new SslStream(s, leaveInnerStreamOpen: false, userCertificateValidationCallback: remoteCertificateValidationCallback);
        }

        internal HttpsConnectionMiddleware(
            ConnectionDelegate next,
            HttpsOptionsCallback httpsOptionsCallback,
            object httpsOptionsCallbackState,
            TimeSpan handshakeTimeout,
            ILoggerFactory loggerFactory)
        {
            _next = next;
            _handshakeTimeout = handshakeTimeout;
            _logger = loggerFactory.CreateLogger<HttpsConnectionMiddleware>();

            _httpsOptionsCallback = httpsOptionsCallback;
            _httpsOptionsCallbackState = httpsOptionsCallbackState;
            _sslStreamFactory = s => new SslStream(s);
        }

        public async Task OnConnectionAsync(ConnectionContext context)
        {
            await Task.Yield();

            if (context.Features.Get<ITlsConnectionFeature>() != null)
            {
                await _next(context);
                return;
            }

            var feature = new Core.Internal.TlsConnectionFeature();
            context.Features.Set<ITlsConnectionFeature>(feature);
            context.Features.Set<ITlsHandshakeFeature>(feature);

            var sslDuplexPipe = CreateSslDuplexPipe(context.Transport, context.Features.Get<IMemoryPoolFeature>()?.MemoryPool);
            var sslStream = sslDuplexPipe.Stream;

            try
            {
                using var cancellationTokenSource = new CancellationTokenSource(_handshakeTimeout);
                if (_httpsOptionsCallback is null)
                {
                    await DoOptionsBasedHandshakeAsync(context, sslStream, feature, cancellationTokenSource.Token);
                }
                else
                {
                    var state = (this, context, feature);
                    await sslStream.AuthenticateAsServerAsync(ServerOptionsCallback, state, cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                KestrelEventSource.Log.TlsHandshakeFailed(context.ConnectionId);
                KestrelEventSource.Log.TlsHandshakeStop(context, null);

                _logger.AuthenticationTimedOut();
                await sslStream.DisposeAsync();
                return;
            }
            catch (IOException ex)
            {
                KestrelEventSource.Log.TlsHandshakeFailed(context.ConnectionId);
                KestrelEventSource.Log.TlsHandshakeStop(context, null);

                _logger.AuthenticationFailed(ex);
                await sslStream.DisposeAsync();
                return;
            }
            catch (AuthenticationException ex)
            {
                KestrelEventSource.Log.TlsHandshakeFailed(context.ConnectionId);
                KestrelEventSource.Log.TlsHandshakeStop(context, null);

                _logger.AuthenticationFailed(ex);

                await sslStream.DisposeAsync();
                return;
            }

            feature.ApplicationProtocol = sslStream.NegotiatedApplicationProtocol.Protocol;
            context.Features.Set<ITlsApplicationProtocolFeature>(feature);

            feature.ClientCertificate = ConvertToX509Certificate2(sslStream.RemoteCertificate);
            feature.CipherAlgorithm = sslStream.CipherAlgorithm;
            feature.CipherStrength = sslStream.CipherStrength;
            feature.HashAlgorithm = sslStream.HashAlgorithm;
            feature.HashStrength = sslStream.HashStrength;
            feature.KeyExchangeAlgorithm = sslStream.KeyExchangeAlgorithm;
            feature.KeyExchangeStrength = sslStream.KeyExchangeStrength;
            feature.Protocol = sslStream.SslProtocol;

            KestrelEventSource.Log.TlsHandshakeStop(context, feature);

            _logger.HttpsConnectionEstablished(context.ConnectionId, sslStream.SslProtocol);

            var originalTransport = context.Transport;

            try
            {
                context.Transport = sslDuplexPipe;

                // Disposing the stream will dispose the sslDuplexPipe
                await using (sslStream)
                await using (sslDuplexPipe)
                {
                    await _next(context);
                    // Dispose the inner stream (SslDuplexPipe) before disposing the SslStream
                    // as the duplex pipe can hit an ODE as it still may be writing.
                }
            }
            finally
            {
                // Restore the original so that it gets closed appropriately
                context.Transport = originalTransport;
            }
        }

        private Task DoOptionsBasedHandshakeAsync(ConnectionContext context, SslStream sslStream, Core.Internal.TlsConnectionFeature feature, CancellationToken cancellationToken)
        {
            // Adapt to the SslStream signature
            ServerCertificateSelectionCallback selector = null;
            if (_serverCertificateSelector != null)
            {
                selector = (sender, name) =>
                {
                    feature.HostName = name;
                    context.Features.Set(sslStream);
                    var cert = _serverCertificateSelector(context, name);
                    if (cert != null)
                    {
                        EnsureCertificateIsAllowedForServerAuth(cert);
                    }
                    return cert;
                };
            }

            var sslOptions = new SslServerAuthenticationOptions
            {
                ServerCertificate = _serverCertificate,
                ServerCertificateContext = _serverCertificateContext,
                ServerCertificateSelectionCallback = selector,
                ClientCertificateRequired = _options.ClientCertificateMode != ClientCertificateMode.NoCertificate,
                EnabledSslProtocols = _options.SslProtocols,
                CertificateRevocationCheckMode = _options.CheckCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
            };

            ConfigureAlpn(sslOptions, _options.HttpProtocols);

            _options.OnAuthenticate?.Invoke(context, sslOptions);

            KestrelEventSource.Log.TlsHandshakeStart(context, sslOptions);

            return sslStream.AuthenticateAsServerAsync(sslOptions, cancellationToken);
        }

        internal static void ConfigureAlpn(SslServerAuthenticationOptions serverOptions, HttpProtocols httpProtocols)
        {
            serverOptions.ApplicationProtocols = new List<SslApplicationProtocol>();

            // This is order sensitive
            if ((httpProtocols & HttpProtocols.Http2) != 0)
            {
                serverOptions.ApplicationProtocols.Add(SslApplicationProtocol.Http2);
                // https://tools.ietf.org/html/rfc7540#section-9.2.1
                serverOptions.AllowRenegotiation = false;
            }

            if ((httpProtocols & HttpProtocols.Http1) != 0)
            {
                serverOptions.ApplicationProtocols.Add(SslApplicationProtocol.Http11);
            }
        }

        internal static bool RemoteCertificateValidationCallback(
            ClientCertificateMode clientCertificateMode,
            Func<X509Certificate2, X509Chain, SslPolicyErrors, bool> clientCertificateValidation,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (certificate == null)
            {
                return clientCertificateMode != ClientCertificateMode.RequireCertificate;
            }

            if (clientCertificateValidation == null)
            {
                if (sslPolicyErrors != SslPolicyErrors.None)
                {
                    return false;
                }
            }

            var certificate2 = ConvertToX509Certificate2(certificate);
            if (certificate2 == null)
            {
                return false;
            }

            if (clientCertificateValidation != null)
            {
                if (!clientCertificateValidation(certificate2, chain, sslPolicyErrors))
                {
                    return false;
                }
            }

            return true;
        }

        private bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            RemoteCertificateValidationCallback(_options.ClientCertificateMode, _options.ClientCertificateValidation, certificate, chain, sslPolicyErrors);

        private SslDuplexPipe CreateSslDuplexPipe(IDuplexPipe transport, MemoryPool<byte> memoryPool)
        {
            var inputPipeOptions = new StreamPipeReaderOptions
            (
                pool: memoryPool,
                bufferSize: memoryPool.GetMinimumSegmentSize(),
                minimumReadSize: memoryPool.GetMinimumAllocSize(),
                leaveOpen: true
            );

            var outputPipeOptions = new StreamPipeWriterOptions
            (
                pool: memoryPool,
                leaveOpen: true
            );

            return new SslDuplexPipe(transport, inputPipeOptions, outputPipeOptions, _sslStreamFactory);
        }

        private static async ValueTask<SslServerAuthenticationOptions> ServerOptionsCallback(SslStream sslStream, SslClientHelloInfo clientHelloInfo, object state, CancellationToken cancellationToken)
        {
            var (middleware, context, feature) = (ValueTuple<HttpsConnectionMiddleware, ConnectionContext, Core.Internal.TlsConnectionFeature>)state;

            feature.HostName = clientHelloInfo.ServerName;
            context.Features.Set(sslStream);

            var sslOptions = await middleware._httpsOptionsCallback(context, sslStream, clientHelloInfo, middleware._httpsOptionsCallbackState, cancellationToken);

            KestrelEventSource.Log.TlsHandshakeStart(context, sslOptions);

            return sslOptions;
        }

        internal static void EnsureCertificateIsAllowedForServerAuth(X509Certificate2 certificate)
        {
            if (!CertificateLoader.IsCertificateAllowedForServerAuth(certificate))
            {
                throw new InvalidOperationException(CoreStrings.FormatInvalidServerCertificateEku(certificate.Thumbprint));
            }
        }

        private static X509Certificate2 ConvertToX509Certificate2(X509Certificate certificate)
        {
            if (certificate == null)
            {
                return null;
            }

            if (certificate is X509Certificate2 cert2)
            {
                return cert2;
            }

            return new X509Certificate2(certificate);
        }

        internal static HttpProtocols ValidateAndNormalizeHttpProtocols(HttpProtocols httpProtocols, ILogger<HttpsConnectionMiddleware> logger)
        {
            // This configuration will always fail per-request, preemptively fail it here. See HttpConnection.SelectProtocol().
            if (httpProtocols == HttpProtocols.Http2)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    throw new NotSupportedException(CoreStrings.Http2NoTlsOsx);
                }
                else if (_isWindowsVersionIncompatibleWithHttp2)
                {
                    throw new NotSupportedException(CoreStrings.Http2NoTlsWin81);
                }
            }
            else if (httpProtocols == HttpProtocols.Http1AndHttp2 && _isWindowsVersionIncompatibleWithHttp2)
            {
                logger.Http2DefaultCiphersInsufficient();
                return HttpProtocols.Http1;
            }

            return httpProtocols;
        }

        private static bool IsWindowsVersionIncompatibleWithHttp2()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var enableHttp2OnWindows81 = AppContext.TryGetSwitch(EnableWindows81Http2, out var enabled) && enabled;
                if (Environment.OSVersion.Version < new Version(6, 3) // Missing ALPN support
                    // Win8.1 and 2012 R2 don't support the right cipher configuration by default.
                    || (Environment.OSVersion.Version < new Version(10, 0) && !enableHttp2OnWindows81))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal static class HttpsConnectionMiddlewareLoggerExtensions
    {

        private static readonly Action<ILogger, Exception> _authenticationFailed =
            LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(1, "AuthenticationFailed"),
                formatString: CoreStrings.AuthenticationFailed);

        private static readonly Action<ILogger, Exception> _authenticationTimedOut =
            LoggerMessage.Define(
                logLevel: LogLevel.Debug,
                eventId: new EventId(2, "AuthenticationTimedOut"),
                formatString: CoreStrings.AuthenticationTimedOut);

        private static readonly Action<ILogger, string, SslProtocols, Exception> _httpsConnectionEstablished =
            LoggerMessage.Define<string, SslProtocols>(
                logLevel: LogLevel.Debug,
                eventId: new EventId(3, "HttpsConnectionEstablished"),
                formatString: CoreStrings.HttpsConnectionEstablished);

        private static readonly Action<ILogger, Exception> _http2DefaultCiphersInsufficient =
            LoggerMessage.Define(
                logLevel: LogLevel.Information,
                eventId: new EventId(4, "Http2DefaultCiphersInsufficient"),
                formatString: CoreStrings.Http2DefaultCiphersInsufficient);

        public static void AuthenticationFailed(this ILogger<HttpsConnectionMiddleware> logger, Exception exception) => _authenticationFailed(logger, exception);

        public static void AuthenticationTimedOut(this ILogger<HttpsConnectionMiddleware> logger) => _authenticationTimedOut(logger, null);

        public static void HttpsConnectionEstablished(this ILogger<HttpsConnectionMiddleware> logger, string connectionId, SslProtocols sslProtocol) => _httpsConnectionEstablished(logger, connectionId, sslProtocol, null);

        public static void Http2DefaultCiphersInsufficient(this ILogger<HttpsConnectionMiddleware> logger) => _http2DefaultCiphersInsufficient(logger, null);
    }
}
