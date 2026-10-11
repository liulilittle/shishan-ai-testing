namespace Shishan.Domain.K005;

/// <summary>
/// Keeps every record the desk holds for the downstream side: the upstream
/// orders it was told about, the copies taken from them, the work orders that
/// follow, the printed paper, and the lines left behind for each step.
/// </summary>
public sealed class SnapshotStore
{
    private readonly object _guard = new();
    private readonly DerivationRules _rules = new();
    private readonly List<UpstreamOrder> _upstreams = new();
    private readonly List<SnapshotOrder> _snapshots = new();
    private readonly List<DownstreamOrder> _downstreams = new();
    private readonly List<SnapshotPrint> _prints = new();
    private readonly List<AuditEntry> _audits = new();
    private int _snapshotSeq;
    private int _orderSeq;
    private DateTime _clock = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Unspecified);

    /// <summary>Tells the desk about an upstream order that will drive the flow.</summary>
    public void Register(UpstreamOrder order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        lock (_guard)
        {
            _upstreams.Add(order);
        }
    }

    /// <summary>The upstream order filed under the given identifier, or null.</summary>
    public UpstreamOrder? FindUpstream(string id)
    {
        lock (_guard)
        {
            foreach (var order in _upstreams)
            {
                if (string.Equals(order.Id, id, StringComparison.Ordinal))
                {
                    return order;
                }
            }

            return null;
        }
    }

    /// <summary>The copy filed under the given identifier, or null.</summary>
    public SnapshotOrder? Locate(string id)
    {
        lock (_guard)
        {
            return LocateUnlocked(id);
        }
    }

    /// <summary>Takes a copy of the upstream order when none is filed yet.</summary>
    public SnapshotOrder Establish(UpstreamOrder upstream)
    {
        if (upstream is null)
        {
            throw new ArgumentNullException(nameof(upstream));
        }

        lock (_guard)
        {
            var existing = LocateUnlocked(upstream.Id);
            if (existing is not null)
            {
                return existing;
            }

            var snapshot = _rules.ComposeSnapshot(upstream.Id, NextSnapshotNumber(), upstream);
            _snapshots.Add(snapshot);
            _downstreams.Add(_rules.ComposeDownstream(NextOrderNumber(), upstream, snapshot));
            _prints.Add(_rules.ComposePrint(upstream, snapshot));
            return snapshot;
        }
    }

    /// <summary>Brings the copy filed under the identifier in line with the upstream content.</summary>
    public SnapshotOrder Refresh(string id, UpstreamOrder upstream)
    {
        if (upstream is null)
        {
            throw new ArgumentNullException(nameof(upstream));
        }

        lock (_guard)
        {
            var index = IndexOfUnlocked(id);
            if (index < 0)
            {
                throw new SnapshotException(SnapshotErrors.CopyMissing(id));
            }

            var current = _snapshots[index];
            var updated = _rules.ComposeRefreshed(upstream, current);
            _snapshots[index] = updated;

            var printIndex = PrintIndexOfUnlocked(id);
            if (printIndex >= 0)
            {
                _prints[printIndex] = _rules.ComposePrint(upstream, updated);
            }

            return updated;
        }
    }

    /// <summary>Retires the copy filed under the identifier together with what follows it.</summary>
    public SnapshotOrder Discard(string id)
    {
        lock (_guard)
        {
            var index = IndexOfUnlocked(id);
            if (index < 0)
            {
                throw new SnapshotException(SnapshotErrors.CopyMissing(id));
            }

            var snapshot = _snapshots[index];
            _snapshots.RemoveAt(index);

            for (var i = _downstreams.Count - 1; i >= 0; i--)
            {
                if (_downstreams[i].BelongsTo(id))
                {
                    _downstreams.RemoveAt(i);
                }
            }

            for (var i = _prints.Count - 1; i >= 0; i--)
            {
                if (_prints[i].BelongsTo(id))
                {
                    _prints.RemoveAt(i);
                }
            }

            return snapshot;
        }
    }

    /// <summary>Leaves a line behind for one step taken on a copy.</summary>
    public void Note(string snapshotId, string action, string operatorName)
    {
        lock (_guard)
        {
            _audits.Add(new AuditEntry(snapshotId, action, operatorName, NextMoment()));
        }
    }

    /// <summary>The paper printed for the given copy, or null.</summary>
    public SnapshotPrint? PrintOf(string id)
    {
        lock (_guard)
        {
            foreach (var print in _prints)
            {
                if (print.BelongsTo(id))
                {
                    return print;
                }
            }

            return null;
        }
    }

    /// <summary>The lines left behind for the given copy, in the order taken.</summary>
    public IReadOnlyList<AuditEntry> AuditsOf(string id)
    {
        lock (_guard)
        {
            return AuditEntries.For(_audits, id);
        }
    }

    /// <summary>Every copy the desk holds, in the order taken.</summary>
    public IReadOnlyList<SnapshotOrder> Snapshots()
    {
        lock (_guard)
        {
            return new List<SnapshotOrder>(_snapshots);
        }
    }

    /// <summary>Every work order the desk holds, in the order taken.</summary>
    public IReadOnlyList<DownstreamOrder> Downstreams()
    {
        lock (_guard)
        {
            return new List<DownstreamOrder>(_downstreams);
        }
    }

    /// <summary>Every print the desk holds, in the order taken.</summary>
    public IReadOnlyList<SnapshotPrint> Prints()
    {
        lock (_guard)
        {
            return new List<SnapshotPrint>(_prints);
        }
    }

    /// <summary>How many copies the desk holds.</summary>
    public int SnapshotCount()
    {
        lock (_guard)
        {
            return _snapshots.Count;
        }
    }

    /// <summary>How many work orders the desk holds.</summary>
    public int DownstreamCount()
    {
        lock (_guard)
        {
            return _downstreams.Count;
        }
    }

    /// <summary>True when the desk already holds a copy under the identifier.</summary>
    public bool Holds(string id)
    {
        lock (_guard)
        {
            return LocateUnlocked(id) is not null;
        }
    }

    private SnapshotOrder? LocateUnlocked(string id)
    {
        foreach (var snapshot in _snapshots)
        {
            if (string.Equals(snapshot.Id, id, StringComparison.Ordinal))
            {
                return snapshot;
            }
        }

        return null;
    }

    private int IndexOfUnlocked(string id)
    {
        for (var i = 0; i < _snapshots.Count; i++)
        {
            if (string.Equals(_snapshots[i].Id, id, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private int PrintIndexOfUnlocked(string id)
    {
        for (var i = 0; i < _prints.Count; i++)
        {
            if (_prints[i].BelongsTo(id))
            {
                return i;
            }
        }

        return -1;
    }

    private string NextSnapshotNumber() => $"SN{(++_snapshotSeq):D4}";

    private string NextOrderNumber() => $"DO{(++_orderSeq):D4}";

    private DateTime NextMoment()
    {
        _clock = _clock.AddTicks(1);
        return _clock;
    }
}