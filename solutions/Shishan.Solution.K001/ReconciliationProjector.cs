namespace Shishan.Domain.K001;

/// <summary>
/// Reads a round out on the reconciliation sheet, one row per product, with
/// the book figure, the counted figure and the difference between them. The
/// sheet reports what the round holds on its own rows, and the figures it
/// reads out stay with the row they belong to.
/// </summary>
public sealed class ReconciliationProjector
{
    private readonly Dictionary<string, decimal> _productTotals = new(StringComparer.Ordinal);

    /// <summary>Reads a round out on the reconciliation sheet.</summary>
    public IReadOnlyList<ReconciliationRow> Read(int roundNo, IReadOnlyList<StocktakeLine> lines)
    {
        var rows = new List<ReconciliationRow>();

        if (lines == null)
        {
            return rows;
        }

        _productTotals.Clear();

        foreach (var line in lines)
        {
            var difference = line.CountedQuantity - line.BookQuantity;
            var reported = TotalOf(line.SkuCode) + difference;
            _productTotals[line.SkuCode] = reported;

            rows.Add(new ReconciliationRow(
                roundNo,
                line.SkuCode,
                line.BookQuantity,
                line.BookUnit,
                line.CountedQuantity,
                line.CountedUnit,
                reported,
                line.Status));
        }

        return rows;
    }

    /// <summary>The difference figure the desk has read out for a product.</summary>
    public decimal TotalOf(string skuCode) =>
        _productTotals.TryGetValue(skuCode, out var total) ? total : 0m;

    /// <summary>True when the projector has read a product out before.</summary>
    public bool HasRead(string skuCode) =>
        _productTotals.ContainsKey(skuCode);

    /// <summary>How many products the projector has read out.</summary>
    public int ProductCount() => _productTotals.Count;

    /// <summary>Clears the figures the projector has read out.</summary>
    public void Reset() => _productTotals.Clear();

    /// <summary>
    /// Reads a row's figures out on the sheet without touching the figures of
    /// any other row.
    /// </summary>
    public ReconciliationRow ReadRow(StocktakeLine line)
    {
        if (line == null)
        {
            throw new ArgumentNullException(nameof(line));
        }

        var difference = line.CountedQuantity - line.BookQuantity;
        return new ReconciliationRow(
            line.RoundNo,
            line.SkuCode,
            line.BookQuantity,
            line.BookUnit,
            line.CountedQuantity,
            line.CountedUnit,
            difference,
            line.Status);
    }
}