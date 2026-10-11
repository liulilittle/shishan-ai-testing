namespace Shishan.Domain.K002;

/// <summary>
/// The answer the counter gives back for a print request. A granted request
/// carries the sheet's number and the standing it left under; a turned-away
/// request carries the standing the register currently holds and the reason
/// the sheet was not let out.
/// </summary>
public sealed record PrintResult(
    bool Granted,
    bool FirstPrint,
    int PrintNumber,
    string Status,
    string? RejectionReason)
{
    /// <summary>True when the sheet was not let out.</summary>
    public bool TurnedAway => !Granted;

    /// <summary>True when the sheet went out as a first sheet of its register.</summary>
    public bool LeftAsFirst => Granted && FirstPrint;

    /// <summary>True when the sheet went out as a repeat sheet.</summary>
    public bool LeftAsReprint => Granted && !FirstPrint;

    /// <summary>True when the answer carries no note about why a sheet was turned away.</summary>
    public bool HasNoRejectionNote => string.IsNullOrWhiteSpace(RejectionReason);

    /// <summary>Read out the way the counter would say it.</summary>
    public override string ToString() =>
        Granted
            ? "granted " + PrintNumber + " (" + Status + ")"
            : "turned away: " + RejectionReason;
}

/// <summary>
/// Builds the counter's answers for print requests, so the service does not
/// have to spell out each reply by hand.
/// </summary>
public static class PrintResultFactory
{
    /// <summary>The answer for a sheet that went out of its register.</summary>
    public static PrintResult Granted(bool firstPrint, int printNumber, string status) =>
        new(true, firstPrint, printNumber, status, null);

    /// <summary>
    /// The answer for a sheet that was turned away because no reason was
    /// given for a repeat.
    /// </summary>
    public static PrintResult TurnedAway(string registerStanding, string reason) =>
        new(false, false, 0, registerStanding, reason);

    /// <summary>
    /// The answer for a sheet that was turned away while the register held
    /// no prior sheet at all.
    /// </summary>
    public static PrintResult TurnedAwayFresh(string reason) =>
        TurnedAway(PrintStatuses.NotPrinted, reason);

    /// <summary>
    /// The answer for a request that could not be read because the category
    /// was not recognised.
    /// </summary>
    public static PrintResult UnreadableCategory(string reason) =>
        TurnedAwayFresh(reason);

    /// <summary>True when the answer granted the sheet out of the register.</summary>
    public static bool IsGranted(PrintResult result) => result != null && result.Granted;

    /// <summary>
    /// The number the sheet was given, or zero when the request was turned
    /// away before any sheet went out.
    /// </summary>
    public static int NumberOf(PrintResult result) => result?.PrintNumber ?? 0;
}