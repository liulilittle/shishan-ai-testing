namespace Shishan.Domain.K002;

/// <summary>
/// Raised when a request cannot be read by the printing desk, such as when
/// the category named is not one the desk keeps.
/// </summary>
public sealed class PrintRequestException : Exception
{
    public PrintRequestException(string message) : base(message)
    {
    }

    public PrintRequestException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// The wording the printing desk uses when it reads a request or turns a
/// sheet away.
/// </summary>
public static class PrintErrors
{
    public const string CategoryRequired = "A print category is required.";

    public const string CategoryUnknown = "The desk does not recognise this print category.";

    public const string RepeatNeedsReason = "A repeat sheet needs a reason.";

    public const string LedgerRowMissing = "A ledger row is needed to build a register view.";
}

/// <summary>
/// The standings a sheet can leave a register under, kept in one place so
/// the ledger and the desk always read them the same way.
/// </summary>
public static class PrintStatuses
{
    public const string NotPrinted = "NotPrinted";

    public const string FirstPrinted = "FirstPrinted";

    public const string Reprinted = "Reprinted";
}

/// <summary>
/// Small wording helpers the counter uses when reading a register view out
/// loud.
/// </summary>
public static class PrintWording
{
    /// <summary>True when the standing says no sheet has left yet.</summary>
    public static bool ReadsNotPrinted(string standing) =>
        string.Equals(standing, PrintStatuses.NotPrinted, StringComparison.Ordinal);

    /// <summary>True when the standing says a first sheet left the register.</summary>
    public static bool ReadsFirstPrinted(string standing) =>
        string.Equals(standing, PrintStatuses.FirstPrinted, StringComparison.Ordinal);

    /// <summary>True when the standing says a repeat sheet left the register.</summary>
    public static bool ReadsReprinted(string standing) =>
        string.Equals(standing, PrintStatuses.Reprinted, StringComparison.Ordinal);

    /// <summary>The standing for a sheet that left as a first sheet.</summary>
    public static string FirstSheet => PrintStatuses.FirstPrinted;

    /// <summary>The standing for a sheet that left as a repeat.</summary>
    public static string RepeatSheet => PrintStatuses.Reprinted;
}