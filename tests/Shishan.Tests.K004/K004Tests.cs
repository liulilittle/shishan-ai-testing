namespace Shishan.Tests.K004;

using System.Collections.Concurrent;

public class K004Tests
{
    // Normal path: the four segments are approved in order and the document
    // settles with exactly one approval per segment.
    [Fact]
    public void Approve_SequentialStages_AllPass()
    {
        var service = new ApprovalWorkflowService();
        var document = service.Start("DOC-SEQ");

        service.Approve("DOC-SEQ", ApprovalStage.Prepare, "u1");
        service.Approve("DOC-SEQ", ApprovalStage.BranchA, "u2");
        service.Approve("DOC-SEQ", ApprovalStage.BranchB, "u3");
        service.Approve("DOC-SEQ", ApprovalStage.Final, "u4");

        Assert.Equal(DocumentStatus.Approved, document.Status);
        Assert.Equal(4, document.StampCount);
        Assert.True(document.IsApproved(ApprovalStage.Prepare));
        Assert.True(document.IsApproved(ApprovalStage.BranchA));
        Assert.True(document.IsApproved(ApprovalStage.BranchB));
        Assert.True(document.IsApproved(ApprovalStage.Final));
    }

    // Out-of-order path: the closing segment must not be approved while a
    // review station in between is still undecided.
    [Fact]
    public void Approve_FinalBeforeAllBranches_Throws()
    {
        var service = new ApprovalWorkflowService();
        var document = service.Start("DOC-SKIP");
        service.Approve("DOC-SKIP", ApprovalStage.Prepare, "u1");
        service.Approve("DOC-SKIP", ApprovalStage.BranchA, "u2");

        Assert.Throws<WorkflowException>(() =>
            service.Approve("DOC-SKIP", ApprovalStage.Final, "u3"));

        Assert.False(document.IsApproved(ApprovalStage.Final));
        Assert.Equal(StageResult.Pending, document.ResultOf(ApprovalStage.BranchB));
    }

    // Out-of-order path: a station must not be approved while its upstream
    // segment is undecided.
    [Fact]
    public void Approve_StationBeforeUpstream_Throws()
    {
        var service = new ApprovalWorkflowService();
        var document = service.Start("DOC-UP");

        Assert.Throws<WorkflowException>(() =>
            service.Approve("DOC-UP", ApprovalStage.Final, "u1"));
        Assert.Throws<WorkflowException>(() =>
            service.Approve("DOC-UP", ApprovalStage.BranchB, "u2"));

        Assert.Equal(0, document.StampCount);
    }

    // Concurrent path: many callers seal the same segment at once and exactly
    // one approval is recorded for that segment.
    [Fact]
    public void ConcurrentApprove_SameStage_OneResult()
    {
        var service = new ApprovalWorkflowService();
        var document = service.Start("DOC-ONE");

        RunTogether(32, _ => service.Approve("DOC-ONE", ApprovalStage.Prepare, "u"));

        Assert.Single(
            document.Stamps,
            stamp => stamp.Stage == ApprovalStage.Prepare && stamp.Result == StageResult.Approved);
    }

    // Concurrent path: two independent stations approved at once each keep
    // their own approval.
    [Fact]
    public void ConcurrentApprove_TwoStages_BothRecorded()
    {
        var service = new ApprovalWorkflowService();
        var document = service.Start("DOC-TWO");
        service.Approve("DOC-TWO", ApprovalStage.Prepare, "u0");

        RunTogether(32, index => service.Approve(
            "DOC-TWO",
            index % 2 == 0 ? ApprovalStage.BranchA : ApprovalStage.BranchB,
            $"u{index}"));

        Assert.Contains(
            document.Stamps,
            stamp => stamp.Stage == ApprovalStage.BranchA && stamp.Result == StageResult.Approved);
        Assert.Contains(
            document.Stamps,
            stamp => stamp.Stage == ApprovalStage.BranchB && stamp.Result == StageResult.Approved);
        Assert.True(document.IsApproved(ApprovalStage.BranchA));
        Assert.True(document.IsApproved(ApprovalStage.BranchB));
    }

