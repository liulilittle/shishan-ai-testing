namespace Shishan.Domain.D005;

/// <summary>
/// One section of the governance board. The board is read out in sections,
/// each gathering the cards that are carried under one household name, so the
/// work line can see all the archives of one name in one place.
/// </summary>
public sealed class BoardSection
{
    public BoardSection(string name, List<BoardCard> cards)
    {
        Name = name;
        Cards = cards ?? new List<BoardCard>();
    }

    /// <summary>The household name the section gathers cards under.</summary>
    public string Name { get; }

    /// <summary>The cards gathered under the section's name.</summary>
    public List<BoardCard> Cards { get; }

    /// <summary>How many cards the section is holding.</summary>
    public int Count => Cards.Count;

    /// <summary>True when the section holds no cards.</summary>
    public bool IsEmpty => Cards.Count == 0;

    /// <summary>Adds a card to the section.</summary>
    public void Add(BoardCard card)
    {
        if (card != null)
        {
            Cards.Add(card);
        }
    }

    /// <summary>The first card in the section, or none when the section is empty.</summary>
    public BoardCard? First()
    {
        if (Cards.Count == 0)
        {
            return null;
        }

        return Cards[0];
    }

    /// <summary>The last card in the section, or none when the section is empty.</summary>
    public BoardCard? Last()
    {
        if (Cards.Count == 0)
        {
            return null;
        }

        return Cards[Cards.Count - 1];
    }

    /// <summary>True when the section holds a card for the given number.</summary>
    public bool HoldsNumber(string number)
    {
        foreach (var card in Cards)
        {
            if (card.HoldsNumber(number))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The section read out as one line for a sheet.</summary>
    public string Label() => Name + " (" + Count + " card" + (Count == 1 ? string.Empty : "s") + ")";
}