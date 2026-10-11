namespace Shishan.Domain.D004;

/// <summary>
/// The rate table kept for the rare booking that arrives in a foreign money.
/// Rates are entered by hand from the morning sheet and are read here when a
/// value has to be brought back into the house money. The house rarely books
/// in anything else, so the table sees little use.
/// </summary>
public sealed class ExchangeRateTable
{
    private readonly Dictionary<string, decimal> _ratesByMoney = new(StringComparer.Ordinal);

    public ExchangeRateTable()
    {
        _ratesByMoney["GBP"] = 1.0000m;
        _ratesByMoney["EUR"] = 0.86m;
        _ratesByMoney["USD"] = 0.78m;
        _ratesByMoney["JPY"] = 0.0052m;
    }

    /// <summary>The rate held for a money, or zero when none is held.</summary>
    public decimal RateOf(string money)
    {
        var key = MoneyKey(money);
        if (string.IsNullOrEmpty(key))
        {
            return 0m;
        }

        lock (_ratesByMoney)
        {
            return _ratesByMoney.TryGetValue(key, out var rate) ? rate : 0m;
        }
    }

    /// <summary>Sets the rate held for a money, read to four decimal places.</summary>
    public void SetRate(string money, decimal rate)
    {
        var key = MoneyKey(money);
        if (string.IsNullOrEmpty(key) || rate <= 0m)
        {
            return;
        }

        lock (_ratesByMoney)
        {
            _ratesByMoney[key] = MoneyMath.Round(rate, 4);
        }
    }

    /// <summary>
    /// Brings an amount from one money into another using the rates held, read
    /// to two decimal places. When either money has no rate the amount is
    /// brought back as nothing.
    /// </summary>
    public decimal Convert(decimal amount, string from, string to)
    {
        var fromRate = RateOf(from);
        var toRate = RateOf(to);
        if (fromRate == 0m || toRate == 0m)
        {
            return 0m;
        }

        var inHouseValue = amount * toRate / fromRate;
        return MoneyMath.Round(inHouseValue);
    }

    /// <summary>Every money the table holds a rate for.</summary>
    public IReadOnlyList<string> Moneys()
    {
        var keys = new List<string>();
        lock (_ratesByMoney)
        {
            keys.AddRange(_ratesByMoney.Keys);
            keys.Sort(StringComparer.Ordinal);
        }

        return keys;
    }

    /// <summary>How many moneys the table holds a rate for.</summary>
    public int Count
    {
        get
        {
            lock (_ratesByMoney)
            {
                return _ratesByMoney.Count;
            }
        }
    }

    /// <summary>True when the table holds a rate for the given money.</summary>
    public bool Knows(string money) => RateOf(money) != 0m;

    /// <summary>Removes every rate from the table.</summary>
    public void Clear()
    {
        lock (_ratesByMoney)
        {
            _ratesByMoney.Clear();
        }
    }

    /// <summary>
    /// The rate that would bring one unit of the given money into the house
    /// money, read to four decimal places.
    /// </summary>
    public decimal InHouseRate(string money)
    {
        var rate = RateOf(money);
        if (rate == 0m)
        {
            return 0m;
        }

        return MoneyMath.Round(1m / rate, 4);
    }

    /// <summary>
    /// The wording used when the desk is told how a value was brought back.
    /// </summary>
    public string Describe(decimal amount, string from, string to)
    {
        var carried = Convert(amount, from, to);
        return amount.ToString("0.00") + " from " + from + " read as " + carried.ToString("0.00") + " in " + to;
    }

    /// <summary>True when the table holds rates for both of the given moneys.</summary>
    public bool CoversBoth(string from, string to) => Knows(from) && Knows(to);

    private static string MoneyKey(string money) =>
        (money ?? string.Empty).Trim().ToUpperInvariant();
}