// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNetCore
{
    public static class TestData
    {
        public static List<string> ListedSharedFxAssemblies;

        public static SortedDictionary<string, string> ListedTargetingPackAssemblies;

        static TestData()
        {
            ListedSharedFxAssemblies = new List<string>()
            {
                "aspnetcorev2_inprocess",
                "Microsoft.AspNetCore",
                "Microsoft.AspNetCore.Antiforgery",
                "Microsoft.AspNetCore.Authentication",
                "Microsoft.AspNetCore.Authentication.Abstractions",
                "Microsoft.AspNetCore.Authentication.Cookies",
                "Microsoft.AspNetCore.Authentication.Core",
                "Microsoft.AspNetCore.Authentication.OAuth",
                "Microsoft.AspNetCore.Authorization",
                "Microsoft.AspNetCore.Authorization.Policy",
                "Microsoft.AspNetCore.Components",
                "Microsoft.AspNetCore.Components.Authorization",
                "Microsoft.AspNetCore.Components.Forms",
                "Microsoft.AspNetCore.Components.Server",
                "Microsoft.AspNetCore.Components.Web",
                "Microsoft.AspNetCore.Connections.Abstractions",
                "Microsoft.AspNetCore.CookiePolicy",
                "Microsoft.AspNetCore.Cors",
                "Microsoft.AspNetCore.Cryptography.Internal",
                "Microsoft.AspNetCore.Cryptography.KeyDerivation",
                "Microsoft.AspNetCore.DataProtection",
                "Microsoft.AspNetCore.DataProtection.Abstractions",
                "Microsoft.AspNetCore.DataProtection.Extensions",
                "Microsoft.AspNetCore.Diagnostics",
                "Microsoft.AspNetCore.Diagnostics.Abstractions",
                "Microsoft.AspNetCore.Diagnostics.HealthChecks",
                "Microsoft.AspNetCore.HostFiltering",
                "Microsoft.AspNetCore.Hosting",
                "Microsoft.AspNetCore.Hosting.Abstractions",
                "Microsoft.AspNetCore.Hosting.Server.Abstractions",
                "Microsoft.AspNetCore.Html.Abstractions",
                "Microsoft.AspNetCore.Http",
                "Microsoft.AspNetCore.Http.Abstractions",
                "Microsoft.AspNetCore.Http.Connections",
                "Microsoft.AspNetCore.Http.Connections.Common",
                "Microsoft.AspNetCore.Http.Extensions",
                "Microsoft.AspNetCore.Http.Features",
                "Microsoft.AspNetCore.HttpOverrides",
                "Microsoft.AspNetCore.HttpsPolicy",
                "Microsoft.AspNetCore.Identity",
                "Microsoft.AspNetCore.Localization",
                "Microsoft.AspNetCore.Localization.Routing",
                "Microsoft.AspNetCore.Metadata",
                "Microsoft.AspNetCore.Mvc",
                "Microsoft.AspNetCore.Mvc.Abstractions",
                "Microsoft.AspNetCore.Mvc.ApiExplorer",
                "Microsoft.AspNetCore.Mvc.Core",
                "Microsoft.AspNetCore.Mvc.Cors",
                "Microsoft.AspNetCore.Mvc.DataAnnotations",
                "Microsoft.AspNetCore.Mvc.Formatters.Json",
                "Microsoft.AspNetCore.Mvc.Formatters.Xml",
                "Microsoft.AspNetCore.Mvc.Localization",
                "Microsoft.AspNetCore.Mvc.Razor",
                "Microsoft.AspNetCore.Mvc.RazorPages",
                "Microsoft.AspNetCore.Mvc.TagHelpers",
                "Microsoft.AspNetCore.Mvc.ViewFeatures",
                "Microsoft.AspNetCore.Razor",
                "Microsoft.AspNetCore.Razor.Runtime",
                "Microsoft.AspNetCore.ResponseCaching",
                "Microsoft.AspNetCore.ResponseCaching.Abstractions",
                "Microsoft.AspNetCore.ResponseCompression",
                "Microsoft.AspNetCore.Rewrite",
                "Microsoft.AspNetCore.Routing",
                "Microsoft.AspNetCore.Routing.Abstractions",
                "Microsoft.AspNetCore.Server.HttpSys",
                "Microsoft.AspNetCore.Server.IIS",
                "Microsoft.AspNetCore.Server.IISIntegration",
                "Microsoft.AspNetCore.Server.Kestrel",
                "Microsoft.AspNetCore.Server.Kestrel.Core",
                "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets",
                "Microsoft.AspNetCore.Session",
                "Microsoft.AspNetCore.SignalR",
                "Microsoft.AspNetCore.SignalR.Common",
                "Microsoft.AspNetCore.SignalR.Core",
                "Microsoft.AspNetCore.SignalR.Protocols.Json",
                "Microsoft.AspNetCore.StaticFiles",
                "Microsoft.AspNetCore.WebSockets",
                "Microsoft.AspNetCore.WebUtilities",
                "Microsoft.Extensions.Caching.Abstractions",
                "Microsoft.Extensions.Caching.Memory",
                "Microsoft.Extensions.Configuration",
                "Microsoft.Extensions.Configuration.Abstractions",
                "Microsoft.Extensions.Configuration.Binder",
                "Microsoft.Extensions.Configuration.CommandLine",
                "Microsoft.Extensions.Configuration.EnvironmentVariables",
                "Microsoft.Extensions.Configuration.FileExtensions",
                "Microsoft.Extensions.Configuration.Ini",
                "Microsoft.Extensions.Configuration.Json",
                "Microsoft.Extensions.Configuration.KeyPerFile",
                "Microsoft.Extensions.Configuration.UserSecrets",
                "Microsoft.Extensions.Configuration.Xml",
                "Microsoft.Extensions.DependencyInjection",
                "Microsoft.Extensions.DependencyInjection.Abstractions",
                "Microsoft.Extensions.Diagnostics.HealthChecks",
                "Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions",
                "Microsoft.Extensions.FileProviders.Abstractions",
                "Microsoft.Extensions.FileProviders.Composite",
                "Microsoft.Extensions.FileProviders.Embedded",
                "Microsoft.Extensions.FileProviders.Physical",
                "Microsoft.Extensions.FileSystemGlobbing",
                "Microsoft.Extensions.Hosting",
                "Microsoft.Extensions.Hosting.Abstractions",
                "Microsoft.Extensions.Http",
                "Microsoft.Extensions.Identity.Core",
                "Microsoft.Extensions.Identity.Stores",
                "Microsoft.Extensions.Localization",
                "Microsoft.Extensions.Localization.Abstractions",
                "Microsoft.Extensions.Logging",
                "Microsoft.Extensions.Logging.Abstractions",
                "Microsoft.Extensions.Logging.Configuration",
                "Microsoft.Extensions.Logging.Console",
                "Microsoft.Extensions.Logging.Debug",
                "Microsoft.Extensions.Logging.EventLog",
                "Microsoft.Extensions.Logging.EventSource",
                "Microsoft.Extensions.Logging.TraceSource",
                "Microsoft.Extensions.ObjectPool",
                "Microsoft.Extensions.Options",
                "Microsoft.Extensions.Options.ConfigurationExtensions",
                "Microsoft.Extensions.Options.DataAnnotations",
                "Microsoft.Extensions.Primitives",
                "Microsoft.Extensions.WebEncoders",
                "Microsoft.JSInterop",
                "Microsoft.Net.Http.Headers",
                "Microsoft.Win32.SystemEvents",
                "System.Diagnostics.EventLog",
                "System.Diagnostics.EventLog.Messages",
                "System.Drawing.Common",
                "System.IO.Pipelines",
                "System.Security.Cryptography.Pkcs",
                "System.Security.Cryptography.Xml",
                "System.Security.Permissions",
                "System.Windows.Extensions"
            };

            ListedTargetingPackAssemblies = new SortedDictionary<string, string>
            {
                { "Microsoft.AspNetCore", "6.0.0.0" },
                { "Microsoft.AspNetCore.Antiforgery", "6.0.0.0" },
                { "Microsoft.AspNetCore.Authentication", "6.0.0.0" },
                { "Microsoft.AspNetCore.Authentication.Abstractions", "6.0.0.0" },
                { "Microsoft.AspNetCore.Authentication.Cookies", "6.0.0.0" },
                { "Microsoft.AspNetCore.Authentication.Core", "6.0.0.0" },
                { "Microsoft.AspNetCore.Authentication.OAuth", "6.0.0.0" },
                { "Microsoft.AspNetCore.Authorization", "6.0.0.0" },
                { "Microsoft.AspNetCore.Authorization.Policy", "6.0.0.0" },
                { "Microsoft.AspNetCore.Components", "6.0.0.0" },
                { "Microsoft.AspNetCore.Components.Authorization", "6.0.0.0" },
                { "Microsoft.AspNetCore.Components.Forms", "6.0.0.0" },
                { "Microsoft.AspNetCore.Components.Server", "6.0.0.0" },
                { "Microsoft.AspNetCore.Components.Web", "6.0.0.0" },
                { "Microsoft.AspNetCore.Connections.Abstractions", "6.0.0.0" },
                { "Microsoft.AspNetCore.CookiePolicy", "6.0.0.0" },
                { "Microsoft.AspNetCore.Cors", "6.0.0.0" },
                { "Microsoft.AspNetCore.Cryptography.Internal", "6.0.0.0" },
                { "Microsoft.AspNetCore.Cryptography.KeyDerivation", "6.0.0.0" },
                { "Microsoft.AspNetCore.DataProtection", "6.0.0.0" },
                { "Microsoft.AspNetCore.DataProtection.Abstractions", "6.0.0.0" },
                { "Microsoft.AspNetCore.DataProtection.Extensions", "6.0.0.0" },
                { "Microsoft.AspNetCore.Diagnostics", "6.0.0.0" },
                { "Microsoft.AspNetCore.Diagnostics.Abstractions", "6.0.0.0" },
                { "Microsoft.AspNetCore.Diagnostics.HealthChecks", "6.0.0.0" },
                { "Microsoft.AspNetCore.HostFiltering", "6.0.0.0" },
                { "Microsoft.AspNetCore.Hosting", "6.0.0.0" },
                { "Microsoft.AspNetCore.Hosting.Abstractions", "6.0.0.0" },
                { "Microsoft.AspNetCore.Hosting.Server.Abstractions", "6.0.0.0" },
                { "Microsoft.AspNetCore.Html.Abstractions", "6.0.0.0" },
                { "Microsoft.AspNetCore.Http", "6.0.0.0" },
                { "Microsoft.AspNetCore.Http.Abstractions", "6.0.0.0" },
                { "Microsoft.AspNetCore.Http.Connections", "6.0.0.0" },
                { "Microsoft.AspNetCore.Http.Connections.Common", "6.0.0.0" },
                { "Microsoft.AspNetCore.Http.Extensions", "6.0.0.0" },
                { "Microsoft.AspNetCore.Http.Features", "6.0.0.0" },
                { "Microsoft.AspNetCore.HttpOverrides", "6.0.0.0" },
                { "Microsoft.AspNetCore.HttpsPolicy", "6.0.0.0" },
                { "Microsoft.AspNetCore.Identity", "6.0.0.0" },
                { "Microsoft.AspNetCore.Localization", "6.0.0.0" },
                { "Microsoft.AspNetCore.Localization.Routing", "6.0.0.0" },
                { "Microsoft.AspNetCore.Metadata", "6.0.0.0" },
                { "Microsoft.AspNetCore.Mvc", "6.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.Abstractions", "6.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.ApiExplorer", "6.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.Core", "6.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.Cors", "6.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.DataAnnotations", "6.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.Formatters.Json", "6.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.Formatters.Xml", "6.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.Localization", "6.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.Razor", "6.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.RazorPages", "6.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.TagHelpers", "6.0.0.0" },
                { "Microsoft.AspNetCore.Mvc.ViewFeatures", "6.0.0.0" },
                { "Microsoft.AspNetCore.Razor", "6.0.0.0" },
                { "Microsoft.AspNetCore.Razor.Runtime", "6.0.0.0" },
                { "Microsoft.AspNetCore.ResponseCaching", "6.0.0.0" },
                { "Microsoft.AspNetCore.ResponseCaching.Abstractions", "6.0.0.0" },
                { "Microsoft.AspNetCore.ResponseCompression", "6.0.0.0" },
                { "Microsoft.AspNetCore.Rewrite", "6.0.0.0" },
                { "Microsoft.AspNetCore.Routing", "6.0.0.0" },
                { "Microsoft.AspNetCore.Routing.Abstractions", "6.0.0.0" },
                { "Microsoft.AspNetCore.Server.HttpSys", "6.0.0.0" },
                { "Microsoft.AspNetCore.Server.IIS", "6.0.0.0" },
                { "Microsoft.AspNetCore.Server.IISIntegration", "6.0.0.0" },
                { "Microsoft.AspNetCore.Server.Kestrel", "6.0.0.0" },
                { "Microsoft.AspNetCore.Server.Kestrel.Core", "6.0.0.0" },
                { "Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets", "6.0.0.0" },
                { "Microsoft.AspNetCore.Session", "6.0.0.0" },
                { "Microsoft.AspNetCore.SignalR", "6.0.0.0" },
                { "Microsoft.AspNetCore.SignalR.Common", "6.0.0.0" },
                { "Microsoft.AspNetCore.SignalR.Core", "6.0.0.0" },
                { "Microsoft.AspNetCore.SignalR.Protocols.Json", "6.0.0.0" },
                { "Microsoft.AspNetCore.StaticFiles", "6.0.0.0" },
                { "Microsoft.AspNetCore.WebSockets", "6.0.0.0" },
                { "Microsoft.AspNetCore.WebUtilities", "6.0.0.0" },
                { "Microsoft.Extensions.Caching.Abstractions", "6.0.0.0" },
                { "Microsoft.Extensions.Caching.Memory", "6.0.0.0" },
                { "Microsoft.Extensions.Configuration", "6.0.0.0" },
                { "Microsoft.Extensions.Configuration.Abstractions", "6.0.0.0" },
                { "Microsoft.Extensions.Configuration.Binder", "6.0.0.0" },
                { "Microsoft.Extensions.Configuration.CommandLine", "6.0.0.0" },
                { "Microsoft.Extensions.Configuration.EnvironmentVariables", "6.0.0.0" },
                { "Microsoft.Extensions.Configuration.FileExtensions", "6.0.0.0" },
                { "Microsoft.Extensions.Configuration.Ini", "6.0.0.0" },
                { "Microsoft.Extensions.Configuration.Json", "6.0.0.0" },
                { "Microsoft.Extensions.Configuration.KeyPerFile", "6.0.0.0" },
                { "Microsoft.Extensions.Configuration.UserSecrets", "6.0.0.0" },
                { "Microsoft.Extensions.Configuration.Xml", "6.0.0.0" },
                { "Microsoft.Extensions.DependencyInjection", "6.0.0.0" },
                { "Microsoft.Extensions.DependencyInjection.Abstractions", "6.0.0.0" },
                { "Microsoft.Extensions.Diagnostics.HealthChecks", "6.0.0.0" },
                { "Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions", "6.0.0.0" },
                { "Microsoft.Extensions.FileProviders.Abstractions", "6.0.0.0" },
                { "Microsoft.Extensions.FileProviders.Composite", "6.0.0.0" },
                { "Microsoft.Extensions.FileProviders.Embedded", "6.0.0.0" },
                { "Microsoft.Extensions.FileProviders.Physical", "6.0.0.0" },
                { "Microsoft.Extensions.FileSystemGlobbing", "6.0.0.0" },
                { "Microsoft.Extensions.Hosting", "6.0.0.0" },
                { "Microsoft.Extensions.Hosting.Abstractions", "6.0.0.0" },
                { "Microsoft.Extensions.Http", "6.0.0.0" },
                { "Microsoft.Extensions.Identity.Core", "6.0.0.0" },
                { "Microsoft.Extensions.Identity.Stores", "6.0.0.0" },
                { "Microsoft.Extensions.Localization", "6.0.0.0" },
                { "Microsoft.Extensions.Localization.Abstractions", "6.0.0.0" },
                { "Microsoft.Extensions.Logging", "6.0.0.0" },
                { "Microsoft.Extensions.Logging.Abstractions", "6.0.0.0" },
                { "Microsoft.Extensions.Logging.Configuration", "6.0.0.0" },
                { "Microsoft.Extensions.Logging.Console", "6.0.0.0" },
                { "Microsoft.Extensions.Logging.Debug", "6.0.0.0" },
                { "Microsoft.Extensions.Logging.EventLog", "6.0.0.0" },
                { "Microsoft.Extensions.Logging.EventSource", "6.0.0.0" },
                { "Microsoft.Extensions.Logging.TraceSource", "6.0.0.0" },
                { "Microsoft.Extensions.ObjectPool", "6.0.0.0" },
                { "Microsoft.Extensions.Options", "6.0.0.0" },
                { "Microsoft.Extensions.Options.ConfigurationExtensions", "6.0.0.0" },
                { "Microsoft.Extensions.Options.DataAnnotations", "6.0.0.0" },
                { "Microsoft.Extensions.Primitives", "6.0.0.0" },
                { "Microsoft.Extensions.WebEncoders", "6.0.0.0" },
                { "Microsoft.JSInterop", "6.0.0.0" },
                { "Microsoft.Net.Http.Headers", "6.0.0.0" },
                { "Microsoft.Win32.Registry", "6.0.0.0" },
                { "System.Diagnostics.EventLog", "6.0.0.0" },
                { "System.IO.Pipelines", "6.0.0.0" },
                { "System.Security.AccessControl", "6.0.0.0" },
                { "System.Security.Cryptography.Cng", "6.0.0.0" },
                { "System.Security.Cryptography.Xml", "6.0.0.0" },
                { "System.Security.Permissions", "6.0.0.0" },
                { "System.Security.Principal.Windows", "6.0.0.0" },
                { "System.Windows.Extensions", "6.0.0.0" }
            };

            if (!VerifyAncmBinary())
            {
                ListedSharedFxAssemblies.Remove("aspnetcorev2_inprocess");
            }
        }

        public static string GetSharedFxVersion() => GetTestDataValue("SharedFxVersion");

        public static string GetDefaultNetCoreTargetFramework() => GetTestDataValue("DefaultNetCoreTargetFramework");

        public static string GetMicrosoftNETCoreAppPackageVersion() => GetTestDataValue("MicrosoftNETCoreAppRuntimeVersion");

        public static string GetReferencePackSharedFxVersion() => GetTestDataValue("ReferencePackSharedFxVersion");

        public static string GetRepositoryCommit() => GetTestDataValue("RepositoryCommit");

        public static string GetSharedFxRuntimeIdentifier() => GetTestDataValue("TargetRuntimeIdentifier");

        public static string GetSharedFrameworkBinariesFromRepo() => GetTestDataValue("SharedFrameworkBinariesFromRepo");

        public static string GetSharedFxDependencies() => GetTestDataValue("SharedFxDependencies");

        public static string GetTargetingPackDependencies() => GetTestDataValue("TargetingPackDependencies");

        public static string GetRuntimeTargetingPackDependencies() => GetTestDataValue("RuntimeTargetingPackDependencies");

        public static string GetAspNetCoreTargetingPackDependencies() => GetTestDataValue("AspNetCoreTargetingPackDependencies");

        public static bool VerifyAncmBinary() => string.Equals(GetTestDataValue("VerifyAncmBinary"), "true", StringComparison.OrdinalIgnoreCase);

        public static string GetTestDataValue(string key)
             => typeof(TestData).Assembly.GetCustomAttributes<TestDataAttribute>().Single(d => d.Key == key).Value;
    }
}
