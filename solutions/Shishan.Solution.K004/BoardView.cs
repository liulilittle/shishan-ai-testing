namespace Shishan.Domain.K004;

/// <summary>
/// The board entry the desk shows for one document: its standing, the segment
/// the walk currently stands on, and the newest result of every segment.
/// </summary>
public sealed record BoardView(
    string Id,
    DocumentStatus Status,
    ApprovalStage CurrentStage,
    IReadOnlyDictionary<ApprovalStage, StageResult> StageResults,
    DateTime? LatestStampTime)
{
    /// <summary>The newest result of a segment, or Pending when none is shown.</summary>
    public StageResult ResultFor(ApprovalStage stage) =>
        StageResults is not null && StageResults.TryGetValue(stage, out var result)
            ? result
            : StageResult.Pending;

    /// <summary>True when the board shows the segment as approved.</summary>
    public bool IsApproved(ApprovalStage stage) => ResultFor(stage) == StageResult.Approved;

    /// <summary>True when the board shows the segment as rejected.</summary>
    public bool IsRejected(ApprovalStage stage) => ResultFor(stage) == StageResult.Rejected;

    /// <summary>The number of segments the board shows as approved.</summary>
    public int ApprovedCount()
    {
        var count = 0;
        foreach (var stage in StageDirectory.Ordered)
        {
            if (IsApproved(stage))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>True when the board shows the document as retired from review.</summary>
    public bool IsRetired() => Status == DocumentStatus.Voided;

    /// <summary>True when the board shows the document as fully approved.</summary>
    public bool IsFullyApproved() => Status == DocumentStatus.Approved;

    /// <summary>Every segment the board shows as pending, in walk order.</summary>
    public IReadOnlyList<ApprovalStage> PendingSegments()
    {
        var pending = new List<ApprovalStage>();
        foreach (var stage in StageDirectory.Ordered)
        {
            if (ResultFor(stage) == StageResult.Pending)
            {
                pending.Add(stage);
            }
        }

        return pending;
    }

    /// <summary>Every segment the board shows as approved, in walk order.</summary>
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

    /// <summary>Every segment the board shows as rejected, in walk order.</summary>
    public IReadOnlyList<ApprovalStage> RejectedSegments()
    {
        var rejected = new List<ApprovalStage>();
        foreach (var stage in StageDirectory.Ordered)
        {
            if (IsRejected(stage))
            {
                rejected.Add(stage);
            }
        }

        return rejected;
    }

    /// <summary>The readable name of the segment the walk stands on.</summary>
    public string CurrentName() => StageDirectory.NameOf(CurrentStage);

    /// <summary>A one-line reading of the board entry.</summary>
    public string Describe() =>
        $"{Id}: {StageDirectory.NameOf(CurrentStage)} ({ApprovedCount()} of {StageDirectory.Count} approved)";
}