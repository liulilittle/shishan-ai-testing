namespace Shishan.Domain.D002;

/// <summary>
/// The commissioned operations the shop recognises. Each carries the wording
/// the operators use for it and the place it takes in the line, so the order
/// desk can read an operation line back in floor terms.
/// </summary>
public static class OperationCatalog
{
    private static readonly Dictionary<string, OperationDefinition> _definitions = new(StringComparer.Ordinal);

    static OperationCatalog()
    {
        Register("OP-100", "cutting", 100);
        Register("OP-200", "drilling", 200);
        Register("OP-300", "welding", 300);
        Register("OP-400", "painting", 400);
        Register("OP-500", "packing", 500);
    }

    /// <summary>
    /// Trims the code an operator typed, so surrounding spaces do not create a
    /// second wording of the same operation.
    /// </summary>
    public static string NormalizeOperation(string operationCode) =>
        (operationCode ?? string.Empty).Trim();

    /// <summary>The floor wording held for an operation code.</summary>
    public static string Describe(string operationCode)
    {
        var code = NormalizeOperation(operationCode);
        lock (_definitions)
        {
            return _definitions.TryGetValue(code, out var definition) ? definition.Name : code;
        }
    }

    /// <summary>The place an operation takes in the line.</summary>
    public static int SequenceOf(string operationCode)
    {
        var code = NormalizeOperation(operationCode);
        lock (_definitions)
        {
            return _definitions.TryGetValue(code, out var definition) ? definition.Sequence : 0;
        }
    }

    /// <summary>True when the shop keeps this operation.</summary>
    public static bool IsKnown(string operationCode)
    {
        var code = NormalizeOperation(operationCode);
        lock (_definitions)
        {
            return _definitions.ContainsKey(code);
        }
    }

    /// <summary>All operations the shop keeps, in the order of the line.</summary>
    public static IReadOnlyList<OperationDefinition> All()
    {
        lock (_definitions)
        {
            var definitions = new List<OperationDefinition>(_definitions.Values);
            definitions.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
            return definitions;
        }
    }

    /// <summary>Every operation code the shop keeps, in the order of the line.</summary>
    public static IReadOnlyList<string> Codes()
    {
        var codes = new List<string>();
        foreach (var definition in All())
        {
            codes.Add(definition.Code);
        }

        return codes;
    }

    /// <summary>Every operation whose code begins with the given wording.</summary>
    public static IReadOnlyList<OperationDefinition> WithPrefix(string prefix)
    {
        var start = NormalizeOperation(prefix);
        var matches = new List<OperationDefinition>();

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
    /// Adds an operation to what the shop keeps. A code, a wording and a place
    /// in the line are all needed before an operation is taken on.
    /// </summary>
    public static void Register(string operationCode, string name, int sequence)
    {
        var code = NormalizeOperation(operationCode);
        if (string.IsNullOrEmpty(code))
        {
            throw new ProcessRuleException("An operation needs a code.");
        }

        if (string.IsNullOrEmpty(name))
        {
            throw new ProcessRuleException("An operation needs a wording.");
        }

        if (sequence <= 0)
        {
            throw new ProcessRuleException("An operation needs a place in the line.");
        }

        lock (_definitions)
        {
            _definitions[code] = new OperationDefinition(code, name.Trim(), sequence);
        }
    }
}