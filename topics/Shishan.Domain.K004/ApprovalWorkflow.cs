namespace Shishan.Domain.K004;

/// <summary>
/// The main record of one approval document. It holds the decisions recorded
/// against each segment and the standing of the document as a whole.
/// </summary>
public sealed class ApprovalWorkflow
{
    private readonly List<StageStamp> _stamps = new();

    /// <summary>The identifier the desk knows the document by.</summary>
    public string Id { get; }

    /// <summary>The standing of the document as a whole.</summary>
    public DocumentStatus Status { get; internal set; } = DocumentStatus.InProgress;

    /// <summary>A counter that moves forward whenever the record changes.</summary>
    internal int Revision { get; private set; }

    /// <summary>Opens a new document under the given identifier.</summary>
    public ApprovalWorkflow(string id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    /// <summary>Every decision recorded on the document, oldest first.</summary>
    public IReadOnlyList<StageStamp> Stamps
    {
        get
        {
            lock (_stamps)
            {
                return new List<StageStamp>(_stamps);
            }
        }
    }

    /// <summary>How many decisions the document currently carries.</summary>
    public int StampCount
    {
        get
        {
            lock (_stamps)
            {
                return _stamps.Count;
            }
        }
    }

    /// <summary>The newest result recorded for a segment, or Pending when none.</summary>
    public StageResult ResultOf(ApprovalStage stage)
    {
        lock (_stamps)
        {
            for (var i = _stamps.Count - 1; i >= 0; i--)
            {
                if (_stamps[i].Stage == stage)
                {
                    return _stamps[i].Result;
                }
            }

            return StageResult.Pending;
        }
    }

    /// <summary>True when the newest decision on a segment is an approval.</summary>
    public bool IsApproved(ApprovalStage stage) => ResultOf(stage) == StageResult.Approved;

    /// <summary>The newest decision recorded on the document, if any.</summary>
    public StageStamp? LatestStamp()
    {
        lock (_stamps)
        {
            return _stamps.Count == 0 ? null : _stamps[^1];
        }
    }

    /// <summary>Every segment that currently carries an approval.</summary>
    public IReadOnlyList<ApprovalStage> ApprovedSegments()
    {
        var approved = new List<ApprovalStage>();
        foreach (var stage in StageDirectory.Ordered)
        {
            if (IsApproved(stage))
            {
                approved.Add(stage);
            }
        }

        return approved;
    }

    /// <summary>Every segment whose newest decision is a rejection.</summary>
    public IReadOnlyList<ApprovalStage> RejectedSegments()
    {
        var rejected = new List<ApprovalStage>();
        foreach (var stage in StageDirectory.Ordered)
        {
            if (ResultOf(stage) == StageResult.Rejected)
            {
                rejected.Add(stage);
            }
        }

        return rejected;
    }

    /// <summary>How many segments currently carry an approval.</summary>
    public int ApprovalCount() => ApprovedSegments().Count;

    /// <summary>True when every segment carries an approval.</summary>
    public bool IsComplete()
    {
        foreach (var stage in StageDirectory.Ordered)
        {
            if (!IsApproved(stage))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Seals a decision onto the document.</summary>
    internal void Record(StageStamp stamp)
    {
        if (stamp is null)
        {
            throw new ArgumentNullException(nameof(stamp));
        }

        lock (_stamps)
        {
            _stamps.Add(stamp);
            Revision = Revision + 1;
        }
    }

    /// <summary>Sends the given segments back and drops their decisions.</summary>
    internal int RemoveSegments(IEnumerable<ApprovalStage> segments)
    {
        if (segments is null)
        {
            throw new ArgumentNullException(nameof(segments));
        }

        lock (_stamps)
        {
            var dropped = _stamps.RemoveAll(stamp => Contains(segments, stamp.Stage));
            if (dropped > 0)
            {
                Revision = Revision + 1;
            }

            return dropped;
        }
    }

    /// <summary>Moves the standing of the document to the given value.</summary>
    internal void SetStatus(DocumentStatus status)
    {
        lock (_stamps)
        {
            if (Status != status)
            {
                Status = status;
                Revision = Revision + 1;
            }
        }
    }

    private static bool Contains(IEnumerable<ApprovalStage> segments, ApprovalStage stage)
    {
        foreach (var candidate in segments)
        {
            if (candidate == stage)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True when at least one decision has been recorded for the segment.</summary>
    public bool HasDecisionFor(ApprovalStage stage) => ResultOf(stage) != StageResult.Pending;

    /// <summary>How many segments still carry no decision.</summary>
    public int UndecidedCount()
    {
        var count = 0;
        foreach (var stage in StageDirectory.Ordered)
        {
            if (!HasDecisionFor(stage))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>The newest result of every segment, keyed by segment.</summary>
    public IReadOnlyDictionary<ApprovalStage, StageResult> ResultMap()
    {
        var results = new Dictionary<ApprovalStage, StageResult>();
        foreach (var stage in StageDirectory.Ordered)
        {
            results[stage] = ResultOf(stage);
        }

        return results;
    }

    /// <summary>A one-line reading of the document record.</summary>
    public string Describe() =>
        $"{Id}: {Status} ({ApprovalCount()} of {StageDirectory.Count} approved)";
}