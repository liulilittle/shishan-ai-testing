namespace Shishan.Domain.K004;

/// <summary>The error raised when a request breaks a document rule.</summary>
public sealed class WorkflowException : Exception
{
    /// <summary>Raises the error with the given wording.</summary>
    public WorkflowException(string message) : base(message)
    {
    }
}

/// <summary>The wording the desk uses when a request is turned away.</summary>
public static class WorkflowErrors
{
    /// <summary>The wording for a missing document identifier.</summary>
    public static string DocumentIdRequired() => "A document identifier is required.";

    /// <summary>The wording for an identifier the desk already holds.</summary>
    public static string DocumentExists(string id) => $"Document '{id}' already exists.";

    /// <summary>The wording for an unknown document identifier.</summary>
    public static string DocumentMissing(string id) => $"Document '{id}' was not found.";

    /// <summary>The wording for a segment that is not part of a document.</summary>
    public static string UnknownSegment(string id, ApprovalStage stage) =>
        $"Segment '{StageDirectory.NameOf(stage)}' is not part of document '{id}'.";

    /// <summary>The wording for a segment whose neighbour is not approved yet.</summary>
    public static string SegmentNotReady(string id, ApprovalStage stage, ApprovalStage required) =>
        $"Segment '{StageDirectory.NameOf(stage)}' of document '{id}' is not open until '{StageDirectory.NameOf(required)}' is approved.";

    /// <summary>The wording for a document that has been retired.</summary>
    public static string DocumentClosed(string id) => $"Document '{id}' is retired and cannot be changed.";

    /// <summary>The wording for a document that is already retired.</summary>
    public static string AlreadyRetired(string id) => $"Document '{id}' is already retired.";
}

/// <summary>
/// Maps document segments to the numeric codes the older paper forms used.
/// Kept for desks that still quote those codes.
/// </summary>
public static class LegacyStageMap
{
    private static readonly Dictionary<ApprovalStage, int> Codes = new()
    {
        [ApprovalStage.Prepare] = 10,
        [ApprovalStage.BranchA] = 20,
        [ApprovalStage.BranchB] = 30,
        [ApprovalStage.Final] = 40,
    };

    private static readonly Dictionary<int, string> Captions = new()
    {
        [10] = "Preparation",
        [20] = "First Review",
        [30] = "Second Review",
        [40] = "Closing",
    };

    /// <summary>The paper-form code for a segment.</summary>
    public static int CodeOf(ApprovalStage stage) => Codes.TryGetValue(stage, out var code) ? code : 0;

    /// <summary>Reads a segment back out of a paper-form code.</summary>
    public static bool TryStageOf(int code, out ApprovalStage stage)
    {
        foreach (var pair in Codes)
        {
            if (pair.Value == code)
            {
                stage = pair.Key;
                return true;
            }
        }

        stage = ApprovalStage.Prepare;
        return false;
    }

    /// <summary>The caption printed beside a paper-form code.</summary>
    public static string CaptionOf(int code) => Captions.TryGetValue(code, out var caption) ? caption : "Unknown";

    /// <summary>Every paper-form code the desk recognizes.</summary>
    public static IReadOnlyList<int> KnownCodes()
    {
        var codes = new List<int>(Codes.Values);
        codes.Sort();
        return codes;
    }

    /// <summary>True when the code belongs to the closing segment.</summary>
    public static bool IsClosingCode(int code) => code == 40;

    /// <summary>True when the code belongs to a review station.</summary>
    public static bool IsReviewCode(int code) => code == 20 || code == 30;

    /// <summary>True when the code belongs to the opening segment.</summary>
    public static bool IsOpeningCode(int code) => code == 10;

    /// <summary>The paper-form code for the segment that follows the given one.</summary>
    public static int NextCodeOf(ApprovalStage stage) => CodeOf(StageDirectory.FollowingOf(stage));

    /// <summary>The paper-form code for the segment that comes before the given one.</summary>
    public static int PreviousCodeOf(ApprovalStage stage) => CodeOf(StageDirectory.PreviousOf(stage));

    /// <summary>True when the code belongs to a station that is not the opening one.</summary>
    public static bool IsLateCode(int code) => code >= 20;

    /// <summary>The caption printed beside a segment.</summary>
    public static string CaptionOf(ApprovalStage stage) => CaptionOf(CodeOf(stage));

    /// <summary>How many paper-form codes the desk recognizes.</summary>
    public static int CodeCount => Codes.Count;

    /// <summary>The codes in the order the walk uses them.</summary>
    public static IReadOnlyList<int> CodesInWalkOrder()
    {
        var codes = new List<int>();
        foreach (var stage in StageDirectory.Ordered)
        {
            codes.Add(CodeOf(stage));
        }

        return codes;
    }
}

/// <summary>
/// Small helpers that give a document a closing line for the daily log.
/// </summary>
public static class CloseOut
{
    /// <summary>A one-line reading of the document for the daily log.</summary>
    public static string Summary(ApprovalWorkflow document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return $"{document.Id}: {document.ApprovalCount()} of {StageDirectory.Ordered.Count} approved";
    }

    /// <summary>True when the segment is the closing one.</summary>
    public static bool IsClosing(ApprovalStage stage) => stage == ApprovalStage.Final;

    /// <summary>How many segments of the document carry an approval.</summary>
    public static int ApprovedCount(ApprovalWorkflow document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return document.ApprovalCount();
    }

    /// <summary>The readable line for one decision.</summary>
    public static string Line(StageStamp stamp)
    {
        if (stamp is null)
        {
            throw new ArgumentNullException(nameof(stamp));
        }

        return stamp.Describe();
    }

    /// <summary>The readable lines for every decision on a document.</summary>
    public static IReadOnlyList<string> Lines(ApprovalWorkflow document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var lines = new List<string>();
        foreach (var stamp in document.Stamps)
        {
            lines.Add(stamp.Describe());
        }

        return lines;
    }

    /// <summary>A short reading of one decision for the daily log.</summary>
    public static string ShortLine(StageStamp stamp) => stamp.IsApproval() ? $"{stamp.Stage} ok" : $"{stamp.Stage} no";

    /// <summary>True when the document carries an approval on every segment.</summary>
    public static bool IsFinished(ApprovalWorkflow document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return document.IsComplete();
    }

    /// <summary>How many decisions the document carries.</summary>
    public static int DecisionCount(ApprovalWorkflow document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return document.StampCount;
    }

    /// <summary>The newest decision on the document, or a blank line when none.</summary>
    public static string LastLine(ApprovalWorkflow document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var latest = document.LatestStamp();
        return latest is null ? string.Empty : latest.Describe();
    }

    /// <summary>The segments of the document that still carry no decision.</summary>
    public static IReadOnlyList<ApprovalStage> OpenSegments(ApprovalWorkflow document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var open = new List<ApprovalStage>();
        foreach (var stage in StageDirectory.Ordered)
        {
            if (!document.HasDecisionFor(stage))
            {
                open.Add(stage);
            }
        }

        return open;
    }
}