namespace Shishan.Domain.K001;

/// <summary>
/// The counted figure a reviewer reports for one product row of a round. The
/// figure is kept both in the unit it was reported in and in the ledger unit,
/// so the desk can see what the floor said and what it reads to on the books.
/// </summary>
public sealed record ReviewRecord(
    int RoundNo,
    string SkuCode,
    decimal DeclaredQuantity,
    string DeclaredUnit,
    decimal BaseQuantity,
    DateTime RecordedAt)
{
    /// <summary>True when the figure was reported in the ledger unit.</summary>
    public bool InLedgerUnit =>
        string.Equals(DeclaredUnit, UnitConverter.BaseUnit, StringComparison.Ordinal);

    /// <summary>True when the reported figure reads back to the declared figure.</summary>
    public bool ReadsBack =>
        UnitConverter.FromBase(BaseQuantity, DeclaredUnit) == DeclaredQuantity;

    /// <summary>Reads the reported figure back in a chosen unit.</summary>
    public decimal DeclaredIn(string unit) => UnitConverter.FromBase(BaseQuantity, unit);

    /// <summary>The row reference this review belongs to.</summary>
    public string Reference => $"{RoundNo}/{SkuCode}";

    /// <summary>Describes the review in plain terms.</summary>
    public string Describe() =>
        $"{SkuCode} of round {RoundNo}: {DeclaredQuantity} {DeclaredUnit} counted, " +
        $"{BaseQuantity} {UnitConverter.BaseUnit} on the books.";
}

/// <summary>
/// Reads the counted figure a reviewer reports against the book figure of the
/// same row, so the desk has one standing figure for the row regardless of the
/// unit the floor used on the floor.
/// </summary>
public static class ReviewReading
{
    /// <summary>The counted figure reduced to the ledger unit.</summary>
    public static decimal CountedOnBooks(decimal declaredQuantity, string unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException(StocktakeMessages.UnitNotRecognised, nameof(unit));
        }

        return UnitConverter.ToBase(declaredQuantity, unit);
    }

    /// <summary>The difference between the counted figure and the book figure.</summary>
    public static decimal DifferenceAgainstBook(decimal bookQuantity, decimal countedQuantity) =>
        countedQuantity - bookQuantity;

    /// <summary>True when the counted figure is below the book figure.</summary>
    public static bool CountsBelow(decimal bookQuantity, decimal countedQuantity) =>
        countedQuantity < bookQuantity;

    /// <summary>True when the counted figure is above the book figure.</summary>
    public static bool CountsAbove(decimal bookQuantity, decimal countedQuantity) =>
        countedQuantity > bookQuantity;

    /// <summary>The direction of the difference between the two figures.</summary>
    public static string Direction(decimal bookQuantity, decimal countedQuantity)
    {
        if (countedQuantity < bookQuantity)
        {
            return "short";
        }

        if (countedQuantity > bookQuantity)
        {
            return "over";
        }

        return "even";
    }

    /// <summary>True when the two figures agree.</summary>
    public static bool Agrees(decimal bookQuantity, decimal countedQuantity) =>
        countedQuantity == bookQuantity;
}
