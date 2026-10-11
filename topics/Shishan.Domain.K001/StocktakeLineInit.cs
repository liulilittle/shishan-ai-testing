namespace Shishan.Domain.K001;

/// <summary>
/// The opening figures of a stocktake row, handed back to the desk when the
/// round is written down. The book figure is kept in the unit it was given in
/// and again in the ledger unit, so the desk can carry the row forward on one
/// footing no matter which counting unit the floor used.
/// </summary>
public sealed record StocktakeLineInit(
    int RoundNo,
    string SkuCode,
    decimal BookQuantity,
    string BookUnit,
    decimal BookBaseQuantity)
{
    /// <summary>True when the row was written in the ledger unit.</summary>
    public bool InLedgerUnit =>
        string.Equals(BookUnit, UnitConverter.BaseUnit, StringComparison.Ordinal);

    /// <summary>True when the book figure reads to a whole number in the ledger unit.</summary>
    public bool RoundsToWhole =>
        decimal.Truncate(BookBaseQuantity) == BookBaseQuantity;

    /// <summary>Reads the book figure back in a chosen unit.</summary>
    public decimal BookIn(string unit) => UnitConverter.FromBase(BookBaseQuantity, unit);

    /// <summary>The row reference the floor uses on the sheet.</summary>
    public string Reference => $"{RoundNo}/{SkuCode}";

    /// <summary>The standing of the row as opened.</summary>
    public StocktakeLineStatus OpenedAs => StocktakeLineStatus.Pending;

    /// <summary>Describes the opening figures in plain terms.</summary>
    public string Describe() =>
        $"{SkuCode} of round {RoundNo}: {BookQuantity} {BookUnit} booked " +
        $"({BookBaseQuantity} {UnitConverter.BaseUnit} on the books).";
}

/// <summary>
/// Handles the opening of a stocktake row from the figures the desk writes
/// down. The book figure is reduced to the ledger unit before the row is
/// carried, and the unit is checked against the units the floor counts in.
/// </summary>
public static class StocktakeOpening
{
    /// <summary>Reduces the book figure of a row to the ledger unit.</summary>
    public static decimal BookToBase(decimal quantity, string unit)
    {
        RequireUnit(unit);
        return UnitConverter.ToBase(quantity, unit);
    }

    /// <summary>Reduces the counted figure of a row to the ledger unit.</summary>
    public static decimal CountedToBase(decimal quantity, string unit)
    {
        RequireUnit(unit);
        return UnitConverter.ToBase(quantity, unit);
    }

    /// <summary>True when the unit the desk wrote is one the floor counts in.</summary>
    public static bool IsUsableUnit(string unit) => UnitConverter.IsKnownUnit(unit);

    /// <summary>Describes the unit the desk wrote in floor terms.</summary>
    public static string DescribeUnit(string unit)
    {
        RequireUnit(unit);
        return UnitConverter.Describe(unit);
    }

    /// <summary>Trims the unit the desk wrote before it is read.</summary>
    public static string CleanUnit(string unit) => UnitConverter.Normalize(unit);

    /// <summary>Checks that a unit is one the floor counts in.</summary>
    private static void RequireUnit(string unit)
    {
        if (!UnitConverter.IsKnownUnit(unit))
        {
            throw new ArgumentException(StocktakeMessages.UnitNotRecognised, nameof(unit));
        }
    }
}
