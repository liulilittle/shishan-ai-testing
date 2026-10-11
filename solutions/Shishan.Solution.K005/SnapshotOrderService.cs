namespace Shishan.Domain.K005;

/// <summary>
/// The public desk for the downstream side. It is told about upstream orders,
/// takes copies from them, brings those copies back in line, serves the printed
/// paper and the lines left behind, and can retire a copy.
/// </summary>
public sealed class SnapshotOrderService
{
    private readonly SnapshotStore _store = new();

    /// <summary>Tells the desk about an upstream order that will drive the flow.</summary>
    public void Register(UpstreamOrder order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        _store.Register(order);
    }

    /// <summary>Takes the copy for a request when the upstream order completes.</summary>
    public SnapshotOrder CreateOrder(string upstreamOrderId)
    {
        if (string.IsNullOrWhiteSpace(upstreamOrderId))
        {
            throw new SnapshotException(SnapshotErrors.IdRequired());
        }

        var upstream = RequireUpstream(upstreamOrderId);
        var snapshot = _store.Establish(upstream);
        _store.Note(upstreamOrderId, "CREATE", upstream.RequestedBy);
        return snapshot;
    }

    /// <summary>Rebuilds the copy for a request from the reconciliation side.</summary>
    public SnapshotOrder Rebuild(string snapshotId)
    {
        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            throw new SnapshotException(SnapshotErrors.IdRequired());
        }

        var upstream = RequireUpstream(snapshotId);
        var snapshot = _store.Establish(upstream);
        _store.Note(snapshotId, "REBUILD", upstream.RequestedBy);
        return snapshot;
    }

    /// <summary>Brings the copy for a request in line with the current upstream content.</summary>
    public SnapshotOrder Refresh(string snapshotId)
    {
        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            throw new SnapshotException(SnapshotErrors.IdRequired());
        }

        var upstream = RequireUpstream(snapshotId);
        if (_store.Locate(snapshotId) is null)
        {
            throw new SnapshotException(SnapshotErrors.CopyMissing(snapshotId));
        }

        return _store.Refresh(snapshotId, upstream);
    }

    /// <summary>Retires the copy for a request together with what follows it.</summary>
    public SnapshotOrder Void(string snapshotId)
    {
        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            throw new SnapshotException(SnapshotErrors.IdRequired());
        }

        var upstream = RequireUpstream(snapshotId);
        var snapshot = _store.Discard(snapshotId);
        _store.Note(snapshotId, "VOID", upstream.RequestedBy);
        return snapshot;
    }

    /// <summary>The paper printed for the given copy, or null when there is none.</summary>
    public SnapshotPrint? GetPrint(string snapshotId)
    {
        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            return null;
        }

        return _store.PrintOf(snapshotId);
    }

    /// <summary>The lines left behind for the given copy, in the order taken.</summary>
    public IReadOnlyList<AuditEntry> GetAuditLog(string snapshotId)
    {
        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            return Array.Empty<AuditEntry>();
        }

        return _store.AuditsOf(snapshotId);
    }

    /// <summary>The copy for the given request, or null when there is none.</summary>
    public SnapshotOrder? FindSnapshot(string snapshotId)
    {
        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            return null;
        }

        return _store.Locate(snapshotId);
    }

    /// <summary>Every copy the desk holds, in the order taken.</summary>
    public IReadOnlyList<SnapshotOrder> ListSnapshots() => _store.Snapshots();

    /// <summary>Every work order the desk holds, in the order taken.</summary>
    public IReadOnlyList<DownstreamOrder> ListDownstreamOrders() => _store.Downstreams();

    /// <summary>Every print the desk holds, in the order taken.</summary>
    public IReadOnlyList<SnapshotPrint> ListPrints() => _store.Prints();

    /// <summary>How many copies the desk holds.</summary>
    public int CopyCount() => _store.SnapshotCount();

    private UpstreamOrder RequireUpstream(string id)
    {
        var upstream = _store.FindUpstream(id);
        if (upstream is null)
        {
            throw new SnapshotException(SnapshotErrors.UpstreamMissing(id));
        }

        return upstream;
    }
}