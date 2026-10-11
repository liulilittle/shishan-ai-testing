namespace Shishan.Domain.D004;

/// <summary>
/// The directory the floor uses to look an account up by its number. The
/// directory is kept in key order and holds every account that has been handed
/// a number, so an account can be found again even when several accounts share
/// a name. The floor desk reads the directory when a number is all it has.
/// </summary>
public sealed class EntryDirectory
{
    private readonly Dictionary<string, LedgerEntry> _byNumber = new(StringComparer.Ordinal);

    /// <summary>Adds an account to the directory, keyed by its number.</summary>
    public void Add(LedgerEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        var number = (entry.Number ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(number))
        {
            return;
        }

        lock (_byNumber)
        {
            if (!_byNumber.ContainsKey(number))
            {
                _byNumber[number] = entry;
            }
        }
    }

    /// <summary>The account carrying the given number, or none when the number is unknown.</summary>
    public LedgerEntry? Find(string number)
    {
        var key = (number ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        lock (_byNumber)
        {
            return _byNumber.TryGetValue(key, out var entry) ? entry : null;
        }
    }

    /// <summary>True when the directory holds the given number.</summary>
    public bool Holds(string number) => Find(number) != null;

    /// <summary>Every number the directory holds, in the order it was added.</summary>
    public IReadOnlyList<string> Numbers()
    {
        var numbers = new List<string>();
        lock (_byNumber)
        {
            numbers.AddRange(_byNumber.Keys);
        }

        return numbers;
    }

    /// <summary>How many accounts the directory holds.</summary>
    public int Count
    {
        get
        {
            lock (_byNumber)
            {
                return _byNumber.Count;
            }
        }
    }

    /// <summary>Removes an account from the directory, keyed by its number.</summary>
    public bool Remove(string number)
    {
        var key = (number ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        lock (_byNumber)
        {
            return _byNumber.Remove(key);
        }
    }

    /// <summary>Removes every account from the directory.</summary>
    public void Clear()
    {
        lock (_byNumber)
        {
            _byNumber.Clear();
        }
    }

    /// <summary>Every account the directory holds, in no particular order.</summary>
    public IReadOnlyList<LedgerEntry> All()
    {
        var entries = new List<LedgerEntry>();
        lock (_byNumber)
        {
            entries.AddRange(_byNumber.Values);
        }

        return entries;
    }

    /// <summary>How many of the accounts in the directory carry the given wording.</summary>
    public int CountNamed(string name)
    {
        var key = (name ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(key))
        {
            return 0;
        }

        var found = 0;
        lock (_byNumber)
        {
            foreach (var entry in _byNumber.Values)
            {
                if (string.Equals(entry.Name, key, StringComparison.Ordinal))
                {
                    found++;
                }
            }
        }

        return found;
    }
}