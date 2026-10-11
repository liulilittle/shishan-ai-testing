namespace Shishan.Domain.K005;

/// <summary>The error raised when a request breaks a rule of the desk.</summary>
public sealed class SnapshotException : Exception
{
    /// <summary>Raises the error with the given wording.</summary>
    public SnapshotException(string message) : base(message)
    {
    }
}

/// <summary>The wording the desk uses when a request is turned away.</summary>
public static class SnapshotErrors
{
    /// <summary>The wording for a missing request identifier.</summary>
    public static string IdRequired() => "A request identifier is required.";

    /// <summary>The wording for an upstream order the desk was never told about.</summary>
    public static string UpstreamMissing(string id) => $"Upstream order '{id}' was not found.";

    /// <summary>The wording for a copy that is not filed under the identifier.</summary>
    public static string CopyMissing(string id) => $"No copy is filed under '{id}'.";

    /// <summary>The wording for a copy that is already filed.</summary>
    public static string CopyPresent(string id) => $"A copy is already filed under '{id}'.";

    /// <summary>The wording for a request that carries nothing to derive.</summary>
    public static string NothingToDerive(string id) => $"Request '{id}' carries nothing to derive.";

    /// <summary>The wording for an order that lists no lines.</summary>
    public static string NoLines(string id) => $"Order '{id}' lists no lines.";

    /// <summary>The wording for a heading amount that disagrees with the lines.</summary>
    public static string HeadingMismatch(string id) => $"Order '{id}' has a heading that disagrees with its lines.";

    /// <summary>The wording for a party that did not raise the request.</summary>
    public static string NotRequester(string id, string party) =>
        $"'{party}' did not raise request '{id}'.";

    /// <summary>The wording for a copy that carries a negative amount.</summary>
    public static string NegativeAmount(string id) => $"Copy '{id}' carries a negative amount.";
}

/// <summary>
/// Reads an upstream order before it drives the flow, keeping the checks the
/// older desk used on paper. Kept for desks that still quote those checks.
/// </summary>
public static class UpstreamValidator
{
    /// <summary>True when the order has a code for every line.</summary>
    public static bool EveryLineHasCode(UpstreamOrder order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        foreach (var item in order.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Sku))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>True when the order lists at least one line.</summary>
    public static bool HasLines(UpstreamOrder order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        return order.LineCount > 0;
    }

    /// <summary>True when the order names the goods and the party.</summary>
    public static bool IsNamed(UpstreamOrder order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        return !string.IsNullOrWhiteSpace(order.ProductName) &&
               !string.IsNullOrWhiteSpace(order.Customer);
    }

    /// <summary>True when a party raised the order.</summary>
    public static bool HasRequester(UpstreamOrder order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        return !string.IsNullOrWhiteSpace(order.RequestedBy);
    }

    /// <summary>True when the heading amount is not negative.</summary>
    public static bool HeadingIsSane(UpstreamOrder order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        return order.DeclaredQuantity >= 0;
    }

    /// <summary>True when every listed line is not negative.</summary>
    public static bool LinesAreSane(UpstreamOrder order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        foreach (var item in order.Items)
        {
            if (item.IsNegative())
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>True when the order passes every check the older desk used.</summary>
    public static bool PassesOldDeskChecks(UpstreamOrder order) =>
        HasLines(order) &&
        IsNamed(order) &&
        HasRequester(order) &&
        HeadingIsSane(order) &&
        LinesAreSane(order) &&
        EveryLineHasCode(order);

    /// <summary>The reasons the order would be turned away by the older desk.</summary>
    public static IReadOnlyList<string> ReasonsAgainst(UpstreamOrder order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        var reasons = new List<string>();
        if (!HasLines(order))
        {
            reasons.Add("no lines");
        }

        if (!IsNamed(order))
        {
            reasons.Add("unnamed");
        }

        if (!HasRequester(order))
        {
            reasons.Add("no requester");
        }

        if (!HeadingIsSane(order))
        {
            reasons.Add("heading below zero");
        }

        if (!LinesAreSane(order))
        {
            reasons.Add("line below zero");
        }

        if (!EveryLineHasCode(order))
        {
            reasons.Add("line without a code");
        }

        return reasons;
    }

    /// <summary>A readable reading of the older desk checks.</summary>
    public static string Describe(UpstreamOrder order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        var reasons = ReasonsAgainst(order);
        return reasons.Count == 0 ? $"{order.Id}: passes" : $"{order.Id}: {string.Join(", ", reasons)}";
    }
}

/// <summary>
/// Small helpers that read how far an older reading of an order has drifted
/// from its current content. Kept for the daily reconciliation note.
/// </summary>
public static class EnsureFresh
{
    /// <summary>How far the heading amount sits from the listed amount.</summary>
    public static int HeadingGap(UpstreamOrder order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        return order.DeclaredQuantity - order.RealQuantity;
    }

    /// <summary>True when the heading amount and the listed amount agree.</summary>
    public static bool HeadingAgrees(UpstreamOrder order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        return order.DeclaredQuantity == order.RealQuantity;
    }

    /// <summary>True when a copy carries the same amount as the order currently lists.</summary>
    public static bool CopyAgreesWithLines(SnapshotOrder copy, UpstreamOrder order)
    {
        if (copy is null)
        {
            throw new ArgumentNullException(nameof(copy));
        }

        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        return copy.Quantity == order.RealQuantity;
    }

    /// <summary>True when a print carries the same amount as a copy.</summary>
    public static bool PrintAgreesWithCopy(SnapshotPrint print, SnapshotOrder copy)
    {
        if (print is null)
        {
            throw new ArgumentNullException(nameof(print));
        }

        if (copy is null)
        {
            throw new ArgumentNullException(nameof(copy));
        }

        return print.Quantity == copy.Quantity;
    }

    /// <summary>True when the names on the order and the copy agree.</summary>
    public static bool NamesAgree(UpstreamOrder order, SnapshotOrder copy)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        if (copy is null)
        {
            throw new ArgumentNullException(nameof(copy));
        }

        return copy.Names(order.ProductName) && copy.ForCustomer(order.Customer);
    }

    /// <summary>A readable note on how far a copy has drifted from its order.</summary>
    public static string Note(UpstreamOrder order, SnapshotOrder copy)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        if (copy is null)
        {
            throw new ArgumentNullException(nameof(copy));
        }

        var drift = copy.Quantity - order.RealQuantity;
        return $"{copy.Number}: drift {drift}";
    }
}