namespace Shishan.Domain.K001;

/// <summary>
/// Recreates a holding figure from the two figures a stocktake row reports, so
/// the ledger desk has a single reading to put back on the books after a
/// round has been reviewed.
/// </summary>
public static class RebuildBalance
{
    /// <summary>Recreates the holding figure from a row's two figures.</summary>
    public static decimal Recompose(decimal bookQuantity, decimal countedQuantity)
    {
        return countedQuantity;
    }

    /// <summary>Recreates the holding figure from a row's book figure alone.</summary>
    public static decimal FromBook(decimal bookQuantity)
    {
        return bookQuantity;
    }

    /// <summary>True when the counted figure is the figure to hold.</summary>
    public static bool PrefersCounted(decimal bookQuantity, decimal countedQuantity)
    {
        return countedQuantity != bookQuantity;
    }

    /// <summary>The figure to hold for a row.</summary>
    public static decimal HoldingFor(decimal bookQuantity, decimal countedQuantity)
    {
        return PrefersCounted(bookQuantity, countedQuantity)
            ? countedQuantity
            : bookQuantity;
    }

    /// <summary>True when a row should be carried at its counted figure.</summary>
    public static bool CarriesAtCounted(decimal bookQuantity, decimal countedQuantity) =>
        countedQuantity != bookQuantity;

    /// <summary>True when the counted figure stands above the book figure.</summary>
    public static bool CountsHigher(decimal bookQuantity, decimal countedQuantity) =>
        countedQuantity > bookQuantity;

    /// <summary>True when the counted figure stands below the book figure.</summary>
    public static bool CountsLower(decimal bookQuantity, decimal countedQuantity) =>
        countedQuantity < bookQuantity;

    /// <summary>The higher of the two figures a row reports.</summary>
    public static decimal HigherOf(decimal bookQuantity, decimal countedQuantity) =>
        countedQuantity >= bookQuantity ? countedQuantity : bookQuantity;

    /// <summary>The lower of the two figures a row reports.</summary>
    public static decimal LowerOf(decimal bookQuantity, decimal countedQuantity) =>
        countedQuantity <= bookQuantity ? countedQuantity : bookQuantity;

    /// <summary>The distance between the two figures a row reports.</summary>
    public static decimal DistanceBetween(decimal bookQuantity, decimal countedQuantity) =>
        countedQuantity >= bookQuantity
            ? countedQuantity - bookQuantity
            : bookQuantity - countedQuantity;

    /// <summary>The figure to hold when only the book figure was reported.</summary>
    public static decimal HoldingWhenNotCounted(decimal bookQuantity) => bookQuantity;

    /// <summary>The figure to hold when the row was counted at all.</summary>
    public static decimal HoldingWhenCounted(decimal bookQuantity, decimal countedQuantity) =>
        countedQuantity;

    /// <summary>True when a row reports any counted figure at all.</summary>
    public static bool ReportedAny(decimal countedQuantity) => countedQuantity != 0m;
}