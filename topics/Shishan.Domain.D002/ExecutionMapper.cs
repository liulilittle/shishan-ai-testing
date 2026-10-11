namespace Shishan.Domain.D002;

/// <summary>
/// Turns stored operation lines into the floor-friendly read shown to
/// operators, so the order desk does not have to know the raw values.
/// </summary>
public static class ExecutionMapper
{
    /// <summary>Reads one operation line in floor terms.</summary>
    public static ExecutionView ToView(OperationExecution execution)
    {
        if (execution == null)
        {
            throw new ArgumentNullException(nameof(execution));
        }

        return new ExecutionView(
            execution.OrderId,
            execution.OperationCode,
            OperationCatalog.Describe(execution.OperationCode),
            execution.OperatorName,
            execution.State.Label(),
            execution.IsFinished);
    }

    /// <summary>Reads a run of operation lines in floor terms.</summary>
    public static IReadOnlyList<ExecutionView> ToViews(IEnumerable<OperationExecution> executions)
    {
        var views = new List<ExecutionView>();
        if (executions == null)
        {
            return views;
        }

        foreach (var execution in executions)
        {
            views.Add(ToView(execution));
        }

        return views;
    }

    /// <summary>Reads only the lines the floor has not signed off yet.</summary>
    public static IReadOnlyList<ExecutionView> ToOpenViews(IEnumerable<OperationExecution> executions)
    {
        var views = new List<ExecutionView>();
        if (executions == null)
        {
            return views;
        }

        foreach (var execution in executions)
        {
            if (execution.IsOpen)
            {
                views.Add(ToView(execution));
            }
        }

        return views;
    }

    /// <summary>Reads only the lines the floor has signed off.</summary>
    public static IReadOnlyList<ExecutionView> ToFinishedViews(IEnumerable<OperationExecution> executions)
    {
        var views = new List<ExecutionView>();
        if (executions == null)
        {
            return views;
        }

        foreach (var execution in executions)
        {
            if (execution.IsFinished)
            {
                views.Add(ToView(execution));
            }
        }

        return views;
    }
}