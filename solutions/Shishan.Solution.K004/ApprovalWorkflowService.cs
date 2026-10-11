namespace Shishan.Domain.K004;

/// <summary>
/// The public entry point used by the desk. It opens documents, seals segment
/// decisions, retires documents and serves the board.
/// </summary>
public sealed class ApprovalWorkflowService
{
    private readonly object _turn = new();
    private readonly WorkflowStore _documents = new();
    private readonly WorkflowPolicy _policy = new();
    private readonly BoardProjector _board = new();

    /// <summary>Opens a document under the given identifier.</summary>
    public ApprovalWorkflow Start(string docId)
    {
        if (string.IsNullOrWhiteSpace(docId))
        {
            throw new WorkflowException(WorkflowErrors.DocumentIdRequired());
        }

        lock (_turn)
        {
            var document = _documents.Add(docId);
            _board.Project(document);
            return document;
        }
    }

    /// <summary>Seals an approval against a segment of the document.</summary>
    public ApprovalWorkflow Approve(string docId, ApprovalStage stage, string approver)
    {
        if (approver is null)
        {
            throw new ArgumentNullException(nameof(approver));
        }

        if (!StageDirectory.IsKnown(stage))
        {
            throw new WorkflowException(WorkflowErrors.UnknownSegment(docId, stage));
        }

        lock (_turn)
        {
            var document = _documents.Require(docId);
            _policy.EnsureOpen(document);
            _policy.EnsureApprovable(document, stage);

            if (document.IsApproved(stage))
            {
                return document;
            }

            document.Record(StageStamp.Approval(stage, approver));
            _policy.RefreshCompletion(document);
            _board.Project(document);
            return document;
        }
    }

    /// <summary>Seals a rejection against a segment and sends the rest back.</summary>
    public ApprovalWorkflow Reject(string docId, ApprovalStage stage, string reason)
    {
        if (reason is null)
        {
            throw new ArgumentNullException(nameof(reason));
        }

        if (!StageDirectory.IsKnown(stage))
        {
            throw new WorkflowException(WorkflowErrors.UnknownSegment(docId, stage));
        }

        lock (_turn)
        {
            var document = _documents.Require(docId);
            _policy.EnsureOpen(document);
            _policy.EnsureRejectable(document, stage);

            document.Record(StageStamp.Rejection(stage, reason));
            document.RemoveSegments(WorkflowPolicy.DownstreamOf(stage));
            _policy.RefreshCompletion(document);
            _board.Project(document);
            return document;
        }
    }

    /// <summary>Retires the document from further review.</summary>
    public ApprovalWorkflow Void(string docId, string reason)
    {
        if (reason is null)
        {
            throw new ArgumentNullException(nameof(reason));
        }

        lock (_turn)
        {
            var document = _documents.Require(docId);
            _policy.EnsureVoidable(document);
            document.SetStatus(DocumentStatus.Voided);
            _board.Project(document);
            return document;
        }
    }

    /// <summary>The board entries for every document the desk is holding.</summary>
    public IReadOnlyList<BoardView> GetBoard()
    {
        lock (_turn)
        {
            var views = new List<BoardView>();
            foreach (var document in _documents.All())
            {
                views.Add(_board.Project(document));
            }

            return views;
        }
    }
}