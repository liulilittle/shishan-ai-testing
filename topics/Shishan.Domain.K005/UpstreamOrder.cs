namespace Shishan.Domain.K005;

/// <summary>
/// The upstream order whose content drives the copy taken for the downstream
/// side. Its content may keep changing after that copy is taken.
/// </summary>
public sealed class UpstreamOrder
{
    private readonly List<UpstreamItem> _items;

    /// <summary>Opens an upstream order under the given identifier.</summary>
    public UpstreamOrder(
        string id,
        string productName,
        string customer,
        string requestedBy,
        int declaredQuantity,
        IEnumerable<UpstreamItem> items)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("An order identifier is required.", nameof(id));
        }

        Id = id;
        ProductName = productName ?? string.Empty;
        Customer = customer ?? string.Empty;
        RequestedBy = requestedBy ?? string.Empty;
        DeclaredQuantity = declaredQuantity;
        _items = new List<UpstreamItem>();
        if (items is not null)
        {
            foreach (var item in items)
            {
                if (item is not null)
                {
                    _items.Add(item);
                }
            }
        }
    }

    /// <summary>The identifier the request is known by.</summary>
    public string Id { get; }

    /// <summary>The name of the goods on the order.</summary>
    public string ProductName { get; private set; }

    /// <summary>The party the goods are meant for.</summary>
    public string Customer { get; private set; }

    /// <summary>The party that raised the upstream order.</summary>
    public string RequestedBy { get; private set; }

    /// <summary>The amount written on the order heading.</summary>
    public int DeclaredQuantity { get; private set; }

    /// <summary>Every line listed on the order, in the order given.</summary>
    public IReadOnlyList<UpstreamItem> Items
    {
        get
        {
            lock (_items)
            {
                return new List<UpstreamItem>(_items);
            }
        }
    }

    /// <summary>How many lines the order currently lists.</summary>
    public int LineCount
    {
        get
        {
            lock (_items)
            {
                return _items.Count;
            }
        }
    }

    /// <summary>The combined amount of the listed lines.</summary>
    public int RealQuantity => UpstreamItems.TotalOf(Items);

    /// <summary>How many listed lines carry a positive amount.</summary>
    public int StockedLineCount => UpstreamItems.CountPositive(Items);

    /// <summary>Replaces the content of the order with a fresh set of values.</summary>
    public void Amend(string productName, string customer, int declaredQuantity, IEnumerable<UpstreamItem> items)
    {
        ProductName = productName ?? string.Empty;
        Customer = customer ?? string.Empty;
        DeclaredQuantity = declaredQuantity;
        lock (_items)
        {
            _items.Clear();
            if (items is not null)
            {
                foreach (var item in items)
                {
                    if (item is not null)
                    {
                        _items.Add(item);
                    }
                }
            }
        }
    }

    /// <summary>Moves the order to a different party that raised it.</summary>
    public void Reassign(string requestedBy)
    {
        RequestedBy = requestedBy ?? string.Empty;
    }

    /// <summary>True when any listed line carries the given code.</summary>
    public bool HasCode(string sku) => UpstreamItems.FindByCode(Items, sku) is not null;

    /// <summary>The line carrying the given code, or null when there is none.</summary>
    public UpstreamItem? LineFor(string sku) => UpstreamItems.FindByCode(Items, sku);

    /// <summary>The distinct codes listed on the order, in first-seen order.</summary>
    public IReadOnlyList<string> Codes() => UpstreamItems.CodesOf(Items);

    /// <summary>True when the order lists nothing.</summary>
    public bool IsEmpty() => LineCount == 0;

    /// <summary>True when the heading amount differs from the listed amount.</summary>
    public bool HeadingDiffersFromLines() => DeclaredQuantity != RealQuantity;

    /// <summary>A readable reading of the order.</summary>
    public string Describe() =>
        $"{Id}: {ProductName} for {Customer} ({RealQuantity}, heading {DeclaredQuantity})";
}