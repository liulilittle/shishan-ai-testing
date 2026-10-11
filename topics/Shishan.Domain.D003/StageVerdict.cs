namespace Shishan.Domain.D003;

/// <summary>
/// A decision left by one reviewer on one stage of one cycle. It keeps who
/// looked at the task, what they decided, and when the decision was left.
/// </summary>
public sealed record StageVerdict(AuditStage Stage, AuditResult Result, string Reviewer, DateTime Time)
{
    /// <summary>Creates a decision that lets a task through a stage.</summary>
    public static StageVerdict Pass(AuditStage stage, string reviewer) =>
        new(stage, AuditResult.Passed, reviewer, DateTime.UtcNow);

    /// <summary>Creates a decision that turns a task back at a stage.</summary>
    public static StageVerdict Reject(AuditStage stage, string reviewer) =>
        new(stage, AuditResult.Rejected, reviewer, DateTime.UtcNow);

    /// <summary>True when the decision lets the task through.</summary>
    public bool Passed => Result == AuditResult.Passed;

    /// <summary>True when the decision turns the task back.</summary>
    public bool Rejected => Result == AuditResult.Rejected;

    /// <summary>True when the decision was left on the given stage.</summary>
    public bool IsOn(AuditStage stage) => Stage == stage;

    /// <summary>True when the decision was left by the given reviewer.</summary>
    public bool LeftBy(string reviewer) =>
        string.Equals(Reviewer, reviewer, StringComparison.Ordinal);

    /// <summary>True when the decision was left before the given moment.</summary>
    public bool LeftBefore(DateTime moment) => Time < moment;

    /// <summary>The wording the desk uses when it shows the decision.</summary>
    public string Describe() =>
        Reviewer + " gave " + Result.Label() + " on the " + Stage.Label();

    /// <summary>Returns the same decision under a different reviewer name.</summary>
    public StageVerdict By(string reviewer) => new(Stage, Result, reviewer, Time);

    /// <summary>True when the decision lets the task continue past the stage.</summary>
    public bool LetsMoveOn => Result == AuditResult.Passed;

    /// <summary>True when the decision happened on the later of the two stages.</summary>
    public bool IsSecondStageDecision => Stage == AuditStage.Second;

    /// <summary>The wording the reviewer used, kept short for a printed line.</summary>
    public string ShortLine() => Result.Label() + " / " + Reviewer + " / " + Time.ToString("s");

    /// <summary>True when the decision is more recent than the given one.</summary>
    public bool MoreRecentThan(StageVerdict other) => Time > other.Time;
}