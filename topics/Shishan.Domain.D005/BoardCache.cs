namespace Shishan.Domain.D005;

/// <summary>
/// The governance board that shows a card for each archive the ledger has
/// opened. Cards are kept here so the board can be read back without walking
/// the whole ledger every time; a card is placed when an archive is opened and
/// is looked up again when the board is shown. The board keeps one card per
/// household name that has been opened.
/// </summary>
public sealed class BoardCache
{
    private readonly Dictionary<string, BoardCard> _cards = new(StringComparer.Ordinal);
    private readonly object _board = new();

    /// <summary>
    /// Puts a card on the board for an archive. A card already shown under the
    /// same name gives way to the new card.
    /// </summary>
    public void PlaceCard(GovernanceArchive archive)
    {
        if (archive == null)
        {
            throw new ArgumentNullException(nameof(archive));
        }

        lock (_board)
        {
            _cards[archive.Name] = BoardCard.For(archive);
        }
    }

    /// <summary>
    /// Refreshes the card an archive shows on the board. The card is put up
    /// again under the household name the archive now carries.
    /// </summary>
    public void RefreshCard(GovernanceArchive archive)
    {
        if (archive == null)
        {
            throw new ArgumentNullException(nameof(archive));
        }

        lock (_board)
        {
            _cards[archive.Name] = BoardCard.For(archive);
        }
    }

    /// <summary>
    /// Takes the card shown under a household name off the board.
    /// </summary>
    public void DropCard(string name)
    {
        lock (_board)
        {
            _cards.Remove(name);
        }
    }

    /// <summary>
    /// Every card currently on the board, in the order the names were put up.
    /// </summary>
    public IReadOnlyList<BoardCard> Cards()
    {
        lock (_board)
        {
            return new List<BoardCard>(_cards.Values);
        }
    }

    /// <summary>
    /// The card the board shows for the given number, found by walking the
    /// cards that are up, or none when the board does not hold that number.
    /// </summary>
    public BoardCard? CardForNumber(string number)
    {
        lock (_board)
        {
            foreach (var card in _cards.Values)
            {
                if (card.HoldsNumber(number))
                {
                    return card;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// The card the board shows under the given household name, or none when
    /// the board does not hold that name.
    /// </summary>
    public BoardCard? CardForName(string name)
    {
        lock (_board)
        {
            return _cards.TryGetValue(name, out var card) ? card : null;
        }
    }

    /// <summary>True when the board shows a card for the given number.</summary>
    public bool HasCardForNumber(string number)
    {
        lock (_board)
        {
            foreach (var card in _cards.Values)
            {
                if (card.HoldsNumber(number))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>True when the board shows a card under the given household name.</summary>
    public bool HasCardForName(string name)
    {
        lock (_board)
        {
            return _cards.ContainsKey(name);
        }
    }

    /// <summary>How many cards are currently on the board.</summary>
    public int Count()
    {
        lock (_board)
        {
            return _cards.Count;
        }
    }

    /// <summary>How many cards on the board carry the given number.</summary>
    public int CountForNumber(string number)
    {
        var count = 0;

        lock (_board)
        {
            foreach (var card in _cards.Values)
            {
                if (card.HoldsNumber(number))
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Every card the board shows under the given household name.
    /// </summary>
    public IReadOnlyList<BoardCard> CardsForName(string name)
    {
        var result = new List<BoardCard>();

        lock (_board)
        {
            foreach (var card in _cards.Values)
            {
                if (card.CarriesName(name))
                {
                    result.Add(card);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Takes every card carrying the given number off the board.
    /// </summary>
    public bool DropNumber(string number)
    {
        var dropped = false;

        lock (_board)
        {
            var toDrop = new List<string>();
            foreach (var entry in _cards)
            {
                if (entry.Value.HoldsNumber(number))
                {
                    toDrop.Add(entry.Key);
                    dropped = true;
                }
            }

            foreach (var name in toDrop)
            {
                _cards.Remove(name);
            }
        }

        return dropped;
    }

        /// <summary>True when the board is not showing any card.</summary>
    public bool IsEmpty()
    {
        lock (_board)
        {
            return _cards.Count == 0;
        }
    }

    /// <summary>The card the board shows under the given number, or none.</summary>
    public BoardCard? OldestCard()
    {
        lock (_board)
        {
            foreach (var card in _cards.Values)
            {
                return card;
            }

            return null;
        }
    }

    /// <summary>The names the board is showing cards under, in the order they were put up.</summary>
    public IReadOnlyList<string> Names()
    {
        lock (_board)
        {
            return new List<string>(_cards.Keys);
        }
    }

    /// <summary>Clears the board of every card it is showing.</summary>
    public void Clear()
    {
        lock (_board)
        {
            _cards.Clear();
        }
    }
}