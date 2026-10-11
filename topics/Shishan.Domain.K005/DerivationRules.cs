namespace Shishan.Domain.K005;

/// <summary>
/// Turns the content of an upstream order into the records the downstream side
/// keeps: the copy taken at establishment, the work order that follows it, and
/// the paper printed for it.
/// </summary>
public sealed class DerivationRules
{
    /// <summary>The copy taken when an upstream order is established.</summary>
    public SnapshotOrder ComposeSnapshot(string id, string number, UpstreamOrder upstream)
    {
        if (upstream is null)
        {
            throw new ArgumentNullException(nameof(upstream));
        }

        return new SnapshotOrder(
            id,
            number,
            upstream.ProductName,
            upstream.Customer,
            upstream.RealQuantity);
    }

    /// <summary>The copy brought in line with the current upstream content.</summary>
    public SnapshotOrder ComposeRefreshed(UpstreamOrder upstream, SnapshotOrder current)
    {
        if (upstream is null)
        {
            throw new ArgumentNullException(nameof(upstream));
        }

        if (current is null)
        {
            throw new ArgumentNullException(nameof(current));
        }

        return new SnapshotOrder(
            current.Id,
            current.Number,
            upstream.ProductName,
            upstream.Customer,
            upstream.DeclaredQuantity);
    }

    /// <summary>The work order that follows a copy.</summary>
    public DownstreamOrder ComposeDownstream(string number, UpstreamOrder upstream, SnapshotOrder snapshot)
    {
        if (upstream is null)
        {
            throw new ArgumentNullException(nameof(upstream));
        }

        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return new DownstreamOrder(
            number,
            snapshot.Id,
            upstream.ProductName,
            upstream.Customer,
            upstream.RealQuantity);
    }

    /// <summary>The paper printed for a copy.</summary>
    public SnapshotPrint ComposePrint(UpstreamOrder upstream, SnapshotOrder snapshot)
    {
        if (upstream is null)
        {
            throw new ArgumentNullException(nameof(upstream));
        }

        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return new SnapshotPrint(
            snapshot.Id,
            upstream.ProductName,
            upstream.Customer,
            upstream.RealQuantity);
    }

    /// <summary>The paper printed for a copy, read from the copy itself.</summary>
    public SnapshotPrint ComposePrintFrom(SnapshotOrder snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return new SnapshotPrint(
            snapshot.Id,
            snapshot.ProductName,
            snapshot.Customer,
            snapshot.Quantity);
    }

    /// <summary>The amount the upstream order really lists.</summary>
    public int ListedAmountOf(UpstreamOrder upstream)
    {
        if (upstream is null)
        {
            throw new ArgumentNullException(nameof(upstream));
        }

        return upstream.RealQuantity;
    }

    /// <summary>The amount written on the heading of the upstream order.</summary>
    public int HeadingAmountOf(UpstreamOrder upstream)
    {
        if (upstream is null)
        {
            throw new ArgumentNullException(nameof(upstream));
        }

        return upstream.DeclaredQuantity;
    }

    /// <summary>True when the heading amount and the listed amount agree.</summary>
    public bool HeadingAgreesWithLines(UpstreamOrder upstream)
    {
        if (upstream is null)
        {
            throw new ArgumentNullException(nameof(upstream));
        }

        return upstream.DeclaredQuantity == upstream.RealQuantity;
    }

    /// <summary>The amount carried by the copy that a refresh should leave behind.</summary>
    public int RefreshedAmountOf(UpstreamOrder upstream)
    {
        if (upstream is null)
        {
            throw new ArgumentNullException(nameof(upstream));
        }

        return upstream.RealQuantity;
    }

    /// <summary>A readable reading of the content a copy would carry.</summary>
    public string DescribeContent(UpstreamOrder upstream)
    {
        if (upstream is null)
        {
            throw new ArgumentNullException(nameof(upstream));
        }

        return $"{upstream.ProductName} for {upstream.Customer} x{upstream.RealQuantity}";
    }
}