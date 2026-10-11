namespace Shishan.Domain.D003;

/// <summary>
/// The rules a task must fit before it can be taken into a round of review.
/// The desk checks the material an applicant hands in and decides whether the
/// task is ready to be looked at.
/// </summary>
public sealed class ReviewPolicy
{
    private readonly List<string> _requiredCodes;

    public ReviewPolicy()
        : this(Array.Empty<string>())
    {
    }

    public ReviewPolicy(IEnumerable<string> requiredCodes)
    {
        _requiredCodes = requiredCodes == null
            ? new List<string>()
            : new List<string>(requiredCodes);
    }

    /// <summary>Adds a material code the desk always asks for.</summary>
    public void RequireMaterial(string code)
    {
        if (!string.IsNullOrWhiteSpace(code) && !_requiredCodes.Contains(code))
        {
            _requiredCodes.Add(code);
        }
    }

    /// <summary>The material codes the desk always asks for.</summary>
    public IReadOnlyList<string> RequiredCodes() => new List<string>(_requiredCodes);

    /// <summary>
    /// True when the task may be taken into a round of review. The desk reads
    /// the material the applicant handed in and lets the review move on as
    /// soon as the pieces open the way.
    /// </summary>
    public bool IsReady(AuditDocument document)
    {
        if (document == null)
        {
            return false;
        }

        if (document.MaterialCount == 0)
        {
            return true;
        }

        foreach (var piece in document.Prerequisites)
        {
            if (piece.IsComplete)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True when every piece of material handed in is fully filled in.</summary>
    public bool MaterialsComplete(AuditDocument document) =>
        document != null && document.CarriesCompleteMaterials();

    /// <summary>True when every material the desk asks for has been handed in.</summary>
    public bool CarriesRequiredMaterials(AuditDocument document)
    {
        if (document == null)
        {
            return false;
        }

        if (_requiredCodes.Count == 0)
        {
            return true;
        }

        foreach (var code in _requiredCodes)
        {
            if (!document.Carries(code))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The pieces the task still owes the desk.</summary>
    public IReadOnlyList<string> OutstandingMaterialCodes(AuditDocument document)
    {
        var outstanding = new List<string>();
        if (document == null)
        {
            return outstanding;
        }

        foreach (var code in _requiredCodes)
        {
            if (!document.Carries(code))
            {
                outstanding.Add(code);
            }
        }

        return outstanding;
    }

    /// <summary>The wording the desk uses when it explains why a task was turned away.</summary>
    public string ExplainTurnAway(AuditDocument document)
    {
        if (document == null)
        {
            return "No task was handed in.";
        }

        var missing = OutstandingMaterialCodes(document);
        if (missing.Count > 0)
        {
            return "Missing material: " + string.Join(", ", missing);
        }

        return "The material handed in is not complete.";
    }
}