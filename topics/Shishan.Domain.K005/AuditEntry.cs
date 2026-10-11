namespace Shishan.Domain.K005;

/// <summary>
/// A line left behind for each step taken on a copy. Every line should carry
/// the party that took the step.
/// </summary>
public sealed record AuditEntry(string SnapshotId, string Action, string Operator, DateTime At)
{
    /// <summary>True when the line carries the party that took the step.</summary>
    public bool HasOperator() => !string.IsNullOrWhiteSpace(Operator);

    /// <summary>True when the line records the taking of a copy.</summary>
    public bool IsCreation() => string.Equals(Action, "CREATE", StringComparison.Ordinal);

    /// <summary>True when the line records a rebuild of a copy.</summary>
    public bool IsRebuild() => string.Equals(Action, "REBUILD", StringComparison.Ordinal);

    /// <summary>True when the line records the retiring of a copy.</summary>
    public bool IsVoid() => string.Equals(Action, "VOID", StringComparison.Ordinal);

    /// <summary>True when the line belongs to the given copy.</summary>
    public bool BelongsTo(string snapshotId) =>
        string.Equals(SnapshotId, snapshotId, StringComparison.Ordinal);

    /// <summary>True when the line was left at or before the given moment.</summary>
    public bool AtOrBefore(DateTime moment) => At <= moment;

    /// <summary>True when the line was left at or after the given moment.</summary>
    public bool AtOrAfter(DateTime moment) => At >= moment;

    /// <summary>A readable reading of the line.</summary>
    public string Describe() =>
        $"{(HasOperator() ? Operator : "?")} {Action} {SnapshotId} at {At:yyyy-MM-dd HH:mm:ss}";
}

/// <summary>Ways of reading a set of lines together.</summary>
public static class AuditEntries
{
    /// <summary>The lines belonging to the given copy, in the order given.</summary>
    public static IReadOnlyList<AuditEntry> For(IEnumerable<AuditEntry> entries, string snapshotId)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        var kept = new List<AuditEntry>();
        foreach (var entry in entries)
        {
            if (entry is not null && entry.BelongsTo(snapshotId))
            {
                kept.Add(entry);
            }
        }

        return kept;
    }

    /// <summary>True when every line carries the party that took the step.</summary>
    public static bool EveryHasOperator(IEnumerable<AuditEntry> entries)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        var any = false;
        foreach (var entry in entries)
        {
            if (entry is null)
            {
                continue;
            }

            any = true;
            if (!entry.HasOperator())
            {
                return false;
            }
        }

        return any;
    }

    /// <summary>True when the moments of the lines never step backwards.</summary>
    public static bool MomentsNeverStepBack(IEnumerable<AuditEntry> entries)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        DateTime? previous = null;
        foreach (var entry in entries)
        {
            if (entry is null)
            {
                continue;
            }

            if (previous is not null && entry.At < previous.Value)
            {
                return false;
            }

            previous = entry.At;
        }

        return true;
    }

    /// <summary>The newest line, or null when there is none.</summary>
    public static AuditEntry? Newest(IEnumerable<AuditEntry> entries)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        AuditEntry? newest = null;
        foreach (var entry in entries)
        {
            if (entry is null)
            {
                continue;
            }

            if (newest is null || entry.At > newest.At)
            {
                newest = entry;
            }
        }

        return newest;
    }

    /// <summary>How many lines belong to the given copy.</summary>
    public static int CountFor(IEnumerable<AuditEntry> entries, string snapshotId) =>
        For(entries, snapshotId).Count;

    /// <summary>A readable reading of every line, joined for a log entry.</summary>
    public static string DescribeAll(IEnumerable<AuditEntry> entries)
    {
        if (entries is null)
        {
            throw new ArgumentNullException(nameof(entries));
        }

        var parts = new List<string>();
        foreach (var entry in entries)
        {
            parts.Add(entry is null ? "?" : entry.Describe());
        }

        return string.Join(" | ", parts);
    }
}