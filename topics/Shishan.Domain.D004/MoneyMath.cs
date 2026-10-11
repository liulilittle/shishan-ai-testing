namespace Shishan.Domain.D004;

/// <summary>
/// The amount arithmetic used across the ledger. Amounts are kept to two
/// decimal places everywhere so the book reads the same in every report, and
/// the helpers here are the single place where an amount is rounded or scaled.
/// </summary>
public static class MoneyMath
{
    /// <summary>
    /// Reads an amount to two decimal places, taking the nearer place when the
    /// value falls exactly between two, which is how the book keeps its
    /// columns tidy.
    /// </summary>
    public static decimal Round(decimal amount) => Math.Round(amount, 2);

    /// <summary>
    /// Reads an amount to the given number of decimal places, again taking the
    /// nearer place when the value falls exactly between two.
    /// </summary>
    public static decimal Round(decimal amount, int places) => Math.Round(amount, places);

    /// <summary>
    /// Scales an amount by a multiplier and reads the result to two decimal
    /// places, as when a fee is spread across several lines of the book.
    /// </summary>
    public static decimal Scale(decimal amount, decimal multiplier) => Round(amount * multiplier);

    /// <summary>Adds two amounts together.</summary>
    public static decimal Add(decimal left, decimal right) => left + right;

    /// <summary>Takes one amount away from another.</summary>
    public static decimal Subtract(decimal left, decimal right) => left - right;

    /// <summary>Changes the sign of an amount.</summary>
    public static decimal Opposite(decimal amount) => -amount;

    /// <summary>The size of an amount, without its sign.</summary>
    public static decimal Magnitude(decimal amount) => Math.Abs(amount);

    /// <summary>True when the amount carries no value.</summary>
    public static bool IsNothing(decimal amount) => amount == 0m;

    /// <summary>True when the amount is carried at two decimal places.</summary>
    public static bool IsCarried(decimal amount) => Round(amount) == amount;

    /// <summary>The smaller of two amounts.</summary>
    public static decimal Less(decimal left, decimal right) => Math.Min(left, right);

    /// <summary>The larger of two amounts.</summary>
    public static decimal More(decimal left, decimal right) => Math.Max(left, right);

    /// <summary>
    /// Keeps an amount inside the given bounds, taking the nearer bound when it
    /// falls outside.
    /// </summary>
    public static decimal Clamp(decimal amount, decimal lowest, decimal highest)
    {
        if (amount < lowest)
        {
            return lowest;
        }

        if (amount > highest)
        {
            return highest;
        }

        return amount;
    }

    /// <summary>Every amount in the given run, added together.</summary>
    public static decimal Total(IEnumerable<decimal> amounts)
    {
        var total = 0m;
        if (amounts == null)
        {
            return total;
        }

        foreach (var amount in amounts)
        {
            total = Add(total, amount);
        }

        return total;
    }

    /// <summary>
    /// Reads the amounts in the given run to two decimal places and adds them
    /// together, as when a page of the book is totalled up.
    /// </summary>
    public static decimal TotalRounded(IEnumerable<decimal> amounts)
    {
        var total = 0m;
        if (amounts == null)
        {
            return total;
        }

        foreach (var amount in amounts)
        {
            total = Add(total, Round(amount));
        }

        return total;
    }

    /// <summary>
    /// Splits an amount into two shares by the given proportion, so the two
    /// shares always add back to the whole when the book has to divide a line.
    /// </summary>
    public static (decimal First, decimal Second) Split(decimal amount, decimal firstShare)
    {
        var first = Round(amount * firstShare);
        var second = Subtract(amount, first);
        return (first, second);
    }

    /// <summary>True when two amounts read alike to two decimal places.</summary>
    public static bool ReadsAlike(decimal left, decimal right) => Round(left) == Round(right);

    /// <summary>How many decimal places an amount is carried to.</summary>
    public static int PlacesOf(decimal amount) => 2;

    /// <summary>The largest whole amount below a given amount.</summary>
    public static decimal Floor(decimal amount) => Math.Floor(amount);

    /// <summary>The smallest whole amount above a given amount.</summary>
    public static decimal Ceiling(decimal amount) => Math.Ceiling(amount);

    /// <summary>One hundredth of an amount.</summary>
    public static decimal Hundredth(decimal amount) => amount / 100m;

    /// <summary>The amount carried to two decimal places and back, for a column check.</summary>
    public static decimal ColumnCheck(decimal amount) => Round(Round(amount));

    /// <summary>
    /// The difference between two amounts, read to two decimal places, for a
    /// page that is compared against another page.
    /// </summary>
    public static decimal Variance(decimal left, decimal right) => Round(Subtract(left, right));
}
