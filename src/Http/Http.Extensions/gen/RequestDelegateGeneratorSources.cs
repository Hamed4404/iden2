// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp;
namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator;

internal static class RequestDelegateGeneratorSources
{
    private const string SourceHeader = """
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#nullable enable
""";

    public static string GeneratedCodeAttribute => $@"[System.CodeDom.Compiler.GeneratedCodeAttribute(""{typeof(RequestDelegateGeneratorSources).Assembly.FullName}"", ""{typeof(RequestDelegateGeneratorSources).Assembly.GetName().Version}"")]";

    public static string ContentMetadataTypes => """
    file static class GeneratedMetadataConstants
    {
        public static readonly string[] JsonContentType = new [] { "application/json" };
        public static readonly string[] PlaintextContentType = new [] { "text/plain" };
    }

    file sealed class GeneratedProducesResponseTypeMetadata : IProducesResponseTypeMetadata
    {
        public GeneratedProducesResponseTypeMetadata(Type? type, int statusCode, string[] contentTypes)
        {
            Type = type;
            StatusCode = statusCode;
            ContentTypes = contentTypes;
        }

        public Type? Type { get; }

        public int StatusCode { get; }

        public IEnumerable<string> ContentTypes { get; }
    }
""";

    public static string PopulateEndpointMetadataMethod => """
        private static void PopulateMetadataForEndpoint<T>(MethodInfo method, EndpointBuilder builder)
            where T : IEndpointMetadataProvider
        {
            T.PopulateMetadata(method, builder);
        }
""";

    public static string PopulateEndpointParameterMetadataMethod => """
        private static void PopulateMetadataForParameter<T>(ParameterInfo parameter, EndpointBuilder builder)
            where T : IEndpointParameterMetadataProvider
        {
            T.PopulateMetadata(parameter, builder);
        }
""";

    public static string TryResolveBodyAsyncMethod => """
        private static async ValueTask<(bool, T?)> TryResolveBodyAsync<T>(HttpContext httpContext, LogOrThrowExceptionHelper logOrThrowExceptionHelper, bool allowEmpty, string parameterTypeName, string parameterName, bool isInferred = false)
        {
            var feature = httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpRequestBodyDetectionFeature>();

            if (feature?.CanHaveBody == true)
            {
                if (!httpContext.Request.HasJsonContentType())
                {
                    logOrThrowExceptionHelper.UnexpectedJsonContentType(httpContext.Request.ContentType);
                    httpContext.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    return (false, default);
                }
                try
                {
                    var bodyValue = await httpContext.Request.ReadFromJsonAsync<T>();
                    if (!allowEmpty && bodyValue == null)
                    {
                        if (!isInferred)
                        {
                            logOrThrowExceptionHelper.RequiredParameterNotProvided(parameterTypeName, parameterName, "body");
                        }
                        else
                        {
                            logOrThrowExceptionHelper.ImplicitBodyNotProvided(parameterName);
                        }
                        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                        return (false, bodyValue);
                    }
                    return (true, bodyValue);
                }
                catch (BadHttpRequestException badHttpRequestException)
                {
                    logOrThrowExceptionHelper.RequestBodyIOException(badHttpRequestException);
                    httpContext.Response.StatusCode = badHttpRequestException.StatusCode;
                    return (false, default);
                }
                catch (IOException ioException)
                {
                    logOrThrowExceptionHelper.RequestBodyIOException(ioException);
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return (false, default);
                }
                catch (System.Text.Json.JsonException jsonException)
                {
                    logOrThrowExceptionHelper.InvalidJsonRequestBody(parameterTypeName, parameterName, jsonException);
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return (false, default);
                }
            }
            else if (!allowEmpty)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }

            return (allowEmpty, default);
        }
""";

    public static string TryResolveFormAsyncMethod => """
        private static async Task<(bool, object?)> TryResolveFormAsync(
            HttpContext httpContext,
            LogOrThrowExceptionHelper logOrThrowExceptionHelper,
            string parameterTypeName,
            string parameterName)
        {
            object? formValue = null;
            var feature = httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpRequestBodyDetectionFeature>();

            if (feature?.CanHaveBody == true)
            {
                if (!httpContext.Request.HasFormContentType)
                {
                    logOrThrowExceptionHelper.UnexpectedNonFormContentType(httpContext.Request.ContentType);
                    httpContext.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                    return (false, null);
                }

                try
                {
                    formValue = await httpContext.Request.ReadFormAsync();
                }
                catch (BadHttpRequestException ex)
                {
                    logOrThrowExceptionHelper.RequestBodyIOException(ex);
                    httpContext.Response.StatusCode = ex.StatusCode;
                    return (false, null);
                }
                catch (IOException ex)
                {
                    logOrThrowExceptionHelper.RequestBodyIOException(ex);
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return (false, null);
                }
                catch (InvalidDataException ex)
                {
                    logOrThrowExceptionHelper.InvalidFormRequestBody(parameterTypeName, parameterName, ex);
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return (false, null);
                }
            }

            return (true, formValue);
        }
""";

