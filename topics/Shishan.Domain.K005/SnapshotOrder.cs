namespace Shishan.Domain.K005;

/// <summary>
/// The copy taken from an upstream order for the downstream side. One request
/// should end up with exactly one of these.
/// </summary>
public sealed class SnapshotOrder
{
    /// <summary>Takes a copy under the given identifier and printed number.</summary>
    public SnapshotOrder(string id, string number, string productName, string customer, int quantity)
    {
        Id = id ?? string.Empty;
        Number = number ?? string.Empty;
        ProductName = productName ?? string.Empty;
        Customer = customer ?? string.Empty;
        Quantity = quantity;
    }

    /// <summary>The identifier the copy is filed under, shared with the request.</summary>
    public string Id { get; }

    /// <summary>The number shown on the printed copy.</summary>
    public string Number { get; }

    /// <summary>The name of the goods at the time the copy was taken.</summary>
    public string ProductName { get; }

    /// <summary>The party the goods are meant for at the time the copy was taken.</summary>
    public string Customer { get; }

    /// <summary>The amount carried on the copy.</summary>
    public int Quantity { get; }

    /// <summary>True when the copy carries a positive amount.</summary>
    public bool HasContent() => Quantity > 0;

    /// <summary>True when the copy carries nothing.</summary>
    public bool IsEmpty() => Quantity == 0;

    /// <summary>True when the copy names the given goods, ignoring letter case.</summary>
    public bool Names(string productName) =>
        string.Equals(ProductName, productName, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the copy is meant for the given party, ignoring letter case.</summary>
    public bool ForCustomer(string customer) =>
        string.Equals(Customer, customer, StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the copy carries the same content as the other copy.</summary>
    public bool SameContentAs(SnapshotOrder other) =>
        other is not null &&
        string.Equals(ProductName, other.ProductName, StringComparison.Ordinal) &&
        string.Equals(Customer, other.Customer, StringComparison.Ordinal) &&
        Quantity == other.Quantity;

    /// <summary>A copy of this one carrying the given amount.</summary>
    public SnapshotOrder WithQuantity(int quantity) =>
        new(Id, Number, ProductName, Customer, quantity);

    /// <summary>A copy of this one carrying different content under the same identity.</summary>
    public SnapshotOrder WithContent(string productName, string customer, int quantity) =>
        new(Id, Number, productName, customer, quantity);

    /// <summary>A readable reading of the copy.</summary>
    public string Describe() => $"{Number} ({Id}): {ProductName} for {Customer} x{Quantity}";
}

/// <summary>Ways of reading a set of copies together.</summary>
public static class SnapshotOrders
{
    /// <summary>The first copy filed under the given identifier, or null.</summary>
    public static SnapshotOrder? FindById(IEnumerable<SnapshotOrder> copies, string id)
    {
        if (copies is null)
        {
            throw new ArgumentNullException(nameof(copies));
        }

        foreach (var copy in copies)
        {
            if (copy is not null && string.Equals(copy.Id, id, StringComparison.Ordinal))
            {
                return copy;
            }
        }

        return null;
    }

    /// <summary>The copies whose product name matches, in the order given.</summary>
    public static IReadOnlyList<SnapshotOrder> Named(IEnumerable<SnapshotOrder> copies, string productName)
    {
        if (copies is null)
        {
            throw new ArgumentNullException(nameof(copies));
        }

        var kept = new List<SnapshotOrder>();
        foreach (var copy in copies)
        {
            if (copy is not null && copy.Names(productName))
            {
                kept.Add(copy);
            }
        }

        return kept;
    }

    /// <summary>The combined amount across the copies.</summary>
    public static int TotalOf(IEnumerable<SnapshotOrder> copies)
    {
        if (copies is null)
        {
            throw new ArgumentNullException(nameof(copies));
        }

        var total = 0;
        foreach (var copy in copies)
        {
            if (copy is not null)
            {
                total += copy.Quantity;
            }
        }

        return total;
    }

    /// <summary>True when more than one copy shares an identifier.</summary>
    public static bool HasRepeatedId(IEnumerable<SnapshotOrder> copies)
    {
        if (copies is null)
        {
            throw new ArgumentNullException(nameof(copies));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var copy in copies)
        {
            if (copy is not null && !seen.Add(copy.Id))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A readable reading of every copy, joined for a log entry.</summary>
    public static string DescribeAll(IEnumerable<SnapshotOrder> copies)
    {
        if (copies is null)
        {
            throw new ArgumentNullException(nameof(copies));
        }

        var parts = new List<string>();
        foreach (var copy in copies)
        {
            parts.Add(copy is null ? "?" : copy.Describe());
        }

        return string.Join(" | ", parts);
    }
}