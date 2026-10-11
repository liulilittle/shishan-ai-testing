namespace Shishan.Domain.D001;

/// <summary>
/// One step an approval file has taken. A step carries the code the office
/// uses to name it, the plain wording that sits beside it, and the moment the
/// step was reached.
/// </summary>
public sealed record ProgressNode(string Code, string Name, DateTime Time)
{
    /// <summary>A step that carries no detail, used where no step was reached.</summary>
    public static readonly ProgressNode None = new(string.Empty, string.Empty, DateTime.MinValue);

    /// <summary>True when the step has no code of its own.</summary>
    public bool IsNone => string.IsNullOrEmpty(Code);

    /// <summary>True when the step carries a wording.</summary>
    public bool HasWording => !string.IsNullOrEmpty(Name);

    /// <summary>True when the step was reached on the given day.</summary>
    public bool OccurredOn(DateTime day) => Time.Date == day.Date;

    /// <summary>True when the step was reached before the given moment.</summary>
    public bool Before(DateTime moment) => Time < moment;

    /// <summary>True when the step was reached after the given moment.</summary>
    public bool After(DateTime moment) => Time > moment;

    /// <summary>True when the step was reached in the same year as the given moment.</summary>
    public bool InSameYear(DateTime moment) => Time.Year == moment.Year;

    /// <summary>True when two steps carry the same code.</summary>
    public bool SameCode(ProgressNode other) =>
        other != null && string.Equals(Code, other.Code, StringComparison.Ordinal);

    /// <summary>True when one step names the same code.</summary>
    public bool Named(string code) =>
        string.Equals(Code, code, StringComparison.Ordinal);

    /// <summary>How the office reads the step out loud.</summary>
    public string ReadOut() => Name + " on " + Time;

    /// <summary>The short form the desk prints on the list.</summary>
    public string ShortForm() => Code + " - " + Name;

    /// <summary>The day the step was reached, as the desk writes it.</summary>
    public string DayStamp() => Time.ToString("yyyy-MM-dd");

    /// <summary>The step's own wording, or the code when no wording was given.</summary>
    public string WordingOrCode() => HasWording ? Name : Code;

    /// <summary>The step that comes later of two.</summary>
    public static ProgressNode? LaterOf(ProgressNode? left, ProgressNode? right)
    {
        if (left == null)
        {
            return right;
        }

        if (right == null)
        {
            return left;
        }

        return left.Time >= right.Time ? left : right;
    }

    /// <summary>The step that came earlier of two.</summary>
    public static ProgressNode? EarlierOf(ProgressNode? left, ProgressNode? right)
    {
        if (left == null)
        {
            return right;
        }

        if (right == null)
        {
            return left;
        }

        return left.Time <= right.Time ? left : right;
    }
}