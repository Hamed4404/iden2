// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Castle.Core.Internal;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class OutputCacheAttributeTests
{
    [Fact]
    public void Attribute_CreatesDefaultPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.Default));
        var policy = attribute.BuildPolicy();

        Assert.Equal(DefaultPolicy.Instance, policy);
    }

    [Fact]
    public async Task Attribute_CreatesExpirePolicy()
    {
        var context = TestUtils.CreateUninitializedContext();

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.Duration));
        await attribute.BuildPolicy().CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Equal(42, context.ResponseExpirationTimeSpan?.TotalSeconds);
    }

    [Fact]
    public async Task Attribute_CreatesNoStorePolicy()
    {
        var context = TestUtils.CreateUninitializedContext();

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.NoStore));
        await attribute.BuildPolicy().CacheRequestAsync(context, cancellation: default);

        Assert.False(context.EnableOutputCaching);
    }

    [Fact]
    public async Task Attribute_CreatesNamedPolicy()
    {
        var options = new OutputCacheOptions();
        options.AddPolicy("MyPolicy", b => b.Expire(TimeSpan.FromSeconds(42)));

        var context = TestUtils.CreateTestContext(options: options);

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.PolicyName));
        await attribute.BuildPolicy().CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Equal(42, context.ResponseExpirationTimeSpan?.TotalSeconds);
    }

    [Fact]
    public async Task Attribute_CreatesVaryByHeaderPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.HttpContext.Request.Headers["HeaderA"] = "ValueA";
        context.HttpContext.Request.Headers["HeaderB"] = "ValueB";

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.VaryByHeaderNames));
        await attribute.BuildPolicy().CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("HeaderA", (IEnumerable<string>)context.CacheVaryByRules.HeaderNames);
        Assert.Contains("HeaderC", (IEnumerable<string>)context.CacheVaryByRules.HeaderNames);
        Assert.DoesNotContain("HeaderB", (IEnumerable<string>)context.CacheVaryByRules.HeaderNames);
    }

    [Fact]
    public async Task Attribute_CreatesVaryByQueryPolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.HttpContext.Request.QueryString = new QueryString("?QueryA=ValueA&QueryB=ValueB");

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.VaryByQueryKeys));
        await attribute.BuildPolicy().CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("QueryA", (IEnumerable<string>)context.CacheVaryByRules.QueryKeys);
        Assert.Contains("QueryC", (IEnumerable<string>)context.CacheVaryByRules.QueryKeys);
        Assert.DoesNotContain("QueryB", (IEnumerable<string>)context.CacheVaryByRules.QueryKeys);
    }

    [Fact]
    public async Task Attribute_CreatesVaryByRoutePolicy()
    {
        var context = TestUtils.CreateUninitializedContext();
        context.HttpContext.Request.RouteValues = new Routing.RouteValueDictionary()
        {
            ["RouteA"] = "ValueA",
            ["RouteB"] = 123.456,
        };

        var attribute = OutputCacheMethods.GetAttribute(nameof(OutputCacheMethods.VaryByRouteValues));
        await attribute.BuildPolicy().CacheRequestAsync(context, cancellation: default);

        Assert.True(context.EnableOutputCaching);
        Assert.Contains("RouteA", (IEnumerable<string>)context.CacheVaryByRules.RouteValues);
        Assert.Contains("RouteC", (IEnumerable<string>)context.CacheVaryByRules.RouteValues);
        Assert.DoesNotContain("RouteB", (IEnumerable<string>)context.CacheVaryByRules.RouteValues);
    }

    private class OutputCacheMethods
    {
        public static OutputCacheAttribute GetAttribute(string methodName)
        {
            return typeof(OutputCacheMethods).GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetAttribute<OutputCacheAttribute>();
        }

        [OutputCache()]
        public static void Default() { }

        [OutputCache(Duration = 42)]
        public static void Duration() { }

        [OutputCache(NoStore = true)]
        public static void NoStore() { }

        [OutputCache(PolicyName = "MyPolicy")]
        public static void PolicyName() { }

        [OutputCache(VaryByHeaderNames = new[] { "HeaderA", "HeaderC" })]
        public static void VaryByHeaderNames() { }

        [OutputCache(VaryByQueryKeys = new[] { "QueryA", "QueryC" })]
        public static void VaryByQueryKeys() { }

        [OutputCache(VaryByRouteValues = new[] { "RouteA", "RouteC" })]
        public static void VaryByRouteValues() { }
    }
}
