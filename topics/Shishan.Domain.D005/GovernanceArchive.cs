namespace Shishan.Domain.D005;

/// <summary>
/// One archive kept on the governance ledger. The number is the standing
/// identity of the archive and never changes once the archive is opened. The
/// name is the household the archive is carried for and can be corrected from
/// one round to the next as the household reports changes to its details.
/// </summary>
public sealed class GovernanceArchive
{
    public GovernanceArchive(string number, string name)
    {
        Number = number;
        Name = name;
        Round = 0;
        RegisteredAt = DateTime.UtcNow;
    }

    /// <summary>The standing identity of the archive.</summary>
    public string Number { get; }

    /// <summary>The household the archive is carried under.</summary>
    public string Name { get; private set; }

    /// <summary>How many correction rounds the archive has been through.</summary>
    public int Round { get; private set; }

    /// <summary>When the archive was opened.</summary>
    public DateTime RegisteredAt { get; }

    /// <summary>When the last correction round was applied.</summary>
    public DateTime? CorrectedAt { get; private set; }

    /// <summary>
    /// Applies a correction round under the given household name. The number
    /// is not touched; only the name and the round counter move on.
    /// </summary>
    public void Rename(string name)
    {
        Name = name;
        Round++;
        CorrectedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Notes a correction round while the household keeps its current name.
    /// </summary>
    public void NoteCorrection()
    {
        Round++;
        CorrectedAt = DateTime.UtcNow;
    }

    /// <summary>True when the archive carries the given number.</summary>
    public bool MatchesNumber(string number) =>
        string.Equals(Number, number, StringComparison.Ordinal);

    /// <summary>True when the archive is carried under the given household name.</summary>
    public bool CarriesName(string name) =>
        string.Equals(Name, name, StringComparison.Ordinal);

    /// <summary>True when the archive was corrected after the given moment.</summary>
    public bool CorrectedAfter(DateTime moment) =>
        CorrectedAt.HasValue && CorrectedAt.Value > moment;

    /// <summary>True when at least one correction round has been applied.</summary>
    public bool HasCorrection => Round > 0;

    /// <summary>True when the archive was opened before the given moment.</summary>
    public bool OpenedBefore(DateTime moment) => RegisteredAt < moment;

    /// <summary>The number and current name together, as the ledger reads them.</summary>
    public string Reference => Number + " " + Name;

    /// <summary>The correction outlook of the archive, in words.</summary>
    public string Outlook()
    {
        if (Round == 0)
        {
            return "as registered";
        }

        if (Round == 1)
        {
            return "corrected once";
        }

        return "corrected " + Round + " times";
    }

    public override string ToString() => Reference + " (" + Outlook() + ")";
}