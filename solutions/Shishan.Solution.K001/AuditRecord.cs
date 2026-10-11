namespace Shishan.Domain.K001;

/// <summary>
/// The written-down sign-off for a product row after the desk reviews the
/// counted figure against the book figure. The difference is kept in the
/// ledger unit, and the record stays on the round even when the figures agree,
/// so the review is always left written down for the desk.
/// </summary>
public sealed record AuditRecord(
    int RoundNo,
    string SkuCode,
    decimal BookQuantity,
    decimal CountedQuantity,
    decimal Delta,
    DateTime AuditedAt)
{
    /// <summary>True when the figures agree.</summary>
    public bool IsBalanced => Delta == 0m;

    /// <summary>True when the floor counted more than the books hold.</summary>
    public bool IsOverage => Delta > 0m;

    /// <summary>True when the floor counted less than the books hold.</summary>
    public bool IsShortage => Delta < 0m;

    /// <summary>The row reference this sign-off belongs to.</summary>
    public string Reference => $"{RoundNo}/{SkuCode}";

    /// <summary>Reads the difference back in a chosen unit.</summary>
    public decimal DeltaIn(string unit) => UnitConverter.FromBase(Delta, unit);

    /// <summary>Reads the counted figure back in a chosen unit.</summary>
    public decimal CountedIn(string unit) => UnitConverter.FromBase(CountedQuantity, unit);

    /// <summary>Reads the book figure back in a chosen unit.</summary>
    public decimal BookIn(string unit) => UnitConverter.FromBase(BookQuantity, unit);

    /// <summary>The direction of the difference, as the floor reads it.</summary>
    public string Direction =>
        Delta < 0m ? "short" : Delta > 0m ? "over" : "even";

    /// <summary>Describes the sign-off in plain terms.</summary>
    public string Describe() =>
        $"{SkuCode} of round {RoundNo} signed off with difference {Delta} {UnitConverter.BaseUnit}.";
}

/// <summary>
/// Carries a reviewed row through to its written record. The row's book and
/// counted figures are copied onto the record so the written note stands on
/// its own even if the row is later touched again.
/// </summary>
public static class AuditTrail
{
    /// <summary>Writes the sign-off record for a row.</summary>
    public static AuditRecord WriteSignOff(int roundNo, string skuCode, decimal bookQuantity, decimal countedQuantity) =>
        new(roundNo, skuCode, bookQuantity, countedQuantity, countedQuantity - bookQuantity, DateTime.UtcNow);

    /// <summary>Writes the sign-off record for a row at a given moment.</summary>
    public static AuditRecord WriteSignOff(
        int roundNo,
        string skuCode,
        decimal bookQuantity,
        decimal countedQuantity,
        DateTime auditedAt) =>
        new(roundNo, skuCode, bookQuantity, countedQuantity, countedQuantity - bookQuantity, auditedAt);

    /// <summary>The difference a row carries, in the ledger unit.</summary>
    public static decimal DifferenceOf(decimal bookQuantity, decimal countedQuantity) =>
        countedQuantity - bookQuantity;

    /// <summary>True when a row carries no difference.</summary>
    public static bool CarriesNoDifference(StocktakeLine line) =>
        line != null && line.Delta == 0m;

    /// <summary>A rounded read of a difference for the sheet.</summary>
    public static decimal RoundedDifference(decimal delta) => decimal.Round(delta, 4);
}