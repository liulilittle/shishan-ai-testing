namespace Shishan.Domain.K003;

/// <summary>
/// The office book that keeps the sheet numbers whose wall page still has to
/// be brought up to date after a hand-in, so the page shows the readings the
/// sheet most recently received. The page keeper calls on this book when it
/// takes a turn around the sheets.
/// </summary>
public sealed class WriteBackQueue
{
    private readonly Queue<string> _pageNotes = new();
    private readonly HashSet<string> _pageAskedFor = new(StringComparer.Ordinal);

    /// <summary>Notes that the page of the given sheet should be brought up to date.</summary>
    public void AlsoDoPage(string inspectionNo)
    {
        var number = InspectionItem.NormalizeCode(inspectionNo);
        if (string.IsNullOrEmpty(number))
        {
            return;
        }

        _pageNotes.Enqueue(number);
    }

    /// <summary>How many pages are still outstanding on the book.</summary>
    public int PagesOutstanding => _pageNotes.Count;

    /// <summary>True when the book still has pages to bring up to date.</summary>
    public bool HasOutstandingPages => _pageNotes.Count > 0;

    /// <summary>The sheet numbers whose pages are still outstanding, in order.</summary>
    public IReadOnlyList<string> OutstandingPageNumbers() => new List<string>(_pageNotes);

    /// <summary>
    /// Brings up to date every page the book has noted so far, using the rows
    /// the sheets hold at the moment they are reached.
    /// </summary>
    public int DoAllPages(InspectionStore store)
    {
        var done = 0;
        while (_pageNotes.Count > 0)
        {
            var number = _pageNotes.Dequeue();
            var sheet = store.FindNamed(number);
            if (sheet == null)
            {
                continue;
            }

            store.ComposePage(sheet);
            done++;
        }

        return done;
    }

    /// <summary>Reads the number of the next page to be done without doing it.</summary>
    public string NextPageNumber()
    {
        return _pageNotes.Count > 0 ? _pageNotes.Peek() : string.Empty;
    }

    /// <summary>True when the given sheet has a page already noted.</summary>
    public bool IsNoted(string inspectionNo)
    {
        var number = InspectionItem.NormalizeCode(inspectionNo);
        return _pageAskedFor.Contains(number);
    }

    /// <summary>Reads every sheet number that has been noted at least once.</summary>
    public IReadOnlyList<string> SheetsNoted() => new List<string>(_pageAskedFor);

    /// <summary>How many sheet numbers have been noted in all.</summary>
    public int SheetsNotedCount => _pageAskedFor.Count;

    /// <summary>True when the given sheet has no page noted yet.</summary>
    public bool NeverNoted(string inspectionNo)
    {
        var number = InspectionItem.NormalizeCode(inspectionNo);
        return !_pageAskedFor.Contains(number);
    }

    /// <summary>Reads the outstanding pages, joined as one plain line.</summary>
    public string DescribeOutstandingPages() =>
        _pageNotes.Count == 0 ? "none" : string.Join(", ", _pageNotes);
}