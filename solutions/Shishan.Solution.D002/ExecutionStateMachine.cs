namespace Shishan.Domain.D002;

/// <summary>
/// Moves a downstream order's operation line along, and works out how an order
/// is doing from the lines that belong to it.
/// </summary>
public sealed class ExecutionStateMachine
{
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

    /// <summary>How the order is doing, read from the lines that belong to it.</summary>
    public OrderSummary Summarize(IEnumerable<OperationExecution> executions)
    {
        var waiting = 0;
        var inProgress = 0;
        var done = 0;

        if (executions != null)
        {
            foreach (var execution in executions)
            {
                switch (execution.State)
                {
                    case OperationState.Pending:
                        waiting++;
                        break;
                    case OperationState.Running:
                        inProgress++;
                        break;
                    case OperationState.Completed:
                        done++;
                        break;
                }
            }
        }

        return new OrderSummary(waiting, inProgress, done);
    }

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
}