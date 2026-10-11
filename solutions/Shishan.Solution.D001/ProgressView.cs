namespace Shishan.Domain.D001;

/// <summary>
/// The reading of one approval file's current step, ready for the desk. The
/// apply number and business number sit beside the step, so a view can be
/// matched to the file it came from.
/// </summary>
public sealed record ProgressView(string ApplyNo, string BusinessNo, string NodeCode, string NodeName, DateTime NodeTime)
{
    /// <summary>A view that carries no detail, used where no file was found.</summary>
    public static readonly ProgressView Blank = new(string.Empty, string.Empty, string.Empty, string.Empty, DateTime.MinValue);

    /// <summary>True when the view holds a step.</summary>
    public bool HasStep => !string.IsNullOrEmpty(NodeCode);

    /// <summary>True when the view holds no step yet.</summary>
    public bool HasNoStep => string.IsNullOrEmpty(NodeCode);

    /// <summary>The step as the office reads it out.</summary>
    public string ReadOut() => NodeName + " / " + NodeTime;

    /// <summary>The step's day, as the desk writes it.</summary>
    public string DayStamp() => NodeTime.ToString("yyyy-MM-dd");

    /// <summary>True when the view belongs to the given apply number.</summary>
    public bool OfApply(string applyNo) =>
        string.Equals(ApplyNo, applyNo, StringComparison.Ordinal);

    /// <summary>True when the view belongs to the given business number.</summary>
    public bool OfBusiness(string businessNo) =>
        string.Equals(BusinessNo, businessNo, StringComparison.Ordinal);

    /// <summary>True when the view matches the given step code.</summary>
    public bool AtStep(string code) =>
        string.Equals(NodeCode, code, StringComparison.Ordinal);

    /// <summary>True when the view has moved past the given step code.</summary>
    public bool PastStep(string code)
    {
        var current = ApprovalCatalog.PositionOf(NodeCode);
        var other = ApprovalCatalog.PositionOf(code);
        return current > 0 && other > 0 && current > other;
    }

    /// <summary>The view kept for a file that has not taken a step.</summary>
    public ProgressView WithoutStep() => new(ApplyNo, BusinessNo, string.Empty, string.Empty, DateTime.MinValue);

    /// <summary>The short form the desk prints on the list.</summary>
    public string ShortForm() => ApplyNo + " - " + NodeName;

    /// <summary>The one of two views that reached its step later.</summary>
    public static ProgressView? LaterStep(ProgressView? left, ProgressView? right)
    {
        if (left == null)
        {
            return right;
        }

        if (right == null)
        {
            return left;
        }

        return left.NodeTime >= right.NodeTime ? left : right;
    }

    /// <summary>True when the view names the same file as the given document.</summary>
    public bool OfDocument(ApprovalDocument doc)
    {
        if (doc == null)
        {
            return false;
        }

        return string.Equals(ApplyNo, doc.ApplyNo, StringComparison.Ordinal)
            && string.Equals(BusinessNo, doc.BusinessNo, StringComparison.Ordinal);
    }
}