    // Residue cleanup: rejecting an upstream segment clears the decisions left
    // on every segment downstream of it.
    [Fact]
    public void Reject_Upstream_ClearsDownstreamStamps()
    {
        var service = new ApprovalWorkflowService();
        var document = service.Start("DOC-RJ");
        service.Approve("DOC-RJ", ApprovalStage.Prepare, "u1");
        service.Approve("DOC-RJ", ApprovalStage.BranchA, "u2");
        service.Approve("DOC-RJ", ApprovalStage.BranchB, "u3");

        service.Reject("DOC-RJ", ApprovalStage.Prepare, "redo");

        Assert.Equal(StageResult.Rejected, document.ResultOf(ApprovalStage.Prepare));
        Assert.False(document.IsApproved(ApprovalStage.BranchA));
        Assert.False(document.IsApproved(ApprovalStage.BranchB));
        Assert.False(document.IsApproved(ApprovalStage.Final));
        Assert.DoesNotContain(document.Stamps, stamp =>
            stamp.Stage == ApprovalStage.BranchA ||
            stamp.Stage == ApprovalStage.BranchB ||
            stamp.Stage == ApprovalStage.Final);
    }

    // Retired path: once the document is retired no approval or rejection is
    // accepted against it.
    [Fact]
    public void Void_ThenApproveOrReject_Throws()
    {
        var service = new ApprovalWorkflowService();
        var document = service.Start("DOC-VO");
        service.Approve("DOC-VO", ApprovalStage.Prepare, "u1");
        service.Void("DOC-VO", "admin");

        Assert.Throws<WorkflowException>(() =>
            service.Approve("DOC-VO", ApprovalStage.BranchA, "u2"));
        Assert.Throws<WorkflowException>(() =>
            service.Reject("DOC-VO", ApprovalStage.BranchB, "redo"));

        Assert.Equal(DocumentStatus.Voided, document.Status);
    }

    // Board path: the board mirrors the live record and shows the station the
    // walk currently stands on.
    [Fact]
    public void Board_MatchesCurrentStage()
    {
        var service = new ApprovalWorkflowService();
        service.Start("DOC-BD");
        service.Approve("DOC-BD", ApprovalStage.Prepare, "u1");
        service.Approve("DOC-BD", ApprovalStage.BranchA, "u2");

        var board = service.GetBoard().Single(view => view.Id == "DOC-BD");

        Assert.Equal(DocumentStatus.InProgress, board.Status);
        Assert.Equal(ApprovalStage.BranchB, board.CurrentStage);
        Assert.Equal(StageResult.Approved, board.ResultFor(ApprovalStage.Prepare));
        Assert.Equal(StageResult.Approved, board.ResultFor(ApprovalStage.BranchA));
        Assert.Equal(StageResult.Pending, board.ResultFor(ApprovalStage.BranchB));
        Assert.Equal(StageResult.Pending, board.ResultFor(ApprovalStage.Final));
    }

    // Board path: after a rejection the board steps back to the rejected
    // segment and drops the downstream results.
    [Fact]
    public void Board_AfterReject_ReflectsReset()
    {
        var service = new ApprovalWorkflowService();
        service.Start("DOC-BR");
        service.Approve("DOC-BR", ApprovalStage.Prepare, "u1");
        service.Approve("DOC-BR", ApprovalStage.BranchA, "u2");
        service.Reject("DOC-BR", ApprovalStage.Prepare, "redo");

        var board = service.GetBoard().Single(view => view.Id == "DOC-BR");

        Assert.Equal(ApprovalStage.Prepare, board.CurrentStage);
        Assert.Equal(StageResult.Rejected, board.ResultFor(ApprovalStage.Prepare));
        Assert.Equal(StageResult.Pending, board.ResultFor(ApprovalStage.BranchA));
        Assert.Equal(StageResult.Pending, board.ResultFor(ApprovalStage.BranchB));
        Assert.Equal(StageResult.Pending, board.ResultFor(ApprovalStage.Final));
    }

