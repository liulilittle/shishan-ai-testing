namespace Shishan.Domain.K002;

/// <summary>
/// One row of the print ledger: a single sheet that the counter let out,
/// with the number it was given in its register, the category it belongs
/// to, the document it was made for, the reason (if any) that was noted for
/// it, the standing it left the counter under, and the moment it went out.
/// </summary>
public sealed record PrintLedgerEntry(
    int PrintNumber,
    PrintCategory Category,
    string DocumentId,
    string Reason,
    string Status,
    DateTime PrintedAt)
{
    /// <summary>Called out in the form the register shows it.</summary>
    public override string ToString() =>
        PrintNumber + " / " + Category + " / " + DocumentId + " / " + Status;

    /// <summary>True when the row belongs to the given category.</summary>
    public bool BelongsTo(PrintCategory category) => Category == category;

    /// <summary>True when the row left the counter under the given standing.</summary>
    public bool CarriesStanding(string standing) =>
        string.Equals(Status, standing, StringComparison.Ordinal);

    /// <summary>True when the row was the first the register let out.</summary>
    public bool WasFirst => CarriesStanding(PrintStatuses.FirstPrinted);

    /// <summary>True when the row was a repeat sheet with a noted reason.</summary>
    public bool WasReprint => CarriesStanding(PrintStatuses.Reprinted);

    /// <summary>The time the row left the counter, kept in fixed form.</summary>
    public DateTime LeftCounter => PrintedAt;

    /// <summary>
    /// The number the row holds in its register, which stays put even when
    /// the ledger is read out of order.
    /// </summary>
    public int RegisteredNumber => PrintNumber;

    /// <summary>
    /// A short descriptor of what the sheet is for, using the wording of the
    /// category rather than its code, so the ledger stays readable on the
    /// floor.
    /// </summary>
    public string SheetDescriptor =>
        PrintCategoryReader.SpokenForm(Category) + " " + DocumentId;
}

/// <summary>
/// Working helpers for rows of the print ledger. These read a set of rows
/// without changing anything, so the same rows can be asked the same
/// question from more than one place in the shop.
/// </summary>
public static class PrintLedgerRowReader
{
    /// <summary>
    /// The row in the set that carries the given number, or null when the
    /// set holds no row with that number.
    /// </summary>
    public static PrintLedgerEntry? FindByNumber(IEnumerable<PrintLedgerEntry> rows, int printNumber)
    {
        if (printNumber <= 0 || rows == null)
        {
            return null;
        }

        foreach (var row in rows)
        {
            if (row.PrintNumber == printNumber)
            {
                return row;
            }
        }

        return null;
    }

    /// <summary>The rows in the set that belong to the given category.</summary>
    public static IReadOnlyList<PrintLedgerEntry> OfCategory(
        IEnumerable<PrintLedgerEntry> rows,
        PrintCategory category)
    {
        var result = new List<PrintLedgerEntry>();
        if (rows == null)
        {
            return result;
        }

        foreach (var row in rows)
        {
            if (row.BelongsTo(category))
            {
                result.Add(row);
            }
        }

        return result;
    }

    /// <summary>How many rows in the set belong to the given category.</summary>
    public static int CountOfCategory(IEnumerable<PrintLedgerEntry> rows, PrintCategory category) =>
        OfCategory(rows, category).Count;

    /// <summary>
    /// The row in the set that most recently left the counter for the given
    /// category, or null when the set holds no row for that category.
    /// </summary>
    public static PrintLedgerEntry? LatestForCategory(
        IEnumerable<PrintLedgerEntry> rows,
        PrintCategory category)
    {
        PrintLedgerEntry? latest = null;
        if (rows == null)
        {
            return null;
        }

        foreach (var row in rows)
        {
            if (!row.BelongsTo(category))
            {
                continue;
            }

            if (latest == null || row.RegisteredNumber > latest.RegisteredNumber)
            {
                latest = row;
            }
        }

        return latest;
    }

    /// <summary>True when the set holds no rows at all.</summary>
    public static bool IsEmpty(IEnumerable<PrintLedgerEntry> rows) =>
        rows == null || !rows.Any();

    /// <summary>
    /// The highest number held anywhere in the set, or zero when the set is
    /// empty.
    /// </summary>
    public static int HighestNumber(IEnumerable<PrintLedgerEntry> rows)
    {
        var highest = 0;
        if (rows == null)
        {
            return highest;
        }

        foreach (var row in rows)
        {
            if (row.PrintNumber > highest)
            {
                highest = row.PrintNumber;
            }
        }

        return highest;
    }

    /// <summary>
    /// The rows in the set that match the given standing, so the counter can
    /// see every first sheet or every repeat sheet that went out.
    /// </summary>
    public static IReadOnlyList<PrintLedgerEntry> WithStanding(
        IEnumerable<PrintLedgerEntry> rows,
        string standing)
    {
        var result = new List<PrintLedgerEntry>();
        if (rows == null)
        {
            return result;
        }

        foreach (var row in rows)
        {
            if (row.CarriesStanding(standing))
            {
                result.Add(row);
            }
        }

        return result;
    }
}