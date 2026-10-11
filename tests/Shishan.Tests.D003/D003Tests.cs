namespace Shishan.Tests.D003;

public class D003Tests
{
    private static AuditDocument Doc(string id, params Prerequisite[] prerequisites) =>
        new(id, "Doc-" + id, prerequisites);

    // Submission gate: a task with no material must not be taken into review
    [Fact]
    public void Submit_WithoutPrerequisites_Throws()
    {
        var svc = new AuditService();

        var empty = Doc("DOC-EMPTY-A");
        Assert.Throws<SubmissionDeniedException>(() => svc.Submit(empty));

        var blank = new AuditDocument("DOC-EMPTY-B", "No materials", new List<Prerequisite>());
        Assert.Throws<SubmissionDeniedException>(() => svc.Submit(blank));
    }

    // Normal path: the first stage can be reviewed first, and the round moves on
    [Fact]
    public void Review_FirstStage_FromFirst()
    {
        var svc = new AuditService();
        var doc = Doc("DOC-FIRST", new Prerequisite("P1", "Project plan"));
        svc.Submit(doc);

        svc.Review(doc.Id, 1, AuditStage.First, AuditResult.Passed, "reviewer-a");

        var detail = svc.GetDetail(doc.Id);
        Assert.NotNull(detail);
        Assert.Equal(1, detail.CycleNumber);
        Assert.Equal(AuditStage.Second, detail.CurrentStage);
        Assert.Equal(AuditResult.Passed, detail.FirstStageResult);
        Assert.Equal("reviewer-a", detail.FirstReviewer);

        var secondDoc = Doc("DOC-FIRST-2", new Prerequisite("P2", "Cost quote"));
        svc.Submit(secondDoc);
        svc.Review(secondDoc.Id, 1, AuditStage.First, AuditResult.Passed, "reviewer-b");
        var secondDetail = svc.GetDetail(secondDoc.Id);
        Assert.NotNull(secondDetail);
        Assert.Equal(AuditResult.Passed, secondDetail.FirstStageResult);
        Assert.Equal(AuditStage.Second, secondDetail.CurrentStage);
    }

    // Boundary: the second stage cannot be worked before the first
    [Fact]
    public void Review_OutOfOrderStage_Throws()
    {
        var svc = new AuditService();
        var doc = Doc("DOC-ORDER", new Prerequisite("P1", "Project plan"));
        svc.Submit(doc);

        Assert.Throws<StageOrderException>(
            () => svc.Review(doc.Id, 1, AuditStage.Second, AuditResult.Passed, "reviewer-a"));

        var secondDoc = Doc("DOC-ORDER-2", new Prerequisite("P2", "Cost quote"));
        svc.Submit(secondDoc);
        Assert.Throws<StageOrderException>(
            () => svc.Review(secondDoc.Id, 1, AuditStage.Second, AuditResult.Rejected, "reviewer-b"));
    }

    // Resubmit: a fresh round must not carry the conclusions of the old one
    [Fact]
    public void Resubmit_NewCycle_DetailExcludesPrevious()
    {
        var svc = new AuditService();
        var doc = Doc("DOC-RESUB", new Prerequisite("P1", "Project plan"));
        svc.Submit(doc);
        svc.Review(doc.Id, 1, AuditStage.First, AuditResult.Rejected, "reviewer-a");

        svc.Resubmit(doc.Id);

        var detail = svc.GetDetail(doc.Id);
        Assert.NotNull(detail);
        Assert.Equal(2, detail.CycleNumber);
        Assert.Null(detail.FirstStageResult);
        Assert.Null(detail.SecondStageResult);
        Assert.Null(detail.FirstReviewer);
        Assert.Null(detail.SecondReviewer);
    }

    // Resubmit: the fresh round must start again at the first stage
    [Fact]
    public void Resubmit_NewCycle_RestartsAtFirstStage()
    {
        var svc = new AuditService();
        var doc = Doc("DOC-RESTART", new Prerequisite("P1", "Project plan"));
        svc.Submit(doc);
        svc.Review(doc.Id, 1, AuditStage.First, AuditResult.Rejected, "reviewer-a");

        svc.Resubmit(doc.Id);

        svc.Review(doc.Id, 2, AuditStage.First, AuditResult.Passed, "reviewer-b");

        var detail = svc.GetDetail(doc.Id);
        Assert.NotNull(detail);
        Assert.Equal(2, detail.CycleNumber);
        Assert.Equal(AuditResult.Passed, detail.FirstStageResult);
        Assert.Equal("reviewer-b", detail.FirstReviewer);
        Assert.Equal(AuditStage.Second, detail.CurrentStage);
    }

