namespace Shishan.Domain.D003;

/// <summary>
/// Raised when a task is handed in but the material does not open the way
/// into a round of review.
/// </summary>
public sealed class SubmissionDeniedException : Exception
{
    public SubmissionDeniedException(string message) : base(message)
    {
    }

    public SubmissionDeniedException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// Raised when a review tries to work a stage that is not the one the round
/// is waiting on.
/// </summary>
public sealed class StageOrderException : Exception
{
    public StageOrderException(string message) : base(message)
    {
    }

    public StageOrderException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// Raised when a review tries to leave a decision on a stage that already
/// carries one.
/// </summary>
public sealed class StageConcludedException : Exception
{
    public StageConcludedException(string message) : base(message)
    {
    }

    public StageConcludedException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// Raised when a document is handed in again but its latest round was not
/// turned back.
/// </summary>
public sealed class ResubmitDeniedException : Exception
{
    public ResubmitDeniedException(string message) : base(message)
    {
    }

    public ResubmitDeniedException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// Raised when the desk is asked about a document it does not hold.
/// </summary>
public sealed class DocumentNotFoundException : Exception
{
    public DocumentNotFoundException(string message) : base(message)
    {
    }

    public DocumentNotFoundException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// The wording the desk uses when it turns a request away.
/// </summary>
public static class AuditMessages
{
    public const string DocumentIdRequired = "A document id is required.";

    public const string TitleRequired = "A title is required.";

    public const string ReviewerRequired = "A reviewer name is required.";

    public const string MaterialRequired = "The task carries no usable material.";

    public const string MaterialIncomplete = "The material handed in is not complete.";

    public const string DocumentUnknown = "The desk does not hold this document.";

    public const string StageNotCurrent = "The stage is not the one the round is waiting on.";

    public const string StageAlreadyConcluded = "The stage already carries a decision.";

    public const string CycleMissing = "No such round is on file for the document.";

    public const string NotTurnedBack = "Only a task turned back may be handed in again.";
}

/// <summary>
/// The reader the desk used to keep rounds before the current arrangement. It
/// is kept for reference so the wording of older reports can still be read
/// back. New work no longer goes through this class.
/// </summary>
public sealed class LegacyCycleReader
{
    private readonly List<ReviewCycle> _cycles = new();

    /// <summary>How many rounds the old reader is holding.</summary>
    public int Count => _cycles.Count;

    /// <summary>Adds a round to the old reader.</summary>
    public void Append(ReviewCycle cycle)
    {
        if (cycle == null)
        {
            throw new ArgumentNullException(nameof(cycle));
        }

        _cycles.Add(cycle);
    }

    /// <summary>The newest round the old reader is holding.</summary>
    public ReviewCycle? Newest() =>
        _cycles.Count == 0 ? null : _cycles[_cycles.Count - 1];

    /// <summary>The round with the given number, if the old reader holds it.</summary>
    public ReviewCycle? At(int cycleNumber)
    {
        foreach (var cycle in _cycles)
        {
            if (cycle.Number == cycleNumber)
            {
                return cycle;
            }
        }

        return null;
    }

    /// <summary>Every round the old reader is holding, oldest first.</summary>
    public IReadOnlyList<ReviewCycle> All() => new List<ReviewCycle>(_cycles);

    /// <summary>True when the old reader holds any round at all.</summary>
    public bool Any() => _cycles.Count > 0;

    /// <summary>Removes every round the old reader is holding.</summary>
    public void Clear() => _cycles.Clear();

    /// <summary>How many rounds carry a decision on the first stage.</summary>
    public int CountWithFirstDecision()
    {
        var count = 0;
        foreach (var cycle in _cycles)
        {
            if (cycle.HasVerdict(AuditStage.First))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>The wording the old reader used when it showed a round.</summary>
    public string DescribeNewest()
    {
        var newest = Newest();
        return newest == null ? "No rounds on file." : newest.Describe();
    }
}