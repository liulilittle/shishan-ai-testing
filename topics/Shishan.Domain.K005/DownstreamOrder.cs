namespace Shishan.Domain.K005;

/// <summary>
/// The work order derived from a copy of an upstream order. One request should
/// end up with exactly one of these.
/// </summary>
public sealed class DownstreamOrder
{
    /// <summary>Derives a work order linked to the copy it came from.</summary>
    public DownstreamOrder(string number, string snapshotId, string productName, string customer, int quantity)
    {
        Number = number ?? string.Empty;
        SnapshotId = snapshotId ?? string.Empty;
        ProductName = productName ?? string.Empty;
        Customer = customer ?? string.Empty;
        Quantity = quantity;
    }

    /// <summary>The number shown on the work order.</summary>
    public string Number { get; }

    /// <summary>The identifier of the copy the work order came from.</summary>
    public string SnapshotId { get; }

    /// <summary>The name of the goods to work on.</summary>
    public string ProductName { get; }

    /// <summary>The party the goods are meant for.</summary>
    public string Customer { get; }

    /// <summary>The amount the work order asks for.</summary>
    public int Quantity { get; }

    /// <summary>True when the work order asks for a positive amount.</summary>
    public bool HasContent() => Quantity > 0;

    /// <summary>True when the work order asks for nothing.</summary>
    public bool IsEmpty() => Quantity == 0;

    /// <summary>True when the work order belongs to the given copy.</summary>
    public bool BelongsTo(string snapshotId) =>
        string.Equals(SnapshotId, snapshotId, StringComparison.Ordinal);

    /// <summary>True when the work order names the given goods.</summary>
    public bool Names(string productName) =>
        string.Equals(ProductName, productName, StringComparison.OrdinalIgnoreCase);

    /// <summary>A copy of the work order asking for the given amount.</summary>
    public DownstreamOrder WithQuantity(int quantity) =>
        new(Number, SnapshotId, ProductName, Customer, quantity);

    /// <summary>A readable reading of the work order.</summary>
    public string Describe() => $"{Number} from {SnapshotId}: {ProductName} x{Quantity}";
}

/// <summary>Ways of reading a set of work orders together.</summary>
public static class DownstreamOrders
{
    /// <summary>The first work order belonging to the given copy, or null.</summary>
    public static DownstreamOrder? FindBySnapshot(IEnumerable<DownstreamOrder> orders, string snapshotId)
    {
        if (orders is null)
        {
            throw new ArgumentNullException(nameof(orders));
        }

        foreach (var order in orders)
        {
            if (order is not null && order.BelongsTo(snapshotId))
            {
                return order;
            }
        }

        return null;
    }

    /// <summary>The work orders asking for a positive amount, in the order given.</summary>
    public static IReadOnlyList<DownstreamOrder> WithContent(IEnumerable<DownstreamOrder> orders)
    {
        if (orders is null)
        {
            throw new ArgumentNullException(nameof(orders));
        }

        var kept = new List<DownstreamOrder>();
        foreach (var order in orders)
        {
            if (order is not null && order.HasContent())
            {
                kept.Add(order);
            }
        }

        return kept;
    }

    /// <summary>The combined amount across the work orders.</summary>
    public static int TotalOf(IEnumerable<DownstreamOrder> orders)
    {
        if (orders is null)
        {
            throw new ArgumentNullException(nameof(orders));
        }

        var total = 0;
        foreach (var order in orders)
        {
            if (order is not null)
            {
                total += order.Quantity;
            }
        }

        return total;
    }

    /// <summary>True when more than one work order belongs to the same copy.</summary>
    public static bool HasRepeatedSource(IEnumerable<DownstreamOrder> orders)
    {
        if (orders is null)
        {
            throw new ArgumentNullException(nameof(orders));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var order in orders)
        {
            if (order is not null && !seen.Add(order.SnapshotId))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>A readable reading of every work order, joined for a log entry.</summary>
    public static string DescribeAll(IEnumerable<DownstreamOrder> orders)
    {
        if (orders is null)
        {
            throw new ArgumentNullException(nameof(orders));
        }

        var parts = new List<string>();
        foreach (var order in orders)
        {
            parts.Add(order is null ? "?" : order.Describe());
        }

        return string.Join(" | ", parts);
    }
}