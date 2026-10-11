namespace Shishan.Domain.K001;

/// <summary>
/// The standing of one product row on a stocktake round. A row holds the book
/// figure written down when the round opened and the counted figure the floor
/// reported, both reduced to the ledger unit so the desk can compare them on
/// the same footing.
/// </summary>
public enum StocktakeLineStatus
{
    /// <summary>The row has been opened with its book figure.</summary>
    Pending = 0,

    /// <summary>The floor has reported the counted figure for the row.</summary>
    Counted = 1,

    /// <summary>The desk has signed the row off after review.</summary>
    Audited = 2,
}

/// <summary>
/// One product row on a stocktake round. A row always belongs to the round it
/// was opened on, and every figure it carries is read only against the book
/// figure of the same row.
/// </summary>
public sealed class StocktakeLine
{
    internal StocktakeLine(int roundNo, string skuCode, decimal bookQuantity, string bookUnit)
    {
        RoundNo = roundNo;
        SkuCode = skuCode;
        BookQuantity = bookQuantity;
        BookUnit = bookUnit;
        Status = StocktakeLineStatus.Pending;
    }

    /// <summary>The round this row belongs to.</summary>
    public int RoundNo { get; }

    /// <summary>The product this row counts.</summary>
    public string SkuCode { get; }

    /// <summary>The book figure for the product, in the ledger unit.</summary>
    public decimal BookQuantity { get; }

    /// <summary>The unit the book figure was written down in.</summary>
    public string BookUnit { get; }

    /// <summary>The counted figure reported by the floor, in the ledger unit.</summary>
    public decimal CountedQuantity { get; private set; }

    /// <summary>The unit the counted figure was reported in.</summary>
    public string? CountedUnit { get; private set; }

    /// <summary>How far the row has come.</summary>
    public StocktakeLineStatus Status { get; private set; }

    /// <summary>Whether the floor has reported a counted figure for the row.</summary>
    public bool IsCounted =>
        Status == StocktakeLineStatus.Counted || Status == StocktakeLineStatus.Audited;

    /// <summary>Whether the desk has signed the row off.</summary>
    public bool IsAudited => Status == StocktakeLineStatus.Audited;

    /// <summary>The difference between the counted figure and the book figure.</summary>
    public decimal Delta => CountedQuantity - BookQuantity;

    /// <summary>True when the counted figure differs from the book figure.</summary>
    public bool HasDelta => CountedQuantity != BookQuantity;

    /// <summary>True when the counted figure agrees with the book figure.</summary>
    public bool IsBalanced => CountedQuantity == BookQuantity;

    /// <summary>How much more was counted than booked, in the ledger unit.</summary>
    public decimal Overage => CountedQuantity - BookQuantity;

    /// <summary>How much less was counted than booked, in the ledger unit.</summary>
    public decimal Shortage => BookQuantity - CountedQuantity;

    /// <summary>The larger of the book and counted figures.</summary>
    public decimal HigherFigure => CountedQuantity >= BookQuantity ? CountedQuantity : BookQuantity;

    /// <summary>The smaller of the book and counted figures.</summary>
    public decimal LowerFigure => CountedQuantity <= BookQuantity ? CountedQuantity : BookQuantity;

    /// <summary>Reads the counted figure back in the given unit.</summary>
    public decimal CountedIn(string unit) => UnitConverter.FromBase(CountedQuantity, unit);

    /// <summary>Reads the book figure back in the given unit.</summary>
    public decimal BookIn(string unit) => UnitConverter.FromBase(BookQuantity, unit);

    /// <summary>Reads the difference back in the given unit.</summary>
    public decimal DeltaIn(string unit) => UnitConverter.FromBase(Delta, unit);

    /// <summary>The row as the floor reads it on the sheet.</summary>
    public string ReadOut =>
        $"{SkuCode} round {RoundNo}: {BookQuantity} {BookUnit} booked, {CountedQuantity} counted";

    /// <summary>The standing of the row as the floor reports it.</summary>
    public string Standing => Status switch
    {
        StocktakeLineStatus.Pending => "open",
        StocktakeLineStatus.Counted => "counted",
        _ => "signed off",
    };

    /// <summary>Whether the counted figure was reported in the ledger unit.</summary>
    public bool CountedInLedgerUnit =>
        CountedUnit != null && string.Equals(CountedUnit, UnitConverter.BaseUnit, StringComparison.Ordinal);

    /// <summary>Whether any counted figure has been written down for the row.</summary>
    public bool WasCounted => Status != StocktakeLineStatus.Pending;

    /// <summary>Records the counted figure reported by the floor.</summary>
    internal void SetCounted(decimal countedQuantity, string unit)
    {
        CountedQuantity = countedQuantity;
        CountedUnit = unit;
        Status = StocktakeLineStatus.Counted;
    }

    /// <summary>Signs the row off after review.</summary>
    internal void SetAudited() => Status = StocktakeLineStatus.Audited;

    /// <summary>Opens the row again so the floor can count it afresh.</summary>
    internal void Reopen()
    {
        CountedQuantity = 0m;
        CountedUnit = null;
        Status = StocktakeLineStatus.Pending;
    }

    public override string ToString() => ReadOut;
}
