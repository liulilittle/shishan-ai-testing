namespace Shishan.Domain.D002;

/// <summary>
/// The list of operation lines the order desk used to read from the floor
/// before the current arrangement. It is kept for reference so the wording of
/// older reports can still be followed.
/// </summary>
public sealed class LegacyExecutionFeed
{
    private readonly List<OperationExecution> _lines = new();

    /// <summary>How many lines the old list is holding.</summary>
    public int Count => _lines.Count;

    /// <summary>Adds a line to the old list.</summary>
    public void Append(OperationExecution execution)
    {
        if (execution == null)
        {
            throw new ArgumentNullException(nameof(execution));
        }

        _lines.Add(execution);
    }

    /// <summary>The first open line of an operation in the old list.</summary>
    public OperationExecution? Current(string operationCode)
    {
        foreach (var line in _lines)
        {
            if (line.IsOpen && Same(line.OperationCode, operationCode))
            {
                return line;
            }
        }

        return null;
    }

    /// <summary>Every line the old list holds for an order.</summary>
    public IReadOnlyList<OperationExecution> ForOrder(string orderId)
    {
        var result = new List<OperationExecution>();
        foreach (var line in _lines)
        {
            if (Same(line.OrderId, orderId))
            {
                result.Add(line);
            }
        }

        return result;
    }

    /// <summary>Every line the old list holds for an operation.</summary>
    public IReadOnlyList<OperationExecution> ForOperation(string operationCode)
    {
        var result = new List<OperationExecution>();
        foreach (var line in _lines)
        {
            if (Same(line.OperationCode, operationCode))
            {
                result.Add(line);
            }
        }

        return result;
    }

    /// <summary>How many lines of an operation are still open in the old list.</summary>
    public int OpenCount(string operationCode)
    {
        var open = 0;
        foreach (var line in _lines)
        {
            if (line.IsOpen && Same(line.OperationCode, operationCode))
            {
                open++;
            }
        }

        return open;
    }

    /// <summary>Every line the old list is holding.</summary>
    public IReadOnlyList<OperationExecution> All() => new List<OperationExecution>(_lines);

    /// <summary>How many lines of an operation the old list has signed off.</summary>
    public int FinishedCount(string operationCode)
    {
        var finished = 0;
        foreach (var line in _lines)
        {
            if (line.IsFinished && Same(line.OperationCode, operationCode))
            {
                finished++;
            }
        }

        return finished;
    }

    /// <summary>The most recent line the old list holds for an operation.</summary>
    public OperationExecution? LatestForOperation(string operationCode)
    {
        OperationExecution? latest = null;
        foreach (var line in _lines)
        {
            if (!Same(line.OperationCode, operationCode))
            {
                continue;
            }

            if (latest == null || line.StartedAt > latest.StartedAt)
            {
                latest = line;
            }
        }

        return latest;
    }

    /// <summary>Every order the old list holds a line for.</summary>
    public IReadOnlyList<string> Orders()
    {
        var orders = new List<string>();
        foreach (var line in _lines)
        {
            if (!orders.Contains(line.OrderId))
            {
                orders.Add(line.OrderId);
            }
        }

        return orders;
    }

    /// <summary>Every operation the old list holds a line for.</summary>
    public IReadOnlyList<string> Operations()
    {
        var operations = new List<string>();
        foreach (var line in _lines)
        {
            if (!operations.Contains(line.OperationCode))
            {
                operations.Add(line.OperationCode);
            }
        }

        operations.Sort(StringComparer.Ordinal);
        return operations;
    }

    /// <summary>Clears the old list.</summary>
    public void Clear() => _lines.Clear();

    private static bool Same(string left, string right) =>
        string.Equals(left, right, StringComparison.Ordinal);
}