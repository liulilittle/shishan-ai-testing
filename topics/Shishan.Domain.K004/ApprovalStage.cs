namespace Shishan.Domain.K004;

/// <summary>
/// The segments a document passes through, in the order the desk walks them.
/// Prepare opens the document, Branch A and Branch B are the two review
/// stations, and Final closes it.
/// </summary>
public enum ApprovalStage
{
    /// <summary>The opening segment.</summary>
    Prepare = 1,

    /// <summary>The first review station.</summary>
    BranchA = 2,

    /// <summary>The second review station.</summary>
    BranchB = 3,

    /// <summary>The closing segment.</summary>
    Final = 4,
}

/// <summary>Readable helpers for a segment value.</summary>
public static class ApprovalStageExtensions
{
    /// <summary>True when the segment opens the document.</summary>
    public static bool IsOpening(this ApprovalStage stage) => stage == ApprovalStage.Prepare;

    /// <summary>True when the segment is one of the two review stations.</summary>
    public static bool IsReview(this ApprovalStage stage) =>
        stage == ApprovalStage.BranchA || stage == ApprovalStage.BranchB;

    /// <summary>True when the segment closes the document.</summary>
    public static bool IsClosing(this ApprovalStage stage) => stage == ApprovalStage.Final;

    /// <summary>The readable name of the segment.</summary>
    public static string Readable(this ApprovalStage stage) => StageDirectory.NameOf(stage);

    /// <summary>The position of the segment in the walk, counting from one.</summary>
    public static int Position(this ApprovalStage stage) => StageDirectory.PositionOf(stage);

    /// <summary>The segment that follows this one.</summary>
    public static ApprovalStage Next(this ApprovalStage stage) => StageDirectory.FollowingOf(stage);
}