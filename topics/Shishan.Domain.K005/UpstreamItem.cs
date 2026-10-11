namespace Shishan.Domain.K005;

/// <summary>One line of goods listed on an upstream order.</summary>
public sealed record UpstreamItem(string Sku, int Qty)
{
    /// <summary>True when the line still carries a positive amount.</summary>
    public bool IsPositive() => Qty > 0;

    /// <summary>True when the line carries no amount at all.</summary>
    public bool IsEmpty() => Qty == 0;

    /// <summary>True when the line carries a negative amount.</summary>
    public bool IsNegative() => Qty < 0;

    /// <summary>True when the line matches the given code, ignoring letter case.</summary>
    public bool HasCode(string sku) => string.Equals(Sku, sku, StringComparison.OrdinalIgnoreCase);

    /// <summary>A copy of the line carrying the given amount.</summary>
    public UpstreamItem WithAmount(int qty) => this with { Qty = qty };

    /// <summary>A copy of the line under the given code.</summary>
    public UpstreamItem WithCode(string sku) => this with { Sku = sku };

    /// <summary>A readable reading of the line.</summary>
    public string Describe() => $"{Sku} x{Qty}";

    /// <summary>True when both the code and the amount match the other line.</summary>
    public bool SameAs(UpstreamItem other) =>
        other is not null && HasCode(other.Sku) && Qty == other.Qty;
}

/// <summary>Ways of reading a set of lines together.</summary>
public static class UpstreamItems
{
    /// <summary>The combined amount of every line, counting negative lines too.</summary>
    public static int TotalOf(IEnumerable<UpstreamItem> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var total = 0;
        foreach (var item in items)
        {
            if (item is null)
            {
                throw new ArgumentException("A line cannot be absent.", nameof(items));
            }

            total += item.Qty;
        }

        return total;
    }

    /// <summary>The combined amount of the positive lines only.</summary>
    public static int PositiveTotalOf(IEnumerable<UpstreamItem> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var total = 0;
        foreach (var item in items)
        {
            if (item is not null && item.IsPositive())
            {
                total += item.Qty;
            }
        }

        return total;
    }

    /// <summary>How many lines carry a positive amount.</summary>
    public static int CountPositive(IEnumerable<UpstreamItem> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var count = 0;
        foreach (var item in items)
        {
            if (item is not null && item.IsPositive())
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>The lines with a positive amount, in the order given.</summary>
    public static IReadOnlyList<UpstreamItem> PositiveOnly(IEnumerable<UpstreamItem> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var kept = new List<UpstreamItem>();
        foreach (var item in items)
        {
            if (item is not null && item.IsPositive())
            {
                kept.Add(item);
            }
        }

        return kept;
    }

    /// <summary>The distinct codes across the lines, in first-seen order.</summary>
    public static IReadOnlyList<string> CodesOf(IEnumerable<UpstreamItem> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var codes = new List<string>();
        foreach (var item in items)
        {
            if (item is not null && seen.Add(item.Sku))
            {
                codes.Add(item.Sku);
            }
        }

        return codes;
    }

    /// <summary>The first line carrying the given code, or null when there is none.</summary>
    public static UpstreamItem? FindByCode(IEnumerable<UpstreamItem> items, string sku)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        foreach (var item in items)
        {
            if (item is not null && item.HasCode(sku))
            {
                return item;
            }
        }

        return null;
    }

    /// <summary>True when the lines carry nothing at all.</summary>
    public static bool IsEmpty(IEnumerable<UpstreamItem> items) => TotalOf(items) == 0;

    /// <summary>A readable reading of every line, joined for a log entry.</summary>
    public static string DescribeAll(IEnumerable<UpstreamItem> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var parts = new List<string>();
        foreach (var item in items)
        {
            parts.Add(item is null ? "?" : item.Describe());
        }

        return string.Join(", ", parts);
    }
}