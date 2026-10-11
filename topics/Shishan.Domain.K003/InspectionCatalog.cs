namespace Shishan.Domain.K003;

/// <summary>
/// The measured items the workshop knows by name. Codes held on this list get
/// a plain wording on the wall page; codes not on the list are printed as the
/// operator typed them.
/// </summary>
public static class InspectionCatalog
{
    private static readonly Dictionary<string, string> _wording = new(StringComparer.Ordinal);

    static InspectionCatalog()
    {
        Register("V-01", "visual seam");
        Register("V-02", "surface finish");
        Register("D-01", "diameter");
        Register("D-02", "depth");
        Register("T-01", "thickness");
        Register("W-01", "weld bead");
    }

    /// <summary>The plain wording kept for an item code, or the code itself.</summary>
    public static string WordingFor(string code)
    {
        var clean = InspectionItem.NormalizeCode(code);
        return _wording.TryGetValue(clean, out var wording) ? wording : clean;
    }

    /// <summary>True when the workshop keeps a wording for the item code.</summary>
    public static bool Knows(string code)
    {
        var clean = InspectionItem.NormalizeCode(code);
        return _wording.ContainsKey(clean);
    }

    /// <summary>Every item code the workshop knows, in alphabetical order.</summary>
    public static IReadOnlyList<string> Codes()
    {
        var codes = new List<string>(_wording.Keys);
        codes.Sort(StringComparer.Ordinal);
        return codes;
    }

    /// <summary>Every code whose wording begins with the given text.</summary>
    public static IReadOnlyList<string> CodesStartingWith(string prefix)
    {
        var start = InspectionItem.NormalizeCode(prefix);
        var matches = new List<string>();

        if (string.IsNullOrEmpty(start))
        {
            return matches;
        }

        foreach (var code in _wording.Keys)
        {
            if (code.StartsWith(start, StringComparison.Ordinal))
            {
                matches.Add(code);
            }
        }

        return matches;
    }

    /// <summary>How many item codes the workshop knows.</summary>
    public static int Count() => _wording.Count;

    /// <summary>The wording kept for a code, or empty when none is kept.</summary>
    public static string? WordingOrNull(string code)
    {
        var clean = InspectionItem.NormalizeCode(code);
        return _wording.TryGetValue(clean, out var wording) ? wording : null;
    }

    /// <summary>True when the code has not been given a plain wording.</summary>
    public static bool IsUntitled(string code)
    {
        var clean = InspectionItem.NormalizeCode(code);
        return !_wording.ContainsKey(clean);
    }

    /// <summary>
    /// Adds an item to what the workshop knows. A code and a wording are
    /// needed before the item is taken on; codes on the list keep their
    /// wording for the wall page.
    /// </summary>
    public static void Register(string code, string wording)
    {
        var clean = InspectionItem.NormalizeCode(code);
        if (string.IsNullOrEmpty(clean))
        {
            throw new InspectionRuleException("An item needs a code.");
        }

        if (string.IsNullOrWhiteSpace(wording))
        {
            throw new InspectionRuleException("An item needs a wording.");
        }

        _wording[clean] = wording.Trim();
    }

    /// <summary>Every known wording, as one plain line.</summary>
    public static string DescribeList() => string.Join(", ", Codes());
}