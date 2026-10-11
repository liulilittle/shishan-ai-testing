namespace Shishan.Domain.D003;

/// <summary>
/// Keeps every round of review a document has gone through. The latest round
/// is the one the desk is working on, and older rounds stay on file so the
/// history of a task can be read back.
/// </summary>
public sealed class CycleArchive
{
    private readonly Dictionary<string, List<ReviewCycle>> _roundsByDocument = new(StringComparer.Ordinal);

    /// <summary>Opens the very first round of review for a document.</summary>
    public ReviewCycle OpenFirst(string documentId)
    {
        var first = new ReviewCycle(1);
        Append(documentId, first);
        return first;
    }

    /// <summary>
    /// Opens a new round of review after the document was turned back. The
    /// new round picks up where the old one ended, keeping the decisions that
    /// were already left on file.
    /// </summary>
    public ReviewCycle OpenNext(string documentId)
    {
        var latest = Latest(documentId);
        if (latest == null)
        {
            throw new DocumentNotFoundException("No round to follow for " + documentId + ".");
        }

        var fresh = new ReviewCycle(latest.Number + 1);
        fresh.Record(AuditStage.First, latest.FirstVerdict!);
        fresh.Record(AuditStage.Second, latest.SecondVerdict!);
        Append(documentId, fresh);
        return fresh;
    }

    /// <summary>The latest round of review for a document, if any has been opened.</summary>
    public ReviewCycle? Latest(string documentId)
    {
        if (!_roundsByDocument.TryGetValue(documentId, out var rounds))
        {
            return null;
        }

        return rounds.Count == 0 ? null : rounds[rounds.Count - 1];
    }

    /// <summary>The round with the given number for a document, if one exists.</summary>
    public ReviewCycle? Find(string documentId, int cycleNumber)
    {
        if (!_roundsByDocument.TryGetValue(documentId, out var rounds))
        {
            return null;
        }

        foreach (var round in rounds)
        {
            if (round.Number == cycleNumber)
            {
                return round;
            }
        }

        return null;
    }

    /// <summary>Every round of review kept for a document, oldest first.</summary>
    public IReadOnlyList<ReviewCycle> Rounds(string documentId)
    {
        if (!_roundsByDocument.TryGetValue(documentId, out var rounds))
        {
            return Array.Empty<ReviewCycle>();
        }

        return new List<ReviewCycle>(rounds);
    }

    /// <summary>How many rounds of review are kept for a document.</summary>
    public int RoundCount(string documentId)
    {
        if (!_roundsByDocument.TryGetValue(documentId, out var rounds))
        {
            return 0;
        }

        return rounds.Count;
    }

    /// <summary>True when any round of review has been opened for a document.</summary>
    public bool HasRounds(string documentId) => _roundsByDocument.ContainsKey(documentId);

    /// <summary>Every document that has at least one round on file.</summary>
    public IReadOnlyList<string> DocumentsWithRounds()
    {
        var ids = new List<string>(_roundsByDocument.Keys);
        ids.Sort(StringComparer.Ordinal);
        return ids;
    }

    /// <summary>How many documents have at least one round on file.</summary>
    public int DocumentCount => _roundsByDocument.Count;

    private void Append(string documentId, ReviewCycle cycle)
    {
        if (!_roundsByDocument.TryGetValue(documentId, out var rounds))
        {
            rounds = new List<ReviewCycle>();
            _roundsByDocument[documentId] = rounds;
        }

        rounds.Add(cycle);
    }

    /// <summary>True when the given round is the newest one on file for a document.</summary>
    public bool IsLatest(string documentId, ReviewCycle cycle)
    {
        var latest = Latest(documentId);
        return latest != null && latest.Number == cycle.Number;
    }

    /// <summary>The round number that should follow the newest one on file.</summary>
    public int NextNumber(string documentId)
    {
        var latest = Latest(documentId);
        return latest == null ? 1 : latest.Number + 1;
    }

    /// <summary>How many rounds for a document were turned back at the first stage.</summary>
    public int TurnedBackAtFirstCount(string documentId)
    {
        var count = 0;
        if (!_roundsByDocument.TryGetValue(documentId, out var rounds))
        {
            return count;
        }

        foreach (var round in rounds)
        {
            if (round.RejectedAtFirst)
            {
                count++;
            }
        }

        return count;
    }
}