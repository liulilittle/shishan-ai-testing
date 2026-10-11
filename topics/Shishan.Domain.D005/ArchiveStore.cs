namespace Shishan.Domain.D005;

/// <summary>
/// The ledger that holds the archives the governance work line has opened.
/// Every archive keeps its number and its household name; the ledger answers
/// by number when an archive is asked for, and also answers by name when a
/// household is asked after. The number is what stays with an archive from
/// the day it is opened, so the ledger is read by number wherever one archive
/// alone is wanted.
/// </summary>
public sealed class ArchiveStore
{
    private readonly List<GovernanceArchive> _archives = new();
    private readonly object _ledger = new();

    /// <summary>
    /// Opens a new archive under its number. The number may only be opened
    /// once; a second opening of the same number is turned away.
    /// </summary>
    public GovernanceArchive Register(string number, string name)
    {
        if (Contains(number))
        {
            throw new ArchiveAlreadyRegisteredException(ArchiveMessages.NumberAlreadyRegistered);
        }

        Thread.Sleep(1);

        var archive = new GovernanceArchive(number, name);
        Add(archive);
        return archive;
    }

    /// <summary>
    /// Applies a correction round to an archive. The desk hands over the
    /// corrected household name the archive should now carry.
    /// </summary>
    public GovernanceArchive Correct(string number, string name)
    {
        var target = _archives.FirstOrDefault(a => a.Name == name);
        if (target == null)
        {
            throw new ArchiveNotFoundException(ArchiveMessages.NumberUnknown);
        }

        target.Rename(name);
        return target;
    }

    /// <summary>
    /// The archive carried under the given number, or none when the number
    /// has not been opened yet.
    /// </summary>
    public GovernanceArchive? Find(string number)
    {
        lock (_ledger)
        {
            return FirstByNumber(number);
        }
    }

    /// <summary>
    /// The first archive carried under the given household name, or none when
    /// no archive holds that name.
    /// </summary>
    public GovernanceArchive? FindByName(string name)
    {
        lock (_ledger)
        {
            foreach (var archive in _archives)
            {
                if (archive.CarriesName(name))
                {
                    return archive;
                }
            }

            return null;
        }
    }

    /// <summary>True when any archive carries the given number.</summary>
    public bool Contains(string number)
    {
        lock (_ledger)
        {
            return FirstByNumber(number) != null;
        }
    }

