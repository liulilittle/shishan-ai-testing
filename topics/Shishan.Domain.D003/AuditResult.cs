namespace Shishan.Domain.D003;

/// <summary>
/// What a reviewer can say about a task at one stage of a cycle. A task is
/// either let through, turned back, or still waits for a decision.
/// </summary>
public enum AuditResult
{
    Pending = 0,
    Passed = 1,
    Rejected = 2,
}

/// <summary>
/// Plain wording and small helpers for the results above.
/// </summary>
public static class AuditResultExtensions
{
    /// <summary>True when the result lets the task through.</summary>
    public static bool IsPass(this AuditResult result) => result == AuditResult.Passed;

    /// <summary>True when the result turns the task back.</summary>
    public static bool IsReject(this AuditResult result) => result == AuditResult.Rejected;

    /// <summary>True when a decision has been reached rather than left pending.</summary>
    public static bool IsDecision(this AuditResult result) => result != AuditResult.Pending;

    /// <summary>The wording the desk uses when it names the result.</summary>
    public static string Label(this AuditResult result) => result switch
    {
        AuditResult.Passed => "passed",
        AuditResult.Rejected => "rejected",
        _ => "pending",
    };

    /// <summary>The result a reviewer typed, read from their wording.</summary>
    public static AuditResult FromWording(string result)
    {
        var clean = (result ?? string.Empty).Trim();
        if (string.Equals(clean, "pass", StringComparison.OrdinalIgnoreCase))
        {
            return AuditResult.Passed;
        }

        if (string.Equals(clean, "reject", StringComparison.OrdinalIgnoreCase))
        {
            return AuditResult.Rejected;
        }

        return AuditResult.Pending;
    }

    /// <summary>The opposite of a pass when a task is turned back.</summary>
    public static AuditResult Invert(this AuditResult result) => result switch
    {
        AuditResult.Passed => AuditResult.Rejected,
        AuditResult.Rejected => AuditResult.Passed,
        _ => AuditResult.Pending,
    };

    /// <summary>True when the result matches the given decision wording.</summary>
    public static bool Says(this AuditResult result, string wording) =>
        string.Equals(result.Label(), wording ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the result came from the given reviewer's desk.</summary>
    public static bool IsUsableIn(this AuditResult result, StageVerdict verdict) =>
        verdict != null && verdict.Result == result;

    /// <summary>True when the result is the one that lets a task move on.</summary>
    public static bool LetsThrough(this AuditResult result) => result == AuditResult.Passed;
}
