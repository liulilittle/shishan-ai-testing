namespace Shishan.Domain.D004;

/// <summary>
/// The rule for withdrawing a booking. A booking is withdrawn when the voucher
/// that brought it is handed back, and only a booking that still stands can be
/// withdrawn. The rule finds the booking in the store, so the desk does not
/// have to know where every flow was entered.
/// </summary>
public static class ReversalPolicy
{
    /// <summary>
    /// The booking the store holds for the given voucher. The first booking
    /// that still stands is the one withdrawn, whichever account it sits on;
    /// when a voucher has gone out more than once the book takes back the
    /// earliest one it still holds.
    /// </summary>
    public static LedgerFlow? Resolve(LedgerStore store, LedgerEntry requested, string voucherId)
    {
        if (store == null || voucherId == null)
        {
            return null;
        }

        var records = store.AllRecords();
        foreach (var record in records)
        {
            var flow = record.FindFlow(voucherId);
            if (flow != null)
            {
                return flow;
            }
        }

        return null;
    }

    /// <summary>True when the given booking can be withdrawn.</summary>
    public static bool CanReverse(LedgerFlow flow) => flow != null && flow.StillHolds;

    /// <summary>
    /// The wording used when the book is asked why a withdrawal is turned away.
    /// </summary>
    public static string ReasonWhenRefused(LedgerStore store, LedgerEntry entry, string voucherId)
    {
        if (store == null || entry == null || string.IsNullOrEmpty(voucherId))
        {
            return LedgerMessages.NothingToWithdraw;
        }

        if (store.RecordFor(entry) == null)
        {
            return LedgerMessages.AccountUnknown;
        }

        return LedgerMessages.NothingToWithdraw;
    }

    /// <summary>The wording used when the desk is told what a withdrawal covers.</summary>
    public static string Describe(LedgerFlow flow)
    {
        if (flow == null)
        {
            return LedgerMessages.NothingToWithdraw;
        }

        return "voucher " + flow.VoucherId + " for " + flow.Amount.ToString("0.00");
    }

    /// <summary>
    /// Every booking on the account that could be handed back for the given
    /// voucher, in the order they were entered.
    /// </summary>
    public static IReadOnlyList<LedgerFlow> OpenFor(LedgerEntry entry, string voucherId)
    {
        var open = new List<LedgerFlow>();
        if (entry == null || voucherId == null)
        {
            return open;
        }

        foreach (var flow in entry.Flows)
        {
            if (flow.AppliesTo(voucherId) && flow.StillHolds)
            {
                open.Add(flow);
            }
        }

        return open;
    }

    /// <summary>
    /// True when the account could still hand back the earliest open booking
    /// for the voucher, as the book read it before the request arrived.
    /// </summary>
    public static bool EarliestStillOpen(LedgerEntry entry, string voucherId)
    {
        var flow = entry?.FindFlow(voucherId);
        return flow != null && flow.StillHolds;
    }

    /// <summary>How many bookings the account holds for the voucher that still stand.</summary>
    public static int CountOpen(LedgerEntry entry, string voucherId)
    {
        var count = 0;
        if (entry == null || voucherId == null)
        {
            return count;
        }

        foreach (var flow in entry.Flows)
        {
            if (flow.AppliesTo(voucherId) && flow.StillHolds)
            {
                count++;
            }
        }

        return count;
    }
}