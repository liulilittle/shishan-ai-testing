namespace Shishan.Domain.D004;

/// <summary>
/// Holds every account the ledger keeps, one stored record per account number.
/// The record is the account itself, with the bookings entered on it and the
/// running value the book carries for it. Booking and withdrawal work on the
/// record the caller's account resolves to.
/// </summary>
public sealed class LedgerStore
{
    private readonly Dictionary<string, LedgerEntry> _recordsByNumber = new(StringComparer.Ordinal);

    /// <summary>
    /// Opens the given account in the store, keeping one record per account
    /// number, so two accounts that share a name are never served by the same
    /// record.
    /// </summary>
    public void Register(LedgerEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        var number = (entry.Number ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(number))
        {
            throw new ArgumentException(LedgerMessages.NumberRequired, nameof(entry));
        }

        lock (_recordsByNumber)
        {
            _recordsByNumber[number] = entry;
        }
    }

    /// <summary>
    /// The record the ledger serves for the given account. The account is
    /// served by the record held under its number, so accounts that share a
    /// name are never served by the same record.
    /// </summary>
    public LedgerEntry? RecordFor(LedgerEntry entry)
    {
        if (entry == null)
        {
            return null;
        }

        var number = (entry.Number ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(number))
        {
            return null;
        }

        lock (_recordsByNumber)
        {
            return _recordsByNumber.TryGetValue(number, out var record) ? record : null;
        }
    }

    /// <summary>True when the record carries a booking for the voucher that still stands.</summary>
    public bool HasValidFlow(LedgerEntry? record, string voucherId)
    {
        if (record == null || voucherId == null)
        {
            return false;
        }

        foreach (var flow in record.Flows)
        {
            if (flow.AppliesTo(voucherId) && flow.StillHolds)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Books a flow on a record and lifts the value the book carries for it.
    /// The voucher is not looked at again here; the caller decides whether the
    /// booking is wanted before this is called.
    /// </summary>
    public void AddFlow(LedgerEntry record, string voucherId, decimal amount)
    {
        if (record == null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        lock (record.Flows)
        {
            record.Flows.Add(new LedgerFlow(voucherId, amount));
            record.Balance = MoneyMath.Add(record.Balance, amount);
        }
    }

    /// <summary>
    /// Withdraws a booking from a record and lowers the value the book carries
    /// for it. A booking that no longer stands is left alone.
    /// </summary>
    public void Invalidate(LedgerEntry record, LedgerFlow flow)
    {
        if (record == null || flow == null)
        {
            return;
        }

        lock (record.Flows)
        {
            if (!flow.StillHolds)
            {
                return;
            }

            flow.Withdraw();
            record.Balance = MoneyMath.Subtract(record.Balance, flow.Amount);
        }
    }

    /// <summary>The running value the book carries for a record.</summary>
    public decimal BalanceOf(LedgerEntry? record)
    {
        if (record == null)
        {
            return 0m;
        }

        lock (record.Flows)
        {
            return record.Balance;
        }
    }

    /// <summary>Every booking on a record, in the order it was entered.</summary>
    public IReadOnlyList<LedgerFlow> FlowsOf(LedgerEntry record)
    {
        var result = new List<LedgerFlow>();
        if (record == null)
        {
            return result;
        }

        lock (record.Flows)
        {
            result.AddRange(record.Flows);
        }

        return result;
    }

    /// <summary>Every booking on a record that still stands, in entry order.</summary>
        public IReadOnlyList<LedgerFlow> ValidFlowsOf(LedgerEntry? record)
    {
        var result = new List<LedgerFlow>();
        if (record == null)
        {
            return result;
        }

        lock (record.Flows)
        {
            foreach (var flow in record.Flows)
            {
                if (flow.StillHolds)
                {
                    result.Add(flow);
                }
            }
        }

        return result;
    }

    /// <summary>Every booking on a record that has been handed back, in entry order.</summary>
    public IReadOnlyList<LedgerFlow> WithdrawnFlowsOf(LedgerEntry record)
    {
        var result = new List<LedgerFlow>();
        if (record == null)
        {
            return result;
        }

        lock (record.Flows)
        {
            foreach (var flow in record.Flows)
            {
                if (!flow.StillHolds)
                {
                    result.Add(flow);
                }
            }
        }

        return result;
    }

    /// <summary>Every record the ledger holds, in no particular order.</summary>
    public IReadOnlyList<LedgerEntry> AllRecords()
    {
        var result = new List<LedgerEntry>();
        lock (_recordsByNumber)
        {
            result.AddRange(_recordsByNumber.Values);
        }

        return result;
    }

    /// <summary>How many records the ledger holds.</summary>
    public int Count
    {
        get
        {
            lock (_recordsByNumber)
            {
                return _recordsByNumber.Count;
            }
        }
    }

    /// <summary>True when the ledger holds a record for the given wording.</summary>
    public bool HoldsName(string name)
    {
        var key = (name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        lock (_recordsByNumber)
        {
            return _recordsByNumber.ContainsKey(key);
        }
    }

    /// <summary>True when the ledger already serves the given account.</summary>
    public bool Knows(LedgerEntry entry) => RecordFor(entry) != null;

    /// <summary>Removes every record the ledger holds.</summary>
    public void Clear()
    {
        lock (_recordsByNumber)
        {
            _recordsByNumber.Clear();
        }
    }
}
