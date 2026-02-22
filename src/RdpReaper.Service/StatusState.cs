using System;
using System.Threading;

namespace RdpReaper.Service;

public sealed class StatusState
{
    private long _lastEventTicks;
    private int _activeBans;

    public void UpdateLastEvent(DateTimeOffset time)
    {
        Interlocked.Exchange(ref _lastEventTicks, time.UtcTicks);
    }

    public DateTimeOffset? GetLastEvent()
    {
        var ticks = Interlocked.Read(ref _lastEventTicks);
        return ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    public void SetActiveBans(int count)
    {
        Interlocked.Exchange(ref _activeBans, count);
    }

    public int GetActiveBans()
    {
        return Interlocked.CompareExchange(ref _activeBans, 0, 0);
    }
}
