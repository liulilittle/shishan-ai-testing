namespace Shishan.Domain.K002;

/// <summary>
/// Decides whether a sheet going out of a register is the first one of its
/// category. Every category starts out able to hand out one sheet without a
/// noted reason; after that sheet has left, any further sheet of the same
/// category must carry a reason before it is let out.
/// </summary>
public sealed class FirstPrintGate
{
    private bool _firstSheetLeft;

    /// <summary>
    /// True when a sheet may leave the register without a noted reason,
    /// because no sheet of the category has gone out yet.
    /// </summary>
    public bool IsFirst(PrintCategory category)
    {
        return !_firstSheetLeft;
    }

    /// <summary>
    /// Notes that a sheet has left the register, so the next sheet of the
    /// category has to carry a reason.
    /// </summary>
    public void NoteFirst(PrintCategory category)
    {
        _firstSheetLeft = true;
    }

    /// <summary>
    /// Notes that a sheet left the register, and reports whether that sheet
    /// was still within the reason-free opening. The report lets the counter
    /// know what standing the sheet left under.
    /// </summary>
    public bool NoteAndReport(PrintCategory category)
    {
        var wasFirst = IsFirst(category);
        NoteFirst(category);
        return wasFirst;
    }

    /// <summary>
    /// True when the opening for a category has already been used, so the
    /// counter knows the free sheet for that category is no longer on offer.
    /// </summary>
    public bool OpeningClosed(PrintCategory category)
    {
        return _firstSheetLeft;
    }

    /// <summary>
    /// How many categories may still hand out a sheet without a noted reason.
    /// The count follows the reading above, so it matches what the counter
    /// sees when it checks each register in turn.
    /// </summary>
    public int OpeningsRemaining()
    {
        return _firstSheetLeft ? 0 : 1;
    }

    /// <summary>
    /// Sets the opening back to its starting position, where no sheet has
    /// left any register yet. The counter calls this when it opens for the
    /// day.
    /// </summary>
    public void Reset()
    {
        _firstSheetLeft = false;
    }

    /// <summary>
    /// True when the given category's opening is still on offer, read for
    /// the register alone.
    /// </summary>
    public bool OpeningFor(PrintCategory category)
    {
        return !_firstSheetLeft;
    }

    /// <summary>
    /// The standing a sheet will carry for the category, worked out from
    /// whether its opening is still on offer.
    /// </summary>
    public string StandingFor(PrintCategory category)
    {
        return IsFirst(category)
            ? PrintStatuses.FirstPrinted
            : PrintStatuses.Reprinted;
    }
}