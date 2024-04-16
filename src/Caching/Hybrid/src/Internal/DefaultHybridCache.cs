// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

/// <summary>
/// The inbuilt ASP.NET implementation of <see cref="HybridCache"/>.
/// </summary>
internal sealed partial class DefaultHybridCache : HybridCache
{
    private readonly IDistributedCache backendCache;
    private readonly IMemoryCache localCache;
    private readonly IServiceProvider services;
    private readonly IHybridCacheSerializerFactory[] serializerFactories;
    private readonly HybridCacheOptions options;
    private readonly BackendFeatures features;

    private readonly HybridCacheEntryFlags defaultFlags;
    private readonly TimeSpan defaultExpiration;
    private readonly TimeSpan defaultLocalCacheExpiration;

    private readonly DistributedCacheEntryOptions defaultDistributedCacheExpiration;

    [Flags]
    private enum BackendFeatures
    {
        None = 0,
        Buffers = 1 << 0,
    }

    public DefaultHybridCache(IOptions<HybridCacheOptions> options, IDistributedCache backendCache, IMemoryCache localCache, IServiceProvider services)
    {
        this.backendCache = backendCache ?? throw new ArgumentNullException(nameof(backendCache));
        this.localCache = localCache ?? throw new ArgumentNullException(nameof(localCache));
        this.services = services ?? throw new ArgumentNullException(nameof(services));
        this.options = options.Value;

        // perform type-tests on the backend once only
        if (backendCache is IBufferDistributedCache)
        {
            this.features |= BackendFeatures.Buffers;
        }

        // When resolving serializers via the factory API, we will want the *last* instance,
        // i.e. "last added wins"; we can optimize by reversing the array ahead of time, and
        // taking the first match
        var factories = services.GetServices<IHybridCacheSerializerFactory>().ToArray();
        Array.Reverse(factories);
        this.serializerFactories = factories;

        MaximumPayloadBytes = checked((int)this.options.MaximumPayloadBytes); // for now hard-limit to 2GiB

        var defaultEntryOptions = this.options.DefaultEntryOptions;
        defaultFlags = defaultEntryOptions?.Flags ?? HybridCacheEntryFlags.None;
        defaultExpiration = defaultEntryOptions?.Expiration ?? TimeSpan.FromMinutes(5);
        defaultLocalCacheExpiration = defaultEntryOptions?.LocalCacheExpiration ?? TimeSpan.FromMinutes(1);

        defaultDistributedCacheExpiration = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = defaultExpiration };
    }

    internal IDistributedCache BackendCache => backendCache;
    internal IMemoryCache LocalCache => localCache;

    internal HybridCacheOptions Options => options;

    private bool BackendBuffers => (features & BackendFeatures.Buffers) != 0;

    public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> underlyingDataCallback, HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default)
    {
        bool canBeCanceled = token.CanBeCanceled;
        if (canBeCanceled)
        {
            token.ThrowIfCancellationRequested();
        }

        var flags = options?.Flags ?? defaultFlags;
        if ((flags & HybridCacheEntryFlags.DisableLocalCacheRead) == 0 && localCache.TryGetValue(key, out var untyped) && untyped is CacheItem<T> typed)
        {
            // short-circuit
            return new(typed.GetValue());
        }

        if (GetOrCreateStampede<TState, T>(key, flags, out var stampede, canBeCanceled))
        {
            // new query; we're responsible for making it happen
            if (canBeCanceled)
            {
                // *we* might cancel, but someone else might be depending on the result; start the
                // work independently, then we'll with join the outcome
                stampede.QueueUserWorkItem(in state, underlyingDataCallback, options);
            }
            else
            {
                // we're going to run to completion; no need to get complicated
                _ = stampede.ExecuteDirectAsync(in state, underlyingDataCallback, options); // this larger task includes L2 write etc
                return stampede.UnwrapAsync();
            }
        }

        return stampede.JoinAsync(token);
    }

    public override ValueTask RemoveKeyAsync(string key, CancellationToken token = default)
    {
        localCache.Remove(key);
        return new(backendCache.RemoveAsync(key, token));
    }

    public override ValueTask RemoveTagAsync(string tag, CancellationToken token = default)
        => default; // tags not yet implemented

    public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IReadOnlyCollection<string>? tags = null, CancellationToken token = default)
    {
        // since we're forcing a write: disable L1+L2 read; we'll use a direct pass-thru of the value as the callback, to reuse all the code;
        // note also that stampede token is not shared with anyone else
        var flags = (options?.Flags ?? defaultFlags) | (HybridCacheEntryFlags.DisableLocalCacheRead | HybridCacheEntryFlags.DisableDistributedCacheRead);
        var state = new StampedeState<T, T>(this, new StampedeKey(key, flags), token);
        return new(state.ExecuteDirectAsync(value, static (state, _) => new(state), options)); // note this spans L2 write etc
    }
}
