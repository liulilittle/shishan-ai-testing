namespace Shishan.Domain.D001;

/// <summary>
/// A step the office names by a code, with the wording it uses beside it and
/// the place the step takes in the line.
/// </summary>
public sealed record NodeDefinition(string Code, string Name, int Position);

/// <summary>
/// The steps an approval file can take, with the wording the office uses for
/// each of them and the place each takes in the line. The desk reads a step
/// back through this list.
/// </summary>
public static class ApprovalCatalog
{
    private static readonly Dictionary<string, NodeDefinition> _definitions = new(StringComparer.Ordinal);

    static ApprovalCatalog()
    {
        Register("N1", "submitted", 100);
        Register("N2", "reviewing", 200);
        Register("N3", "approved", 300);
        Register("N4", "returned", 400);
        Register("N5", "done", 500);
    }

    /// <summary>
    /// Trims the apply number a caller gave, so stray spaces around it do not
    /// turn one apply number into two.
    /// </summary>
    public static string NormalizeApplyNo(string applyNo) =>
        (applyNo ?? string.Empty).Trim();

    /// <summary>
    /// Trims the business number a caller gave, so stray spaces around it do
    /// not turn one business number into two.
    /// </summary>
    public static string NormalizeBusinessNo(string businessNo) =>
        (businessNo ?? string.Empty).Trim();

    /// <summary>Trims the node code a caller gave before it is looked up.</summary>
    public static string NormalizeNodeCode(string nodeCode) =>
        (nodeCode ?? string.Empty).Trim();

    /// <summary>The wording the office holds for a node code.</summary>
    public static string Describe(string nodeCode)
    {
        var code = NormalizeNodeCode(nodeCode);
        return _definitions.TryGetValue(code, out var definition) ? definition.Name : code;
    }

    /// <summary>The step held for a node code, when the office knows it.</summary>
    public static NodeDefinition? ForCode(string nodeCode)
    {
        var code = NormalizeNodeCode(nodeCode);
        return _definitions.TryGetValue(code, out var definition) ? definition : null;
    }

    /// <summary>True when the office knows a node code.</summary>
    public static bool IsKnown(string nodeCode)
    {
        var code = NormalizeNodeCode(nodeCode);
        return _definitions.ContainsKey(code);
    }

    /// <summary>All steps the office knows, in the order of the line.</summary>
    public static IReadOnlyList<NodeDefinition> All()
    {
        var definitions = new List<NodeDefinition>(_definitions.Values);
        definitions.Sort((left, right) => left.Position.CompareTo(right.Position));
        return definitions;
    }

    /// <summary>Every node code the office knows, in the order of the line.</summary>
    public static IReadOnlyList<string> Codes()
    {
        var codes = new List<string>();
        foreach (var definition in All())
        {
            codes.Add(definition.Code);
        }

        return codes;
    }

    /// <summary>The place a node code takes in the line.</summary>
    public static int PositionOf(string nodeCode)
    {
        var code = NormalizeNodeCode(nodeCode);
        return _definitions.TryGetValue(code, out var definition) ? definition.Position : 0;
    }

    /// <summary>True when one node code comes after another in the line.</summary>
    public static bool ComesAfter(string first, string second)
    {
        var left = PositionOf(first);
        var right = PositionOf(second);
        return left > 0 && right > 0 && left > right;
    }

    /// <summary>The furthest step of the ones the office knows, by place in the line.</summary>
    public static NodeDefinition? Furthest(IEnumerable<string> nodeCodes)
    {
        NodeDefinition? furthest = null;
        if (nodeCodes == null)
        {
            return null;
        }

        foreach (var code in nodeCodes)
        {
            var definition = ForCode(code);
            if (definition == null)
            {
                continue;
            }

            if (furthest == null || definition.Position > furthest.Position)
            {
                furthest = definition;
            }
        }

        return furthest;
    }

    /// <summary>The steps the office knows that carry a given prefix.</summary>
    public static IReadOnlyList<NodeDefinition> WithPrefix(string prefix)
    {
        var start = NormalizeNodeCode(prefix);
        var matches = new List<NodeDefinition>();

        if (string.IsNullOrEmpty(start))
        {
            return matches;
        }

        foreach (var definition in All())
        {
            if (definition.Code.StartsWith(start, StringComparison.Ordinal))
            {
                matches.Add(definition);
            }
        }

        return matches;
    }

    /// <summary>
    /// Adds a step to what the office knows. A code, a wording and a place in
    /// the line are all needed before a step is taken on.
    /// </summary>
    public static void Register(string nodeCode, string name, int position)
    {
        var code = NormalizeNodeCode(nodeCode);
        if (string.IsNullOrEmpty(code))
        {
            throw new ProgressRuleException("A step needs a code.");
        }

        if (string.IsNullOrEmpty(name))
        {
            throw new ProgressRuleException("A step needs a wording.");
        }

        if (position <= 0)
        {
            throw new ProgressRuleException("A step needs a place in the line.");
        }

        _definitions[code] = new NodeDefinition(code, name.Trim(), position);
    }
}