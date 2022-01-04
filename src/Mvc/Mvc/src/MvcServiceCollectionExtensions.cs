// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.HotReload;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up MVC services in an <see cref="IServiceCollection" />.
/// </summary>
public static class MvcServiceCollectionExtensions
{
    /// <summary>
    /// Adds MVC services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    public static IMvcBuilder AddMvc(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddControllersWithViews();
        return services.AddRazorPages();
    }

    /// <summary>
    /// Adds MVC services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="setupAction">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    public static IMvcBuilder AddMvc(this IServiceCollection services, Action<MvcOptions> setupAction)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (setupAction == null)
        {
            throw new ArgumentNullException(nameof(setupAction));
        }

        var builder = services.AddMvc();
        builder.Services.Configure(setupAction);

        return builder;
    }

    /// <summary>
    /// Adds services for controllers to the specified <see cref="IServiceCollection"/>. This method will not
    /// register services used for views or pages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features with controllers for an API. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcApiExplorerMvcCoreBuilderExtensions.AddApiExplorer(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCorsMvcCoreBuilderExtensions.AddCors(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcCoreMvcCoreBuilderExtensions.AddFormatterMappings(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers with views call <see cref="AddControllersWithViews(IServiceCollection)"/>
    /// on the resulting builder.
    /// </para>
    /// <para>
    /// To add services for pages call <see cref="AddRazorPages(IServiceCollection)"/>
    /// on the resulting builder.
    /// </para>
    /// </remarks>
    public static IMvcBuilder AddControllers(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var builder = AddControllersCore(services);
        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    /// <summary>
    /// Adds services for controllers to the specified <see cref="IServiceCollection"/>. This method will not
    /// register services used for views or pages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="entryAssembly">Application entry assembly used for <see cref="ApplicationPart"/> discovering.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features with controllers for an API. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcApiExplorerMvcCoreBuilderExtensions.AddApiExplorer(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCorsMvcCoreBuilderExtensions.AddCors(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcCoreMvcCoreBuilderExtensions.AddFormatterMappings(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers with views call <see cref="AddControllersWithViews(IServiceCollection)"/>
    /// on the resulting builder.
    /// </para>
    /// <para>
    /// To add services for pages call <see cref="AddRazorPages(IServiceCollection)"/>
    /// on the resulting builder.
    /// </para>
    /// </remarks>
    public static IMvcBuilder AddControllers(this IServiceCollection services, Assembly entryAssembly)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var builder = AddControllersCore(services, entryAssembly);

        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    /// <summary>
    /// Adds services for controllers to the specified <see cref="IServiceCollection"/>. This method will not
    /// register services used for views or pages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configure">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features with controllers for an API. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcApiExplorerMvcCoreBuilderExtensions.AddApiExplorer(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCorsMvcCoreBuilderExtensions.AddCors(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcCoreMvcCoreBuilderExtensions.AddFormatterMappings(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers with views call <see cref="AddControllersWithViews(IServiceCollection)"/>
    /// on the resulting builder.
    /// </para>
    /// <para>
    /// To add services for pages call <see cref="AddRazorPages(IServiceCollection)"/>
    /// on the resulting builder.
    /// </para>
    /// </remarks>
    public static IMvcBuilder AddControllers(this IServiceCollection services, Action<MvcOptions>? configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // This method excludes all of the view-related services by default.
        var builder = AddControllersCore(services);
        if (configure != null)
        {
            builder.AddMvcOptions(configure);
        }

        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    /// <summary>
    /// Adds services for controllers to the specified <see cref="IServiceCollection"/>. This method will not
    /// register services used for views or pages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="entryAssembly">Application entry assembly used for <see cref="ApplicationPart"/> discovering.</param>
    /// <param name="configure">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features with controllers for an API. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcApiExplorerMvcCoreBuilderExtensions.AddApiExplorer(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCorsMvcCoreBuilderExtensions.AddCors(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcCoreMvcCoreBuilderExtensions.AddFormatterMappings(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers with views call <see cref="AddControllersWithViews(IServiceCollection)"/>
    /// on the resulting builder.
    /// </para>
    /// <para>
    /// To add services for pages call <see cref="AddRazorPages(IServiceCollection)"/>
    /// on the resulting builder.
    /// </para>
    /// </remarks>
    public static IMvcBuilder AddControllers(this IServiceCollection services, Assembly entryAssembly, Action<MvcOptions>? configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (entryAssembly == null)
        {
            throw new ArgumentNullException(nameof(entryAssembly));
        }

        // This method excludes all of the view-related services by default.
        var builder = AddControllersCore(services, entryAssembly);

        if (configure != null)
        {
            builder.AddMvcOptions(configure);
        }

        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    private static IMvcCoreBuilder AddControllersCore(IServiceCollection services, Assembly? entryAssembly = null)
    {
        // This method excludes all of the view-related services by default.
        var builder = AddMvcCore(services, entryAssembly)
            .AddApiExplorer()
            .AddAuthorization()
            .AddCors()
            .AddDataAnnotations()
            .AddFormatterMappings();

        if (MetadataUpdater.IsSupported)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IActionDescriptorChangeProvider, HotReloadService>());
        }

        return builder;
    }

    /// <summary>
    /// Adds services for controllers to the specified <see cref="IServiceCollection"/>. This method will not
    /// register services used for pages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features with controllers with views. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcApiExplorerMvcCoreBuilderExtensions.AddApiExplorer(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCorsMvcCoreBuilderExtensions.AddCors(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddFormatterMappings(IMvcCoreBuilder)"/>,
    /// <see cref="TagHelperServicesExtensions.AddCacheTagHelper(IMvcCoreBuilder)"/>,
    /// <see cref="MvcViewFeaturesMvcCoreBuilderExtensions.AddViews(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcRazorMvcCoreBuilderExtensions.AddRazorViewEngine(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for pages call <see cref="AddRazorPages(IServiceCollection)"/>.
    /// </para>
    /// </remarks>
    public static IMvcBuilder AddControllersWithViews(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var builder = AddControllersWithViewsCore(services);
        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    /// <summary>
    /// Adds services for controllers to the specified <see cref="IServiceCollection"/>. This method will not
    /// register services used for pages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="entryAssembly">Application entry assembly used for <see cref="ApplicationPart"/> discovering.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features with controllers with views. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcApiExplorerMvcCoreBuilderExtensions.AddApiExplorer(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCorsMvcCoreBuilderExtensions.AddCors(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddFormatterMappings(IMvcCoreBuilder)"/>,
    /// <see cref="TagHelperServicesExtensions.AddCacheTagHelper(IMvcCoreBuilder)"/>,
    /// <see cref="MvcViewFeaturesMvcCoreBuilderExtensions.AddViews(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcRazorMvcCoreBuilderExtensions.AddRazorViewEngine(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for pages call <see cref="AddRazorPages(IServiceCollection)"/>.
    /// </para>
    /// </remarks>
    public static IMvcBuilder AddControllersWithViews(this IServiceCollection services, Assembly entryAssembly)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (entryAssembly == null)
        {
            throw new ArgumentNullException(nameof(entryAssembly));
        }

        var builder = AddControllersWithViewsCore(services, entryAssembly);
        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    /// <summary>
    /// Adds services for controllers to the specified <see cref="IServiceCollection"/>. This method will not
    /// register services used for pages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="entryAssembly">Application entry assembly used for <see cref="ApplicationPart"/> discovering.</param>
    /// <param name="configure">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features with controllers with views. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcApiExplorerMvcCoreBuilderExtensions.AddApiExplorer(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCorsMvcCoreBuilderExtensions.AddCors(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddFormatterMappings(IMvcCoreBuilder)"/>,
    /// <see cref="TagHelperServicesExtensions.AddCacheTagHelper(IMvcCoreBuilder)"/>,
    /// <see cref="MvcViewFeaturesMvcCoreBuilderExtensions.AddViews(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcRazorMvcCoreBuilderExtensions.AddRazorViewEngine(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for pages call <see cref="AddRazorPages(IServiceCollection)"/>.
    /// </para>
    /// </remarks>
    public static IMvcBuilder AddControllersWithViews(this IServiceCollection services, Assembly entryAssembly, Action<MvcOptions>? configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (entryAssembly == null)
        {
            throw new ArgumentNullException(nameof(entryAssembly));
        }

        // This method excludes all of the view-related services by default.
        var builder = AddControllersWithViewsCore(services, entryAssembly);
        if (configure != null)
        {
            builder.AddMvcOptions(configure);
        }

        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    /// <summary>
    /// Adds services for controllers to the specified <see cref="IServiceCollection"/>. This method will not
    /// register services used for pages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configure">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features with controllers with views. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcApiExplorerMvcCoreBuilderExtensions.AddApiExplorer(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCorsMvcCoreBuilderExtensions.AddCors(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddFormatterMappings(IMvcCoreBuilder)"/>,
    /// <see cref="TagHelperServicesExtensions.AddCacheTagHelper(IMvcCoreBuilder)"/>,
    /// <see cref="MvcViewFeaturesMvcCoreBuilderExtensions.AddViews(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcRazorMvcCoreBuilderExtensions.AddRazorViewEngine(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for pages call <see cref="AddRazorPages(IServiceCollection)"/>.
    /// </para>
    /// </remarks>
    public static IMvcBuilder AddControllersWithViews(this IServiceCollection services, Action<MvcOptions>? configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // This method excludes all of the view-related services by default.
        var builder = AddControllersWithViewsCore(services);
        if (configure != null)
        {
            builder.AddMvcOptions(configure);
        }

        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    private static IMvcCoreBuilder AddControllersWithViewsCore(IServiceCollection services, Assembly? entryAssembly  = null)
    {
        var builder = AddControllersCore(services, entryAssembly)
            .AddViews()
            .AddRazorViewEngine()
            .AddCacheTagHelper();

        AddTagHelpersFrameworkParts(builder.PartManager);

        return builder;
    }

    /// <summary>
    /// Adds services for pages to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features for pages. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// <see cref="TagHelperServicesExtensions.AddCacheTagHelper(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcRazorPagesMvcCoreBuilderExtensions.AddRazorPages(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers for APIs call <see cref="AddControllers(IServiceCollection)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers with views call <see cref="AddControllersWithViews(IServiceCollection)"/>.
    /// </para>
    /// </remarks>
    public static IMvcBuilder AddRazorPages(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var builder = AddRazorPagesCore(services);
        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    /// <summary>
    /// Adds services for pages to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="entryAssembly">Application entry assembly used for <see cref="ApplicationPart"/> discovering.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features for pages. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// <see cref="TagHelperServicesExtensions.AddCacheTagHelper(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcRazorPagesMvcCoreBuilderExtensions.AddRazorPages(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers for APIs call <see cref="AddControllers(IServiceCollection)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers with views call <see cref="AddControllersWithViews(IServiceCollection)"/>.
    /// </para>
    /// </remarks>
    public static IMvcBuilder AddRazorPages(this IServiceCollection services, Assembly entryAssembly)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (entryAssembly == null)
        {
            throw new ArgumentNullException(nameof(entryAssembly));
        }

        var builder = AddRazorPagesCore(services, entryAssembly);
        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    /// <summary>
    /// Adds services for pages to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="configure">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features for pages. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// <see cref="TagHelperServicesExtensions.AddCacheTagHelper(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcRazorPagesMvcCoreBuilderExtensions.AddRazorPages(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers for APIs call <see cref="AddControllers(IServiceCollection)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers with views call <see cref="AddControllersWithViews(IServiceCollection)"/>.
    /// </para>
    /// </remarks>
    public static IMvcBuilder AddRazorPages(this IServiceCollection services, Action<RazorPagesOptions>? configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var builder = AddRazorPagesCore(services);
        if (configure != null)
        {
            builder.AddRazorPages(configure);
        }

        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    /// <summary>
    /// Adds services for pages to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="entryAssembly">Application entry assembly used for <see cref="ApplicationPart"/> discovering.</param>
    /// <param name="configure">An <see cref="Action{MvcOptions}"/> to configure the provided <see cref="MvcOptions"/>.</param>
    /// <returns>An <see cref="IMvcBuilder"/> that can be used to further configure the MVC services.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the MVC services for the commonly used features for pages. This
    /// combines the effects of <see cref="MvcCoreServiceCollectionExtensions.AddMvcCore(IServiceCollection)"/>,
    /// <see cref="MvcCoreMvcCoreBuilderExtensions.AddAuthorization(IMvcCoreBuilder)"/>,
    /// <see cref="MvcDataAnnotationsMvcCoreBuilderExtensions.AddDataAnnotations(IMvcCoreBuilder)"/>,
    /// <see cref="TagHelperServicesExtensions.AddCacheTagHelper(IMvcCoreBuilder)"/>,
    /// and <see cref="MvcRazorPagesMvcCoreBuilderExtensions.AddRazorPages(IMvcCoreBuilder)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers for APIs call <see cref="AddControllers(IServiceCollection)"/>.
    /// </para>
    /// <para>
    /// To add services for controllers with views call <see cref="AddControllersWithViews(IServiceCollection)"/>.
    /// </para>
    /// </remarks>
    public static IMvcBuilder AddRazorPages(this IServiceCollection services, Assembly entryAssembly, Action<RazorPagesOptions>? configure)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (entryAssembly == null)
        {
            throw new ArgumentNullException(nameof(entryAssembly));
        }

        var builder = AddRazorPagesCore(services, entryAssembly);
        if (configure != null)
        {
            builder.AddRazorPages(configure);
        }

        return new MvcBuilder(builder.Services, builder.PartManager);
    }

    private static IMvcCoreBuilder AddRazorPagesCore(IServiceCollection services, Assembly? entryAssembly = null)
    {
        // This method includes the minimal things controllers need. It's not really feasible to exclude the services
        // for controllers.
        var builder = AddMvcCore(services, entryAssembly)
            .AddAuthorization()
            .AddDataAnnotations()
            .AddRazorPages()
            .AddCacheTagHelper();

        AddTagHelpersFrameworkParts(builder.PartManager);

        if (MetadataUpdater.IsSupported)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IActionDescriptorChangeProvider, HotReloadService>());
        }

        return builder;
    }

    private static IMvcCoreBuilder AddMvcCore(IServiceCollection services, Assembly? entryAssembly)
        => entryAssembly == null ? services.AddMvcCore() : services.AddMvcCore(entryAssembly);

    internal static void AddTagHelpersFrameworkParts(ApplicationPartManager partManager)
    {
        var mvcTagHelpersAssembly = typeof(InputTagHelper).Assembly;
        if (!partManager.ApplicationParts.OfType<AssemblyPart>().Any(p => p.Assembly == mvcTagHelpersAssembly))
        {
            partManager.ApplicationParts.Add(new FrameworkAssemblyPart(mvcTagHelpersAssembly));
        }

        var mvcRazorAssembly = typeof(UrlResolutionTagHelper).Assembly;
        if (!partManager.ApplicationParts.OfType<AssemblyPart>().Any(p => p.Assembly == mvcRazorAssembly))
        {
            partManager.ApplicationParts.Add(new FrameworkAssemblyPart(mvcRazorAssembly));
        }
    }

    [DebuggerDisplay("{Name}")]
    private class FrameworkAssemblyPart : AssemblyPart, ICompilationReferencesProvider
    {
        public FrameworkAssemblyPart(Assembly assembly)
            : base(assembly)
        {
        }

        IEnumerable<string> ICompilationReferencesProvider.GetReferencePaths() => Enumerable.Empty<string>();
    }
}
