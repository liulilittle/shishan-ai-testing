namespace Shishan.Domain.K005;

/// <summary>
/// The paper printed for a copy of an upstream order. One request should end up
/// with exactly one print, kept in line with the copy it belongs to.
/// </summary>
public sealed record SnapshotPrint(string SnapshotId, string ProductName, string Customer, int Quantity)
{
    /// <summary>True when the print carries a positive amount.</summary>
    public bool HasContent() => Quantity > 0;

    /// <summary>True when the print carries nothing.</summary>
    public bool IsEmpty() => Quantity == 0;

    /// <summary>True when the print belongs to the given copy.</summary>
    public bool BelongsTo(string snapshotId) =>
        string.Equals(SnapshotId, snapshotId, StringComparison.Ordinal);

    /// <summary>True when the print names the given goods.</summary>
    public bool Names(string productName) =>
        string.Equals(ProductName, productName, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the print is meant for the given party.</summary>
    public bool ForCustomer(string customer) =>
        string.Equals(Customer, customer, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the print carries the same content as the other print.</summary>
    public bool SameContentAs(SnapshotPrint other) =>
        other is not null &&
        string.Equals(SnapshotId, other.SnapshotId, StringComparison.Ordinal) &&
        string.Equals(ProductName, other.ProductName, StringComparison.Ordinal) &&
        string.Equals(Customer, other.Customer, StringComparison.Ordinal) &&
        Quantity == other.Quantity;

    /// <summary>A copy of the print carrying the given amount.</summary>
    public SnapshotPrint WithQuantity(int quantity) =>
        this with { Quantity = quantity };

    /// <summary>A copy of the print carrying different content.</summary>
    public SnapshotPrint WithContent(string productName, string customer, int quantity) =>
        this with { ProductName = productName, Customer = customer, Quantity = quantity };

    /// <summary>A readable reading of the print.</summary>
    public string Describe() => $"{SnapshotId}: {ProductName} for {Customer} x{Quantity}";
}

/// <summary>Ways of reading a set of prints together.</summary>
public static class SnapshotPrints
{
    /// <summary>The first print belonging to the given copy, or null.</summary>
    public static SnapshotPrint? FindBySnapshot(IEnumerable<SnapshotPrint> prints, string snapshotId)
    {
        if (prints is null)
        {
            throw new ArgumentNullException(nameof(prints));
        }

        foreach (var print in prints)
        {
            if (print is not null && print.BelongsTo(snapshotId))
            {
                return print;
            }
        }

        return null;
    }

    /// <summary>The prints naming the given goods, in the order given.</summary>
    public static IReadOnlyList<SnapshotPrint> Named(IEnumerable<SnapshotPrint> prints, string productName)
    {
        if (prints is null)
        {
            throw new ArgumentNullException(nameof(prints));
        }

        var kept = new List<SnapshotPrint>();
        foreach (var print in prints)
        {
            if (print is not null && print.Names(productName))
            {
                kept.Add(print);
            }
        }

        return kept;
    }

    /// <summary>The combined amount across the prints.</summary>
    public static int TotalOf(IEnumerable<SnapshotPrint> prints)
    {
        if (prints is null)
        {
            throw new ArgumentNullException(nameof(prints));
        }

        var total = 0;
        foreach (var print in prints)
        {
            if (print is not null)
            {
                total += print.Quantity;
            }
        }

        return total;
    }

    /// <summary>True when more than one print belongs to the same copy.</summary>
    public static bool HasRepeatedSource(IEnumerable<SnapshotPrint> prints)
    {
        if (prints is null)
        {
            throw new ArgumentNullException(nameof(prints));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var print in prints)
        {
            if (print is not null && !seen.Add(print.SnapshotId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A readable reading of every print, joined for a log entry.</summary>
    public static string DescribeAll(IEnumerable<SnapshotPrint> prints)
    {
        if (prints is null)
        {
            throw new ArgumentNullException(nameof(prints));
        }

        var parts = new List<string>();
        foreach (var print in prints)
        {
            parts.Add(print is null ? "?" : print.Describe());
        }

        return string.Join(" | ", parts);
    }
}