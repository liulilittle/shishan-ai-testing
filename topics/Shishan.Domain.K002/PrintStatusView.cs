namespace Shishan.Domain.K002;

/// <summary>
/// What a register currently shows for one category: the standing it is in,
/// the number of the last sheet that left for that category, and the moment
/// that sheet went out. A category that has never printed keeps its register
/// standing as not printed.
/// </summary>
public sealed record PrintStatusView(
    PrintCategory Category,
    string Status,
    int LastPrintNumber,
    DateTime LatestAt)
{
    /// <summary>True when at least one sheet has left the register for this category.</summary>
    public bool HasPrinted => LastPrintNumber > 0;

    /// <summary>True when the register shows the given standing.</summary>
    public bool ShowsStanding(string standing) =>
        string.Equals(Status, standing, StringComparison.Ordinal);

    /// <summary>True when the register has not let any sheet out for this category.</summary>
    public bool NeverPrinted => ShowsStanding(PrintStatuses.NotPrinted);

    /// <summary>Read out the way the counter would say it.</summary>
    public override string ToString() =>
        Category + " / " + Status + (HasPrinted ? " / " + LastPrintNumber : string.Empty);
}

/// <summary>
/// Builds the register views the counter shows, and answers the small
/// questions the desk asks about a category's standing.
/// </summary>
public static class PrintStatusBuilder
{
    /// <summary>The view for a register that has not let any sheet out yet.</summary>
    public static PrintStatusView Fresh(PrintCategory category) =>
        new(category, PrintStatuses.NotPrinted, 0, default);

    /// <summary>The view built from a row that left the register.</summary>
    public static PrintStatusView FromRow(PrintLedgerEntry row)
    {
        if (row == null)
        {
            throw new PrintRequestException(PrintErrors.LedgerRowMissing);
        }

        return new PrintStatusView(row.Category, row.Status, row.PrintNumber, row.PrintedAt);
    }

    /// <summary>
    /// The standing a sheet will carry when it goes out, based on whether it
    /// is the first sheet of its register or a repeat.
    /// </summary>
    public static string StandingFor(bool firstPrint) =>
        firstPrint ? PrintStatuses.FirstPrinted : PrintStatuses.Reprinted;

    /// <summary>True when the two views are reading the same register.</summary>
    public static bool ReadsSameRegister(PrintStatusView left, PrintStatusView right) =>
        left != null && right != null && left.Category == right.Category;

    /// <summary>
    /// The later of the two views' sheet moments, used when the desk wants
    /// to know which register moved last.
    /// </summary>
    public static PrintStatusView LaterOf(PrintStatusView left, PrintStatusView right)
    {
        if (left == null)
        {
            return right!;
        }

        if (right == null)
        {
            return left;
        }

        return right.LatestAt >= left.LatestAt ? right : left;
    }
}