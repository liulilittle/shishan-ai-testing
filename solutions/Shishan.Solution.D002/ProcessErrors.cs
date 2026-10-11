namespace Shishan.Domain.D002;

/// <summary>
/// Raised when a request does not fit the rules the shop keeps for operations.
/// </summary>
public sealed class ProcessRuleException : Exception
{
    public ProcessRuleException(string message) : base(message)
    {
    }

    public ProcessRuleException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// The wording the order desk uses when it turns a request away.
/// </summary>
public static class ProcessMessages
{
    public const string OrderIdRequired = "A downstream order id is required.";

    public const string OperationCodeRequired = "An operation code is required.";

    public const string OperatorRequired = "An operator name is required.";

    public const string OperationUnknown = "The shop does not keep this operation.";
}