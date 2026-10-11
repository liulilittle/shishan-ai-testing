namespace Shishan.Domain.K004;

/// <summary>
/// One decision sealed against a document segment.
/// </summary>
public sealed record StageStamp(
    ApprovalStage Stage,
    StageResult Result,
    string Operator,
    string Note,
    DateTime RecordedAt)
{
    /// <summary>An approval decision on the given segment.</summary>
    public static StageStamp Approval(ApprovalStage stage, string actor) =>
        new(stage, StageResult.Approved, actor ?? string.Empty, string.Empty, DateTime.UtcNow);

    /// <summary>A rejection decision on the given segment.</summary>
    public static StageStamp Rejection(ApprovalStage stage, string note) =>
        new(stage, StageResult.Rejected, string.Empty, note ?? string.Empty, DateTime.UtcNow);

    /// <summary>A decision that carries no outcome yet.</summary>
    public static StageStamp Pending(ApprovalStage stage) =>
        new(stage, StageResult.Pending, string.Empty, string.Empty, DateTime.UtcNow);

    /// <summary>True when the decision is an approval.</summary>
    public bool IsApproval() => Result == StageResult.Approved;

    /// <summary>True when the decision is a rejection.</summary>
    public bool IsRejection() => Result == StageResult.Rejected;

    /// <summary>True when the decision carries an outcome.</summary>
    public bool IsDecided() => Result != StageResult.Pending;

    /// <summary>The actor shown on the decision, or the desk when none is named.</summary>
    public string ActorOrDesk() => string.IsNullOrEmpty(Operator) ? "Desk" : Operator;

    /// <summary>Short readable form of the decision, used by desks and logs.</summary>
    public string Describe()
    {
        switch (Result)
        {
            case StageResult.Approved when !string.IsNullOrEmpty(Operator):
                return $"{Stage} approved by {Operator}";
            case StageResult.Approved:
                return $"{Stage} approved";
            case StageResult.Rejected when !string.IsNullOrEmpty(Note):
                return $"{Stage} rejected: {Note}";
            case StageResult.Rejected:
                return $"{Stage} rejected";
            default:
                return $"{Stage} pending";
        }
    }
}