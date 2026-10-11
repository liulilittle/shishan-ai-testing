namespace Shishan.Domain.K002;

/// <summary>
/// The front desk for the printing room. A sheet goes out of a register for
/// a category; the first sheet of each category leaves without a noted
/// reason, and any later sheet has to carry one. Every sheet is given a
/// number and recorded in the ledger, and the desk can read the ledger back
/// in pages and ask how a register is doing.
/// </summary>
public sealed class PrintService
{
    private readonly FirstPrintGate _first;
    private readonly PrintLedgerStore _store;

    /// <summary>A desk with its own register and its own ledger.</summary>
    public PrintService()
        : this(new FirstPrintGate(), new PrintLedgerStore())
    {
    }

    /// <summary>A desk running on the given register keeper and ledger.</summary>
    public PrintService(FirstPrintGate first, PrintLedgerStore store)
    {
        _first = first ?? throw new ArgumentNullException(nameof(first));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Handles a print request for a category. The first sheet of the
    /// category leaves without a reason; a later sheet is let out only when
    /// a reason is noted with it. Every sheet that leaves is numbered and
    /// recorded. The opening check, the numbering and the recording all
    /// happen under one held position, so one category's opening is crossed
    /// once even when several requests arrive at the same moment.
    /// </summary>
    public PrintResult RequestPrint(string category, string reason)
    {
        var cat = PrintCategoryReader.Require(category);

        lock (_store.Gate)
        {
            var isFirst = _first.IsFirst(cat);
            if (!isFirst && string.IsNullOrWhiteSpace(reason))
            {
                var register = _store.StatusOf(cat);
                return PrintResultFactory.TurnedAway(register.Status, PrintErrors.RepeatNeedsReason);
            }

            var now = DateTime.UtcNow;
            var documentId = BuildDocumentId(cat, now);
            var standing = _first.StandingFor(cat);

            var row = _store.Append(cat, documentId, reason, standing, now);
            _first.NoteFirst(cat);

            return PrintResultFactory.Granted(isFirst, row.PrintNumber, standing);
        }
    }

    /// <summary>
    /// What a register currently shows for a category, read from the
    /// category's own rows.
    /// </summary>
    public PrintStatusView GetStatus(string category)
    {
        var cat = PrintCategoryReader.Require(category);
        lock (_store.Gate)
        {
            return _store.StatusOf(cat);
        }
    }

    /// <summary>
    /// One page of the print ledger. Pass "All" to read every register
    /// together, or the name of a category to read just that register's
    /// rows.
    /// </summary>
    public PagedLedger GetLedger(string category, int page, int pageSize)
    {
        var cat = ResolveLedgerCategory(category);
        lock (_store.Gate)
        {
            return _store.QueryLedger(cat, page, pageSize);
        }
    }

    /// <summary>How many sheets have left the register for the given category.</summary>
    public int CountPrinted(string category)
    {
        var cat = PrintCategoryReader.Require(category);
        lock (_store.Gate)
        {
            return _store.CountFor(cat);
        }
    }

    /// <summary>
    /// The ledger row that carries the given number, or null when no row
    /// carries it.
    /// </summary>
    public PrintLedgerEntry? FindLedgerEntry(int printNumber)
    {
        lock (_store.Gate)
        {
            return _store.FindByNumber(printNumber);
        }
    }

    /// <summary>
    /// How many sheets the ledgers hold in all, across every register.
    /// </summary>
    public int TotalSheets()
    {
        lock (_store.Gate)
        {
            return _store.TotalRows();
        }
    }

    private static string BuildDocumentId(PrintCategory cat, DateTime now)
    {
        var label = PrintCategoryRegistry.DrawerLabel(cat);
        var minuteTag = now.ToString("HHmm");
        return label + "-" + minuteTag;
    }

    private static PrintCategory? ResolveLedgerCategory(string category)
    {
        var clean = (category ?? string.Empty).Trim();
        if (string.Equals(clean, LedgerAll, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.IsNullOrEmpty(clean))
        {
            throw new ArgumentException(PrintErrors.CategoryRequired, nameof(category));
        }

        return PrintCategoryReader.Require(clean);
    }

    /// <summary>The wording used to ask the ledger for every register at once.</summary>
    public const string LedgerAll = "All";
}