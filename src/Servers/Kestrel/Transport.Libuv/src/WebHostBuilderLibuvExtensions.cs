// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting
{
    [Obsolete("The libuv transport is obsolete and will be removed in a future release. See https://aka.ms/libuvtransport for details.", error: false)]
    public static class WebHostBuilderLibuvExtensions
    {
        /// <summary>
        /// Specify Libuv as the transport to be used by Kestrel.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
        /// </returns>
        [Obsolete("The libuv transport is obsolete and will be removed in a future release. See https://aka.ms/libuvtransport for details.", error: false)]
        public static IWebHostBuilder UseLibuv(this IWebHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IConnectionListenerFactory, LibuvTransportFactory>();
            });
        }

        /// <summary>
        /// Specify Libuv as the transport to be used by Kestrel.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <param name="configureOptions">
        /// A callback to configure Libuv options.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
        /// </returns>
        [Obsolete("The libuv transport is obsolete and will be removed in a future release. See https://aka.ms/libuvtransport for details.", error: false)]
        public static IWebHostBuilder UseLibuv(this IWebHostBuilder hostBuilder, Action<LibuvTransportOptions> configureOptions)
        {
            return hostBuilder.UseLibuv().ConfigureServices(services =>
            {
                services.Configure(configureOptions);
            });
        }
    }
}
