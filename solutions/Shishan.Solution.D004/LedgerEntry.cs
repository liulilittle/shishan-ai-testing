namespace Shishan.Domain.D004;

/// <summary>
/// One account in the ledger. Every account is recognised by its number; the
/// name is the wording used on the floor, and several accounts may well carry
/// the same name. The number is the one marker that always tells one account
/// apart from another, so it is the marker the book relies on.
/// </summary>
public sealed class LedgerEntry
{
    /// <summary>
    /// Opens an account with the given number and name. The number is the
    /// marker of the account; the name is only the wording people use for it.
    /// </summary>
    public LedgerEntry(string number, string name)
    {
        Number = number ?? string.Empty;
        Name = name ?? string.Empty;
    }

    /// <summary>The number that marks this account.</summary>
    public string Number { get; }

    /// <summary>The wording used for this account on the floor.</summary>
    public string Name { get; }

    /// <summary>Every booking entered on this account, in the order it was entered.</summary>
    internal List<LedgerFlow> Flows { get; } = new();

    /// <summary>The running value the book carries for this account.</summary>
    internal decimal Balance { get; set; }

    /// <summary>
    /// The first booking for the given voucher that still stands on this
    /// account, or none when the voucher was never booked here or was already
    /// handed back.
    /// </summary>
    internal LedgerFlow? FindFlow(string voucherId)
    {
        if (voucherId == null)
        {
            return null;
        }

        foreach (var flow in Flows)
        {
            if (flow.AppliesTo(voucherId) && flow.StillHolds)
            {
                return flow;
            }
        }

        return null;
    }

    /// <summary>True when a booking for the given voucher still stands here.</summary>
    internal bool HasOpenBooking(string voucherId) => FindFlow(voucherId) != null;

    /// <summary>True when this account carries a booking for the given voucher at all.</summary>
    internal bool Carries(string voucherId)
    {
        if (voucherId == null)
        {
            return false;
        }

        foreach (var flow in Flows)
        {
            if (flow.AppliesTo(voucherId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Every booking on this account that still stands, in entry order.</summary>
    internal IReadOnlyList<LedgerFlow> OpenFlows()
    {
        var open = new List<LedgerFlow>();
        foreach (var flow in Flows)
        {
            if (flow.StillHolds)
            {
                open.Add(flow);
            }
        }

        return open;
    }

    /// <summary>Every booking on this account that has been handed back.</summary>
    internal IReadOnlyList<LedgerFlow> WithdrawnFlows()
    {
        var withdrawn = new List<LedgerFlow>();
        foreach (var flow in Flows)
        {
            if (!flow.StillHolds)
            {
                withdrawn.Add(flow);
            }
        }

        return withdrawn;
    }

    /// <summary>How many bookings this account carries in all.</summary>
    internal int BookingCount => Flows.Count;

    /// <summary>How many bookings on this account still stand.</summary>
    internal int OpenCount
    {
        get
        {
            var count = 0;
            foreach (var flow in Flows)
            {
                if (flow.StillHolds)
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>
    /// The value of the bookings that still stand on this account, added
    /// together with the sign each was entered with.
    /// </summary>
    internal decimal NetValue()
    {
        var net = 0m;
        foreach (var flow in Flows)
        {
            if (flow.StillHolds)
            {
                net = MoneyMath.Add(net, flow.Amount);
            }
        }

        return net;
    }

    /// <summary>
    /// The value of the bookings entered from the given moment onwards that
    /// still stand, as the floor reads a day's work off the book.
    /// </summary>
    internal decimal ValueSince(DateTime moment)
    {
        var value = 0m;
        foreach (var flow in Flows)
        {
            if (flow.StillHolds && flow.EnteredAt >= moment)
            {
                value = MoneyMath.Add(value, flow.Amount);
            }
        }

        return value;
    }

    /// <summary>
    /// The first booking on this account that still stands, or none when every
    /// booking has been handed back or nothing was ever entered.
    /// </summary>
    internal LedgerFlow? OldestOpen()
    {
        foreach (var flow in Flows)
        {
            if (flow.StillHolds)
            {
                return flow;
            }
        }

        return null;
    }

    /// <summary>
    /// The most recent booking on this account that still stands, or none when
    /// every booking has been handed back or nothing was ever entered.
    /// </summary>
    internal LedgerFlow? NewestOpen()
    {
        LedgerFlow? newest = null;
        foreach (var flow in Flows)
        {
            if (flow.StillHolds && (newest == null || flow.EnteredAt >= newest.EnteredAt))
            {
                newest = flow;
            }
        }

        return newest;
    }

    /// <summary>The number and name together, as the book reads them out.</summary>
    public string Reference => Number + " / " + Name;

    /// <summary>True when both accounts carry the same number.</summary>
    public bool CarriesSameNumberAs(LedgerEntry other) =>
        other != null && string.Equals(Number, other.Number, StringComparison.Ordinal);

    /// <summary>True when both accounts are known by the same wording.</summary>
    public bool SharesNameWith(LedgerEntry other) =>
        other != null && string.Equals(Name, other.Name, StringComparison.Ordinal);

    public override string ToString() => Reference;

    public override bool Equals(object? obj) => obj is LedgerEntry other && CarriesSameNumberAs(other);

    public override int GetHashCode() => Number.GetHashCode(StringComparison.Ordinal);
}
