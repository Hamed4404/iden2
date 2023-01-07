// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.Mvc;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class MvcAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.AmbiguousActionRoute
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            var compilation = context.Compilation;
            var wellKnownTypes = WellKnownTypes.GetOrCreate(compilation);
            var routeUsageCache = RouteUsageCache.GetOrCreate(compilation);

            var concurrentQueue = new ConcurrentQueue<List<ActionRoute>>();

            context.RegisterSymbolAction(context =>
            {
                var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
                if (MvcDetector.IsController(namedTypeSymbol, wellKnownTypes))
                {
                    // Pool and reuse lists for each block.
                    if (!concurrentQueue.TryDequeue(out var actionRoutes))
                    {
                        actionRoutes = new List<ActionRoute>();
                    }

                    PopulateActionRoutes(context, wellKnownTypes, routeUsageCache, namedTypeSymbol, actionRoutes);

                    DetectAmbiguousActionRoutes(context, wellKnownTypes, actionRoutes);

                    // Return to the pool.
                    actionRoutes.Clear();
                    concurrentQueue.Enqueue(actionRoutes);
                }
            }, SymbolKind.NamedType);
        });
    }

    private static void PopulateActionRoutes(SymbolAnalysisContext context, WellKnownTypes wellKnownTypes, RouteUsageCache routeUsageCache, INamedTypeSymbol namedTypeSymbol, List<ActionRoute> actionRoutes)
    {
        foreach (var member in namedTypeSymbol.GetMembers())
        {
            if (member is IMethodSymbol methodSymbol &&
                MvcDetector.IsAction(methodSymbol, wellKnownTypes))
            {
                foreach (var attribute in methodSymbol.GetAttributes())
                {
                    if (attribute.AttributeClass is null || !wellKnownTypes.IsType(attribute.AttributeClass, RouteAttributeTypes, out var match))
                    {
                        continue;
                    }

                    var routeUsage = GetRouteUsageModel(attribute, routeUsageCache, context.CancellationToken);
                    if (routeUsage is null)
                    {
                        continue;
                    }

                    actionRoutes.Add(new ActionRoute(methodSymbol, routeUsage, GetHttpMethods(match.Value)));
                }
            }
        }
    }

    private static ImmutableArray<string> GetHttpMethods(WellKnownType match)
    {
        switch (match)
        {
            case WellKnownType.Microsoft_AspNetCore_Mvc_RouteAttribute:
                // No HTTP method.
                return ImmutableArray<string>.Empty;
            case WellKnownType.Microsoft_AspNetCore_Mvc_HttpDeleteAttribute:
                return ImmutableArray.Create("DELETE");
            case WellKnownType.Microsoft_AspNetCore_Mvc_HttpGetAttribute:
                return ImmutableArray.Create("GET");
            case WellKnownType.Microsoft_AspNetCore_Mvc_HttpHeadAttribute:
                return ImmutableArray.Create("HEAD");
            case WellKnownType.Microsoft_AspNetCore_Mvc_HttpOptionsAttribute:
                return ImmutableArray.Create("OPTIONS");
            case WellKnownType.Microsoft_AspNetCore_Mvc_HttpPatchAttribute:
                return ImmutableArray.Create("PATCH");
            case WellKnownType.Microsoft_AspNetCore_Mvc_HttpPostAttribute:
                return ImmutableArray.Create("POST");
            case WellKnownType.Microsoft_AspNetCore_Mvc_HttpPutAttribute:
                return ImmutableArray.Create("PUT");
            default:
                throw new InvalidOperationException("Unexpected well known type:" + match);
        }
    }

    private static readonly WellKnownType[] RouteAttributeTypes = new[]
    {
        WellKnownType.Microsoft_AspNetCore_Mvc_RouteAttribute,
        WellKnownType.Microsoft_AspNetCore_Mvc_HttpDeleteAttribute,
        WellKnownType.Microsoft_AspNetCore_Mvc_HttpGetAttribute,
        WellKnownType.Microsoft_AspNetCore_Mvc_HttpHeadAttribute,
        WellKnownType.Microsoft_AspNetCore_Mvc_HttpOptionsAttribute,
        WellKnownType.Microsoft_AspNetCore_Mvc_HttpPatchAttribute,
        WellKnownType.Microsoft_AspNetCore_Mvc_HttpPostAttribute,
        WellKnownType.Microsoft_AspNetCore_Mvc_HttpPutAttribute
    };

    private static RouteUsageModel? GetRouteUsageModel(AttributeData attribute, RouteUsageCache routeUsageCache, CancellationToken cancellationToken)
    {
        if (attribute.ConstructorArguments.IsEmpty || attribute.ApplicationSyntaxReference is null)
        {
            return null;
        }

        if (attribute.ApplicationSyntaxReference.GetSyntax(cancellationToken) is AttributeSyntax attributeSyntax &&
            attributeSyntax.ArgumentList is { } argumentList)
        {
            var attributeArgument = argumentList.Arguments[0];
            if (attributeArgument.Expression is LiteralExpressionSyntax literalExpression)
            {
                return routeUsageCache.Get(literalExpression.Token, cancellationToken);
            }
        }

        return null;
    }

    private record struct ActionRoute(IMethodSymbol ActionSymbol, RouteUsageModel RouteUsageModel, ImmutableArray<string> HttpMethods);
}
