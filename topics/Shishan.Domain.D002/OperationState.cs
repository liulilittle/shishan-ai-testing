namespace Shishan.Domain.D002;

/// <summary>
/// How far a downstream order has come on one commissioned operation.
/// The values follow the order the shop works through them.
/// </summary>
public enum OperationState
{
    Pending = 0,
    Running = 1,
    Completed = 2
}

/// <summary>
/// Plain wording and small helpers for the values above.
/// </summary>
public static class OperationStateExtensions
{
    public static bool IsFinished(this OperationState state) => state == OperationState.Completed;

    public static bool IsOpen(this OperationState state) => state != OperationState.Completed;

    public static string Label(this OperationState state) => state switch
    {
        OperationState.Pending => "not started",
        OperationState.Running => "in progress",
        OperationState.Completed => "done",
        _ => "unknown"
    };

    public static OperationState Next(this OperationState state) => state switch
    {
        OperationState.Pending => OperationState.Running,
        OperationState.Running => OperationState.Completed,
        OperationState.Completed => OperationState.Completed,
        _ => OperationState.Completed
    };
}