    public static string TryParseExplicitMethod => """
        private static bool TryParseExplicit<T>(string? s, IFormatProvider? provider, [MaybeNullWhen(returnValue: false)] out T result) where T: IParsable<T>
            => T.TryParse(s, provider, out result);
""";

    public static string BindAsyncMethod => """
        private static ValueTask<T?> BindAsync<T>(HttpContext context, ParameterInfo parameter)
            where T : class, IBindableFromHttpContext<T>
        {
            return T.BindAsync(context, parameter);
        }
""";

    public static string ResolveFromRouteOrQueryMethod => """
        private static Func<HttpContext, StringValues> ResolveFromRouteOrQuery(string parameterName, IEnumerable<string>? routeParameterNames)
        {
            return routeParameterNames?.Contains(parameterName, StringComparer.OrdinalIgnoreCase) == true
                ? (httpContext) => new StringValues((string?)httpContext.Request.RouteValues[parameterName])
                : (httpContext) => httpContext.Request.Query[parameterName];
        }
""";

    public static string WriteToResponseAsyncMethod => """
        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
            Justification = "The 'JsonSerializer.IsReflectionEnabledByDefault' feature switch, which is set to false by default for trimmed ASP.NET apps, ensures the JsonSerializer doesn't use Reflection.")]
        [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "See above.")]
        private static Task WriteToResponseAsync<T>(T? value, HttpContext httpContext, JsonTypeInfo<T> jsonTypeInfo)
        {
            var runtimeType = value?.GetType();
            if (runtimeType is null || jsonTypeInfo.Type == runtimeType || jsonTypeInfo.PolymorphismOptions is not null)
            {
                return httpContext.Response.WriteAsJsonAsync(value!, jsonTypeInfo);
            }

            return httpContext.Response.WriteAsJsonAsync<object?>(value, jsonTypeInfo.Options);
        }
""";

    public static string ResolveJsonBodyOrServiceMethod => """
        private static Func<HttpContext, bool, ValueTask<(bool, T?)>> ResolveJsonBodyOrService<T>(LogOrThrowExceptionHelper logOrThrowExceptionHelper, string parameterTypeName, string parameterName, IServiceProviderIsService? serviceProviderIsService = null)
        {
            if (serviceProviderIsService is not null)
            {
                if (serviceProviderIsService.IsService(typeof(T)))
                {
                    return static (httpContext, isOptional) => new ValueTask<(bool, T?)>((true, httpContext.RequestServices.GetService<T>()));
                }
            }
            return (httpContext, isOptional) => TryResolveBodyAsync<T>(httpContext, logOrThrowExceptionHelper, isOptional, parameterTypeName, parameterName, isInferred: true);
        }
""";

