namespace Shishan.Domain.D004;

/// <summary>
/// Raised when a request breaks a rule the book keeps, such as booking against
/// an account that was never opened or carrying an amount the book cannot
/// write down.
/// </summary>
public sealed class LedgerRuleException : Exception
{
    public LedgerRuleException(string message)
        : base(message)
    {
    }

    public LedgerRuleException(string message, Exception inner)
        : base(message, inner)
    {
    }

    /// <summary>
    /// Raises a rule exception for the book, carrying the given wording.
    /// </summary>
    public static LedgerRuleException Because(string message) => new(message);

    /// <summary>
    /// Raises a rule exception for the book when an account is unknown to it.
    /// </summary>
    public static LedgerRuleException UnknownAccount() =>
        new(LedgerMessages.AccountUnknown);
}

/// <summary>
/// The wording the book uses when it turns a request away at the door.
/// </summary>
public static class LedgerMessages
{
    public const string NumberRequired = "An account number is required.";

    public const string NameRequired = "An account name is required.";

    public const string VoucherRequired = "A voucher id is required.";

    public const string AccountUnknown = "The book does not know this account.";

    public const string NothingToWithdraw = "There is nothing to withdraw for this voucher.";

    public const string AlreadyBooked = "This voucher has already been booked.";

    public const string AmountInvalid = "The amount is outside the book's range.";

    public const string ValueCarried = "The value the book carries for this account.";

    public const string OpenBookings = "The bookings on this account that still stand.";

    public const string WithdrawnBookings = "The bookings on this account that have been handed back.";

    public const string BookKeptToTwo = "The book carries every amount to two decimal places.";
}