namespace Shishan.Domain.D005;

/// <summary>
/// The wording the governance desk uses when it turns a request away.
/// </summary>
public static class ArchiveMessages
{
    public const string NumberRequired = "An archive number is required.";

    public const string NameRequired = "A household name is required.";

    public const string NumberAlreadyRegistered = "An archive under this number is already open.";

    public const string NumberUnknown = "No archive is open under this number.";
}

/// <summary>
/// Raised when a request does not fit the rules the governance line keeps for
/// its archives.
/// </summary>
public class ArchiveRuleException : Exception
{
    public ArchiveRuleException(string message) : base(message)
    {
    }

    public ArchiveRuleException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// Raised when an archive is asked to open under a number that has already
/// been opened.
/// </summary>
public sealed class ArchiveAlreadyRegisteredException : ArchiveRuleException
{
    public ArchiveAlreadyRegisteredException(string message) : base(message)
    {
    }

    public ArchiveAlreadyRegisteredException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// Raised when an archive is corrected under a number the ledger does not
/// hold.
/// </summary>
public sealed class ArchiveNotFoundException : ArchiveRuleException
{
    public ArchiveNotFoundException(string message) : base(message)
    {
    }

    public ArchiveNotFoundException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// The plain file that held the governance archives before the ledger came
/// into use. It is kept for reference so old sheets can still be read back;
/// nothing writes to it any more.
/// </summary>
public sealed class ArchiveReferenceFile
{
    private readonly List<string> _lines = new();

    /// <summary>How many lines the old file is holding.</summary>
    public int Count => _lines.Count;

    /// <summary>Adds a line to the old file.</summary>
    public void Append(string line)
    {
        if (line != null)
        {
            _lines.Add(line);
        }
    }

    /// <summary>The first line that begins with the given number.</summary>
    public string? FirstFor(string number)
    {
        foreach (var line in _lines)
        {
            if (line.StartsWith(number, StringComparison.Ordinal))
            {
                return line;
            }
        }

        return null;
    }

    /// <summary>Every line the old file is holding.</summary>
    public IReadOnlyList<string> All() => new List<string>(_lines);

    /// <summary>Clears the old file.</summary>
    public void Clear() => _lines.Clear();
}

/// <summary>
/// The read-out of the governance board. A run of cards is arranged into
/// sections by the name they are carried under, and a card can be read out as
/// one line, so the board can be pasted onto a sheet for the work line.
/// </summary>
public static class ArchiveBoard
{
    /// <summary>
    /// Arranges a run of cards into board sections, one section per name, in
    /// the order the names first appear.
    /// </summary>
    public static IReadOnlyList<BoardSection> Arrange(IEnumerable<BoardCard> cards)
    {
        var sections = new List<BoardSection>();
        if (cards == null)
        {
            return sections;
        }

        foreach (var card in cards)
        {
            var section = sections.FirstOrDefault(s => s.Name == card.Name);
            if (section == null)
            {
                section = new BoardSection(card.Name, new List<BoardCard>());
                sections.Add(section);
            }

            section.Cards.Add(card);
        }

        return sections;
    }

    /// <summary>The card read out as one line for a sheet.</summary>
    public static string ToLine(BoardCard card)
    {
        if (card == null)
        {
            return string.Empty;
        }

        return card.Number + " | " + card.Name + " | round " + card.Round;
    }
}