    public static string LogOrThrowExceptionHelperClass => $$"""
    file sealed class LogOrThrowExceptionHelper
    {
        private readonly ILogger? _rdgLogger;
        private readonly bool _shouldThrow;

        public LogOrThrowExceptionHelper(IServiceProvider? serviceProvider, RequestDelegateFactoryOptions? options)
        {
            var loggerFactory = serviceProvider?.GetRequiredService<ILoggerFactory>();
            _rdgLogger = loggerFactory?.CreateLogger("{{typeof(RequestDelegateGenerator)}}");
            _shouldThrow = options?.ThrowOnBadRequest ?? false;
        }

        public void RequestBodyIOException(IOException exception)
        {
            if (_rdgLogger != null)
            {
                _requestBodyIOException(_rdgLogger, exception);
            }
        }

        private static readonly Action<ILogger, Exception?> _requestBodyIOException =
            LoggerMessage.Define(LogLevel.Debug, new EventId({{RequestDelegateCreationLogging.RequestBodyIOExceptionEventId}}, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.RequestBodyIOExceptionEventName, true)}}), {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.RequestBodyIOExceptionMessage, true)}});

        public void InvalidJsonRequestBody(string parameterTypeName, string parameterName, Exception exception)
        {
            if (_shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.InvalidJsonRequestBodyExceptionMessage, true)}}, parameterTypeName, parameterName);
                throw new BadHttpRequestException(message, exception);
            }

            if (_rdgLogger != null)
            {
                _invalidJsonRequestBody(_rdgLogger, parameterTypeName, parameterName, exception);
            }
        }

        private static readonly Action<ILogger, string, string, Exception?> _invalidJsonRequestBody =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId({{RequestDelegateCreationLogging.InvalidJsonRequestBodyEventId}}, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.InvalidJsonRequestBodyEventName, true)}}), {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.InvalidJsonRequestBodyLogMessage, true)}});

        public void ParameterBindingFailed(string parameterTypeName, string parameterName, string sourceValue)
        {
            if (_shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.ParameterBindingFailedExceptionMessage, true)}}, parameterTypeName, parameterName, sourceValue);
                throw new BadHttpRequestException(message);
            }

            if (_rdgLogger != null)
            {
                _parameterBindingFailed(_rdgLogger, parameterTypeName, parameterName, sourceValue, null);
            }
        }

        private static readonly Action<ILogger, string, string, string, Exception?> _parameterBindingFailed =
            LoggerMessage.Define<string, string, string>(LogLevel.Debug, new EventId({{RequestDelegateCreationLogging.ParameterBindingFailedEventId}}, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.ParameterBindingFailedEventName, true)}}), {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.ParameterBindingFailedLogMessage, true)}});

        public void RequiredParameterNotProvided(string parameterTypeName, string parameterName, string source)
        {
            if (_shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.RequiredParameterNotProvidedExceptionMessage, true)}}, parameterTypeName, parameterName, source);
                throw new BadHttpRequestException(message);
            }

            if (_rdgLogger != null)
            {
                _requiredParameterNotProvided(_rdgLogger, parameterTypeName, parameterName, source, null);
            }
        }

        private static readonly Action<ILogger, string, string, string, Exception?> _requiredParameterNotProvided =
            LoggerMessage.Define<string, string, string>(LogLevel.Debug, new EventId({{RequestDelegateCreationLogging.RequiredParameterNotProvidedEventId}}, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.RequiredParameterNotProvidedEventName, true)}}), {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.RequiredParameterNotProvidedLogMessage, true)}});

        public void ImplicitBodyNotProvided(string parameterName)
        {
            if (_shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.ImplicitBodyNotProvidedExceptionMessage, true)}}, parameterName);
                throw new BadHttpRequestException(message);
            }

            if (_rdgLogger != null)
            {
                _implicitBodyNotProvided(_rdgLogger, parameterName, null);
            }
        }

        private static readonly Action<ILogger, string, Exception?> _implicitBodyNotProvided =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId({{RequestDelegateCreationLogging.ImplicitBodyNotProvidedEventId}}, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.ImplicitBodyNotProvidedEventName, true)}}), {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.ImplicitBodyNotProvidedLogMessage, true)}});

        public void UnexpectedJsonContentType(string? contentType)
        {
            if (_shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.UnexpectedJsonContentTypeExceptionMessage, true)}}, contentType);
                throw new BadHttpRequestException(message, StatusCodes.Status415UnsupportedMediaType);
            }

            if (_rdgLogger != null)
            {
                _unexpectedJsonContentType(_rdgLogger, contentType ?? "(none)", null);
            }
        }

        private static readonly Action<ILogger, string, Exception?> _unexpectedJsonContentType =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId({{RequestDelegateCreationLogging.UnexpectedJsonContentTypeEventId}}, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.UnexpectedJsonContentTypeEventName, true)}}), {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.UnexpectedJsonContentTypeLogMessage, true)}});

        public void UnexpectedNonFormContentType(string? contentType)
        {
            if (_shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.UnexpectedFormContentTypeExceptionMessage, true)}}, contentType);
                throw new BadHttpRequestException(message, StatusCodes.Status415UnsupportedMediaType);
            }

            if (_rdgLogger != null)
            {
                _unexpectedNonFormContentType(_rdgLogger, contentType ?? "(none)", null);
            }
        }

        private static readonly Action<ILogger, string, Exception?> _unexpectedNonFormContentType =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId({{RequestDelegateCreationLogging.UnexpectedFormContentTypeEventId}}, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.UnexpectedFormContentTypeLogEventName, true)}}), {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.UnexpectedFormContentTypeLogMessage, true)}});

        public void InvalidFormRequestBody(string parameterTypeName, string parameterName, Exception exception)
        {
            if (_shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.InvalidFormRequestBodyExceptionMessage, true)}}, parameterTypeName, parameterName);
                throw new BadHttpRequestException(message, exception);
            }

            if (_rdgLogger != null)
            {
                _invalidFormRequestBody(_rdgLogger, parameterTypeName, parameterName, exception);
            }
        }

        private static readonly Action<ILogger, string, string, Exception?> _invalidFormRequestBody =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId({{RequestDelegateCreationLogging.InvalidFormRequestBodyEventId}}, {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.InvalidFormRequestBodyEventName, true)}}), {{SymbolDisplay.FormatLiteral(RequestDelegateCreationLogging.InvalidFormRequestBodyLogMessage, true)}});
    }
""";

