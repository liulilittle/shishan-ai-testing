namespace Shishan.Domain.D004;

/// <summary>
/// The checks made before the ledger takes on a request, so the book is never
/// asked to work without the facts it needs. Every public door of the ledger
/// passes through here first.
/// </summary>
public static class LedgerGuard
{
    /// <summary>
    /// Checks that an account can be opened with the given facts. A number and
    /// a name are both needed before the book will open an account.
    /// </summary>
    public static void VerifyForOpening(string number, string name)
    {
        if (string.IsNullOrEmpty(number))
        {
            throw new ArgumentException(LedgerMessages.NumberRequired, nameof(number));
        }

        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(LedgerMessages.NameRequired, nameof(name));
        }
    }

    /// <summary>
    /// Checks that a booking can be entered. The account must be given and a
    /// voucher id is needed before the book will enter anything.
    /// </summary>
    public static void VerifyForBooking(LedgerEntry entry, string voucherId)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (string.IsNullOrEmpty(voucherId))
        {
            throw new ArgumentException(LedgerMessages.VoucherRequired, nameof(voucherId));
        }
    }

    /// <summary>
    /// Checks that a booking can be withdrawn. The same facts are needed as
    /// when a booking is entered.
    /// </summary>
    public static void VerifyForWithdrawal(LedgerEntry entry, string voucherId)
    {
        VerifyForBooking(entry, voucherId);
    }

    /// <summary>
    /// Checks that the standing of an account can be read. The account must be
    /// given before the book will say anything about it.
    /// </summary>
    public static void VerifyForInquiry(LedgerEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }
    }

    /// <summary>
    /// Checks that an amount can be carried on the book. The value must sit
    /// inside the range the book can write down.
    /// </summary>
    public static void VerifyForAmount(decimal amount)
    {
        if (amount == decimal.MinValue || amount == decimal.MaxValue)
        {
            throw new ArgumentException(LedgerMessages.AmountInvalid, nameof(amount));
        }
    }

    /// <summary>True when the voucher id is one the book can write down.</summary>
    public static bool WritableVoucher(string voucherId) =>
        !string.IsNullOrEmpty(voucherId) && voucherId.Length <= 40;

    /// <summary>
    /// The voucher id with the surrounding spaces taken off, as the book writes
    /// it down.
    /// </summary>
    public static string WrittenVoucher(string voucherId) =>
        (voucherId ?? string.Empty).Trim();
}