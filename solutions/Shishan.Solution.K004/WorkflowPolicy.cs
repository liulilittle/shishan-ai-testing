namespace Shishan.Domain.K004;

/// <summary>
/// The rules the desk applies to a document before a segment decision is
/// sealed, together with the neighbouring segments a decision touches.
/// </summary>
public sealed class WorkflowPolicy
{
    private static readonly ApprovalStage[] Empty = Array.Empty<ApprovalStage>();

    /// <summary>
    /// The segments that must already be approved before the given segment can
    /// be approved.
    /// </summary>
    public static ApprovalStage[] RequirementsOf(ApprovalStage stage) => stage switch
    {
        ApprovalStage.BranchA => new[] { ApprovalStage.Prepare },
        ApprovalStage.BranchB => new[] { ApprovalStage.Prepare },
        ApprovalStage.Final => new[] { ApprovalStage.Prepare, ApprovalStage.BranchA, ApprovalStage.BranchB },
        _ => Empty,
    };

    /// <summary>
    /// The segments that a rejection of the given segment sends back for a new
    /// walk.
    /// </summary>
    public static ApprovalStage[] DownstreamOf(ApprovalStage stage) => stage switch
    {
        ApprovalStage.Prepare => new[] { ApprovalStage.BranchA, ApprovalStage.BranchB, ApprovalStage.Final },
        ApprovalStage.BranchA => new[] { ApprovalStage.Final },
        ApprovalStage.BranchB => new[] { ApprovalStage.Final },
        _ => Empty,
    };

    /// <summary>
    /// Confirms that a document may still be walked, so that a decision can be
    /// sealed against it.
    /// </summary>
    public void EnsureOpen(ApprovalWorkflow document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (string.IsNullOrWhiteSpace(document.Id))
        {
            throw new WorkflowException(WorkflowErrors.DocumentIdRequired());
        }

        if (document.Status == DocumentStatus.Voided)
        {
            throw new WorkflowException(WorkflowErrors.DocumentClosed(document.Id));
        }
    }

    /// <summary>Confirms that an approval may be sealed against a segment.</summary>
    public void EnsureApprovable(ApprovalWorkflow document, ApprovalStage stage)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.IsApproved(stage))
        {
            return;
        }

        foreach (var required in RequirementsOf(stage))
        {
            if (!document.IsApproved(required))
            {
                throw new WorkflowException(WorkflowErrors.SegmentNotReady(document.Id, stage, required));
            }
        }
    }

    /// <summary>Confirms that a rejection may be sealed against a segment.</summary>
    public void EnsureRejectable(ApprovalWorkflow document, ApprovalStage stage)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (!StageDirectory.IsKnown(stage))
        {
            throw new WorkflowException(WorkflowErrors.UnknownSegment(document.Id, stage));
        }
    }

    /// <summary>Confirms that the document may be retired from review.</summary>
    public void EnsureVoidable(ApprovalWorkflow document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (document.Status == DocumentStatus.Voided)
        {
            throw new WorkflowException(WorkflowErrors.AlreadyRetired(document.Id));
        }
    }

    /// <summary>Brings the document standing in line with its segment decisions.</summary>
    public void RefreshCompletion(ApprovalWorkflow document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        document.SetStatus(document.IsComplete() ? DocumentStatus.Approved : DocumentStatus.InProgress);
    }

    /// <summary>True when every requirement for the segment is already approved.</summary>
    public static bool IsReady(ApprovalWorkflow document, ApprovalStage stage)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        foreach (var required in RequirementsOf(stage))
        {
            if (!document.IsApproved(required))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The requirements of the segment that are not approved yet.</summary>
    public static IReadOnlyList<ApprovalStage> OutstandingOf(ApprovalWorkflow document, ApprovalStage stage)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var outstanding = new List<ApprovalStage>();
        foreach (var required in RequirementsOf(stage))
        {
            if (!document.IsApproved(required))
            {
                outstanding.Add(required);
            }
        }

        return outstanding;
    }

    /// <summary>A short reading of the rule that applies to a segment.</summary>
    public static string DescribeRule(ApprovalStage stage)
    {
        var required = RequirementsOf(stage);
        if (required.Length == 0)
        {
            return $"{StageDirectory.NameOf(stage)} opens the walk";
        }

        var names = new List<string>();
        foreach (var item in required)
        {
            names.Add(StageDirectory.NameOf(item));
        }

        return $"{StageDirectory.NameOf(stage)} follows {string.Join(", ", names)}";
    }
}