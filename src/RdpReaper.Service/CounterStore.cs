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
    private readonly ConcurrentDictionary<string, SubnetBucket> _subnetBuckets = new(StringComparer.OrdinalIgnoreCase);

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

    public (int total, int unique) AddSubnetFailure(string subnet, string ip, DateTimeOffset time, TimeSpan window)
    {
        var bucket = _subnetBuckets.GetOrAdd(subnet, _ => new SubnetBucket());
        var cutoff = time - window;

        lock (bucket.Sync)
        {
            bucket.Events.Enqueue(new SubnetEvent(time, ip));
            while (bucket.Events.Count > 0 && bucket.Events.Peek().Time < cutoff)
            {
                bucket.Events.Dequeue();
            }

            bucket.UniqueIps.Clear();
            foreach (var entry in bucket.Events)
            {
                bucket.UniqueIps.Add(entry.Ip);
            }

            return (bucket.Events.Count, bucket.UniqueIps.Count);
        }
    }

    private sealed record SubnetEvent(DateTimeOffset Time, string Ip);

    private sealed class SubnetBucket
    {
        public readonly Queue<SubnetEvent> Events = new();
        public readonly HashSet<string> UniqueIps = new(StringComparer.OrdinalIgnoreCase);
        public readonly object Sync = new();
    }
}
