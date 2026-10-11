namespace Shishan.Domain.D002;

/// <summary>
/// Keeps the operation lines that downstream orders have opened. Lines are
/// held together by the operation they belong to, so the order desk can find
/// the work an operation has on the floor.
/// </summary>
public sealed class ExecutionStore
{
    private readonly Dictionary<string, List<OperationExecution>> _linesByOperation = new(StringComparer.Ordinal);

    /// <summary>
    /// The first line of an operation that the floor has not signed off yet.
    /// </summary>
    public OperationExecution? FindFirstOpen(string operationCode)
    {
        lock (_linesByOperation)
        {
            if (!_linesByOperation.TryGetValue(operationCode, out var lines))
            {
                return null;
            }

            foreach (var line in lines)
            {
                if (line.IsOpen)
                {
                    return line;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// The open line of an operation that belongs to the given order.
    /// </summary>
    public OperationExecution? FindOpenForOrder(string operationCode, string orderId)
    {
        lock (_linesByOperation)
        {
            if (!_linesByOperation.TryGetValue(operationCode, out var lines))
            {
                return null;
            }

            foreach (var line in lines)
            {
                if (line.IsOpen && string.Equals(line.OrderId, orderId, StringComparison.Ordinal))
                {
                    return line;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Adds an operation line to what the floor is holding.
    /// </summary>
    public void Add(string operationCode, OperationExecution execution)
    {
        if (execution == null)
        {
            throw new ArgumentNullException(nameof(execution));
        }

        lock (_linesByOperation)
        {
            if (!_linesByOperation.TryGetValue(operationCode, out var lines))
            {
                lines = new List<OperationExecution>();
                _linesByOperation[operationCode] = lines;
            }

            lines.Add(execution);
        }
    }

    /// <summary>
    /// Every line the given order holds. The order desk reads an order's
    /// progress out of the operations the order has taken on.
    /// </summary>
    public IReadOnlyList<OperationExecution> LinesFor(string orderId)
    {
        var result = new List<OperationExecution>();

        lock (_linesByOperation)
        {
            foreach (var lines in _linesByOperation.Values)
            {
                foreach (var line in lines)
                {
                    if (string.Equals(line.OrderId, orderId, StringComparison.Ordinal))
                    {
                        result.AddRange(lines);
                        break;
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Every line held for an operation, in the order the floor opened them.
    /// </summary>
    public IReadOnlyList<OperationExecution> LinesOfOperation(string operationCode)
    {
        lock (_linesByOperation)
        {
            if (!_linesByOperation.TryGetValue(operationCode, out var lines))
            {
                return new List<OperationExecution>();
            }

            return new List<OperationExecution>(lines);
        }
    }

    /// <summary>
    /// The lines of an operation that belong to one order, in the order the
    /// floor opened them.
    /// </summary>
    public IReadOnlyList<OperationExecution> LinesOfOrderAndOperation(string orderId, string operationCode)
    {
        var result = new List<OperationExecution>();

        lock (_linesByOperation)
        {
            if (!_linesByOperation.TryGetValue(operationCode, out var lines))
            {
                return result;
            }

            foreach (var line in lines)
            {
                if (string.Equals(line.OrderId, orderId, StringComparison.Ordinal))
                {
                    result.Add(line);
                }
            }
        }

        return result;
    }

    /// <summary>Every order the floor currently holds a line for.</summary>
    public IReadOnlyList<string> KnownOrders()
    {
        var orders = new List<string>();

        lock (_linesByOperation)
        {
            foreach (var lines in _linesByOperation.Values)
            {
                foreach (var line in lines)
                {
                    if (!orders.Contains(line.OrderId))
                    {
                        orders.Add(line.OrderId);
                    }
                }
            }
        }

        return orders;
    }

    /// <summary>Every operation the floor currently holds a line for.</summary>
    public IReadOnlyList<string> KnownOperations()
    {
        lock (_linesByOperation)
        {
            var operations = new List<string>(_linesByOperation.Keys);
            operations.Sort(StringComparer.Ordinal);
            return operations;
        }
    }

    /// <summary>True when the floor holds any line for an operation.</summary>
    public bool Holds(string operationCode)
    {
        lock (_linesByOperation)
        {
            return _linesByOperation.TryGetValue(operationCode, out var lines) && lines.Count > 0;
        }
    }

    /// <summary>How many lines the floor is holding in all.</summary>
    public int TotalLines()
    {
        lock (_linesByOperation)
        {
            var total = 0;
            foreach (var lines in _linesByOperation.Values)
            {
                total += lines.Count;
            }

            return total;
        }
    }

    /// <summary>How many lines an operation has on the floor.</summary>
    public int CountOfOperation(string operationCode)
    {
        lock (_linesByOperation)
        {
            return _linesByOperation.TryGetValue(operationCode, out var lines) ? lines.Count : 0;
        }
    }

    /// <summary>
    /// How many of an operation's lines the floor has not signed off yet.
    /// </summary>
    public int OpenCountOfOperation(string operationCode)
    {
        lock (_linesByOperation)
        {
            if (!_linesByOperation.TryGetValue(operationCode, out var lines))
            {
                return 0;
            }

            var open = 0;
            foreach (var line in lines)
            {
                if (line.IsOpen)
                {
                    open++;
                }
            }

            return open;
        }
    }

    /// <summary>Every line the floor is holding, across all operations.</summary>
    public IReadOnlyList<OperationExecution> AllLines()
    {
        var result = new List<OperationExecution>();

        lock (_linesByOperation)
        {
            foreach (var lines in _linesByOperation.Values)
            {
                result.AddRange(lines);
            }
        }

        return result;
    }

    /// <summary>The lines of an operation the floor has not signed off yet.</summary>
    public IReadOnlyList<OperationExecution> OpenLinesOfOperation(string operationCode)
    {
        var result = new List<OperationExecution>();

        lock (_linesByOperation)
        {
            if (!_linesByOperation.TryGetValue(operationCode, out var lines))
            {
                return result;
            }

            foreach (var line in lines)
            {
                if (line.IsOpen)
                {
                    result.Add(line);
                }
            }
        }

        return result;
    }

    /// <summary>Takes a line off what the floor is holding.</summary>
    public bool Remove(string operationCode, OperationExecution execution)
    {
        if (execution == null)
        {
            return false;
        }

        lock (_linesByOperation)
        {
            if (!_linesByOperation.TryGetValue(operationCode, out var lines))
            {
                return false;
            }

            return lines.Remove(execution);
        }
    }

    /// <summary>Clears everything the floor is holding.</summary>
    public void Clear()
    {
        lock (_linesByOperation)
        {
            _linesByOperation.Clear();
        }
    }
}