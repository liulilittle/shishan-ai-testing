namespace Shishan.Domain.D003;

/// <summary>
/// The desk that receives tasks, walks them through the two stages of a round
/// of review, and files every round so the history of a task can be read back.
/// An applicant whose task is turned back may hand it in again for a fresh
/// round.
/// </summary>
public sealed class AuditService
{
    private readonly ReviewPolicy _policy;
    private readonly CycleArchive _archive;
    private readonly Dictionary<string, AuditDocument> _documents;
    private readonly object _reviewGate;

    public AuditService()
        : this(new ReviewPolicy(), new CycleArchive())
    {
    }

    public AuditService(ReviewPolicy policy, CycleArchive archive)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _archive = archive ?? throw new ArgumentNullException(nameof(archive));
        _documents = new Dictionary<string, AuditDocument>(StringComparer.Ordinal);
        _reviewGate = new object();
    }

    /// <summary>
    /// Takes a task into review. The desk first checks the material the
    /// applicant handed in; a task without the material needed is not taken
    /// in, and the applicant is told why.
    /// </summary>
    public void Submit(AuditDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (!_policy.IsReady(document))
        {
            throw new SubmissionDeniedException(_policy.ExplainTurnAway(document));
        }

        _documents[document.Id] = document;
        _archive.OpenFirst(document.Id);
    }

    /// <summary>
    /// Leaves a reviewer's decision on a stage of a round. The stage must be
    /// the one the round is waiting on, and the stage must not already carry
    /// a decision from another reviewer. The check and the hand-over happen
    /// together so the stage keeps exactly one decision.
    /// </summary>
    public void Review(string documentId, int cycleNumber, AuditStage stage, AuditResult result, string reviewer)
    {
        if (string.IsNullOrWhiteSpace(reviewer))
        {
            throw new ArgumentException(AuditMessages.ReviewerRequired, nameof(reviewer));
        }

        lock (_reviewGate)
        {
            var cycle = FindCycle(documentId, cycleNumber);
            if (cycle == null)
            {
                throw new DocumentNotFoundException(AuditMessages.DocumentUnknown);
            }

            if (cycle.CurrentStage != stage)
            {
                throw new StageOrderException(AuditMessages.StageNotCurrent);
            }

            if (cycle.HasVerdict(stage))
            {
                throw new StageConcludedException(AuditMessages.StageAlreadyConcluded);
            }

            cycle.Record(stage, new StageVerdict(stage, result, reviewer, DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Hands a turned-back task in again and opens a fresh round of review.
    /// Only a task whose latest round was turned back may be handed in again.
    /// </summary>
    public void Resubmit(string documentId)
    {
        var latest = CurrentCycle(documentId);
        if (latest == null)
        {
            throw new DocumentNotFoundException(AuditMessages.DocumentUnknown);
        }

        if (!latest.TurnedBack)
        {
            throw new ResubmitDeniedException(AuditMessages.NotTurnedBack);
        }

        _archive.OpenNext(documentId);
    }

    /// <summary>The view of a document's latest round, or null when the document is not on file.</summary>
    public ReviewDetail? GetDetail(string documentId)
    {
        var cycle = CurrentCycle(documentId);
        if (cycle == null)
        {
            return null;
        }

        var document = _documents.TryGetValue(documentId, out var found) ? found : null;
        return document == null ? null : ReviewDetail.From(document, cycle);
    }

    /// <summary>The round a document is working on now, or null when the document is not on file.</summary>
    public ReviewCycle? CurrentCycle(string documentId)
    {
        if (!_archive.HasRounds(documentId))
        {
            return null;
        }

        return _archive.Latest(documentId);
    }

    /// <summary>The round with the given number, or null when no such round exists.</summary>
    public ReviewCycle? FindCycle(string documentId, int cycleNumber)
    {
        if (!_documents.ContainsKey(documentId))
        {
            return null;
        }

        return _archive.Find(documentId, cycleNumber);
    }

    /// <summary>True when the desk holds the given document.</summary>
    public bool HoldsDocument(string documentId) => _documents.ContainsKey(documentId);

    /// <summary>How many documents the desk is holding.</summary>
    public int DocumentCount => _documents.Count;

    /// <summary>Every document the desk is holding, by its desk number.</summary>
    public IReadOnlyList<AuditDocument> Documents()
    {
        var docs = new List<AuditDocument>(_documents.Values);
        docs.Sort((left, right) => string.Compare(left.Id, right.Id, StringComparison.Ordinal));
        return docs;
    }

    /// <summary>Every round a document has walked through, oldest first.</summary>
    public IReadOnlyList<ReviewCycle> History(string documentId) => _archive.Rounds(documentId);

    /// <summary>How many rounds a document has walked through.</summary>
    public int RoundCount(string documentId) => _archive.RoundCount(documentId);

    /// <summary>How many stages still wait for a decision on the document's latest round.</summary>
    public int OpenStageCount(string documentId)
    {
        var latest = CurrentCycle(documentId);
        if (latest == null)
        {
            return 0;
        }

        var open = 0;
        if (!latest.HasVerdict(AuditStage.First))
        {
            open++;
        }

        if (!latest.HasVerdict(AuditStage.Second))
        {
            open++;
        }

        return open;
    }

    /// <summary>The reviewer of a stage on the latest round, if a decision was left.</summary>
    public string? ReviewerOfLatestStage(string documentId, AuditStage stage)
    {
        var latest = CurrentCycle(documentId);
        if (latest == null)
        {
            return null;
        }

        var verdict = latest.VerdictOn(stage);
        return verdict?.Reviewer;
    }

    /// <summary>
    /// The material the desk asks for when a task is handed in, so the desk
    /// and the applicant agree on the opening list.
    /// </summary>
    public IReadOnlyList<string> ExpectedMaterialCodes() => _policy.RequiredCodes();
}