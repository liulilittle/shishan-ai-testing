namespace Shishan.Domain.D003;

/// <summary>
/// The view of a document's latest round shown on the review desk. It carries
/// the stage the round is waiting on and the decisions already left, so a
/// reviewer can see at a glance how the task is doing.
/// </summary>
public sealed record ReviewDetail(
    string DocumentId,
    int CycleNumber,
    AuditStage CurrentStage,
    AuditResult? FirstStageResult,
    AuditResult? SecondStageResult,
    string? FirstReviewer,
    string? SecondReviewer)
{
    /// <summary>Builds the view of a round from the round itself.</summary>
    public static ReviewDetail From(AuditDocument document, ReviewCycle cycle)
    {
        if (document == null || cycle == null)
        {
            throw new ArgumentNullException(cycle == null ? nameof(cycle) : nameof(document));
        }

        return new ReviewDetail(
            document.Id,
            cycle.Number,
            cycle.CurrentStage,
            cycle.FirstVerdict?.Result,
            cycle.SecondVerdict?.Result,
            cycle.FirstVerdict?.Reviewer,
            cycle.SecondVerdict?.Reviewer);
    }

    /// <summary>True when the view belongs to the given document.</summary>
    public bool BelongsTo(string documentId) =>
        string.Equals(DocumentId, documentId, StringComparison.Ordinal);

    /// <summary>True when the round still waits on the given stage.</summary>
    public bool WaitingOn(AuditStage stage) => stage == AuditStage.First
        ? FirstStageResult == null
        : SecondStageResult == null;

    /// <summary>The result left on the given stage, if any.</summary>
    public AuditResult? ResultOn(AuditStage stage) => stage == AuditStage.First
        ? FirstStageResult
        : SecondStageResult;

    /// <summary>True when both stages have a decision.</summary>
    public bool Completed => FirstStageResult != null && SecondStageResult != null;

    /// <summary>The wording the desk uses when it shows the view.</summary>
    public string Describe() =>
        DocumentId + " / round " + CycleNumber + " / " + CurrentStage.Label();

    /// <summary>True when both stages let the task through.</summary>
    public bool PassedEverything =>
        FirstStageResult == AuditResult.Passed && SecondStageResult == AuditResult.Passed;

    /// <summary>True when any stage turned the task back.</summary>
    public bool TurnedBackAnywhere =>
        FirstStageResult == AuditResult.Rejected || SecondStageResult == AuditResult.Rejected;

    /// <summary>The stage with a pending decision, if any.</summary>
    public AuditStage? PendingStage()
    {
        if (FirstStageResult == null)
        {
            return AuditStage.First;
        }

        if (SecondStageResult == null)
        {
            return AuditStage.Second;
        }

        return null;
    }

    /// <summary>True when the latest round number is the one given.</summary>
    public bool IsRound(int cycleNumber) => CycleNumber == cycleNumber;
}