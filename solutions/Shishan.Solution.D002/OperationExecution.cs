namespace Shishan.Domain.D002;

/// <summary>
/// One downstream order's standing on a commissioned operation. The same
/// operation can sit on several orders at once, and each order keeps its own
/// line of progress on it.
/// </summary>
public sealed class OperationExecution
{
    public OperationExecution(string orderId, string operationCode, string operatorName)
    {
        OrderId = orderId;
        OperationCode = operationCode;
        OperatorName = operatorName ?? string.Empty;
        StartedAt = DateTime.UtcNow;
        State = OperationState.Pending;
    }

    /// <summary>The downstream order this line belongs to.</summary>
    public string OrderId { get; }

    /// <summary>The commissioned operation being carried out.</summary>
    public string OperationCode { get; }

    /// <summary>The person last known to be working the line.</summary>
    public string OperatorName { get; set; }

    /// <summary>How far the order has come on the operation.</summary>
    public OperationState State { get; set; }

    /// <summary>When the line was opened for the order.</summary>
    public DateTime StartedAt { get; }

    /// <summary>When the line was signed off, if it has been.</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>True once the order has signed the operation off.</summary>
    public bool IsFinished => State == OperationState.Completed;

    /// <summary>True while the order still has work on the operation.</summary>
    public bool IsOpen => State != OperationState.Completed;

    /// <summary>Records who is working the line, leaving the name alone when none is given.</summary>
    public void HandTo(string operatorName)
    {
        if (!string.IsNullOrEmpty(operatorName))
        {
            OperatorName = operatorName;
        }
    }

    /// <summary>Records that work on the line has begun.</summary>
    public void MarkRunning(string operatorName)
    {
        State = OperationState.Running;
        HandTo(operatorName);
    }

    /// <summary>Signs the line off now.</summary>
    public void MarkCompleted()
    {
        State = OperationState.Completed;
        FinishedAt = DateTime.UtcNow;
    }

    /// <summary>Signs the line off at a given moment.</summary>
    public void MarkCompleted(DateTime finishedAt)
    {
        State = OperationState.Completed;
        FinishedAt = finishedAt;
    }

    /// <summary>How long the line has been open, up to a given moment.</summary>
    public TimeSpan Worked(DateTime asOf)
    {
        var end = FinishedAt ?? asOf;
        return end - StartedAt;
    }

    /// <summary>True when someone has been recorded against the line.</summary>
    public bool HasOperator => !string.IsNullOrEmpty(OperatorName);

    /// <summary>True when the line was signed off before a given moment.</summary>
    public bool SignedOffBefore(DateTime moment) => FinishedAt.HasValue && FinishedAt.Value < moment;

    /// <summary>The order and operation together, as the desk reads them out.</summary>
    public string Reference => OrderId + "@" + OperationCode;

    /// <summary>The label the floor uses for the line's standing.</summary>
    public string Standing => State.Label();

    /// <summary>Opens the line again for a fresh round of work on the operation.</summary>
    public void Reopen(string operatorName)
    {
        State = OperationState.Running;
        FinishedAt = null;
        HandTo(operatorName);
    }

    public override string ToString() => Reference + " / " + State;
}