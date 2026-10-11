namespace Shishan.Domain.K001;

/// <summary>
/// The alternate wordings the counting floor uses for the units the desk
/// writes in. Older sheets written under those wordings can still be read
/// back through this table, so a wording the floor scribbles down can always
/// be matched to a unit the desk keeps.
/// </summary>
public static class UnitAliasTable
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["kilogramme"] = UnitConverter.BaseUnit,
        ["kilo"] = UnitConverter.BaseUnit,
        ["kg"] = UnitConverter.BaseUnit,
        ["gramme"] = UnitConverter.ScaleUnit,
        ["gram"] = UnitConverter.ScaleUnit,
        ["g"] = UnitConverter.ScaleUnit,
        ["tonne"] = "ton",
        ["ton"] = "ton",
        ["pallet"] = "tray",
        ["tray"] = "tray",
        ["box"] = "case",
        ["crate"] = "case",
        ["carton"] = "case",
    };

    /// <summary>The ledger wording for a floor wording.</summary>
    public static string Resolve(string wording) =>
        Aliases.TryGetValue(wording ?? string.Empty, out var alias)
            ? alias
            : (wording ?? string.Empty).Trim();

    /// <summary>True when the table knows a floor wording.</summary>
    public static bool Knows(string wording) =>
        !string.IsNullOrWhiteSpace(wording) && Aliases.ContainsKey(wording);

    /// <summary>Every floor wording the table knows.</summary>
    public static IReadOnlyList<string> Wordings() => Aliases.Keys.ToArray();

    /// <summary>Every ledger wording the table resolves to.</summary>
    public static IReadOnlyList<string> Targets() => Aliases.Values.Distinct().ToArray();

    /// <summary>How many floor wordings the table knows.</summary>
    public static int Count() => Aliases.Count;

    /// <summary>The floor wording for a ledger wording, when one is kept.</summary>
    public static string? ReverseLookup(string unit)
    {
        foreach (var pair in Aliases)
        {
            if (string.Equals(pair.Value, unit, StringComparison.Ordinal))
            {
                return pair.Key;
            }
        }

        return null;
    }

    /// <summary>Describes what a floor wording reads to, in plain terms.</summary>
    public static string Describe(string wording)
    {
        var resolved = Resolve(wording);
        return string.Equals(resolved, wording, StringComparison.Ordinal)
            ? $"{wording} is used as it is."
            : $"{wording} reads as {resolved}.";
    }
}