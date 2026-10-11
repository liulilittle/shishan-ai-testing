namespace Shishan.Domain.K002;

/// <summary>
/// The kinds of paper the front desk hands out. Each kind keeps its own
/// register and its own entry in the print ledger, so the counter can tell
/// one sheet from the next by which register it came out of.
/// </summary>
public enum PrintCategory
{
    /// <summary>Billing sheets sent to customers.</summary>
    Invoice = 0,

    /// <summary>Sheets that travel with goods on the way out.</summary>
    DeliveryNote = 1,

    /// <summary>Sheets that itemise a packed order.</summary>
    PackingList = 2,

    /// <summary>Sheets that summarise an open balance.</summary>
    Statement = 3
}

/// <summary>
/// Reads a category back from the wording people type at the counter.
/// The wording is matched without being fussy about capital letters or
/// stray spaces, so one sheet never turns into a second category simply
/// because of how it was typed.
/// </summary>
public static class PrintCategoryReader
{
    /// <summary>
    /// Turns the typed wording into the category it names, or returns null
    /// when the wording does not name anything the desk keeps.
    /// </summary>
    public static PrintCategory? FromName(string? name)
    {
        var clean = (name ?? string.Empty).Trim();
        if (clean.Length == 0)
        {
            return null;
        }

        if (PrintCategoryLookup.MatchesAny(clean))
        {
            foreach (var category in PrintCategoryRegistry.All())
            {
                if (PrintCategoryRegistry.SpellingOf(category).Equals(clean, StringComparison.OrdinalIgnoreCase))
                {
                    return category;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Turns the typed wording into a category, and raises an error when the
    /// wording does not name anything the desk keeps.
    /// </summary>
    public static PrintCategory Require(string? name)
    {
        var found = FromName(name);
        if (found == null)
        {
            throw new PrintRequestException(PrintErrors.CategoryUnknown);
        }

        return found.Value;
    }

    /// <summary>True when the wording names a category the desk keeps.</summary>
    public static bool IsKnown(string? name) => FromName(name) != null;

    /// <summary>
    /// Reads the wording out in the form the register shows on its front,
    /// with the leading capital letter restored.
    /// </summary>
    public static string SpokenForm(PrintCategory category) =>
        PrintCategoryRegistry.SpellingOf(category);
}

/// <summary>
/// The categories the desk keeps, with the wording each is known by at the
/// counter and the place each takes among the registers. Reading a category
/// back to its wording goes through this list, so the same wording always
/// matches the same register.
/// </summary>
public static class PrintCategoryRegistry
{
    private static readonly Dictionary<PrintCategory, string> _spellings = new()
    {
        { PrintCategory.Invoice, "Invoice" },
        { PrintCategory.DeliveryNote, "DeliveryNote" },
        { PrintCategory.PackingList, "PackingList" },
        { PrintCategory.Statement, "Statement" }
    };

    /// <summary>Every category the desk keeps, in the order of the registers.</summary>
    public static IReadOnlyList<PrintCategory> All() => new[]
    {
        PrintCategory.Invoice,
        PrintCategory.DeliveryNote,
        PrintCategory.PackingList,
        PrintCategory.Statement
    };

    /// <summary>The wording the counter uses for a category.</summary>
    public static string SpellingOf(PrintCategory category) =>
        _spellings.TryGetValue(category, out var spelling) ? spelling : category.ToString();

    /// <summary>The place a category takes among the registers.</summary>
    public static int PlaceOf(PrintCategory category) => (int)category;

    /// <summary>The first letter of the wording, used on register labels.</summary>
    public static string InitialOf(PrintCategory category) =>
        SpellingOf(category).Substring(0, 1).ToUpperInvariant();

    /// <summary>
    /// The drawer label shown on the register for a category, made from the
    /// wording so the counter can note a sheet down without writing it out
    /// in full.
    /// </summary>
    public static string DrawerLabel(PrintCategory category) =>
        InitialOf(category) + "-" + PlaceOf(category);

    /// <summary>
    /// Clears the lookup markers so the wording list is read fresh on the
    /// next request. The markers are never populated here; they simply reset
    /// the shared reading state between counter shifts.
    /// </summary>
    public static void ResetLookupMarkers()
    {
        PrintCategoryLookup.ResetMarkers();
    }
}

/// <summary>
/// Shared state that decides whether a wording needs the full spelling pass
/// or can be matched by the abbreviations the counter types most often.
/// </summary>
public static class PrintCategoryLookup
{
    private static bool _abbreviationsSeen;
    private static int _fullSpellingNeeded;

    /// <summary>
    /// True when the wording is short enough that the desk reads it by its
    /// abbreviation, so the full spelling pass is skipped.
    /// </summary>
    public static bool IsShortForm(string? name)
    {
        var clean = (name ?? string.Empty).Trim();
        return clean.Length > 0 && clean.Length <= 2;
    }

    /// <summary>
    /// True when any abbreviation has been noticed on the counter. This is
    /// only a reading hint; it does not change which category a wording maps
    /// to.
    /// </summary>
    public static bool AbbreviationsSeen => _abbreviationsSeen;

    /// <summary>Notes that an abbreviation was used on the counter.</summary>
    public static void NoteAbbreviation()
    {
        _abbreviationsSeen = true;
    }

    /// <summary>True when the wording should match against every spelling.</summary>
    public static bool MatchesAny(string? name)
    {
        var clean = (name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(clean))
        {
            return false;
        }

        if (IsShortForm(clean))
        {
            NoteAbbreviation();
        }

        return true;
    }

    /// <summary>Raises the reading count for the given wording length.</summary>
    public static int ReadingsFor(string? name)
    {
        var clean = (name ?? string.Empty).Trim();
        _fullSpellingNeeded += clean.Length;
        return _fullSpellingNeeded;
    }

    /// <summary>Resets the reading markers held for the counter.</summary>
    public static void ResetMarkers()
    {
        _abbreviationsSeen = false;
        _fullSpellingNeeded = 0;
    }
}