    public static string PropertyAsParameterInfoClass = """
    file sealed class PropertyAsParameterInfo : ParameterInfo
    {
        private readonly PropertyInfo _underlyingProperty;
        private readonly ParameterInfo? _constructionParameterInfo;

        public PropertyAsParameterInfo(bool isOptional, PropertyInfo propertyInfo)
        {
            Debug.Assert(propertyInfo != null, "PropertyInfo must be provided.");

            AttrsImpl = (ParameterAttributes)propertyInfo.Attributes;
            NameImpl = propertyInfo.Name;
            MemberImpl = propertyInfo;
            ClassImpl = propertyInfo.PropertyType;

            // It is not a real parameter in the delegate, so,
            // not defining a real position.
            PositionImpl = -1;

            _underlyingProperty = propertyInfo;
            IsOptional = isOptional;
        }

        public PropertyAsParameterInfo(bool isOptional, PropertyInfo property, ParameterInfo? parameterInfo)
            : this(isOptional, property)
        {
            _constructionParameterInfo = parameterInfo;
        }

        public override bool HasDefaultValue
        => _constructionParameterInfo is not null && _constructionParameterInfo.HasDefaultValue;
        public override object? DefaultValue
            => _constructionParameterInfo?.DefaultValue;
        public override int MetadataToken => _underlyingProperty.MetadataToken;
        public override object? RawDefaultValue
            => _constructionParameterInfo?.RawDefaultValue;

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            var attributes = _constructionParameterInfo?.GetCustomAttributes(attributeType, inherit);

            if (attributes == null || attributes is { Length: 0 })
            {
                attributes = _underlyingProperty.GetCustomAttributes(attributeType, inherit);
            }

            return attributes;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            var constructorAttributes = _constructionParameterInfo?.GetCustomAttributes(inherit);

            if (constructorAttributes == null || constructorAttributes is { Length: 0 })
            {
                return _underlyingProperty.GetCustomAttributes(inherit);
            }

            var propertyAttributes = _underlyingProperty.GetCustomAttributes(inherit);

            // Since the constructors attributes should take priority we will add them first,
            // as we usually call it as First() or FirstOrDefault() in the argument creation
            var mergedAttributes = new object[constructorAttributes.Length + propertyAttributes.Length];
            Array.Copy(constructorAttributes, mergedAttributes, constructorAttributes.Length);
            Array.Copy(propertyAttributes, 0, mergedAttributes, constructorAttributes.Length, propertyAttributes.Length);

            return mergedAttributes;
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            var attributes = new List<CustomAttributeData>(
                _constructionParameterInfo?.GetCustomAttributesData() ?? Array.Empty<CustomAttributeData>());
            attributes.AddRange(_underlyingProperty.GetCustomAttributesData());

            return attributes.AsReadOnly();
        }

        public override Type[] GetOptionalCustomModifiers()
            => _underlyingProperty.GetOptionalCustomModifiers();

        public override Type[] GetRequiredCustomModifiers()
            => _underlyingProperty.GetRequiredCustomModifiers();

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return (_constructionParameterInfo is not null && _constructionParameterInfo.IsDefined(attributeType, inherit)) ||
                _underlyingProperty.IsDefined(attributeType, inherit);
        }

        public new bool IsOptional { get; }
    }
""";

