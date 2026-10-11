namespace Shishan.Domain.D001;

/// <summary>
/// The reader the office used before the current desk was put in place. It
/// read progress out of the old paper ledger, where one file carried one
/// business number and the apply number was written by hand on the cover.
/// The office keeps it so the wording of older reports can still be followed.
/// </summary>
public sealed class LegacyProgressReader
{
    private readonly List<LegacyFileCard> _cards = new();

    /// <summary>How many cards the old ledger holds.</summary>
    public int Count => _cards.Count;

    /// <summary>Adds a card to the old ledger.</summary>
    public void Add(LegacyFileCard card)
    {
        if (card == null)
        {
            throw new ArgumentNullException(nameof(card));
        }

        _cards.Add(card);
    }

    /// <summary>The card kept under an apply number in the old ledger.</summary>
    public LegacyFileCard? Find(string applyNo)
    {
        foreach (var card in _cards)
        {
            if (Same(card.ApplyNo, applyNo))
            {
                return card;
            }
        }

        return null;
    }

    /// <summary>Every card the old ledger holds for a business number.</summary>
    public IReadOnlyList<LegacyFileCard> ForBusiness(string businessNo)
    {
        var result = new List<LegacyFileCard>();
        foreach (var card in _cards)
        {
            if (Same(card.BusinessNo, businessNo))
            {
                result.Add(card);
            }
        }

        return result;
    }

    /// <summary>The most recent step the old ledger holds for an apply number.</summary>
    public ProgressNode? LatestStep(string applyNo)
    {
        var card = Find(applyNo);
        return card == null ? null : card.Latest;
    }

    /// <summary>How many cards the old ledger holds for a business number.</summary>
    public int CountForBusiness(string businessNo)
    {
        var count = 0;
        foreach (var card in _cards)
        {
            if (Same(card.BusinessNo, businessNo))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Every business number the old ledger holds a card for.</summary>
    public IReadOnlyList<string> BusinessNumbers()
    {
        var numbers = new List<string>();
        foreach (var card in _cards)
        {
            if (!numbers.Contains(card.BusinessNo))
            {
                numbers.Add(card.BusinessNo);
            }
        }

        numbers.Sort(StringComparer.Ordinal);
        return numbers;
    }

    /// <summary>Every apply number the old ledger holds a card for.</summary>
    public IReadOnlyList<string> ApplyNumbers()
    {
        var numbers = new List<string>();
        foreach (var card in _cards)
        {
            if (!numbers.Contains(card.ApplyNo))
            {
                numbers.Add(card.ApplyNo);
            }
        }

        numbers.Sort(StringComparer.Ordinal);
        return numbers;
    }

    /// <summary>Every card the old ledger holds, in the order they were added.</summary>
    public IReadOnlyList<LegacyFileCard> All()
    {
        var result = new List<LegacyFileCard>(_cards);
        return result;
    }

    /// <summary>How many cards the old ledger holds that have taken a step.</summary>
    public int StartedCount()
    {
        var started = 0;
        foreach (var card in _cards)
        {
            if (card.StepCount > 0)
            {
                started++;
            }
        }

        return started;
    }

    /// <summary>Clears the old ledger.</summary>
    public void Clear() => _cards.Clear();

    /// <summary>Reads the old ledger back as the office used to report it.</summary>
    public IReadOnlyList<string> OldReport(string businessNo)
    {
        var lines = new List<string>();
        foreach (var card in ForBusiness(businessNo))
        {
            lines.Add(card.Reference + " - " + card.Describe());
        }

        return lines;
    }

    private static bool Same(string left, string right) =>
        string.Equals(left, right, StringComparison.Ordinal);
}

/// <summary>
/// One file card from the old paper ledger. It holds the business number the
/// file served and the apply number written on the cover, together with the
/// steps that were stamped on the card.
/// </summary>
public sealed class LegacyFileCard
{
    private readonly List<ProgressNode> _steps = new();

    public LegacyFileCard(string businessNo, string applyNo, string heading)
    {
        BusinessNo = businessNo ?? string.Empty;
        ApplyNo = applyNo ?? string.Empty;
        Heading = heading ?? string.Empty;
    }

    public string BusinessNo { get; }

    public string ApplyNo { get; }

    public string Heading { get; }

    /// <summary>The steps stamped on the card.</summary>
    public IReadOnlyList<ProgressNode> Steps => _steps;

    /// <summary>The most recent step stamped on the card.</summary>
    public ProgressNode? Latest => _steps.Count == 0 ? null : _steps[_steps.Count - 1];

    /// <summary>The first step stamped on the card.</summary>
    public ProgressNode? First => _steps.Count == 0 ? null : _steps[0];

    /// <summary>The reference the office used for the card.</summary>
    public string Reference => BusinessNo + "/" + ApplyNo;

    /// <summary>Stamps a step on the card.</summary>
    public void Stamp(ProgressNode step)
    {
        if (step == null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        _steps.Add(step);
    }

    /// <summary>Reads the card as the old desk used to read it out.</summary>
    public string Describe()
    {
        if (_steps.Count == 0)
        {
            return Heading + " - not started";
        }

        return Heading + " - " + Latest!.Name;
    }

    /// <summary>How many steps are stamped on the card.</summary>
    public int StepCount => _steps.Count;

    /// <summary>True when the card carries a step with the given code.</summary>
    public bool HasStep(string code)
    {
        foreach (var step in _steps)
        {
            if (string.Equals(step.Code, code, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}