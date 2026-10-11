namespace Shishan.Domain.K001;

/// <summary>
/// One product row of a round as it is read out on the reconciliation sheet.
/// The row carries the book figure, the counted figure and the difference
/// between them, all in the ledger unit, together with how far the row has
/// come on the round.
/// </summary>
public sealed record ReconciliationRow(
    int RoundNo,
    string SkuCode,
    decimal BookQuantity,
    string BookUnit,
    decimal CountedQuantity,
    string? CountedUnit,
    decimal Delta,
    StocktakeLineStatus Status)
{
    /// <summary>True when the row shows no difference.</summary>
    public bool IsBalanced => Delta == 0m;

    /// <summary>True when the counted figure exceeds the book figure.</summary>
    public bool IsOverage => Delta > 0m;

    /// <summary>True when the counted figure falls short of the book figure.</summary>
    public bool IsShortage => Delta < 0m;

    /// <summary>True when the desk has signed the row off.</summary>
    public bool IsSignedOff => Status == StocktakeLineStatus.Audited;

    /// <summary>The row reference on the reconciliation sheet.</summary>
    public string Reference => $"{RoundNo}/{SkuCode}";

    /// <summary>Reads the book figure back in a chosen unit.</summary>
    public decimal BookIn(string unit) => UnitConverter.FromBase(BookQuantity, unit);

    /// <summary>Reads the counted figure back in a chosen unit.</summary>
    public decimal CountedIn(string unit) => UnitConverter.FromBase(CountedQuantity, unit);

    /// <summary>Reads the difference back in a chosen unit.</summary>
    public decimal DeltaIn(string unit) => UnitConverter.FromBase(Delta, unit);

    /// <summary>The direction of the difference, as the floor reads it.</summary>
    public string Direction =>
        Delta < 0m ? "short" : Delta > 0m ? "over" : "even";

    /// <summary>Describes the row in plain terms.</summary>
    public string Describe() =>
        $"{SkuCode} of round {RoundNo}: {BookQuantity} booked, {CountedQuantity} counted, " +
        $"{Delta} {UnitConverter.BaseUnit} difference.";
}

/// <summary>
/// Reads the reconciliation figures of a round so the desk has one line per
/// product. The reading stays on the row's own book and counted figures and
/// never draws on figures reported for another row.
/// </summary>
public static class ReconciliationReading
{
    /// <summary>The difference of a row, in the ledger unit.</summary>
    public static decimal DifferenceOf(StocktakeLine line) =>
        line == null ? 0m : line.CountedQuantity - line.BookQuantity;

    /// <summary>True when a row has been counted on the round.</summary>
    public static bool WasCounted(StocktakeLine line) => line != null && line.IsCounted;

    /// <summary>The row's counted figure, or the book figure when none was reported.</summary>
    public static decimal CountedOrBook(StocktakeLine line) =>
        line == null ? 0m : line.IsCounted ? line.CountedQuantity : line.BookQuantity;

    /// <summary>True when the row's counted figure matches its book figure.</summary>
    public static bool MatchesBook(StocktakeLine line) =>
        line != null && line.CountedQuantity == line.BookQuantity;

    /// <summary>The number of counted rows on a round.</summary>
    public static int CountedRows(IEnumerable<StocktakeLine> lines)
    {
        var count = 0;
        if (lines == null)
        {
            return count;
        }

        foreach (var line in lines)
        {
            if (line.IsCounted)
            {
                count++;
            }
        }

        return count;
    }
}