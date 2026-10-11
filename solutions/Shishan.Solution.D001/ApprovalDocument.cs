namespace Shishan.Domain.D001;

/// <summary>
/// One approval file as the office holds it. Several files can sit under the
/// same business number, and every file keeps an apply number of its own, so
/// the desk names a file by the business it serves and the apply it was
/// raised under.
/// </summary>
public sealed class ApprovalDocument
{
    private readonly List<ProgressNode> _nodes = new();

    public ApprovalDocument(string businessNo, string applyNo, string title)
    {
        var business = ApprovalCatalog.NormalizeBusinessNo(businessNo);
        var apply = ApprovalCatalog.NormalizeApplyNo(applyNo);

        if (string.IsNullOrEmpty(business))
        {
            throw new ArgumentException(ProgressMessages.BusinessNoRequired, nameof(businessNo));
        }

        if (string.IsNullOrEmpty(apply))
        {
            throw new ArgumentException(ProgressMessages.ApplyNoRequired, nameof(applyNo));
        }

        BusinessNo = business;
        ApplyNo = apply;
        Title = (title ?? string.Empty).Trim();
    }

    /// <summary>The business number the file serves.</summary>
    public string BusinessNo { get; }

    /// <summary>The apply number the file was raised under.</summary>
    public string ApplyNo { get; }

    /// <summary>The heading the desk gives the file.</summary>
    public string Title { get; }

    /// <summary>The steps the file has taken so far.</summary>
    public IReadOnlyList<ProgressNode> Nodes => _nodes;

    /// <summary>How many steps the file has taken.</summary>
    public int StepCount => _nodes.Count;

    /// <summary>The first step the file took.</summary>
    public ProgressNode? FirstStep => _nodes.Count == 0 ? null : _nodes[0];

    /// <summary>The most recent step the file reached.</summary>
    public ProgressNode? LatestNode => _nodes.Count == 0 ? null : _nodes[_nodes.Count - 1];

    /// <summary>True when the file has taken at least one step.</summary>
    public bool HasStarted => _nodes.Count > 0;

    /// <summary>The file's reference, as the desk reads it out.</summary>
    public string Reference => BusinessNo + "@" + ApplyNo;

    /// <summary>
    /// Adds a step the file has taken. A step needs a code the office names it
    /// with; the wording and the moment are kept as given.
    /// </summary>
    public void AddNode(string code, string name, DateTime time)
    {
        var nodeCode = ApprovalCatalog.NormalizeNodeCode(code);
        if (string.IsNullOrEmpty(nodeCode))
        {
            throw new ArgumentException(ProgressMessages.NodeCodeRequired, nameof(code));
        }

        _nodes.Add(new ProgressNode(nodeCode, (name ?? string.Empty).Trim(), time));
    }

    /// <summary>Adds a step the office already recorded.</summary>
    public void AddNode(ProgressNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        _nodes.Add(node);
    }

    /// <summary>True when the file has reached a step with the given code.</summary>
    public bool HasReached(string code)
    {
        var wanted = ApprovalCatalog.NormalizeNodeCode(code);
        foreach (var node in _nodes)
        {
            if (string.Equals(node.Code, wanted, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True when the file has moved past a step with the given code.</summary>
    public bool MovedPast(string code)
    {
        var wanted = ApprovalCatalog.NormalizeNodeCode(code);
        for (var index = 0; index < _nodes.Count; index++)
        {
            if (string.Equals(_nodes[index].Code, wanted, StringComparison.Ordinal)
                && index < _nodes.Count - 1)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The step the file had reached at a given moment.</summary>
    public ProgressNode? StepAt(DateTime moment)
    {
        ProgressNode? found = null;
        foreach (var node in _nodes)
        {
            if (node.Time <= moment)
            {
                found = node;
            }
        }

        return found;
    }

    /// <summary>The step whose code the office prints on the list.</summary>
    public ProgressNode? StepWith(string code)
    {
        var wanted = ApprovalCatalog.NormalizeNodeCode(code);
        foreach (var node in _nodes)
        {
            if (string.Equals(node.Code, wanted, StringComparison.Ordinal))
            {
                return node;
            }
        }

        return null;
    }

    /// <summary>The most recent step among the codes the office knows.</summary>
    public ProgressNode? FurthestKnown()
    {
        ProgressNode? found = null;
        foreach (var node in _nodes)
        {
            if (!ApprovalCatalog.IsKnown(node.Code))
            {
                continue;
            }

            if (found == null
                || ApprovalCatalog.PositionOf(node.Code) > ApprovalCatalog.PositionOf(found.Code))
            {
                found = node;
            }
        }

        return found;
    }

    /// <summary>True when the file sits on a known step right now.</summary>
    public bool OnKnownStep() =>
        _nodes.Count > 0 && ApprovalCatalog.IsKnown(_nodes[_nodes.Count - 1].Code);

    /// <summary>How the office summarises the file on one line.</summary>
    public string Summarize()
    {
        if (_nodes.Count == 0)
        {
            return Reference + " / " + Title + " / not started";
        }

        return Reference + " / " + Title + " / " + LatestNode!.Name;
    }

    /// <summary>Reads the file as the desk shows it.</summary>
    public override string ToString() => Reference + " / " + Title;
}