    // Boundary: only a turned-back task may be handed in again
    [Fact]
    public void Resubmit_WithoutPriorRejection_Throws()
    {
        var svc = new AuditService();
        var doc = Doc("DOC-NOREJ", new Prerequisite("P1", "Project plan"));
        svc.Submit(doc);

        Assert.Throws<ResubmitDeniedException>(() => svc.Resubmit(doc.Id));

        var passed = Doc("DOC-PASSED", new Prerequisite("P2", "Cost quote"));
        svc.Submit(passed);
        svc.Review(passed.Id, 1, AuditStage.First, AuditResult.Passed, "reviewer-a");
        Assert.Throws<ResubmitDeniedException>(() => svc.Resubmit(passed.Id));
    }

    // Same-stage review: many reviewers concluding the same stage at the same
    // time must leave exactly one decision and one winner
    [Fact]
    public async Task ConcurrentReview_SameStage_OnlyOneResultWins()
    {
        var svc = new AuditService();
        var doc = Doc("DOC-CONC", new Prerequisite("P1", "Project plan"));
        svc.Submit(doc);
        svc.Review(doc.Id, 1, AuditStage.First, AuditResult.Passed, "reviewer-a");

        const int Reviewers = 16;
        var successCount = 0;
        using var gate = new Barrier(Reviewers);

        var tasks = Enumerable.Range(0, Reviewers)
            .Select(i => Task.Run(() =>
            {
                gate.SignalAndWait();
                try
                {
                    svc.Review(
                        doc.Id,
                        1,
                        AuditStage.Second,
                        i % 2 == 0 ? AuditResult.Passed : AuditResult.Rejected,
                        "reviewer-" + i);
                    Interlocked.Increment(ref successCount);
                }
                catch (StageConcludedException)
                {
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, successCount);

        var detail = svc.GetDetail(doc.Id);
        Assert.NotNull(detail);
        Assert.NotNull(detail.SecondStageResult);
        Assert.NotNull(detail.SecondReviewer);
    }

    // Same-stage review: the decision left must be one of the decisions handed in
    [Fact]
    public async Task ConcurrentReview_RecordedResult_IsOneOfSubmitted()
    {
        var svc = new AuditService();
        var doc = Doc("DOC-CONC2", new Prerequisite("P1", "Project plan"));
        svc.Submit(doc);
        svc.Review(doc.Id, 1, AuditStage.First, AuditResult.Passed, "reviewer-a");

        const int Reviewers = 16;
        var submitted = new List<(AuditResult Result, string Reviewer)>();
        for (var i = 0; i < Reviewers; i++)
        {
            submitted.Add((i % 2 == 0 ? AuditResult.Passed : AuditResult.Rejected, "reviewer-" + i));
        }

        using var gate = new Barrier(Reviewers);
        var tasks = submitted
            .Select(pair => Task.Run(() =>
            {
                gate.SignalAndWait();
                try
                {
                    svc.Review(doc.Id, 1, AuditStage.Second, pair.Result, pair.Reviewer);
                }
                catch (StageConcludedException)
                {
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        var detail = svc.GetDetail(doc.Id);
        Assert.NotNull(detail);
        Assert.NotNull(detail.SecondStageResult);
        Assert.NotNull(detail.SecondReviewer);
        Assert.Contains(
            submitted,
            pair => pair.Result == detail.SecondStageResult && pair.Reviewer == detail.SecondReviewer);
    }

    // Boundary: a document the desk does not hold has no detail
    [Fact]
    public void Detail_UnknownDocument_ReturnsNull()
    {
        var svc = new AuditService();
        svc.Submit(Doc("DOC-KNOWN", new Prerequisite("P1", "Project plan")));

        Assert.Null(svc.GetDetail("DOC-UNKNOWN-A"));
        Assert.Null(svc.GetDetail("DOC-UNKNOWN-B"));
    }

    // Error path: a document the desk does not hold cannot be reviewed
    [Fact]
    public void Review_UnknownDocument_Throws()
    {
        var svc = new AuditService();

        Assert.Throws<DocumentNotFoundException>(
            () => svc.Review("DOC-GONE-A", 1, AuditStage.First, AuditResult.Passed, "reviewer-a"));
        Assert.Throws<DocumentNotFoundException>(
            () => svc.Review("DOC-GONE-B", 5, AuditStage.Second, AuditResult.Rejected, "reviewer-b"));
    }
}