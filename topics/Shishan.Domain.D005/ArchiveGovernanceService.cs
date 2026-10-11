namespace Shishan.Domain.D005;

/// <summary>
/// The desk that fronts the governance work line for archives. A household is
/// opened under a unique number; the archive can be corrected round by round
/// by number; the board beside the desk shows a card for each archive that
/// has been opened. The desk reads the ledger for one archive and reads the
/// board for the whole set.
/// </summary>
public sealed class ArchiveGovernanceService
{
    private readonly ArchiveStore _store;
    private readonly BoardCache _board;

    public ArchiveGovernanceService()
        : this(new ArchiveStore(), new BoardCache())
    {
    }

    public ArchiveGovernanceService(ArchiveStore store, BoardCache board)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _board = board ?? throw new ArgumentNullException(nameof(board));
    }

    /// <summary>
    /// Opens a new archive under its number. A number that has already been
    /// opened is turned away, so the same archive is not opened twice.
    /// </summary>
    public GovernanceArchive Register(string number, string name)
    {
        RequireNumber(number);
        RequireName(name);

        var archive = _store.Register(number, name);
        _board.PlaceCard(archive);
        return archive;
    }

    /// <summary>
    /// Applies a correction round to the archive that carries the given
    /// number, moving the archive to the corrected household name. Only the
    /// archive named by the number is corrected; a different archive that
    /// happens to carry the same name is left alone.
    /// </summary>
    public GovernanceArchive Correct(string number, string name)
    {
        RequireNumber(number);
        RequireName(name);

        var archive = _store.Correct(number, name);
        _board.RefreshCard(archive);
        return archive;
    }

    /// <summary>
    /// The archive carried under the given number, or none when the number
    /// has not been opened.
    /// </summary>
    public GovernanceArchive? Find(string number)
    {
        RequireNumber(number);
        return _store.Find(number);
    }

    /// <summary>The board as it stands: a card for every archive that has been opened.</summary>
    public IReadOnlyList<BoardCard> GetBoard() => _board.Cards();

    /// <summary>How many archives the ledger is holding.</summary>
    public int GetRegisteredCount() => _store.Count();

    /// <summary>True when the given number has been opened.</summary>
    public bool IsRegistered(string number)
    {
        RequireNumber(number);
        return _store.IsRegistered(number);
    }

    /// <summary>
    /// The card the board shows for the given number, or none when the number
    /// is not on the board.
    /// </summary>
    public BoardCard? GetCard(string number)
    {
        RequireNumber(number);
        return _board.CardForNumber(number);
    }

    /// <summary>How many cards the board is showing.</summary>
    public int GetBoardCount() => _board.Count();

    /// <summary>How many correction rounds the given number has gone through.</summary>
    public int GetRounds(string number)
    {
        RequireNumber(number);
        return _store.RoundsOf(number);
    }

    /// <summary>
    /// The household names the ledger holds, in alphabetic order.
    /// </summary>
    public IReadOnlyList<string> GetHouseholds() => _store.KnownNames();

    /// <summary>Every number the ledger has opened.</summary>
    public IReadOnlyList<string> GetNumbers() => _store.KnownNumbers();

    private static void RequireNumber(string number)
    {
        if (string.IsNullOrEmpty(number))
        {
            throw new ArgumentException(ArchiveMessages.NumberRequired, nameof(number));
        }
    }

    private static void RequireName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(ArchiveMessages.NameRequired, nameof(name));
        }
    }
}