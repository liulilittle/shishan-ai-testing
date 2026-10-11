namespace Shishan.Domain.D004;

/// <summary>
/// One booking entered on an account. A booking carries the voucher that
/// brought it, the value it was entered for, and the moment it was entered.
/// The booking stands until the voucher is handed back, after which it is
/// withdrawn and no longer counts towards the account.
/// </summary>
public sealed class LedgerFlow
{
    internal LedgerFlow(string voucherId, decimal amount)
        : this(voucherId, amount, DateTime.UtcNow)
    {
    }

    internal LedgerFlow(string voucherId, decimal amount, DateTime enteredAt)
    {
        VoucherId = voucherId ?? string.Empty;
        Amount = amount;
        EnteredAt = enteredAt;
    }

    /// <summary>The voucher the booking was entered for.</summary>
    public string VoucherId { get; }

    /// <summary>The value the booking was entered for.</summary>
    public decimal Amount { get; }

    /// <summary>The moment the booking was entered, as the book keeps it.</summary>
    public DateTime EnteredAt { get; }

    /// <summary>True once the voucher has been handed back.</summary>
    public bool IsReversed { get; private set; }

    /// <summary>True while the booking still counts towards the account.</summary>
    public bool StillHolds => !IsReversed;

    /// <summary>True when the booking was made for the given voucher.</summary>
    public bool AppliesTo(string voucherId) =>
        string.Equals(VoucherId, voucherId, StringComparison.Ordinal);

    /// <summary>Withdraws the booking.</summary>
    internal void Withdraw() => IsReversed = true;

    /// <summary>The wording used when the booking is read back on the floor.</summary>
    public string Describe() =>
        "voucher " + VoucherId + " for " + Amount.ToString("0.00");

    /// <summary>The voucher and value together, as the book names the booking.</summary>
    public string Note => VoucherId + " @ " + Amount.ToString("0.00");

    /// <summary>True when both bookings were entered for the same voucher.</summary>
    public bool CarriesSameVoucherAs(LedgerFlow other) =>
        other != null && AppliesTo(other.VoucherId);

    /// <summary>
    /// True when both bookings were entered for the same voucher and value, so
    /// the book can tell one booking from another on the same voucher.
    /// </summary>
    public bool SameDealAs(LedgerFlow other) =>
        other != null && AppliesTo(other.VoucherId) && Amount == other.Amount;

    /// <summary>True when the booking stands and was entered before the given moment.</summary>
    public bool OpenBefore(DateTime moment) =>
        StillHolds && EnteredAt < moment;

    /// <summary>True when the booking stands and was entered from the given moment onwards.</summary>
    public bool OpenFrom(DateTime moment) =>
        StillHolds && EnteredAt >= moment;

    /// <summary>A fresh booking with the same voucher and value, still standing.</summary>
    internal LedgerFlow Renew() => new(VoucherId, Amount);

    /// <summary>The vouchers this booking stands for, joined when more than one.</summary>
    public string Vouchers(string separator)
    {
        if (string.IsNullOrEmpty(separator))
        {
            return VoucherId;
        }

        return VoucherId;
    }

    /// <summary>The day the booking was entered, as the floor reads it.</summary>
    public DateTime Day() => EnteredAt.Date;

    public override string ToString() => Describe();

    public override bool Equals(object? obj) => obj is LedgerFlow other && SameDealAs(other);

    public override int GetHashCode() => HashCode.Combine(VoucherId, Amount);
}