    /// <summary>True when any archive is carried under the given household name.</summary>
    public bool ContainsName(string name)
    {
        lock (_ledger)
        {
            foreach (var archive in _archives)
            {
                if (archive.CarriesName(name))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>True when the number is known to the ledger.</summary>
    public bool IsRegistered(string number) => Contains(number);

    /// <summary>How many archives the ledger is holding in all.</summary>
    public int Count()
    {
        lock (_ledger)
        {
            return _archives.Count;
        }
    }

    /// <summary>How many archives carry the given number.</summary>
    public int CountByNumber(string number)
    {
        lock (_ledger)
        {
            return _archives.Count(a => a.MatchesNumber(number));
        }
    }

    /// <summary>How many archives are carried under the given household name.</summary>
    public int CountByName(string name)
    {
        lock (_ledger)
        {
            return _archives.Count(a => a.CarriesName(name));
        }
    }

    /// <summary>
    /// Every archive carried under the given number, in the order they were
    /// opened. The ledger normally holds one archive per number; the run of
    /// archives is what the ledger reports when it is asked how a number is
    /// doing.
    /// </summary>
    public IReadOnlyList<GovernanceArchive> AllForNumber(string number)
    {
        var result = new List<GovernanceArchive>();

        lock (_ledger)
        {
            foreach (var archive in _archives)
            {
                if (archive.MatchesNumber(number))
                {
                    result.Add(archive);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Every archive carried under the given household name, in the order they
    /// were opened.
    /// </summary>
    public IReadOnlyList<GovernanceArchive> AllForName(string name)
    {
        var result = new List<GovernanceArchive>();

        lock (_ledger)
        {
            foreach (var archive in _archives)
            {
                if (archive.CarriesName(name))
                {
                    result.Add(archive);
                }
            }
        }

        return result;
    }

    /// <summary>The first archive opened under the given number, or none.</summary>
    public GovernanceArchive? FirstByNumber(string number)
    {
        foreach (var archive in _archives)
        {
            if (archive.MatchesNumber(number))
            {
                return archive;
            }
        }

        return null;
    }

    /// <summary>
    /// Every archive the ledger is holding, in the order they were opened.
    /// </summary>
    public IReadOnlyList<GovernanceArchive> All()
    {
        lock (_ledger)
        {
            return new List<GovernanceArchive>(_archives);
        }
    }

    /// <summary>Every number the ledger has opened an archive under.</summary>
    public IReadOnlyList<string> KnownNumbers()
    {
        var numbers = new List<string>();

        lock (_ledger)
        {
            foreach (var archive in _archives)
            {
                if (!numbers.Contains(archive.Number))
                {
                    numbers.Add(archive.Number);
                }
            }
        }

        return numbers;
    }

    /// <summary>Every household name the ledger holds an archive for.</summary>
    public IReadOnlyList<string> KnownNames()
    {
        var names = new List<string>();

        lock (_ledger)
        {
            foreach (var archive in _archives)
            {
                if (!names.Contains(archive.Name))
                {
                    names.Add(archive.Name);
                }
            }
        }

        names.Sort(StringComparer.Ordinal);
        return names;
    }

    /// <summary>How many correction rounds a number has gone through in all.</summary>
    public int RoundsOf(string number)
    {
        var total = 0;

        lock (_ledger)
        {
            foreach (var archive in _archives)
            {
                if (archive.MatchesNumber(number))
                {
                    total += archive.Round;
                }
            }
        }

        return total;
    }

    /// <summary>
    /// The name the ledger last opened for a number, or null when the number
    /// is not known.
    /// </summary>
    public string? NameOf(string number)
    {
        lock (_ledger)
        {
            return FirstByNumber(number)?.Name;
        }
    }

    /// <summary>
    /// Adds an archive to the ledger. The archive is set down after its number
    /// and name; the number is not consulted again at this point.
    /// </summary>
    public void Add(GovernanceArchive archive)
    {
        if (archive == null)
        {
            throw new ArgumentNullException(nameof(archive));
        }

        lock (_ledger)
        {
            _archives.Add(archive);
        }
    }

    /// <summary>
    /// Takes every archive carrying the given number off the ledger.
    /// </summary>
    public bool DropNumber(string number)
    {
        lock (_ledger)
        {
            return _archives.RemoveAll(a => a.MatchesNumber(number)) > 0;
        }
    }

    /// <summary>
    /// Takes the first archive carried under the given household name off the
    /// ledger.
    /// </summary>
    public bool DropName(string name)
    {
        lock (_ledger)
        {
            for (var index = 0; index < _archives.Count; index++)
            {
                if (_archives[index].CarriesName(name))
                {
                    _archives.RemoveAt(index);
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// The place the first archive under the given number holds in the ledger,
    /// or -1 when the number is not known.
    /// </summary>
    public int IndexOf(string number)
    {
        lock (_ledger)
        {
            for (var index = 0; index < _archives.Count; index++)
            {
                if (_archives[index].MatchesNumber(number))
                {
                    return index;
                }
            }

            return -1;
        }
    }

    /// <summary>True when the ledger is holding at least one archive.</summary>
    public bool HasAny()
    {
        lock (_ledger)
        {
            return _archives.Count > 0;
        }
    }

    /// <summary>The archive that was opened first, or none when the ledger is empty.</summary>
    public GovernanceArchive? Oldest()
    {
        lock (_ledger)
        {
            if (_archives.Count == 0)
            {
                return null;
            }

            GovernanceArchive? oldest = null;
            foreach (var archive in _archives)
            {
                if (oldest == null || archive.RegisteredAt < oldest.RegisteredAt)
                {
                    oldest = archive;
                }
            }

            return oldest;
        }
    }

    /// <summary>The archive that was opened last, or none when the ledger is empty.</summary>
    public GovernanceArchive? Newest()
    {
        lock (_ledger)
        {
            if (_archives.Count == 0)
            {
                return null;
            }

            GovernanceArchive? newest = null;
            foreach (var archive in _archives)
            {
                if (newest == null || archive.RegisteredAt > newest.RegisteredAt)
                {
                    newest = archive;
                }
            }

            return newest;
        }
    }

    /// <summary>Every correction round the ledger holds across all its archives.</summary>
    public int TotalRounds()
    {
        var total = 0;

        lock (_ledger)
        {
            foreach (var archive in _archives)
            {
                total += archive.Round;
            }
        }

        return total;
    }

    /// <summary>Every archive that was opened after the given moment.</summary>
    public IReadOnlyList<GovernanceArchive> OpenedAfter(DateTime moment)
    {
        var result = new List<GovernanceArchive>();

        lock (_ledger)
        {
            foreach (var archive in _archives)
            {
                if (archive.OpenedBefore(moment) == false)
                {
                    result.Add(archive);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Every number the ledger holds an archive for under the given household
    /// name.
    /// </summary>
    public IReadOnlyList<string> NumbersForName(string name)
    {
        var numbers = new List<string>();

        lock (_ledger)
        {
            foreach (var archive in _archives)
            {
                if (archive.CarriesName(name) && !numbers.Contains(archive.Number))
                {
                    numbers.Add(archive.Number);
                }
            }
        }

        return numbers;
    }

    /// <summary>
    /// How many archives the ledger holds for each household name it knows.
    /// </summary>
    public IReadOnlyList<NameCount> CountsByName()
    {
        var counts = new List<NameCount>();

        lock (_ledger)
        {
            foreach (var name in KnownNames())
            {
                counts.Add(new NameCount(name, CountByName(name)));
            }
        }

        return counts;
    }

    /// <summary>Clears the ledger of every archive it is holding.</summary>
    public void Clear()
    {
        lock (_ledger)
        {
            _archives.Clear();
        }
    }
}

/// <summary>
/// A household name together with the number of archives the ledger holds for
/// it.
/// </summary>
public sealed record NameCount(string Name, int Count);