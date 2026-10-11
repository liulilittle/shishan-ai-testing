namespace Shishan.Domain.D005;

/// <summary>
/// The running record of the work line's rounds on its archives. Each entry
/// notes a number, the name it was under when the round was noted, and the
/// person who wrote the note, so the history of a number can be followed back
/// after the fact. The line keeps the record beside the ledger itself.
/// </summary>
public sealed class ArchiveAuditLog
{
    private readonly List<AuditEntry> _entries = new();
    private readonly object _log = new();

    /// <summary>Notes a round for a number under the person who wrote the note.</summary>
    public void Note(string number, string name, string clerk)
    {
        lock (_log)
        {
            _entries.Add(new AuditEntry(
                _entries.Count + 1,
                number,
                name,
                clerk ?? string.Empty,
                DateTime.UtcNow));
        }
    }

    /// <summary>Every entry in the record, in the order they were written.</summary>
    public IReadOnlyList<AuditEntry> Entries()
    {
        lock (_log)
        {
            return new List<AuditEntry>(_entries);
        }
    }

    /// <summary>How many entries the record is holding.</summary>
    public int Count()
    {
        lock (_log)
        {
            return _entries.Count;
        }
    }

    /// <summary>The most recent entry, or none when the record is empty.</summary>
    public AuditEntry? Latest()
    {
        lock (_log)
        {
            if (_entries.Count == 0)
            {
                return null;
            }

            return _entries[_entries.Count - 1];
        }
    }

    /// <summary>Every entry the record holds for a number.</summary>
    public IReadOnlyList<AuditEntry> ForNumber(string number)
    {
        var result = new List<AuditEntry>();

        lock (_log)
        {
            foreach (var entry in _entries)
            {
                if (string.Equals(entry.Number, number, StringComparison.Ordinal))
                {
                    result.Add(entry);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Every entry the record holds that was written by the given person.
    /// </summary>
    public IReadOnlyList<AuditEntry> ForClerk(string clerk)
    {
        var result = new List<AuditEntry>();

        lock (_log)
        {
            foreach (var entry in _entries)
            {
                if (string.Equals(entry.Clerk, clerk, StringComparison.Ordinal))
                {
                    result.Add(entry);
                }
            }
        }

        return result;
    }

    /// <summary>True when the record holds at least one entry for the number.</summary>
    public bool Holds(string number)
    {
        lock (_log)
        {
            foreach (var entry in _entries)
            {
                if (string.Equals(entry.Number, number, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Clears the record of every entry it is holding.</summary>
    public void Clear()
    {
        lock (_log)
        {
            _entries.Clear();
        }
    }
}

/// <summary>
/// One line in the ruling record: the number, the name it was under when the
/// line was written, the person who wrote it, and when.
/// </summary>
public sealed record AuditEntry(int Sequence, string Number, string Name, string Clerk, DateTime WrittenAt)
{
    /// <summary>The entry read out as one line for a sheet.</summary>
    public string Line() => Sequence + " | " + Number + " | " + Name + " | " + Clerk;

    /// <summary>True when the entry was written by the given person.</summary>
    public bool By(string clerk) => string.Equals(Clerk, clerk, StringComparison.Ordinal);
}