namespace Shishan.Domain.K004;

/// <summary>
/// The outcome carried by a segment decision.
/// </summary>
public enum StageResult
{
    /// <summary>No decision has been recorded for the segment.</summary>
    Pending = 0,

    /// <summary>The segment has been approved.</summary>
    Approved = 1,

    /// <summary>The segment has been sent back.</summary>
    Rejected = 2,
}

/// <summary>Readable helpers for a segment result value.</summary>
public static class StageResultExtensions
{
    /// <summary>True when a decision has been recorded.</summary>
    public static bool IsDecided(this StageResult result) => result != StageResult.Pending;

    /// <summary>True when the result is an approval.</summary>
    public static bool IsApproval(this StageResult result) => result == StageResult.Approved;

    /// <summary>True when the result is a rejection.</summary>
    public static bool IsRejection(this StageResult result) => result == StageResult.Rejected;

    /// <summary>The readable name of the result.</summary>
    public static string Readable(this StageResult result)
    {
        switch (result)
        {
            case StageResult.Approved:
                return "Approved";
            case StageResult.Rejected:
                return "Rejected";
            default:
                return "Pending";
        }
    }
}