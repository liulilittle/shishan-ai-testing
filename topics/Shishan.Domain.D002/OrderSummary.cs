namespace Shishan.Domain.D002;

/// <summary>
/// A count of one order's operation lines grouped by how far each has come.
/// </summary>
public sealed record OrderSummary(int Pending, int Running, int Completed)
{
    /// <summary>The count for an order with no operation lines yet.</summary>
    public static readonly OrderSummary Empty = new(0, 0, 0);

    /// <summary>Everything counted together.</summary>
    public int Total => Pending + Running + Completed;

    /// <summary>True when nothing has been counted.</summary>
    public bool IsEmpty => Total == 0;

    /// <summary>Everything counted, ready to be read out on the floor.</summary>
    public string Describe() =>
        "not started " + Pending + ", in progress " + Running + ", done " + Completed;

    /// <summary>Adds two summaries together, entry by entry.</summary>
    public OrderSummary Plus(OrderSummary other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        return new OrderSummary(
            Pending + other.Pending,
            Running + other.Running,
            Completed + other.Completed);
    }

    /// <summary>Takes another summary away, entry by entry.</summary>
    public OrderSummary Minus(OrderSummary other)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        return new OrderSummary(
            Pending - other.Pending,
            Running - other.Running,
            Completed - other.Completed);
    }

    /// <summary>The count held for one standing.</summary>
    public int For(OperationState state) => state switch
    {
        OperationState.Pending => Pending,
        OperationState.Running => Running,
        OperationState.Completed => Completed,
        _ => 0
    };

    /// <summary>True when both summaries hold the same counts.</summary>
    public bool Matches(OrderSummary other) =>
        other != null && Pending == other.Pending && Running == other.Running && Completed == other.Completed;

    /// <summary>The standing with the most lines, preferring the earlier one on a tie.</summary>
    public OperationState Leading()
    {
        if (Pending >= Running && Pending >= Completed)
        {
            return OperationState.Pending;
        }

        if (Running >= Completed)
        {
            return OperationState.Running;
        }

        return OperationState.Completed;
    }
}