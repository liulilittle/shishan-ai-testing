namespace Shishan.Domain.D003;

/// <summary>
/// The two doors a submitted task walks through before it is settled. Every
/// cycle opens with the first look and moves to the second look once the
/// first one has been finished.
/// </summary>
public enum AuditStage
{
    First = 1,
    Second = 2,
}

/// <summary>
/// Plain wording and small helpers for the stages above.
/// </summary>
public static class AuditStageExtensions
{
    /// <summary>True when the stage is the first look of a cycle.</summary>
    public static bool IsFirst(this AuditStage stage) => stage == AuditStage.First;

    /// <summary>True when the stage is the second look of a cycle.</summary>
    public static bool IsSecond(this AuditStage stage) => stage == AuditStage.Second;

    /// <summary>The wording the desk uses when it names the stage.</summary>
    public static string Label(this AuditStage stage) => stage switch
    {
        AuditStage.First => "first look",
        AuditStage.Second => "second look",
        _ => "no look",
    };

    /// <summary>The stage that follows this one inside a cycle.</summary>
    public static AuditStage Next(this AuditStage stage) => stage switch
    {
        AuditStage.First => AuditStage.Second,
        _ => AuditStage.Second,
    };

    /// <summary>True when one stage comes before the other in a cycle.</summary>
    public static bool ComesBefore(this AuditStage stage, AuditStage other) =>
        stage == AuditStage.First && other == AuditStage.Second;

    /// <summary>The stage a reviewer wrote, read from the wording they typed.</summary>
    public static AuditStage FromWording(string stage)
    {
        var clean = (stage ?? string.Empty).Trim();
        if (string.Equals(clean, "second", StringComparison.OrdinalIgnoreCase))
        {
            return AuditStage.Second;
        }

        if (string.Equals(clean, "2", StringComparison.Ordinal))
        {
            return AuditStage.Second;
        }

        return AuditStage.First;
    }
}

/// <summary>
/// The names the reviewers type for the stages, kept here so every part of
/// the desk writes them the same way when it records a look.
/// </summary>
public static class AuditStageCatalog
{
    /// <summary>Cleans the wording a reviewer typed for a stage.</summary>
    public static string NormalizeStage(string stage) =>
        (stage ?? string.Empty).Trim();

    /// <summary>Every wording the desk accepts for a stage.</summary>
    public static IReadOnlyList<string> AcceptedWording() =>
        new[] { "first", "second", "1", "2" };

    /// <summary>True when the stage is the first look.</summary>
    public static bool IsFirstLook(AuditStage stage) => stage == AuditStage.First;

    /// <summary>True when the stage is the second look.</summary>
    public static bool IsSecondLook(AuditStage stage) => stage == AuditStage.Second;

    /// <summary>The position a stage takes in a round, first before second.</summary>
    public static int PositionOf(AuditStage stage) => (int)stage;

    /// <summary>True when the first stage has been worked and the second waits.</summary>
    public static bool SecondWaitsForFirst(AuditStage stage) => stage == AuditStage.Second;

    /// <summary>True when the wording names one of the two stages.</summary>
    public static bool Recognizes(string stage)
    {
        var clean = NormalizeStage(stage);
        if (string.IsNullOrEmpty(clean))
        {
            return false;
        }

        foreach (var wording in AcceptedWording())
        {
            if (string.Equals(clean, wording, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
