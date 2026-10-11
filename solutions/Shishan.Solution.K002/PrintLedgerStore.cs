namespace Shishan.Domain.K002;

/// <summary>
/// Holds the rows of the print ledger for every register, in the order the
/// sheets left the counter. Each row keeps the number it was given, so the
/// ledger can be read back page by page and row by row without losing the
/// order the sheets went out in.
/// </summary>
public sealed class PrintLedgerStore
{
    private readonly List<PrintLedgerEntry> _rows = new();
    private int _nextNumber = 1;

    /// <summary>
    /// Held while a sheet's opening is checked, numbered and recorded, so
    /// the three happen together for one request at a time.
    /// </summary>
    public object Gate { get; } = new();

    /// <summary>
    /// Adds a row to the ledger for a sheet that is leaving the counter.
    /// The row is given the next number in the register order, and the entry
    /// is recorded with the standing the sheet left under.
    /// </summary>
    public PrintLedgerEntry Append(
        PrintCategory category,
        string documentId,
        string reason,
        string standing,
        DateTime printedAt)
    {
        lock (_rows)
        {
            var number = _nextNumber;
            _nextNumber++;

            var row = new PrintLedgerEntry(
                number,
                category,
                documentId,
                reason ?? string.Empty,
                standing,
                printedAt);

            _rows.Add(row);
            return row;
        }
    }

    /// <summary>
    /// The row that carries the given number, or null when the ledger holds
    /// no such row.
    /// </summary>
    public PrintLedgerEntry? FindByNumber(int printNumber)
    {
        if (printNumber <= 0)
        {
            return null;
        }

        lock (_rows)
        {
            foreach (var row in _rows)
            {
                if (row.PrintNumber == printNumber)
                {
                    return row;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Every row that belongs to the given category, in the order the sheets
    /// left the counter.
    /// </summary>
    public IReadOnlyList<PrintLedgerEntry> ForCategory(PrintCategory category)
    {
        lock (_rows)
        {
            var result = new List<PrintLedgerEntry>();
            foreach (var row in _rows)
            {
                if (row.BelongsTo(category))
                {
                    result.Add(row);
                }
            }

            return result;
        }
    }

    /// <summary>Every row the ledger holds, across all registers.</summary>
    public IReadOnlyList<PrintLedgerEntry> AllRows()
    {
        lock (_rows)
        {
            return new List<PrintLedgerEntry>(_rows);
        }
    }

    /// <summary>How many rows the ledger holds for the given category.</summary>
    public int CountFor(PrintCategory category)
    {
        lock (_rows)
        {
            var count = 0;
            foreach (var row in _rows)
            {
                if (row.BelongsTo(category))
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>How many rows the ledger holds in all.</summary>
    public int TotalRows()
    {
        lock (_rows)
        {
            return _rows.Count;
        }
    }

    /// <summary>
    /// What the register currently shows for the given category, read from
    /// the category's own rows so one register never shows another's sheets.
    /// </summary>
    public PrintStatusView StatusOf(PrintCategory category)
    {
        lock (_rows)
        {
            PrintLedgerEntry? latest = null;
            foreach (var row in _rows)
            {
                if (row.BelongsTo(category))
                {
                    latest = row;
                }
            }

            return latest != null
                ? PrintStatusBuilder.FromRow(latest)
                : PrintStatusBuilder.Fresh(category);
        }
    }

    /// <summary>
    /// One page of the ledger read back for the counter. When a category is
    /// given the page only carries that category's rows; when no category is
    /// given the page carries every row in the ledger.
    /// </summary>
    public PagedLedger QueryLedger(PrintCategory? category, int page, int pageSize)
    {
        lock (_rows)
        {
            IReadOnlyList<PrintLedgerEntry> rows =
                category == null
                    ? new List<PrintLedgerEntry>(_rows)
                    : ForCategory(category.Value);

            var soundPage = PagedLedgerBuilder.SoundPage(page);
            var soundSize = PagedLedgerBuilder.SoundSize(pageSize);
            var total = rows.Count;
            var totalPages = total == 0 ? 0 : PagedLedgerBuilder.PageCountOf(total, soundSize);
            var start = PagedLedgerBuilder.StartingRow(soundPage, soundSize);

            IReadOnlyList<PrintLedgerEntry> items;
            if (start >= total)
            {
                items = new List<PrintLedgerEntry>();
            }
            else
            {
                items = PagedLedgerBuilder.RowsFor(rows, soundPage, soundSize);
            }

            return new PagedLedger(soundPage, soundSize, total, totalPages, items);
        }
    }

    /// <summary>
    /// Removes every row the ledger holds, so the counter can start a fresh
    /// day's run.
    /// </summary>
    public void Clear()
    {
        lock (_rows)
        {
            _rows.Clear();
            _nextNumber = 1;
        }
    }

    /// <summary>
    /// The number the next sheet leaving the counter will be given, read
    /// without changing anything.
    /// </summary>
    public int NextNumber()
    {
        lock (_rows)
        {
            return _nextNumber;
        }
    }

    /// <summary>True when the ledger holds no rows at all.</summary>
    public bool IsEmpty()
    {
        lock (_rows)
        {
            return _rows.Count == 0;
        }
    }

    /// <summary>
    /// The highest number any sheet has been given so far, or zero when no
    /// sheet has left yet.
    /// </summary>
    public int HighestNumber()
    {
        lock (_rows)
        {
            return PrintLedgerRowReader.HighestNumber(_rows);
        }
    }
}