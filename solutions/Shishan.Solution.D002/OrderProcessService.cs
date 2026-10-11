namespace Shishan.Domain.D002;

/// <summary>
/// The order desk for downstream shop-floor operations. A commissioned
/// operation can appear on several orders at the same time, and several people
/// may work the same order. Every order keeps its own progress on the
/// operation, and the desk can report how an order is doing.
/// </summary>
public sealed class OrderProcessService
{
    private readonly ExecutionStore _store;
    private readonly ExecutionStateMachine _machine;

    public OrderProcessService()
        : this(new ExecutionStore(), new ExecutionStateMachine())
    {
    }

    public OrderProcessService(ExecutionStore store, ExecutionStateMachine machine)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));
    }

    /// <summary>
    /// Opens an order's line on an operation, or hands back the line the order
    /// already has so the order is not opened twice.
    /// </summary>
    public OperationExecution Start(string orderId, string operationCode, string operatorName)
    {
        RequireOrder(orderId);
        RequireOperation(operationCode);

        var code = OperationCatalog.NormalizeOperation(operationCode);

        lock (_store.Gate)
        {
            var existing = _store.Find(orderId, code);
            if (existing != null)
            {
                return existing;
            }

            return _store.Add(_machine.Begin(orderId, code, operatorName));
        }
    }

    /// <summary>
    /// Carries an order's operation line one step further, as an operator
    /// reports work on the floor.
    /// </summary>
    public OperationExecution Advance(string orderId, string operationCode, string operatorName)
    {
        RequireOrder(orderId);
        RequireOperation(operationCode);

        var code = OperationCatalog.NormalizeOperation(operationCode);

        lock (_store.Gate)
        {
            var execution = _store.Find(orderId, code);
            if (execution == null)
            {
                execution = _store.Add(_machine.Begin(orderId, code, operatorName));
            }

            return _machine.Move(execution, operatorName);
        }
    }

    /// <summary>Every operation line held for an order.</summary>
    public IReadOnlyList<OperationExecution> ListExecutions(string orderId)
    {
        RequireOrder(orderId);
        return _store.ForOrder(orderId);
    }

    /// <summary>How the order is doing across the operations it has taken on.</summary>
    public OrderSummary GetSummary(string orderId)
    {
        RequireOrder(orderId);
        return _machine.Summarize(_store.ForOrder(orderId));
    }

    /// <summary>The floor-friendly read of an order's operation lines.</summary>
    public IReadOnlyList<ExecutionView> ListExecutionViews(string orderId)
    {
        RequireOrder(orderId);
        return ExecutionMapper.ToViews(_store.ForOrder(orderId));
    }

    /// <summary>How many operation lines the order holds.</summary>
    public int CountExecutions(string orderId)
    {
        RequireOrder(orderId);
        return _store.ForOrder(orderId).Count;
    }

    /// <summary>How many of an operation's lines the floor has not signed off.</summary>
    public int OpenCount(string operationCode)
    {
        RequireOperation(operationCode);
        return _store.OpenCountOfOperation(OperationCatalog.NormalizeOperation(operationCode));
    }

    /// <summary>True when the shop keeps the operation.</summary>
    public bool IsKnownOperation(string operationCode)
    {
        RequireOperation(operationCode);
        return OperationCatalog.IsKnown(operationCode);
    }

    /// <summary>The operations the shop keeps, in the order of the line.</summary>
    public IReadOnlyList<OperationDefinition> Operations() => OperationCatalog.All();

    private static void RequireOrder(string orderId)
    {
        if (string.IsNullOrEmpty(orderId))
        {
            throw new ArgumentException(ProcessMessages.OrderIdRequired, nameof(orderId));
        }
    }

    private static void RequireOperation(string operationCode)
    {
        if (string.IsNullOrEmpty(operationCode))
        {
            throw new ArgumentException(ProcessMessages.OperationCodeRequired, nameof(operationCode));
        }
    }
}