namespace Shishan.Domain.K002;

/// <summary>
/// One page of the print ledger as the counter reads it. The page holds the
/// rows that fall in its range, together with the numbers the register is
/// keeping: how many rows it holds in all, and how many pages that works out
/// to at the page size the counter asked for.
/// </summary>
public sealed record PagedLedger(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    IReadOnlyList<PrintLedgerEntry> Items)
{
    /// <summary>True when the page came back with no rows on it.</summary>
    public bool EmptyPage => Items == null || Items.Count == 0;

    /// <summary>True when the whole register fits on this one page.</summary>
    public bool RegisterFitsHere => Total <= PageSize;

    /// <summary>True when there is another page after this one.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>True when the register holds nothing at all.</summary>
    public bool RegisterEmpty => Total == 0;

    /// <summary>
    /// The number of the first row this page shows, or zero when the page is
    /// empty.
    /// </summary>
    public int FirstRowNumber => Items != null && Items.Count > 0 ? Items[0].PrintNumber : 0;

    /// <summary>
    /// The number of the last row this page shows, or zero when the page is
    /// empty.
    /// </summary>
    public int LastRowNumber =>
        Items != null && Items.Count > 0 ? Items[^1].PrintNumber : 0;

    /// <summary>Read out the way the counter would say it.</summary>
    public override string ToString() =>
        "page " + Page + " of " + TotalPages + " (" + Items?.Count + " of " + Total + ")";
}

/// <summary>
/// Builds pages of the print ledger and answers the small questions the desk
/// asks about a page before turning it.
/// </summary>
public static class PagedLedgerBuilder
{
    /// <summary>The empty page a register shows when it holds nothing.</summary>
    public static PagedLedger Empty(int page, int pageSize) =>
        new(page, pageSize, 0, 0, new List<PrintLedgerEntry>());

    /// <summary>True when the given page sits inside the register's page count.</summary>
    public static bool PageInRange(PagedLedger ledger, int page) =>
        ledger != null && page >= 1 && page <= ledger.TotalPages;

    /// <summary>
    /// The page size the counter should use, after a too-small page size is
    /// brought up to one row.
    /// </summary>
    public static int SoundSize(int pageSize) => pageSize < 1 ? 1 : pageSize;

    /// <summary>
    /// The page number the counter should use, after a too-small page number
    /// is brought up to the first page.
    /// </summary>
    public static int SoundPage(int page) => page < 1 ? 1 : page;

    /// <summary>
    /// How many pages a register works out to at a given page size, or zero
    /// when the register holds nothing.
    /// </summary>
    public static int PageCountOf(int total, int pageSize)
    {
        if (total <= 0)
        {
            return 0;
        }

        var size = SoundSize(pageSize);
        return (total - 1) / size + 1;
    }

    /// <summary>
    /// The first position this page should read from, counting from zero in
    /// the register's row order.
    /// </summary>
    public static int StartingRow(int page, int pageSize) =>
        (SoundPage(page) - 1) * SoundSize(pageSize);

    /// <summary>
    /// The rows this page shows, taken from the register's row order without
    /// going past the end of the register.
    /// </summary>
    public static IReadOnlyList<PrintLedgerEntry> RowsFor(
        IReadOnlyList<PrintLedgerEntry> rows,
        int page,
        int pageSize)
    {
        if (rows == null || rows.Count == 0)
        {
            return new List<PrintLedgerEntry>();
        }

        var start = StartingRow(page, pageSize);
        if (start >= rows.Count)
        {
            return new List<PrintLedgerEntry>();
        }

        var count = Math.Min(SoundSize(pageSize), rows.Count - start);
        var pageRows = new List<PrintLedgerEntry>(count);
        for (var index = 0; index < count; index++)
        {
            pageRows.Add(rows[start + index]);
        }

        return pageRows;
    }
}