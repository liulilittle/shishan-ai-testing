namespace Shishan.Domain.K003;

/// <summary>
/// The shelf that keeps every inspection sheet the office has opened, together
/// with the page each sheet shows on the wall. Sheets are read and written by
/// their sheet number.
/// </summary>
public sealed class InspectionStore
{
    private readonly Dictionary<string, InspectionSheet> _sheets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, InspectionDetail> _pages = new(StringComparer.Ordinal);

    /// <summary>
    /// Opens a new sheet in the office, takes the starting rows onto its header
    /// and sets the page the sheet will show.
    /// </summary>
    public InspectionSheet Open(
        string inspectionNo,
        IReadOnlyList<InspectionItem> initialRows,
        DateTime openedAt)
    {
        var number = InspectionItem.NormalizeCode(inspectionNo);
        if (_sheets.ContainsKey(number))
        {
            throw new InspectionRuleException(InspectionMessages.AlreadyOpen(number));
        }

        var sheet = new InspectionSheet(number, initialRows, openedAt);
        _sheets[number] = sheet;
        ComposePage(sheet);

        return sheet;
    }

    /// <summary>
    /// Finds the sheet a handed-in set of readings belongs to, by the number
    /// written on the set.
    /// </summary>
    public InspectionSheet? Locate(string inspectionNo)
    {
        var number = InspectionItem.NormalizeCode(inspectionNo);
        return _sheets.TryGetValue(number, out var sheet) ? sheet : null;
    }

    /// <summary>True when the office keeps a sheet with the given number.</summary>
    public bool IsOpen(string inspectionNo)
    {
        var number = InspectionItem.NormalizeCode(inspectionNo);
        return _sheets.ContainsKey(number);
    }

    /// <summary>True when a sheet of the lot has been opened at all.</summary>
    public bool HasAnySheet => _sheets.Count > 0;

    /// <summary>
    /// Puts the handed-in rows on the given sheet, replacing whatever the sheet
    /// held before, and notes the time of the hand-in on the header.
    /// </summary>
    public void ReplaceSubmissions(
        string inspectionNo,
        IReadOnlyList<InspectionItem> rows,
        DateTime updatedAt)
    {
        var sheet = RequireNumbered(inspectionNo);
        sheet.TakeRows(rows);
        sheet.NoteUpdate(updatedAt);
    }

    /// <summary>
    /// Writes a single row onto the given sheet and notes the hand-in time on
    /// the header. Rows already held for the item are updated in place.
    /// </summary>
    public bool KeepItem(string inspectionNo, InspectionItem row, DateTime updatedAt)
    {
        var sheet = RequireNumbered(inspectionNo);
        var kept = sheet.KeepRow(row);
        if (kept)
        {
            sheet.NoteUpdate(updatedAt);
        }

        return kept;
    }

    /// <summary>Reads the rows the given sheet currently holds.</summary>
    public IReadOnlyList<InspectionItem> HeldRowsOf(string inspectionNo)
    {
        var sheet = RequireNumbered(inspectionNo);
        return sheet.CopyOfRows();
    }

    /// <summary>Reads the header time the given sheet currently holds.</summary>
    public DateTime HeaderTimeOf(string inspectionNo)
    {
        var sheet = RequireNumbered(inspectionNo);
        return sheet.HeaderTime;
    }

    /// <summary>
    /// Builds the page the given sheet shows, from the rows the sheet holds at
    /// this moment, and keeps it as the page of the sheet.
    /// </summary>
    public InspectionDetail ComposePage(InspectionSheet sheet)
    {
        if (sheet == null)
        {
            throw new ArgumentNullException(nameof(sheet));
        }

        var rows = RowsInPageOrder(sheet);
        var page = new InspectionDetail(sheet.InspectionNo, sheet.HeaderTime, rows);
        _pages[sheet.InspectionNo] = page;
        return page;
    }

    /// <summary>Builds the page a numbered sheet shows, from the rows it holds.</summary>
    public InspectionDetail ComposePage(string inspectionNo)
    {
        var sheet = FindNamed(inspectionNo);
        if (sheet == null)
        {
            throw new InspectionRuleException(InspectionMessages.UnknownSheet(inspectionNo));
        }

        return ComposePage(sheet);
    }

    /// <summary>The page the office currently shows for the given sheet, if any.</summary>
    public InspectionDetail? PageOf(string inspectionNo)
    {
        var number = InspectionItem.NormalizeCode(inspectionNo);
        return _pages.TryGetValue(number, out var page) ? page : null;
    }

    /// <summary>The sheet the office keeps under the given number, if any.</summary>
    public InspectionSheet? FindNamed(string inspectionNo)
    {
        var number = InspectionItem.NormalizeCode(inspectionNo);
        return _sheets.TryGetValue(number, out var sheet) ? sheet : null;
    }

    /// <summary>Every sheet the office keeps, in the order they were opened.</summary>
    public IReadOnlyList<InspectionSheet> All()
    {
        var all = new List<InspectionSheet>(_sheets.Values);
        return all;
    }

    /// <summary>Every sheet number the office keeps, in alphabetical order.</summary>
    public IReadOnlyList<string> SheetNumbers()
    {
        var numbers = new List<string>(_sheets.Keys);
        numbers.Sort(StringComparer.Ordinal);
        return numbers;
    }

    /// <summary>How many sheets the office keeps.</summary>
    public int SheetCount => _sheets.Count;

    /// <summary>How many sheets hold no rows on the header.</summary>
    public int EmptySheetCount()
    {
        var count = 0;
        foreach (var sheet in _sheets.Values)
        {
            if (sheet.IsEmpty)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Reads the page of a sheet straight from the rows it holds.</summary>
    public InspectionDetail CurrentPageOf(string inspectionNo)
    {
        var page = PageOf(inspectionNo);
        if (page != null)
        {
            return page;
        }

        return ComposePage(inspectionNo);
    }

    /// <summary>True when a page is being shown for the given number.</summary>
    public bool HasPage(string inspectionNo)
    {
        var number = InspectionItem.NormalizeCode(inspectionNo);
        return _pages.ContainsKey(number);
    }

    /// <summary>How many pages the office is showing in all.</summary>
    public int PageCount => _pages.Count;

    /// <summary>The rows shown on the page of a numbered sheet, in display order.</summary>
    private IReadOnlyList<InspectionItem> RowsInPageOrder(InspectionSheet sheet)
    {
        var rows = new List<InspectionItem>();
        foreach (var row in sheet.HeldRows)
        {
            if (!row.IsSetAside)
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    private InspectionSheet RequireNumbered(string inspectionNo)
    {
        var sheet = FindNamed(inspectionNo);
        if (sheet == null)
        {
            throw new InspectionRuleException(InspectionMessages.UnknownSheet(inspectionNo));
        }

        return sheet;
    }
}