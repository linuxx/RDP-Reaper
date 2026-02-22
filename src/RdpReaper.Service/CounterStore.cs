using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RdpReaper.Service;

public sealed class CounterStore
{
    private sealed class CounterBucket
    {
        public readonly Queue<DateTimeOffset> Timestamps = new();
        public readonly object Sync = new();
    }

    private readonly ConcurrentDictionary<string, CounterBucket> _buckets = new(StringComparer.OrdinalIgnoreCase);

    public int AddFailure(string key, DateTimeOffset time, TimeSpan window)
    {
        var bucket = _buckets.GetOrAdd(key, _ => new CounterBucket());
        var cutoff = time - window;

        lock (bucket.Sync)
        {
            bucket.Timestamps.Enqueue(time);
            while (bucket.Timestamps.Count > 0 && bucket.Timestamps.Peek() < cutoff)
            {
                bucket.Timestamps.Dequeue();
            }

            return bucket.Timestamps.Count;
        }
    }
}
