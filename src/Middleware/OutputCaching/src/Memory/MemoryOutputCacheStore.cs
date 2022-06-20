// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.OutputCaching.Memory;

internal sealed class MemoryOutputCacheStore : IOutputCacheStore
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, HashSet<string>> _taggedEntries = new();

    internal MemoryOutputCacheStore(IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);

        _cache = cache;
    }

    public ValueTask EvictByTagAsync(string tag, CancellationToken token)
    {
        if (_taggedEntries.TryGetValue(tag, out var keys))
        {
            foreach (var key in keys)
            {
                _cache.Remove(key);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<byte[]?> GetAsync(string key, CancellationToken token)
    {
        var entry = _cache.Get(key) as byte[];
        return ValueTask.FromResult(entry);
    }

    /// <inheritdoc />
    public ValueTask SetAsync(string key, byte[] value, string[] tags, TimeSpan validFor, CancellationToken token)
    {
        if (tags != null)
        {
            foreach (var tag in tags)
            {
                var keys = _taggedEntries.GetOrAdd(tag, _ => new HashSet<string>());

                // Copy the list of tags to prevent locking

                var local = new HashSet<string>(keys)
                {
                    key
                };

                _taggedEntries[tag] = local;
            }
        }

        _cache.Set(
            key,
            value,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = validFor,
                Size = value.Length
            });

        return ValueTask.CompletedTask;
    }
}
