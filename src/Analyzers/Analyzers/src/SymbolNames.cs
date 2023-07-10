// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Analyzers;

internal static class SymbolNames
{
    public const string ConfigureServicesMethodPrefix = "Configure";

    public const string ConfigureServicesMethodSuffix = "Services";

    public const string ConfigureMethodPrefix = "Configure";

    public static class IApplicationBuilder
    {
        public const string MetadataName = "Microsoft.AspNetCore.Builder.IApplicationBuilder";
    }

    public static class IServiceCollection
    {
        public const string MetadataName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
    }

    public static class ComponentEndpointRouteBuilderExtensions
    {
        public const string MetadataName = "Microsoft.AspNetCore.Builder.ComponentEndpointRouteBuilderExtensions";

        public const string MapBlazorHubMethodName = "MapBlazorHub";
    }

    public static class HubEndpointRouteBuilderExtensions
    {
        public const string MetadataName = "Microsoft.AspNetCore.Builder.HubEndpointRouteBuilderExtensions";

        public const string MapHubMethodName = "MapHub";
    }

    public static class SignalRAppBuilderExtensions
    {
        public const string MetadataName = "Microsoft.AspNetCore.Builder.SignalRAppBuilderExtensions";

        public const string UseSignalRMethodName = "UseSignalR";
    }

    public static class MvcOptions
    {
        public const string MetadataName = "Microsoft.AspNetCore.Mvc.MvcOptions";

        public const string EnableEndpointRoutingPropertyName = "EnableEndpointRouting";
    }

    public static class MvcServiceCollectionExtensions
    {
        public const string AddControllersMethodName = "AddControllers";

        public const string AddControllersWithViewsMethodName = "AddControllersWithViews";

        public const string AddMvcMethodName = "AddMvc";

        public const string AddRazorPagesMethodName = "AddRazorPages";
    }

    public static class ServiceCollectionServiceExtensions
    {
        public const string AddTransientMethodName = "AddTransient";

        public const string AddScopedMethodName = "AddScoped";

        public const string AddSingletonMethodName = "AddSingleton";
    }

    public static class IProblemDetailsWriter
    {
        public const string Name = "IProblemDetailsWriter";
    }
}
