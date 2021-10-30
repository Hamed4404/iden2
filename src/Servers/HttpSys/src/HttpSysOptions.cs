// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    /// <summary>
    /// Contains the options used by HttpSys.
    /// </summary>
    public class HttpSysOptions
    {
        private const uint MaximumRequestQueueNameLength = 260;
        private const Http503VerbosityLevel DefaultRejectionVerbosityLevel = Http503VerbosityLevel.Basic; // Http.sys default.
        private const long DefaultRequestQueueLength = 1000; // Http.sys default.
        internal static readonly int DefaultMaxAccepts = 5 * Environment.ProcessorCount;
        // Matches the default maxAllowedContentLength in IIS (~28.6 MB)
        // https://www.iis.net/configreference/system.webserver/security/requestfiltering/requestlimits#005
        private const long DefaultMaxRequestBodySize = 30000000;

        private Http503VerbosityLevel _rejectionVebosityLevel = DefaultRejectionVerbosityLevel;
        // The native request queue
        private long _requestQueueLength = DefaultRequestQueueLength;
        private long? _maxConnections;
        private RequestQueue? _requestQueue;
        private UrlGroup? _urlGroup;
        private long? _maxRequestBodySize = DefaultMaxRequestBodySize;
        private string? _requestQueueName;

        /// <summary>
        /// Initializes a new <see cref="HttpSysOptions"/>.
        /// </summary>
        public HttpSysOptions()
        {
        }

        /// <summary>
        /// The name of the Http.Sys request queue
        /// The default is `null (Anonymous queue)`.
        /// </summary>
        public string? RequestQueueName
        {
            get => _requestQueueName;
            set
            {
                if (value?.Length > MaximumRequestQueueNameLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                                                          value,
                                                          $"The request queue name should be fewer than {MaximumRequestQueueNameLength} characters in length");
                }
                _requestQueueName = value;
            }
        }

        /// <summary>
        /// This indicates whether the server is responsible for creating and configuring the request queue, or if it should attach to an existing queue.
        /// Most existing configuration options do not apply when attaching to an existing queue.
        /// The default is `RequestQueueMode.Create`.
        /// </summary>
        public RequestQueueMode RequestQueueMode { get; set; }

        /// <summary>
        /// Indicates how client certificates should be populated. The default is to allow a certificate without renegotiation.
        /// This does not change the netsh 'clientcertnegotiation' binding option which will need to be enabled for
        /// ClientCertificateMethod.AllowCertificate to resolve a certificate.
        /// </summary>
        public ClientCertificateMethod ClientCertificateMethod { get; set; } = ClientCertificateMethod.AllowCertificate;

        /// <summary>
        /// The maximum number of concurrent accepts.
        /// The default is 5 times the number of processors as returned by <see cref="Environment.ProcessorCount" />.
        /// </summary>
        /// <remarks>
        /// Defaults to 5 times the number of processors as returned by <see cref="Environment.ProcessorCount" />.
        /// </remarks>
        public int MaxAccepts { get; set; } = DefaultMaxAccepts;

        /// <summary>
        /// Attempt kernel-mode caching for responses with eligible headers.
        /// The response may not include Set-Cookie, Vary, or Pragma headers.
        /// It must include a Cache-Control header that's public and either a shared-max-age or max-age value, or an Expires header.
        /// The default is `true`.
        /// </summary>
        public bool EnableResponseCaching { get; set; } = true;

        /// <summary>
        /// Specify the UrlPrefixCollection to register with HTTP.sys.
        /// The most useful is UrlPrefixCollection.Add, which is used to add a prefix to the collection.
        /// These may be modified at any time prior to disposing the listener.
        /// </summary>
        public UrlPrefixCollection UrlPrefixes { get; } = new UrlPrefixCollection();

        /// <summary>
        /// Http.Sys authentication settings. These may be modified at any time prior to disposing
        /// the listener.
        /// </summary>
        public AuthenticationManager Authentication { get; } = new AuthenticationManager();

        /// <summary>
        /// Exposes the Http.Sys timeout configurations.  These may also be configured in the registry.
        /// These may be modified at any time prior to disposing the listener.
        /// These settings do not apply when attaching to an existing queue.
        /// </summary>
        public TimeoutManager Timeouts { get; } = new TimeoutManager();

        /// <summary>
        /// Indicate if response body writes that fail due to client disconnects should throw exceptions or complete normally.
        /// The default is `false (complete normally)`.
        /// </summary>
        public bool ThrowWriteExceptions { get; set; }

        /// <summary>
        /// The maximum number of concurrent connections to accept. Use -1 for infinite.
        /// Use null to use the registry's machine-wide setting.
        /// The default is `null` (machine-wide setting)
        /// </summary>
        public long? MaxConnections
        {
            get => _maxConnections;
            set
            {
                if (value.HasValue && value < -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be positive, or -1 for infinite.");
                }

                if (value.HasValue && _urlGroup != null)
                {
                    _urlGroup.SetMaxConnections(value.Value);
                }

                _maxConnections = value;
            }
        }

        /// <summary>
        /// The maximum number of requests that can be queued.
        /// The default is 1000.
        /// </summary>
        public long RequestQueueLimit
        {
            get
            {
                return _requestQueueLength;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be greater than zero.");
                }

                if (_requestQueue != null)
                {
                    _requestQueue.SetLengthLimit(_requestQueueLength);
                }
                // Only store it if it succeeds or hasn't started yet
                _requestQueueLength = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum allowed size of any request body in bytes.
        /// When set to null, the maximum request body size is unlimited.
        /// This limit has no effect on upgraded connections which are always unlimited.
        /// This can be overridden per-request via <see cref="IHttpMaxRequestBodySizeFeature"/>.
        /// Defaults to 30,000,000 bytes, which is approximately 28.6MB.
        /// </summary>
        /// <remarks>
        /// Defaults to 30,000,000 bytes, which is approximately 28.6MB.
        /// </remarks>
        public long? MaxRequestBodySize
        {
            get => _maxRequestBodySize;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be greater or equal to zero.");
                }
                _maxRequestBodySize = value;
            }
        }

        /// <summary>
        /// Control whether synchronous input/output is allowed for the HttpContext.Request.Body and HttpContext.Response.Body.
        /// The default is `false`.
        /// </summary>
        public bool AllowSynchronousIO { get; set; }

        /// <summary>
        /// The HTTP.sys behavior when rejecting requests due to throttling conditions.
        /// The devault is <see cref="Http503VerbosityLevel"/>.Basic .
        /// </summary>
        public Http503VerbosityLevel Http503Verbosity
        {
            get
            {
                return _rejectionVebosityLevel;
            }
            set
            {
                if (value < Http503VerbosityLevel.Basic || value > Http503VerbosityLevel.Full)
                {
                    string message = String.Format(
                        CultureInfo.InvariantCulture,
                        "The value must be one of the values defined in the '{0}' enum.",
                        typeof(Http503VerbosityLevel).Name);

                    throw new ArgumentOutOfRangeException(nameof(value), value, message);
                }

                if (_requestQueue != null)
                {
                    _requestQueue.SetRejectionVerbosity(value);
                }
                // Only store it if it succeeds or hasn't started yet
                _rejectionVebosityLevel = value;
            }
        }

        /// <summary>
        /// Inline request processing instead of dispatching to the threadpool.
        /// </summary>
        /// <remarks>
        /// Enabling this setting will run application code on the IO thread to reduce request processing latency.
        /// However, this will limit parallel request processing to <see cref="MaxAccepts"/>. This setting can make
        /// overall throughput worse if requests take long to process.
        /// </remarks>
        public bool UnsafePreferInlineScheduling { get; set; }

        /// <summary>
        /// Configures request headers to use <see cref="Encoding.Latin1"/> encoding.
        /// </summary>
        /// <remarks>
        /// Defaults to `false`, in which case <see cref="Encoding.UTF8"/> will be used. />.
        /// </remarks>
        public bool UseLatin1RequestHeaders { get; set; }

        // Not called when attaching to an existing queue.
        internal void Apply(UrlGroup urlGroup, RequestQueue requestQueue)
        {
            _urlGroup = urlGroup;
            _requestQueue = requestQueue;

            if (_maxConnections.HasValue)
            {
                _urlGroup.SetMaxConnections(_maxConnections.Value);
            }

            if (_requestQueueLength != DefaultRequestQueueLength)
            {
                _requestQueue.SetLengthLimit(_requestQueueLength);
            }

            if (_rejectionVebosityLevel != DefaultRejectionVerbosityLevel)
            {
                _requestQueue.SetRejectionVerbosity(_rejectionVebosityLevel);
            }

            Authentication.SetUrlGroupSecurity(urlGroup);
            Timeouts.SetUrlGroupTimeouts(urlGroup);
        }
    }
}
