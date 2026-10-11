namespace Shishan.Domain.D005;

/// <summary>
/// The card the governance board shows for one archive. It carries the number
/// and the name the archive is shown under, along with the correction round
/// that is current for the archive. The board mirrors the ledger as it is
/// looked at, so every card falls and rises with the archive behind it.
/// </summary>
public sealed record BoardCard(string Number, string Name, int Round)
{
    /// <summary>The card the board would show for an archive.</summary>
    public static BoardCard For(GovernanceArchive archive) =>
        new(archive.Number, archive.Name, archive.Round);

    /// <summary>A copy of the card carried under a different household name.</summary>
    public BoardCard WithName(string name) => this with { Name = name };

    /// <summary>A copy of the card at a later correction round.</summary>
    public BoardCard WithRound(int round) => this with { Round = round };

    /// <summary>True when the card belongs to the given number.</summary>
    public bool HoldsNumber(string number) =>
        string.Equals(Number, number, StringComparison.Ordinal);

    /// <summary>True when the card is carried under the given household name.</summary>
    public bool CarriesName(string name) =>
        string.Equals(Name, name, StringComparison.Ordinal);

    /// <summary>True when the card shows a correction round of at least the one given.</summary>
    public bool ShowsRound(int round) => Round >= round;

    /// <summary>The card read out the way the board shows it.</summary>
    public string Label() => Number + " " + Name + " (" + Round + ")";

    /// <summary>The code the board keeps an archive under.</summary>
    public string Deck() => Name;
}