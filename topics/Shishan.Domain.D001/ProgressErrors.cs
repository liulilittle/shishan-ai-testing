namespace Shishan.Domain.D001;

/// <summary>
/// Raised when a request does not fit the rules the office keeps for approval
/// files.
/// </summary>
public sealed class ProgressRuleException : Exception
{
    public ProgressRuleException(string message) : base(message)
    {
    }

    public ProgressRuleException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// The wording the desk uses when it turns a request away.
/// </summary>
public static class ProgressMessages
{
    public const string BusinessNoRequired = "A business number is required.";

    public const string ApplyNoRequired = "An apply number is required.";

    public const string NodeCodeRequired = "A node code is required.";

    public const string TitleRequired = "A title is required.";

    public const string FileUnknown = "The office does not hold this file.";
}