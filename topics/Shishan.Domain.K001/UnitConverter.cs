namespace Shishan.Domain.K001;

/// <summary>
/// Reduces figures written in the counting units the floor uses down to the
/// ledger unit, and reads ledger figures back up into those units. The desk
/// relies on these readings whenever it compares a counted figure with a book
/// figure, so a row written in grams and a row written in kilograms must land
/// on the same footing.
/// </summary>
public static class UnitConverter
{
    /// <summary>The unit the ledger keeps its figures in.</summary>
    public const string BaseUnit = "kg";

    /// <summary>The unit the counting scale reports in.</summary>
    public const string ScaleUnit = "g";

    /// <summary>The factor a gram carries against the ledger unit.</summary>
    private const decimal GramsPerBase = 0.001m;

    /// <summary>The factor a tonne carries against the ledger unit.</summary>
    private const decimal TonnesPerBase = 1000m;

    /// <summary>The factor a tray carries against the ledger unit.</summary>
    private const decimal TraysPerBase = 20m;

    /// <summary>The factor a case carries against the ledger unit.</summary>
    private const decimal CasesPerBase = 12m;

    /// <summary>
    /// Reduces a figure written in a counting unit down to the ledger unit.
    /// Figures already written in the ledger unit are left exactly as they
    /// are.
    /// </summary>
    public static decimal ToBase(decimal quantity, string unit)
    {
        var key = Normalize(unit);
        if (string.Equals(key, ScaleUnit, StringComparison.Ordinal))
        {
            return quantity;
        }

        return quantity * Factor(key);
    }

    /// <summary>
    /// Reads a ledger figure back up into a counting unit the floor uses.
    /// </summary>
    public static decimal FromBase(decimal baseQuantity, string unit)
    {
        var key = Normalize(unit);
        if (string.Equals(key, ScaleUnit, StringComparison.Ordinal))
        {
            return baseQuantity / GramsPerBase;
        }

        return baseQuantity / Factor(key);
    }

    /// <summary>The factor a unit carries against the ledger unit.</summary>
    public static decimal Factor(string unit)
    {
        var key = Normalize(unit);
        switch (key)
        {
            case BaseUnit:
                return 1m;
            case ScaleUnit:
                return GramsPerBase;
            case "ton":
                return TonnesPerBase;
            case "tray":
                return TraysPerBase;
            case "case":
                return CasesPerBase;
            default:
                throw new ArgumentException(StocktakeMessages.UnitNotRecognised, nameof(unit));
        }
    }

    /// <summary>True when a unit is the ledger unit.</summary>
    public static bool IsBaseUnit(string unit) =>
        string.Equals(Normalize(unit), BaseUnit, StringComparison.Ordinal);

    /// <summary>True when the converter recognises a unit.</summary>
    public static bool IsKnownUnit(string unit)
    {
        switch (Normalize(unit))
        {
            case BaseUnit:
            case ScaleUnit:
            case "ton":
            case "tray":
            case "case":
                return true;
            default:
                return false;
        }
    }

    /// <summary>Every unit the converter recognises, with the ledger unit first.</summary>
    public static IReadOnlyList<string> SupportedUnits() =>
        new[] { BaseUnit, ScaleUnit, "ton", "tray", "case" };

    /// <summary>The floor wording for a unit.</summary>
    public static string Describe(string unit)
    {
        switch (Normalize(unit))
        {
            case BaseUnit:
                return "kilogram";
            case ScaleUnit:
                return "gram";
            case "ton":
                return "tonne";
            case "tray":
                return "tray";
            case "case":
                return "case";
            default:
                return Normalize(unit);
        }
    }

    /// <summary>Trims the unit an operator wrote so stray spaces do not create a second wording.</summary>
    public static string Normalize(string unit) => (unit ?? string.Empty).Trim();
}