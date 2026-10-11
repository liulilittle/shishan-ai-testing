namespace Shishan.Domain.K004;

/// <summary>
/// Turns a document record into the board entry the desk shows to the parties
/// involved.
/// </summary>
public sealed class BoardProjector
{
    private readonly Dictionary<string, BoardNote> _held = new(StringComparer.Ordinal);

    /// <summary>
    /// The board entry for a document. The projector reuses the entry it holds
    /// for the document while the record has not moved on.
    /// </summary>
    public BoardView Project(ApprovalWorkflow document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        if (_held.TryGetValue(document.Id, out var note) && note.Revision == document.Revision)
        {
            return note.View;
        }

        var view = Compose(document);
        _held[document.Id] = new BoardNote(document.Revision, view);
        return view;
    }

    /// <summary>Builds a board entry straight from the current record.</summary>
    public BoardView Compose(ApprovalWorkflow document)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var results = new Dictionary<ApprovalStage, StageResult>();
        foreach (var stage in StageDirectory.Ordered)
        {
            results[stage] = document.ResultOf(stage);
        }

        var current = StageDirectory.CurrentOf(document.ResultOf);
        var latest = document.LatestStamp();

        return new BoardView(
            document.Id,
            document.Status,
            current,
            results,
            latest?.RecordedAt);
    }

    /// <summary>Drops the entry the projector was holding for a document.</summary>
    public void Forget(string id)
    {
        _held.Remove(id);
    }

    /// <summary>The identifiers the projector currently holds an entry for.</summary>
    public IReadOnlyList<string> HeldIds()
    {
        var ids = new List<string>(_held.Keys);
        ids.Sort(StringComparer.Ordinal);
        return ids;
    }

    /// <summary>The board entries for a set of documents, in the order given.</summary>
    public IReadOnlyList<BoardView> ComposeAll(IEnumerable<ApprovalWorkflow> documents)
    {
        if (documents is null)
        {
            throw new ArgumentNullException(nameof(documents));
        }

        var views = new List<BoardView>();
        foreach (var document in documents)
        {
            views.Add(Project(document));
        }

        return views;
    }

    /// <summary>A short reading of a board entry.</summary>
    public static string Describe(BoardView view)
    {
        if (view is null)
        {
            throw new ArgumentNullException(nameof(view));
        }

        return view.Describe();
    }

    /// <summary>The entry the projector holds together with the counter it was built from.</summary>
    private sealed class BoardNote
    {
        public BoardNote(int revision, BoardView view)
        {
            Revision = revision;
            View = view;
        }

        public int Revision { get; }

        public BoardView View { get; }
    }
}