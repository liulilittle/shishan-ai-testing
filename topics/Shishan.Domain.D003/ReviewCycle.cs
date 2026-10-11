namespace Shishan.Domain.D003;

/// <summary>
/// One round of review for a document. Each round walks the task through the
/// two stages, and either stage can let the task through or turn it back. A
/// round is complete once both stages carry a decision.
/// </summary>
public sealed class ReviewCycle
{
    private StageVerdict? _first;
    private StageVerdict? _second;

    public ReviewCycle(int number)
    {
        Number = number;
    }

    /// <summary>Which round of review this is for the document.</summary>
    public int Number { get; }

    /// <summary>The decision left on the first stage, if one has been left.</summary>
    public StageVerdict? FirstVerdict => _first;

    /// <summary>The decision left on the second stage, if one has been left.</summary>
    public StageVerdict? SecondVerdict => _second;

    /// <summary>The stage the round is waiting on right now.</summary>
    public AuditStage CurrentStage =>
        _first == null ? AuditStage.First : AuditStage.Second;

    /// <summary>True when both stages carry a decision.</summary>
    public bool IsComplete => _second != null;

    /// <summary>True when the round still waits on at least one stage.</summary>
    public bool IsOpen => !IsComplete;

    /// <summary>True when the round was turned back at any stage.</summary>
    public bool TurnedBack =>
        (_first != null && _first.Rejected) || (_second != null && _second.Rejected);

    /// <summary>True when the round lets the task through both stages.</summary>
    public bool PassedThrough =>
        _first != null && _first.Passed && _second != null && _second.Passed;

    /// <summary>The decision left on the given stage, if one has been left.</summary>
    public StageVerdict? VerdictOn(AuditStage stage) =>
        stage == AuditStage.First ? _first : _second;

    /// <summary>True when a decision has been left on the given stage.</summary>
    public bool HasVerdict(AuditStage stage) => VerdictOn(stage) is not null;

    /// <summary>
    /// True when the stage is the one the round is waiting on and no decision
    /// has been left there yet.
    /// </summary>
    public bool IsActionDue(AuditStage stage) =>
        CurrentStage == stage && !HasVerdict(stage);

    /// <summary>Leaves a decision on the given stage of this round.</summary>
    public void Record(AuditStage stage, StageVerdict verdict)
    {
        if (stage == AuditStage.First)
        {
            _first = verdict;
        }
        else
        {
            _second = verdict;
        }
    }

    /// <summary>Clears any decision already left on the given stage.</summary>
    public void Clear(AuditStage stage)
    {
        if (stage == AuditStage.First)
        {
            _first = null;
        }
        else
        {
            _second = null;
        }
    }

    /// <summary>The wording the desk uses when it names the round.</summary>
    public string Describe() => "round " + Number + " / " + CurrentStage.Label();

    /// <summary>True when the round follows the given earlier round.</summary>
    public bool Follows(ReviewCycle earlier) => earlier is null || Number == earlier.Number + 1;

    /// <summary>The reviewer who left the first decision, if one has been left.</summary>
    public string? FirstReviewer => _first?.Reviewer;

    /// <summary>The reviewer who left the second decision, if one has been left.</summary>
    public string? SecondReviewer => _second?.Reviewer;

    /// <summary>The result left on the first stage, if one has been left.</summary>
    public AuditResult? FirstResult => _first?.Result;

    /// <summary>The result left on the second stage, if one has been left.</summary>
    public AuditResult? SecondResult => _second?.Result;

    /// <summary>True when the round waits on a second stage after a pass.</summary>
    public bool SecondStageDue =>
        _first != null && _first.Passed && _second == null;

    /// <summary>True when the first stage turned the task back.</summary>
    public bool RejectedAtFirst => _first != null && _first.Rejected;

    /// <summary>True when the second stage turned the task back.</summary>
    public bool RejectedAtSecond => _second != null && _second.Rejected;

    /// <summary>The stage the round was waiting on after the first decision.</summary>
    public AuditStage StageAfterFirst =>
        _first == null ? AuditStage.First : AuditStage.Second;
}