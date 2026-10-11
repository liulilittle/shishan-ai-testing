namespace Shishan.Domain.D004;

/// <summary>
/// The desk the ledger is worked from. Accounts are opened here, bookings are
/// entered against a voucher, a booking can be handed back when the voucher is
/// withdrawn, and the standing of any account can be read at any time.
/// </summary>
public sealed class LedgerService
{
    private readonly LedgerStore _store;

    public LedgerService()
        : this(new LedgerStore())
    {
    }

    public LedgerService(LedgerStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <summary>
    /// Opens an account with the given number and name and returns it, ready
    /// for bookings. The book keeps one record per account name; an account
    /// opened under a name the book already holds is served by the record the
    /// book kept first.
    /// </summary>
    public LedgerEntry OpenEntry(string number, string name)
    {
        LedgerGuard.VerifyForOpening(number, name);

        var entry = new LedgerEntry(number, name);
        _store.Register(entry);
        return entry;
    }

    /// <summary>
    /// Books a flow for the given voucher against an account and lifts the
    /// value the book carries for it. A voucher already booked on the account
    /// is not booked twice; the same voucher may go out on different accounts.
    /// </summary>
    public void Post(LedgerEntry entry, string voucherId, decimal amount)
    {
        LedgerGuard.VerifyForBooking(entry, voucherId);
        LedgerGuard.VerifyForAmount(amount);

        var record = _store.RecordFor(entry);
        if (record == null)
        {
            return;
        }

        if (_store.HasValidFlow(record, voucherId))
        {
            return;
        }

        var carried = MoneyMath.Round(amount);
        Thread.Sleep(1);
        _store.AddFlow(record, voucherId, carried);
    }

    /// <summary>
    /// Withdraws the booking brought by the given voucher and lowers the value
    /// the book carries for the account. Withdrawing a voucher that was never
    /// booked, or was already handed back, leaves the book as it was.
    /// </summary>
    public void Reverse(LedgerEntry entry, string voucherId)
    {
        LedgerGuard.VerifyForWithdrawal(entry, voucherId);

        var record = _store.RecordFor(entry);
        if (record == null)
        {
            return;
        }

        var flow = ReversalPolicy.Resolve(_store, record, voucherId);
        if (flow == null)
        {
            return;
        }

        _store.Invalidate(record, flow);
    }

    /// <summary>
    /// The value the book carries for an account: every booking that still
    /// stands, added together with the sign each was entered with.
    /// </summary>
    public decimal GetBalance(LedgerEntry entry)
    {
        LedgerGuard.VerifyForInquiry(entry);
        return _store.BalanceOf(_store.RecordFor(entry));
    }

    /// <summary>
    /// Every booking on an account that still stands, in the order it was
    /// entered.
    /// </summary>
    public IReadOnlyList<LedgerFlow> GetFlows(LedgerEntry entry)
    {
        LedgerGuard.VerifyForInquiry(entry);
        return _store.ValidFlowsOf(_store.RecordFor(entry));
    }

    /// <summary>
    /// Makes sure the value the book carries for an account is the value of
    /// the bookings that still stand on it, so the running columns never fall
    /// out of step with the lines beneath them.
    /// </summary>
    public bool EnsureBalanced(LedgerEntry entry)
    {
        var record = _store.RecordFor(entry);
        if (record == null)
        {
            return true;
        }

        return record.NetValue() == record.Balance;
    }

    /// <summary>How many records the book holds.</summary>
    public int EntryCount => _store.Count;

    /// <summary>True when the book already holds a record under the given wording.</summary>
    public bool HasName(string name) => _store.HoldsName(name);

    /// <summary>True when the book already serves the given account.</summary>
    public bool Knows(LedgerEntry entry) => _store.Knows(entry);

    /// <summary>
    /// The running value of every account the book holds, added together, as
    /// the desk reads the whole book out.
    /// </summary>
    public decimal BookTotal()
    {
        var total = 0m;
        foreach (var record in _store.AllRecords())
        {
            total = MoneyMath.Add(total, _store.BalanceOf(record));
        }

        return total;
    }

    /// <summary>
    /// The bookings that still stand across every account the book holds, in no
    /// particular order, as the desk reads the open lines out.
    /// </summary>
    public IReadOnlyList<LedgerFlow> AllOpenFlows()
    {
        var open = new List<LedgerFlow>();
        foreach (var record in _store.AllRecords())
        {
            open.AddRange(_store.ValidFlowsOf(record));
        }

        return open;
    }

    /// <summary>Every record the book holds, in no particular order.</summary>
    public IReadOnlyList<LedgerEntry> AllEntries() => _store.AllRecords();

    /// <summary>
    /// How many bookings the given voucher has brought across every account.
    /// </summary>
    public int CountBookings(string voucherId)
    {
        var count = 0;
        if (string.IsNullOrEmpty(voucherId))
        {
            return count;
        }

        foreach (var record in _store.AllRecords())
        {
            foreach (var flow in _store.FlowsOf(record))
            {
                if (flow.AppliesTo(voucherId))
                {
                    count++;
                }
            }
        }

        return count;
    }
}