    public static string GetGeneratedRouteBuilderExtensionsSource(string genericThunks, string thunks, string endpoints, string helperMethods, string helperTypes) => $$"""
{{SourceHeader}}

namespace Microsoft.AspNetCore.Builder
{
    {{GeneratedCodeAttribute}}
    internal sealed class SourceKey
    {
        public string Path { get; init; }
        public int Line { get; init; }

        public SourceKey(string path, int line)
        {
            Path = path;
            Line = line;
        }
    }

{{GetEndpoints(endpoints)}}
}

namespace Microsoft.AspNetCore.Http.Generated
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;
    using System.Text.Json.Serialization.Metadata;
    using System.Threading.Tasks;
    using System.IO;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.AspNetCore.Routing.Patterns;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Json;
    using Microsoft.AspNetCore.Http.Metadata;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;
    using Microsoft.Extensions.Options;

    using MetadataPopulator = System.Func<System.Reflection.MethodInfo, Microsoft.AspNetCore.Http.RequestDelegateFactoryOptions?, Microsoft.AspNetCore.Http.RequestDelegateMetadataResult>;
    using RequestDelegateFactoryFunc = System.Func<System.Delegate, Microsoft.AspNetCore.Http.RequestDelegateFactoryOptions, Microsoft.AspNetCore.Http.RequestDelegateMetadataResult?, Microsoft.AspNetCore.Http.RequestDelegateResult>;

    file static class GeneratedRouteBuilderExtensionsCore
    {
{{GetGenericThunks(genericThunks)}}
{{GetThunks(thunks)}}

        private static EndpointFilterDelegate BuildFilterDelegate(EndpointFilterDelegate filteredInvocation, EndpointBuilder builder, MethodInfo mi)
        {
            var routeHandlerFilters =  builder.FilterFactories;
            var context0 = new EndpointFilterFactoryContext
            {
                MethodInfo = mi,
                ApplicationServices = builder.ApplicationServices,
            };
            var initialFilteredInvocation = filteredInvocation;
            for (var i = routeHandlerFilters.Count - 1; i >= 0; i--)
            {
                var filterFactory = routeHandlerFilters[i];
                filteredInvocation = filterFactory(context0, filteredInvocation);
            }
            return filteredInvocation;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
            Justification = "The 'JsonSerializer.IsReflectionEnabledByDefault' feature switch, which is set to false by default for trimmed ASP.NET apps, ensures the JsonSerializer doesn't use Reflection.")]
        [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "See above.")]
        private static Task ExecuteObjectResult(object? obj, HttpContext httpContext)
        {
            if (obj is IResult r)
            {
                return r.ExecuteAsync(httpContext);
            }
            else if (obj is string s)
            {
                return httpContext.Response.WriteAsync(s);
            }
            else
            {
                return httpContext.Response.WriteAsJsonAsync(obj);
            }
        }

{{helperMethods}}
    }

{{helperTypes}}
{{LogOrThrowExceptionHelperClass}}
}
""";
    private static string GetGenericThunks(string genericThunks) => genericThunks != string.Empty ? $$"""
        private static class GenericThunks<T>
        {
            public static readonly Dictionary<(string, int), (MetadataPopulator, RequestDelegateFactoryFunc)> map = new()
            {
                {{genericThunks}}
            };
        }

        internal static RouteHandlerBuilder MapCore<T>(
            this IEndpointRouteBuilder routes,
            string pattern,
            Delegate handler,
            IEnumerable<string> httpMethods,
            string filePath,
            int lineNumber)
        {
            var (populateMetadata, createRequestDelegate) = GenericThunks<T>.map[(filePath, lineNumber)];
            return RouteHandlerServices.Map(routes, pattern, handler, httpMethods, populateMetadata, createRequestDelegate);
        }
""" : string.Empty;

    private static string GetThunks(string thunks) => thunks != string.Empty ? $$"""
        private static readonly Dictionary<(string, int), (MetadataPopulator, RequestDelegateFactoryFunc)> map = new()
        {
{{thunks}}
        };

        internal static RouteHandlerBuilder MapCore(
            this IEndpointRouteBuilder routes,
            string pattern,
            Delegate handler,
            IEnumerable<string> httpMethods,
            string filePath,
            int lineNumber)
        {
            var (populateMetadata, createRequestDelegate) = map[(filePath, lineNumber)];
            return RouteHandlerServices.Map(routes, pattern, handler, httpMethods, populateMetadata, createRequestDelegate);
        }
""" : string.Empty;

    private static string GetEndpoints(string endpoints) => endpoints != string.Empty ? $$"""
    // This class needs to be internal so that the compiled application
    // has access to the strongly-typed endpoint definitions that are
    // generated by the compiler so that they will be favored by
    // overload resolution and opt the runtime in to the code generated
    // implementation produced here.
    {{GeneratedCodeAttribute}}
    internal static class GenerateRouteBuilderEndpoints
    {
        private static readonly string[] GetVerb = new[] { global::Microsoft.AspNetCore.Http.HttpMethods.Get };
        private static readonly string[] PostVerb = new[] { global::Microsoft.AspNetCore.Http.HttpMethods.Post };
        private static readonly string[] PutVerb = new[]  { global::Microsoft.AspNetCore.Http.HttpMethods.Put };
        private static readonly string[] DeleteVerb = new[] { global::Microsoft.AspNetCore.Http.HttpMethods.Delete };
        private static readonly string[] PatchVerb = new[] { global::Microsoft.AspNetCore.Http.HttpMethods.Patch };

        {{endpoints}}
    }
""" : string.Empty;
}
