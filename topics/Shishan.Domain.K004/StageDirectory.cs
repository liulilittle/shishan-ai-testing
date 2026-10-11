namespace Shishan.Domain.K004;

/// <summary>
/// The fixed order of document segments and the small lookups the desk needs
/// when it lists, labels or walks them.
/// </summary>
public static class StageDirectory
{
    private static readonly ApprovalStage[] Sequence =
    {
        ApprovalStage.Prepare,
        ApprovalStage.BranchA,
        ApprovalStage.BranchB,
        ApprovalStage.Final,
    };

    private static readonly Dictionary<ApprovalStage, string> Names = new()
    {
        [ApprovalStage.Prepare] = "Prepare",
        [ApprovalStage.BranchA] = "Branch A",
        [ApprovalStage.BranchB] = "Branch B",
        [ApprovalStage.Final] = "Final",
    };

    /// <summary>Every segment in the order the desk walks them.</summary>
    public static IReadOnlyList<ApprovalStage> Ordered => Sequence;

    /// <summary>The opening segment.</summary>
    public static ApprovalStage First => ApprovalStage.Prepare;

    /// <summary>The closing segment.</summary>
    public static ApprovalStage Last => ApprovalStage.Final;

    /// <summary>True when the value is one of the four known segments.</summary>
    public static bool IsKnown(ApprovalStage stage) => Names.ContainsKey(stage);

    /// <summary>The position of a segment in the walk, counting from one.</summary>
    public static int PositionOf(ApprovalStage stage)
    {
        for (var i = 0; i < Sequence.Length; i++)
        {
            if (Sequence[i] == stage)
            {
                return i + 1;
            }
        }

        return 0;
    }

    /// <summary>
    /// The segment that follows the given one, or the closing segment when the
    /// given one is last.
    /// </summary>
    public static ApprovalStage FollowingOf(ApprovalStage stage)
    {
        var position = PositionOf(stage);
        if (position <= 0 || position >= Sequence.Length)
        {
            return Last;
        }

        return Sequence[position];
    }

    /// <summary>The readable name of a segment.</summary>
    public static string NameOf(ApprovalStage stage) =>
        Names.TryGetValue(stage, out var name) ? name : stage.ToString();

    /// <summary>
    /// The first segment that has not been approved yet. When every segment is
    /// approved the closing segment is returned.
    /// </summary>
    public static ApprovalStage CurrentOf(Func<ApprovalStage, StageResult> resultOf)
    {
        if (resultOf is null)
        {
            throw new ArgumentNullException(nameof(resultOf));
        }

        foreach (var stage in Sequence)
        {
            if (resultOf(stage) != StageResult.Approved)
            {
                return stage;
            }
        }

        return Last;
    }

    /// <summary>Every segment from the given one to the closing segment.</summary>
    public static IReadOnlyList<ApprovalStage> From(ApprovalStage stage)
    {
        var segments = new List<ApprovalStage>();
        var position = PositionOf(stage);
        if (position <= 0)
        {
            return segments;
        }

        for (var i = position - 1; i < Sequence.Length; i++)
        {
            segments.Add(Sequence[i]);
        }

        return segments;
    }

    /// <summary>The segment that comes before the given one, or the opening segment.</summary>
    public static ApprovalStage PreviousOf(ApprovalStage stage)
    {
        var position = PositionOf(stage);
        if (position <= 1)
        {
            return First;
        }

        return Sequence[position - 2];
    }

    /// <summary>True when the first segment comes earlier in the walk than the second.</summary>
    public static bool IsBefore(ApprovalStage left, ApprovalStage right) =>
        PositionOf(left) < PositionOf(right);

    /// <summary>True when the first segment comes later in the walk than the second.</summary>
    public static bool IsAfter(ApprovalStage left, ApprovalStage right) =>
        PositionOf(left) > PositionOf(right);

    /// <summary>Every segment between the two given ones, ends included, in walk order.</summary>
    public static IReadOnlyList<ApprovalStage> Between(ApprovalStage from, ApprovalStage to)
    {
        var segments = new List<ApprovalStage>();
        var start = PositionOf(from);
        var end = PositionOf(to);
        if (start <= 0 || end <= 0 || start > end)
        {
            return segments;
        }

        for (var i = start - 1; i < end; i++)
        {
            segments.Add(Sequence[i]);
        }

        return segments;
    }

    /// <summary>The readable names of every segment, in walk order.</summary>
    public static IReadOnlyList<string> ReadableNames()
    {
        var names = new List<string>();
        foreach (var stage in Sequence)
        {
            names.Add(NameOf(stage));
        }

        return names;
    }

    /// <summary>How many segments the walk has.</summary>
    public static int Count => Sequence.Length;

    /// <summary>The neighbouring segment that a decision at this segment touches.</summary>
    public static IReadOnlyList<ApprovalStage> Touching(ApprovalStage stage)
    {
        var touched = new List<ApprovalStage>();
        var position = PositionOf(stage);
        if (position <= 0)
        {
            return touched;
        }

        touched.Add(stage);
        if (position > 1)
        {
            touched.Add(Sequence[position - 2]);
        }

        if (position < Sequence.Length)
        {
            touched.Add(Sequence[position]);
        }

        return touched;
    }
}