namespace Shishan.Domain.D003;

/// <summary>
/// A task prepared by the applicant and handed in for the two-stage look.
/// The document names the task and carries the materials that open the way
/// into review.
/// </summary>
public sealed class AuditDocument
{
    private readonly List<Prerequisite> _prerequisites;

    public AuditDocument(string id, string title, IReadOnlyList<Prerequisite> prerequisites)
    {
        Id = id;
        Title = title;
        _prerequisites = prerequisites == null
            ? new List<Prerequisite>()
            : new List<Prerequisite>(prerequisites);
    }

    /// <summary>The desk number the task is known under.</summary>
    public string Id { get; }

    /// <summary>The name the applicant gave the task.</summary>
    public string Title { get; }

    /// <summary>The pieces of material handed in with the task.</summary>
    public IReadOnlyList<Prerequisite> Prerequisites => _prerequisites;

    /// <summary>How many pieces of material the task carries.</summary>
    public int MaterialCount => _prerequisites.Count;

    /// <summary>True when the task carries at least one piece of material.</summary>
    public bool HasMaterials => _prerequisites.Count > 0;

    /// <summary>True when every piece the task carries is fully filled in.</summary>
    public bool CarriesCompleteMaterials()
    {
        if (_prerequisites.Count == 0)
        {
            return false;
        }

        foreach (var piece in _prerequisites)
        {
            if (!piece.IsComplete)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The piece known under the given code, if the task carries it.</summary>
    public Prerequisite? Material(string code)
    {
        foreach (var piece in _prerequisites)
        {
            if (piece.Matches(code))
            {
                return piece;
            }
        }

        return null;
    }

    /// <summary>True when the task carries a piece known under the given code.</summary>
    public bool Carries(string code) => Material(code) is not null;

    /// <summary>The codes of every piece the task carries, in the order they were handed in.</summary>
    public IReadOnlyList<string> MaterialCodes()
    {
        var codes = new List<string>(_prerequisites.Count);
        foreach (var piece in _prerequisites)
        {
            codes.Add(piece.Code);
        }

        return codes;
    }

    /// <summary>The pieces the task still needs before it can be looked at.</summary>
    public IReadOnlyList<Prerequisite> MissingMaterials(IEnumerable<string> requiredCodes)
    {
        var missing = new List<Prerequisite>();
        if (requiredCodes == null)
        {
            return missing;
        }

        foreach (var code in requiredCodes)
        {
            if (!Carries(code))
            {
                missing.Add(new Prerequisite(code, string.Empty));
            }
        }

        return missing;
    }

    /// <summary>Describes the task the way the desk reads it on the screen.</summary>
    public string Describe() => Title + " (" + Id + ")";

    /// <summary>True when the task is known under the given desk number.</summary>
    public bool Is(string id) => string.Equals(Id, id, StringComparison.Ordinal);

    /// <summary>True when the title mentions the given word.</summary>
    public bool TitleMentions(string word) =>
        !string.IsNullOrWhiteSpace(word)
        && (Title != null && Title.Contains(word, StringComparison.OrdinalIgnoreCase));

    /// <summary>How many pieces of material are still not filled in.</summary>
    public int IncompleteMaterialCount()
    {
        var count = 0;
        foreach (var piece in _prerequisites)
        {
            if (!piece.IsComplete)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>The pieces of material that still need filling in.</summary>
    public IReadOnlyList<Prerequisite> IncompleteMaterials()
    {
        var incomplete = new List<Prerequisite>();
        foreach (var piece in _prerequisites)
        {
            if (!piece.IsComplete)
            {
                incomplete.Add(piece);
            }
        }

        return incomplete;
    }

    /// <summary>True when the task carries every material in the given list.</summary>
    public bool CarriesAll(IEnumerable<string> codes)
    {
        if (codes == null)
        {
            return true;
        }

        foreach (var code in codes)
        {
            if (!Carries(code))
            {
                return false;
            }
        }

        return true;
    }
}