    // Board path: the board mirrors the retired standing of the record.
    [Fact]
    public void Board_AfterVoid_ShowsRetired()
    {
        var service = new ApprovalWorkflowService();
        service.Start("DOC-BV");
        service.Void("DOC-BV", "admin");

        var board = service.GetBoard().Single(view => view.Id == "DOC-BV");

        Assert.Equal(DocumentStatus.Voided, board.Status);
    }

    // Error path: decisions against an unknown document are turned away.
    [Fact]
    public void UnknownDocument_Decisions_Throw()
    {
        var service = new ApprovalWorkflowService();

        Assert.Throws<WorkflowException>(() =>
            service.Approve("NOPE", ApprovalStage.Prepare, "u"));
        Assert.Throws<WorkflowException>(() =>
            service.Reject("NOPE", ApprovalStage.Prepare, "redo"));
        Assert.Throws<WorkflowException>(() =>
            service.Void("NOPE", "redo"));
    }

    // Error path: a segment value that is not one of the four is turned away.
    [Fact]
    public void Approve_UnknownSegment_Throws()
    {
        var service = new ApprovalWorkflowService();
        service.Start("DOC-SEG");

        Assert.Throws<WorkflowException>(() =>
            service.Approve("DOC-SEG", (ApprovalStage)99, "u"));
    }

    // Boundary path: a desk holding nothing serves an empty board.
    [Fact]
    public void Board_WithoutDocuments_IsEmpty()
    {
        var service = new ApprovalWorkflowService();

        Assert.Empty(service.GetBoard());
    }

    // Boundary path: the board lists only the documents that were opened.
    [Fact]
    public void Board_ListsOnlyStartedDocuments()
    {
        var service = new ApprovalWorkflowService();
        service.Start("DOC-ONLY");

        var boards = service.GetBoard();

        Assert.Single(boards);
        Assert.Equal("DOC-ONLY", boards[0].Id);
        Assert.DoesNotContain(boards, view => view.Id == "DOC-OTHER");
    }

    // Error path: opening the same identifier twice is turned away.
    [Fact]
    public void Start_SameDocumentTwice_Throws()
    {
        var service = new ApprovalWorkflowService();
        service.Start("DOC-DUP");

        Assert.Throws<WorkflowException>(() => service.Start("DOC-DUP"));
    }

    // Boundary path: an empty or blank identifier is turned away.
    [Fact]
    public void Start_BlankIdentifier_Throws()
    {
        var service = new ApprovalWorkflowService();

        Assert.Throws<WorkflowException>(() => service.Start(string.Empty));
        Assert.Throws<WorkflowException>(() => service.Start("   "));
        Assert.Throws<WorkflowException>(() => service.Start("\t"));
    }

    // Error path: missing operator, reason and approver values are turned away.
    [Fact]
    public void Decisions_MissingArguments_Throw()
    {
        var service = new ApprovalWorkflowService();
        service.Start("DOC-ARG");

        Assert.Throws<ArgumentNullException>(() =>
            service.Approve("DOC-ARG", ApprovalStage.Prepare, null!));
        Assert.Throws<ArgumentNullException>(() =>
            service.Reject("DOC-ARG", ApprovalStage.Prepare, null!));
        Assert.Throws<ArgumentNullException>(() =>
            service.Void("DOC-ARG", null!));
    }

    private static void RunTogether(int threads, Action<int> body)
    {
        using var gate = new Barrier(threads);
        var workers = new Thread[threads];
        var failures = new ConcurrentQueue<Exception>();

        for (var i = 0; i < threads; i++)
        {
            var index = i;
            workers[i] = new Thread(() =>
            {
                gate.SignalAndWait();
                try
                {
                    body(index);
                }
                catch (Exception error)
                {
                    failures.Enqueue(error);
                }
            })
            {
                IsBackground = true,
            };
            workers[i].Start();
        }

        foreach (var worker in workers)
        {
            worker.Join();
        }

        if (!failures.IsEmpty)
        {
            throw new AggregateException(failures);
        }
    }
}