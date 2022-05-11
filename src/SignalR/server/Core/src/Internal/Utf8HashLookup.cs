// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.AspNetCore.SignalR.Internal;

/// <summary>
/// A small dictionary optimized for utf8 string lookup via spans. Adapted from https://github.com/dotnet/runtime/blob/4ed596ef63e60ce54cfb41d55928f0fe45f65cf3/src/libraries/System.Linq.Parallel/src/System/Linq/Parallel/Utils/HashLookup.cs.
/// </summary>
internal sealed class Utf8HashLookup
{
    private int[] _buckets;
    private Slot[] _slots;
    private int _count;

    private const int HashCodeMask = 0x7fffffff;

    internal Utf8HashLookup()
    {
        _buckets = new int[7];
        _slots = new Slot[7];
    }

    internal void Add(string value)
    {
        var hashCode = GetKeyHashCode(value.AsSpan());

        if (_count == _slots.Length)
        {
            Resize();
        }

        int index = _count;
        _count++;

        int bucket = hashCode % _buckets.Length;
        _slots[index].hashCode = hashCode;
        _slots[index].value = value;
        _slots[index].next = _buckets[bucket] - 1;
        _buckets[bucket] = index + 1;
    }

    internal bool TryGetValue(ReadOnlySpan<byte> utf8, [MaybeNullWhen(false), AllowNull] out string value)
    {
        const int StackAllocThreshold = 128;

        // Transcode to utf16 for comparison
        char[]? pooled = null;
        var count = Encoding.UTF8.GetCharCount(utf8);
        var chars = count <= StackAllocThreshold ?
            stackalloc char[StackAllocThreshold] :
            (pooled = ArrayPool<char>.Shared.Rent(count));
        var encoded = Encoding.UTF8.GetChars(utf8, chars);
        var hasValue = TryGetValue(chars[..encoded], out value);
        if (pooled is not null)
        {
            ArrayPool<char>.Shared.Return(pooled);
        }

        return hasValue;
    }

    private bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false), AllowNull] out string value)
    {
        var hashCode = GetKeyHashCode(key);

        for (var i = _buckets[hashCode % _buckets.Length] - 1; i >= 0; i = _slots[i].next)
        {
            if (_slots[i].hashCode == hashCode && key.Equals(_slots[i].value, StringComparison.OrdinalIgnoreCase))
            {
                value = _slots[i].value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static int GetKeyHashCode(ReadOnlySpan<char> key)
    {
        return HashCodeMask & string.GetHashCode(key, StringComparison.OrdinalIgnoreCase);
    }

    private void Resize()
    {
        var newSize = checked(_count * 2 + 1);
        var newBuckets = new int[newSize];
        var newSlots = new Slot[newSize];
        Array.Copy(_slots, newSlots, _count);
        for (int i = 0; i < _count; i++)
        {
            int bucket = newSlots[i].hashCode % newSize;
            newSlots[i].next = newBuckets[bucket] - 1;
            newBuckets[bucket] = i + 1;
        }
        _buckets = newBuckets;
        _slots = newSlots;
    }

    private struct Slot
    {
        internal int hashCode;
        internal int next;
        internal string value;
    }
}
