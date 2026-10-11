namespace Shishan.Domain.D005;

/// <summary>
/// The sheet where the governance work line notes the correction rounds that
/// have been handed in for an archive. One number can be corrected several
/// times; the sheet keeps each round as its own line so the number's rounds
/// can be counted and read back.
/// </summary>
public sealed class CorrectionRegister
{
    private readonly List<CorrectionLine> _lines = new();
    private readonly object _sheet = new();

    /// <summary>Notes a correction round handed in for a number.</summary>
    public void Note(string number, string name)
    {
        lock (_sheet)
        {
            _lines.Add(new CorrectionLine(
                number,
                name,
                _lines.Count + 1,
                DateTime.UtcNow));
        }
    }

    /// <summary>How many correction rounds have been handed in for the number.</summary>
    public int CountFor(string number)
    {
        var count = 0;

        lock (_sheet)
        {
            foreach (var line in _lines)
            {
                if (string.Equals(line.Number, number, StringComparison.Ordinal))
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Every correction round handed in for the given number, in the order
    /// they were written.
    /// </summary>
    public IReadOnlyList<CorrectionLine> ForNumber(string number)
    {
        var result = new List<CorrectionLine>();

        lock (_sheet)
        {
            foreach (var line in _lines)
            {
                if (string.Equals(line.Number, number, StringComparison.Ordinal))
                {
                    result.Add(line);
                }
            }
        }

        return result;
    }

    /// <summary>The last correction round handed in for the number, or none.</summary>
    public CorrectionLine? LastFor(string number)
    {
        CorrectionLine? found = null;

        lock (_sheet)
        {
            foreach (var line in _lines)
            {
                if (string.Equals(line.Number, number, StringComparison.Ordinal))
                {
                    found = line;
                }
            }
        }

        return found;
    }

    /// <summary>How many correction rounds the sheet is holding in all.</summary>
    public int Total()
    {
        lock (_sheet)
        {
            return _lines.Count;
        }
    }

    /// <summary>The name most recently handed in for the given number, or none.</summary>
    public string? LastNameFor(string number)
    {
        var last = LastFor(number);
        return last == null ? null : last.Name;
    }

    /// <summary>True when at least one correction round was handed in for the number.</summary>
    public bool HasRounds(string number) => CountFor(number) > 0;

    /// <summary>Clears the sheet of every correction round it is holding.</summary>
    public void Clear()
    {
        lock (_sheet)
        {
            _lines.Clear();
        }
    }
}

/// <summary>
/// One correction round on the sheet: the number it was handed in for, the
/// name brought with it, the place it took on the sheet, and when it was
/// written.
/// </summary>
public sealed record CorrectionLine(string Number, string Name, int Order, DateTime NotedAt)
{
    /// <summary>The round read out as one line for a sheet.</summary>
    public string Line() => Order + " | " + Number + " | " + Name + " | " + NotedAt;
}