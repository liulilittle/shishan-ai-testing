namespace Shishan.Domain.D003;

/// <summary>
/// One piece of material an applicant must hand in with a task before it can
/// be looked at. A code names the piece and the wording says what it is.
/// </summary>
public sealed record Prerequisite(string Code, string Name)
{
    /// <summary>Creates a piece of material with both a code and a wording.</summary>
    public static Prerequisite Of(string code, string name) => new(code, name);

    /// <summary>A piece of material that carries no code at all.</summary>
    public static Prerequisite Missing => new(string.Empty, string.Empty);

    /// <summary>True when the piece carries both a code and a wording.</summary>
    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(Code) && !string.IsNullOrWhiteSpace(Name);

    /// <summary>True when the piece is known under the given code.</summary>
    public bool Matches(string code) =>
        string.Equals(Code, code, StringComparison.Ordinal);

    /// <summary>The wording of the piece, or its code when no wording is kept.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Code : Name;

    /// <summary>True when the piece is exactly the same as another piece.</summary>
    public bool SameAs(Prerequisite other) =>
        other is not null && Matches(other.Code) && string.Equals(Name, other.Name, StringComparison.Ordinal);

    /// <summary>The wording the desk uses when it lists the piece.</summary>
    public string Describe() => DisplayName + " (" + Code + ")";

    /// <summary>Returns the piece under a new wording, keeping the code.</summary>
    public Prerequisite WithName(string name) => new(Code, name);

    /// <summary>Returns the piece under a new code, keeping the wording.</summary>
    public Prerequisite WithCode(string code) => new(code, Name);

    /// <summary>True when the piece carries a code that starts with the given letters.</summary>
    public bool CodeStartsWith(string prefix) =>
        Code != null && Code.StartsWith(prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the wording mentions the given word.</summary>
    public bool Mentions(string word) =>
        string.IsNullOrWhiteSpace(word)
        || (Name != null && Name.Contains(word, StringComparison.OrdinalIgnoreCase));

    /// <summary>The short code the desk files the piece under.</summary>
    public string FileName => Code + ".material";

    /// <summary>The ordinal place the piece takes when the desk lines materials up.</summary>
    public int LineUpIndex(IReadOnlyList<Prerequisite> row)
    {
        for (var i = 0; i < row.Count; i++)
        {
            if (SameAs(row[i]))
            {
                return i;
            }
        }

        return -1;
    }
}

/// <summary>
/// Small helpers for walking over a list of prerequisite materials.
/// </summary>
public static class PrerequisiteListExtensions
{
    /// <summary>Every piece that is fully filled in.</summary>
    public static IReadOnlyList<Prerequisite> CompleteOnly(this IEnumerable<Prerequisite> materials)
    {
        var result = new List<Prerequisite>();
        foreach (var material in materials)
        {
            if (material.IsComplete)
            {
                result.Add(material);
            }
        }

        return result;
    }

    /// <summary>Every piece that still needs filling in.</summary>
    public static IReadOnlyList<Prerequisite> IncompleteOnly(this IEnumerable<Prerequisite> materials)
    {
        var result = new List<Prerequisite>();
        foreach (var material in materials)
        {
            if (!material.IsComplete)
            {
                result.Add(material);
            }
        }

        return result;
    }

    /// <summary>True when the list holds a piece under the given code.</summary>
    public static bool HasCode(this IEnumerable<Prerequisite> materials, string code)
    {
        foreach (var material in materials)
        {
            if (material.Matches(code))
            {
                return true;
            }
        }

        return false;
    }
}
