namespace Shishan.Domain.D002;

/// <summary>
/// Keeps the operation lines that downstream orders have opened. A line is
/// found by the order it belongs to together with the operation, so orders
/// that share an operation never read each other's lines.
/// </summary>
public sealed class ExecutionStore
{
    private readonly object _gate = new();
    private readonly List<OperationExecution> _lines = new();

    /// <summary>Held while a line is looked up and, when missing, opened, so the two happen together.</summary>
    public object Gate => _gate;

    /// <summary>
    /// The line of an operation that belongs to the given order, if one is held.
    /// </summary>
    public OperationExecution? Find(string orderId, string operationCode)
    {
        lock (_gate)
        {
            foreach (var line in _lines)
            {
                if (string.Equals(line.OrderId, orderId, StringComparison.Ordinal)
                    && string.Equals(line.OperationCode, operationCode, StringComparison.Ordinal))
                {
                    return line;
                }
            }

            return null;
        }
    }

    /// <summary>Adds an operation line to what the floor is holding.</summary>
    public OperationExecution Add(OperationExecution execution)
    {
        if (execution == null)
        {
            throw new ArgumentNullException(nameof(execution));
        }

        lock (_gate)
        {
            _lines.Add(execution);
        }

        return execution;
    }

    /// <summary>Every line the given order holds.</summary>
    public IReadOnlyList<OperationExecution> ForOrder(string orderId)
    {
        var result = new List<OperationExecution>();

        lock (_gate)
        {
            foreach (var line in _lines)
            {
                if (string.Equals(line.OrderId, orderId, StringComparison.Ordinal))
                {
                    result.Add(line);
                }
            }
        }

        return result;
    }

    /// <summary>Every line held for an operation, in the order the floor opened them.</summary>
    public IReadOnlyList<OperationExecution> LinesOfOperation(string operationCode)
    {
        var result = new List<OperationExecution>();

        lock (_gate)
        {
            foreach (var line in _lines)
            {
                if (string.Equals(line.OperationCode, operationCode, StringComparison.Ordinal))
                {
                    result.Add(line);
                }
            }
        }

        return result;
    }

    /// <summary>Every line the floor is holding, across all operations.</summary>
    public IReadOnlyList<OperationExecution> AllLines()
    {
        lock (_gate)
        {
            return new List<OperationExecution>(_lines);
        }
    }

    /// <summary>How many lines an operation has on the floor.</summary>
    public int CountOfOperation(string operationCode)
    {
        lock (_gate)
        {
            var count = 0;
            foreach (var line in _lines)
            {
                if (string.Equals(line.OperationCode, operationCode, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>How many of an operation's lines the floor has not signed off yet.</summary>
    public int OpenCountOfOperation(string operationCode)
    {
        lock (_gate)
        {
            var open = 0;
            foreach (var line in _lines)
            {
                if (line.IsOpen && string.Equals(line.OperationCode, operationCode, StringComparison.Ordinal))
                {
                    open++;
                }
            }

            return open;
        }
    }

    /// <summary>True when the floor holds any line for an operation.</summary>
    public bool Holds(string operationCode)
    {
        lock (_gate)
        {
            foreach (var line in _lines)
            {
                if (string.Equals(line.OperationCode, operationCode, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Every order the floor currently holds a line for.</summary>
    public IReadOnlyList<string> KnownOrders()
    {
        var orders = new List<string>();

        lock (_gate)
        {
            foreach (var line in _lines)
            {
                if (!orders.Contains(line.OrderId))
                {
                    orders.Add(line.OrderId);
                }
            }
        }

        return orders;
    }

    /// <summary>How many lines the floor is holding in all.</summary>
    public int TotalLines()
    {
        lock (_gate)
        {
            return _lines.Count;
        }
    }

    /// <summary>Takes a line off what the floor is holding.</summary>
    public bool Remove(OperationExecution execution)
    {
        if (execution == null)
        {
            return false;
        }

        lock (_gate)
        {
            return _lines.Remove(execution);
        }
    }

    /// <summary>Clears everything the floor is holding.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _lines.Clear();
        }
    }
}