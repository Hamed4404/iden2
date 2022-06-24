// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.RateLimiting;

public class RateLimitingMiddlewareTests : LoggedTest
{
    [Fact]
    public void Ctor_ThrowsExceptionsWhenNullArgs()
    {
        var options = CreateOptionsAccessor();
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>();

        Assert.Throws<ArgumentNullException>(() => new RateLimitingMiddleware(
            null,
            new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
            options,
            Mock.Of<IServiceProvider>()));

        Assert.Throws<ArgumentNullException>(() => new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
        null,
        options,
        Mock.Of<IServiceProvider>()));

        Assert.Throws<ArgumentNullException>(() => new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
        new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
        options,
        null));
    }

    [Fact]
    public async Task RequestsCallNextIfAccepted()
    {
        var flag = false;
        var options = CreateOptionsAccessor();
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(true));
        var middleware = new RateLimitingMiddleware(c =>
        {
            flag = true;
            return Task.CompletedTask;
        },
        new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
        options,
        Mock.Of<IServiceProvider>());

        await middleware.Invoke(new DefaultHttpContext());
        Assert.True(flag);
    }

    [Fact]
    public async Task RequestRejected_CallsOnRejectedAndGives503()
    {
        var onRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false));
        options.Value.OnRejected = (context, token) =>
        {
            onRejectedInvoked = true;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
        new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
        options,
        Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        await middleware.Invoke(context).DefaultTimeout();
        Assert.True(onRejectedInvoked);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
    }

    [Fact]
    public async Task RequestRejected_WinsOverDefaultStatusCode()
    {
        var onRejectedInvoked = false;
        var options = CreateOptionsAccessor();
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false));
        options.Value.OnRejected = (context, token) =>
        {
            onRejectedInvoked = true;
            context.HttpContext.Response.StatusCode = 429;
            return ValueTask.CompletedTask;
        };

        var middleware = new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
        new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
        options,
        Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        await middleware.Invoke(context).DefaultTimeout();
        Assert.True(onRejectedInvoked);
        Assert.Equal(StatusCodes.Status429TooManyRequests, context.Response.StatusCode);
    }

    [Fact]
    public async Task RequestAborted_ThrowsTaskCanceledException()
    {
        var options = CreateOptionsAccessor();
        options.Value.GlobalLimiter = new TestPartitionedRateLimiter<HttpContext>(new TestRateLimiter(false));

        var middleware = new RateLimitingMiddleware(c =>
        {
            return Task.CompletedTask;
        },
        new NullLoggerFactory().CreateLogger<RateLimitingMiddleware>(),
        options,
        Mock.Of<IServiceProvider>());

        var context = new DefaultHttpContext();
        context.RequestAborted = new CancellationToken(true);
        await Assert.ThrowsAsync<TaskCanceledException>(() => middleware.Invoke(context)).DefaultTimeout();
    }

    private IOptions<RateLimiterOptions> CreateOptionsAccessor() => Options.Create(new RateLimiterOptions());

}
