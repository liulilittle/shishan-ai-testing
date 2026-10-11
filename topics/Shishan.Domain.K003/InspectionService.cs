namespace Shishan.Domain.K003;

/// <summary>
/// The front desk for inspection sheets. The desk opens sheets, takes the
/// readings handed in for a sheet, keeps a single row of a sheet up to date
/// when an operator works alone on one item, and reads the wall page and the
/// list of sheets out on request.
/// </summary>
public sealed class InspectionService
{
    private readonly InspectionStore _store;
    private readonly WriteBackQueue _pageBook;

    public InspectionService()
        : this(new InspectionStore(), new WriteBackQueue())
    {
    }

    public InspectionService(InspectionStore store, WriteBackQueue pageBook)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _pageBook = pageBook ?? throw new ArgumentNullException(nameof(pageBook));
    }

    /// <summary>
    /// Opens a sheet in the office with the given starting rows and header time.
    /// </summary>
    public InspectionSheet Register(
        string inspectionNo,
        IReadOnlyList<InspectionItem> initialRows,
        DateTime openedAt)
    {
        var number = RequireNumber(inspectionNo);
        return _store.Open(number, initialRows ?? Array.Empty<InspectionItem>(), openedAt);
    }

    /// <summary>
    /// Hands in a set of readings for the sheet named on the set. The sheet
    /// takes the set onto its header and the wall page is brought up to date,
    /// then the page as it now stands is read back out.
    /// </summary>
    public InspectionDetail Submit(InspectionSubmission submission)
    {
        if (submission == null)
        {
            throw new ArgumentNullException(nameof(submission));
        }

        var sheet = _store.Locate(submission.InspectionNo);
        if (sheet == null)
        {
            throw new InspectionRuleException(InspectionMessages.UnknownSheet(submission.InspectionNo));
        }

        _store.ReplaceSubmissions(
            sheet.InspectionNo,
            submission.Rows,
            submission.SubmittedAt);

        _pageBook.AlsoDoPage(sheet.InspectionNo);
        _pageBook.DoAllPages(_store);

        return _store.PageOf(sheet.InspectionNo)
            ?? _store.ComposePage(sheet);
    }

    /// <summary>
    /// Keeps a single reading up to date on the sheet named at the desk. One
    /// operator working alone on one item can leave the full set of rows alone;
    /// only the reading of the item is written over the one the sheet already
    /// holds, or added when the sheet does not hold it yet.
    /// </summary>
    public InspectionDetail SubmitMaintenance(
        string sheetId,
        string itemCode,
        string value,
        string operatorName)
    {
        var number = RequireNumber(sheetId);
        var code = RequireItemCode(itemCode);

        var sheet = _store.Locate(number);
        if (sheet == null)
        {
            throw new InspectionRuleException(InspectionMessages.UnknownSheet(number));
        }

        var line = InspectionItem.Measure(code, value);
        _store.KeepItem(sheet.InspectionNo, line, DateTime.UtcNow);

        _pageBook.AlsoDoPage(sheet.InspectionNo);
        _pageBook.DoAllPages(_store);

        return _store.PageOf(sheet.InspectionNo)
            ?? _store.ComposePage(sheet);
    }

    /// <summary>The wall page the office currently shows for a sheet, if any.</summary>
    public InspectionDetail? GetDetail(string inspectionNo)
    {
        var number = RequireNumber(inspectionNo);
        return _store.PageOf(number);
    }

    /// <summary>The sheets the office keeps, in the order they were opened.</summary>
    public IReadOnlyList<InspectionSheet> GetSheets() => _store.All();

    /// <summary>Reads a sheet's page as plain floor text.</summary>
    public string ReadPageText(string inspectionNo)
    {
        var page = GetDetail(inspectionNo);
        return page == null ? string.Empty : InspectionFormatter.DescribeDetail(page);
    }

    /// <summary>Reads every sheet as plain floor text, one sheet per entry.</summary>
    public IReadOnlyList<string> ReadAllSheetTexts()
    {
        var texts = new List<string>();
        foreach (var sheet in _store.All())
        {
            texts.Add(InspectionFormatter.DescribeSheet(sheet));
        }

        return texts;
    }

    /// <summary>How many wall pages are still outstanding on the book.</summary>
    public int PagesOutstandingCount() => _pageBook.PagesOutstanding;

    /// <summary>True when every wall page is up to date.</summary>
    public bool AllPagesCurrent() => _pageBook.PagesOutstanding == 0;

    /// <summary>True when the office keeps a sheet under the given number.</summary>
    public bool IsKnownSheet(string inspectionNo)
    {
        return !string.IsNullOrWhiteSpace(inspectionNo) && _store.IsOpen(inspectionNo);
    }

    /// <summary>How many sheets the office keeps.</summary>
    public int SheetCount() => _store.SheetCount;

    /// <summary>The item codes the workshop knows, for the desk list.</summary>
    public IReadOnlyList<string> KnownItemCodes() => InspectionCatalog.Codes();

    /// <summary>Reads the reading a sheet holds for an item code, if it holds one.</summary>
    public string? ReadingOf(string inspectionNo, string itemCode)
    {
        var sheet = _store.FindNamed(inspectionNo);
        if (sheet == null)
        {
            return null;
        }

        var row = sheet.LastReading(itemCode);
        return row == null ? null : row.Value;
    }

    /// <summary>True when a sheet holds a reading for the given item code.</summary>
    public bool CarriesReading(string inspectionNo, string itemCode)
    {
        var sheet = _store.FindNamed(inspectionNo);
        return sheet != null && sheet.Carries(itemCode);
    }

    private static string RequireNumber(string inspectionNo)
    {
        if (string.IsNullOrWhiteSpace(inspectionNo))
        {
            throw new ArgumentException(InspectionMessages.SheetNumberRequired, nameof(inspectionNo));
        }

        return InspectionItem.NormalizeCode(inspectionNo);
    }

    private static string RequireItemCode(string itemCode)
    {
        if (string.IsNullOrWhiteSpace(itemCode))
        {
            throw new ArgumentException(InspectionMessages.ItemCodeRequired, nameof(itemCode));
        }

        return InspectionItem.NormalizeCode(itemCode);
    }
}