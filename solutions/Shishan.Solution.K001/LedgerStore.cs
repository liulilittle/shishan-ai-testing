namespace Shishan.Domain.K001;

/// <summary>
/// The book of record for stocktake rounds. Rows are kept by the round they
/// belong to, and the counted figures the floor reports are written down
/// against the round's own rows, so one round never reads another round's
/// work and every row stands alone on the round it opened on.
/// </summary>
public sealed class LedgerStore
{
    private readonly Dictionary<int, List<StocktakeLine>> _rounds = new();

    /// <summary>Writes a row onto a round.</summary>
    public void Register(int roundNo, StocktakeLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException(nameof(line));
        }

        lock (_rounds)
        {
            if (!_rounds.TryGetValue(roundNo, out var lines))
            {
                lines = new List<StocktakeLine>();
                _rounds[roundNo] = lines;
            }

            lines.Add(line);
        }
    }

    /// <summary>Finds a row of a round by product.</summary>
    public StocktakeLine? Find(int roundNo, string skuCode)
    {
        lock (_rounds)
        {
            if (!_rounds.TryGetValue(roundNo, out var lines))
            {
                return null;
            }

            foreach (var line in lines)
            {
                if (string.Equals(line.SkuCode, skuCode, StringComparison.Ordinal))
                {
                    return line;
                }
            }

            return null;
        }
    }

    /// <summary>Every row a round holds.</summary>
    public IReadOnlyList<StocktakeLine> LinesOf(int roundNo)
    {
        lock (_rounds)
        {
            if (!_rounds.TryGetValue(roundNo, out var lines))
            {
                return Array.Empty<StocktakeLine>();
            }

            return lines.ToArray();
        }
    }

    /// <summary>True when a round holds any row.</summary>
    public bool HasRound(int roundNo)
    {
        lock (_rounds)
        {
            return _rounds.TryGetValue(roundNo, out var lines) && lines.Count > 0;
        }
    }

    /// <summary>How many rounds the book is holding.</summary>
    public int RoundCount()
    {
        lock (_rounds)
        {
            return _rounds.Count;
        }
    }

    /// <summary>How many rows a round holds.</summary>
    public int RowCount(int roundNo)
    {
        lock (_rounds)
        {
            return _rounds.TryGetValue(roundNo, out var lines) ? lines.Count : 0;
        }
    }

    /// <summary>True when a round holds a row for a product.</summary>
    public bool Holds(int roundNo, string skuCode)
    {
        lock (_rounds)
        {
            return Find(roundNo, skuCode) != null;
        }
    }

    /// <summary>
    /// Writes down the counted figure the floor reports for a row of a round,
    /// and hands back the written record of the report.
    /// </summary>
    public ReviewRecord WriteCounted(
        int roundNo,
        string skuCode,
        decimal declaredQuantity,
        string unit,
        decimal baseQuantity)
    {
        lock (_rounds)
        {
            var line = Find(roundNo, skuCode);
            if (line == null)
            {
                throw new ArgumentException(StocktakeMessages.LineNotOnRound, nameof(skuCode));
            }

            line.SetCounted(baseQuantity, unit);

            return new ReviewRecord(
                roundNo,
                skuCode,
                declaredQuantity,
                unit,
                baseQuantity,
                DateTime.UtcNow);
        }
    }

    /// <summary>Every round number the book is holding, in the order they were opened.</summary>
    public IReadOnlyList<int> KnownRounds()
    {
        lock (_rounds)
        {
            var rounds = new List<int>(_rounds.Keys);
            rounds.Sort();
            return rounds;
        }
    }

    /// <summary>Every product the book holds a row for, across all rounds.</summary>
    public IReadOnlyList<string> KnownProducts()
    {
        var products = new List<string>();

        lock (_rounds)
        {
            foreach (var lines in _rounds.Values)
            {
                foreach (var line in lines)
                {
                    if (!products.Contains(line.SkuCode))
                    {
                        products.Add(line.SkuCode);
                    }
                }
            }
        }

        return products;
    }

    /// <summary>How many rows the book is holding in all.</summary>
    public int TotalRows()
    {
        lock (_rounds)
        {
            var total = 0;
            foreach (var lines in _rounds.Values)
            {
                total += lines.Count;
            }

            return total;
        }
    }

    /// <summary>How many of a round's rows have been counted.</summary>
    public int CountedRows(int roundNo)
    {
        lock (_rounds)
        {
            var counted = 0;
            if (!_rounds.TryGetValue(roundNo, out var lines))
            {
                return counted;
            }

            foreach (var line in lines)
            {
                if (line.IsCounted)
                {
                    counted++;
                }
            }

            return counted;
        }
    }

    /// <summary>How many of a round's rows have been signed off.</summary>
    public int SignedOffRows(int roundNo)
    {
        lock (_rounds)
        {
            var signed = 0;
            if (!_rounds.TryGetValue(roundNo, out var lines))
            {
                return signed;
            }

            foreach (var line in lines)
            {
                if (line.IsAudited)
                {
                    signed++;
                }
            }

            return signed;
        }
    }

    /// <summary>Clears every round the book is holding.</summary>
    public void Clear()
    {
        lock (_rounds)
        {
            _rounds.Clear();
        }
    }
}