namespace Shishan.Domain.D002;

/// <summary>
/// Moves a downstream order's operation line along, and keeps the tallies the
/// order desk passes on when it is asked how an order is doing.
/// </summary>
public sealed class ExecutionStateMachine
{
    private readonly Dictionary<string, int> _pendingTallies = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _inProgressTallies = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _doneTallies = new(StringComparer.Ordinal);

    /// <summary>Opens a new line for an order on an operation.</summary>
    public OperationExecution Begin(string orderId, string operationCode, string operatorName) =>
        new(orderId, operationCode, operatorName);

    /// <summary>
    /// Carries an operation line one step further, as an operator reports work.
    /// </summary>
    public OperationExecution Move(OperationExecution execution, string operatorName)
    {
        if (execution == null)
        {
            throw new ArgumentNullException(nameof(execution));
        }

        if (execution.State == OperationState.Pending)
        {
            execution.MarkRunning(operatorName);
        }
        else if (execution.State == OperationState.Running)
        {
            execution.MarkCompleted();
        }

        return execution;
    }

    /// <summary>Notes a line for the order that has not been started yet.</summary>
    public void NotePending(string orderId) => Bump(_pendingTallies, orderId);

    /// <summary>Notes work that has started for the order.</summary>
    public void NoteInProgress(string orderId) => Bump(_inProgressTallies, orderId);

    /// <summary>Notes work the order has signed off.</summary>
    public void NoteDone(string orderId)
    {
        Bump(_doneTallies, orderId);
        Lower(_inProgressTallies, orderId);
    }

    /// <summary>How the order is doing, as the order desk reports it.</summary>
    public OrderSummary Summarize(string orderId) => new(
        Read(_pendingTallies, orderId),
        Read(_inProgressTallies, orderId),
        Read(_doneTallies, orderId));

    /// <summary>True when a standing is the last one the floor works to.</summary>
    public static bool IsTerminal(OperationState state) => state == OperationState.Completed;

    /// <summary>The standings a line can reach from the one it holds now.</summary>
    public static IReadOnlyList<OperationState> StepsFrom(OperationState state) => state switch
    {
        OperationState.Pending => new[] { OperationState.Running },
        OperationState.Running => new[] { OperationState.Completed },
        _ => Array.Empty<OperationState>()
    };

    /// <summary>True when a line may be carried straight to the given standing.</summary>
    public static bool CanMoveTo(OperationState from, OperationState to)
    {
        if (from == OperationState.Pending)
        {
            return to == OperationState.Running;
        }

        if (from == OperationState.Running)
        {
            return to == OperationState.Completed;
        }

        return to == OperationState.Completed;
    }

    /// <summary>The standing a line reaches after a number of steps.</summary>
    public static OperationState StateAfter(OperationState state, int steps)
    {
        var current = state;
        for (var step = 0; step < steps; step++)
        {
            current = current.Next();
        }

        return current;
    }

    /// <summary>The floor wording for a standing.</summary>
    public static string Describe(OperationState state) => state.Label();

    private void Bump(Dictionary<string, int> tallies, string orderId)
    {
        lock (_pendingTallies)
        {
            tallies.TryGetValue(orderId, out var current);
            tallies[orderId] = current + 1;
        }
    }

    private void Lower(Dictionary<string, int> tallies, string orderId)
    {
        lock (_pendingTallies)
        {
            tallies.TryGetValue(orderId, out var current);
            tallies[orderId] = current - 1;
        }
    }

    private int Read(Dictionary<string, int> tallies, string orderId)
    {
        lock (_pendingTallies)
        {
            return tallies.TryGetValue(orderId, out var current) ? current : 0;
        }
    }
}