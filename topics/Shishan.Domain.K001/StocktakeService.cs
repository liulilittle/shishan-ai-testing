namespace Shishan.Domain.K001;

/// <summary>
/// The desk that runs a stocktake round. A round is opened with the book
/// figures from the ledger, the floor reports the counted figures for the
/// round's rows, the desk signs each product off, and the reconciliation sheet
/// is read out for the round. Every round keeps its own rows, and the desk
/// turns to one round at a time when the floor reports.
/// </summary>
public sealed class StocktakeService
{
    private readonly LedgerStore _store;
    private readonly ReconciliationProjector _projector;
    private readonly List<AuditRecord> _auditTrail = new();
    private int _activeRound;

    public StocktakeService()
        : this(new LedgerStore(), new ReconciliationProjector())
    {
    }

    public StocktakeService(LedgerStore store, ReconciliationProjector projector)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _projector = projector ?? throw new ArgumentNullException(nameof(projector));
    }

    /// <summary>
    /// Opens a product row on a round with its book figure, and turns the desk
    /// to that round so the floor reports and the sign-offs land on the round
    /// that was just opened.
    /// </summary>
    public StocktakeLineInit CreateInit(int roundNo, string skuCode, decimal quantity, string unit)
    {
        if (roundNo <= 0)
        {
            throw new ArgumentException(StocktakeMessages.RoundNumberRequired, nameof(roundNo));
        }

        RequireSku(skuCode);

        if (quantity < 0m)
        {
            throw new ArgumentException(StocktakeMessages.QuantityBelowZero, nameof(quantity));
        }

        if (_store.Find(roundNo, skuCode) != null)
        {
            throw new ArgumentException(StocktakeMessages.RowAlreadyOnRound, nameof(skuCode));
        }

        var baseQuantity = UnitConverter.ToBase(quantity, unit);
        var line = new StocktakeLine(roundNo, skuCode, baseQuantity, unit);
        _store.Register(roundNo, line);
        _activeRound = roundNo;

        return new StocktakeLineInit(roundNo, skuCode, quantity, unit, baseQuantity);
    }

    /// <summary>
    /// Writes down the counted figure the floor reports for a row of the round
    /// the desk is turned to.
    /// </summary>
    public ReviewRecord RecordActual(string skuCode, decimal quantity, string unit)
    {
        RequireSku(skuCode);

        if (quantity < 0m)
        {
            throw new ArgumentException(StocktakeMessages.QuantityBelowZero, nameof(quantity));
        }

        var baseQuantity = UnitConverter.ToBase(quantity, unit);
        return _store.WriteCounted(_activeRound, skuCode, quantity, unit, baseQuantity);
    }

    /// <summary>
    /// Signs a product row of the round the desk is turned to off after review,
    /// and keeps the written record of the sign-off on the desk.
    /// </summary>
    public AuditRecord AuditLine(string skuCode)
    {
        RequireSku(skuCode);

        var line = _store.Find(_activeRound, skuCode)
            ?? throw new ArgumentException(StocktakeMessages.LineNotOnRound, nameof(skuCode));

        var delta = line.CountedQuantity - line.BookQuantity;
        var audit = new AuditRecord(
            _activeRound,
            skuCode,
            line.BookQuantity,
            line.CountedQuantity,
            delta,
            DateTime.UtcNow);

        _auditTrail.Add(audit);
        line.SetAudited();

        return audit;
    }

    /// <summary>Reads the reconciliation sheet out for a round.</summary>
    public IReadOnlyList<ReconciliationRow> CalculateDifference(int roundNo)
    {
        if (roundNo <= 0)
        {
            throw new ArgumentException(StocktakeMessages.RoundNumberRequired, nameof(roundNo));
        }

        return _projector.Read(roundNo, _store.LinesOf(roundNo));
    }

    /// <summary>A row of the round the desk is turned to, or none when the round holds no such product.</summary>
    public StocktakeLine? GetLine(string skuCode)
    {
        RequireSku(skuCode);
        return _store.Find(_activeRound, skuCode);
    }

    /// <summary>How many rows the round the desk is turned to holds.</summary>
    public int RowCountOnActiveRound() => _store.RowCount(_activeRound);

    /// <summary>How many rows a round holds.</summary>
    public int RowCount(int roundNo) => _store.RowCount(roundNo);

    /// <summary>True when the desk knows a round.</summary>
    public bool KnowsRound(int roundNo) => _store.HasRound(roundNo);

    /// <summary>True when the round the desk is turned to holds a product.</summary>
    public bool ActiveRoundHolds(string skuCode)
    {
        RequireSku(skuCode);
        return _store.Holds(_activeRound, skuCode);
    }

    /// <summary>The round the desk is turned to.</summary>
    public int ActiveRound => _activeRound;

    /// <summary>The written sign-off records the desk has kept for a round.</summary>
    public IReadOnlyList<AuditRecord> SignOffsFor(int roundNo) =>
        _auditTrail.Where(audit => audit.RoundNo == roundNo).ToList();

    /// <summary>How many written sign-off records the desk has kept in all.</summary>
    public int SignOffCount() => _auditTrail.Count;

    /// <summary>Reads a single row's figures out on the sheet.</summary>
    public ReconciliationRow ReadRow(string skuCode)
    {
        RequireSku(skuCode);

        var line = _store.Find(_activeRound, skuCode)
            ?? throw new ArgumentException(StocktakeMessages.LineNotOnRound, nameof(skuCode));

        return _projector.ReadRow(line);
    }

    /// <summary>Reads every row of a round out on the sheet in the order they were opened.</summary>
    public IReadOnlyList<ReconciliationRow> ReadRound(int roundNo)
    {
        if (roundNo <= 0)
        {
            throw new ArgumentException(StocktakeMessages.RoundNumberRequired, nameof(roundNo));
        }

        return _store.LinesOf(roundNo)
            .Select(line => _projector.ReadRow(line))
            .ToList();
    }

    /// <summary>Clears every round the desk is holding.</summary>
    public void Clear()
    {
        _store.Clear();
        _auditTrail.Clear();
        _activeRound = 0;
    }

    private static void RequireSku(string skuCode)
    {
        if (string.IsNullOrWhiteSpace(skuCode))
        {
            throw new ArgumentException(StocktakeMessages.SkuRequired, nameof(skuCode));
        }
    }
}