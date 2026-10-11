namespace Shishan.Domain.K002;

/// <summary>
/// A record of the paper formats the printing room used to hold for each
/// category before the current desk took over. The room keeps the old list
/// so earlier sheet layouts can still be looked up when an old order is
/// reprinted.
/// </summary>
public sealed class PrintTemplateLibrary
{
    private readonly Dictionary<PrintCategory, PrintTemplate> _formats = new();

    public PrintTemplateLibrary()
    {
        Register(PrintCategory.Invoice, new PrintTemplate("invoice head", "A4", "top copy"));
        Register(PrintCategory.DeliveryNote, new PrintTemplate("delivery slip", "A4", "goods copy"));
        Register(PrintCategory.PackingList, new PrintTemplate("packing slip", "A4", "carton copy"));
        Register(PrintCategory.Statement, new PrintTemplate("statement sheet", "A4", "balance copy"));
    }

    /// <summary>How many formats the old list is holding.</summary>
    public int Count => _formats.Count;

    /// <summary>
    /// The format the old list holds for a category, or null when the list
    /// holds none for it.
    /// </summary>
    public PrintTemplate? FormatFor(PrintCategory category)
    {
        return _formats.TryGetValue(category, out var format) ? format : null;
    }

    /// <summary>Adds a format to the old list, replacing one already held for the category.</summary>
    public void Register(PrintCategory category, PrintTemplate format)
    {
        if (format == null)
        {
            throw new ArgumentNullException(nameof(format));
        }

        _formats[category] = format;
    }

    /// <summary>
    /// The sheet size the old list keeps for a category, or the given fall
    /// back wording when the list holds none for it.
    /// </summary>
    public string SheetSize(PrintCategory category, string fallBack)
    {
        var format = FormatFor(category);
        return format != null ? format.SheetSize : fallBack;
    }

    /// <summary>
    /// Every category the old list holds a format for, in the order of the
    /// registers.
    /// </summary>
    public IReadOnlyList<PrintCategory> KnownCategories()
    {
        var list = new List<PrintCategory>(_formats.Keys);
        list.Sort((left, right) => left.CompareTo(right));
        return list;
    }

    /// <summary>
    /// The copies the old list names for a category, joined into one line so
    /// the paper room can see the run at a glance.
    /// </summary>
    public string CopiesLine(PrintCategory category)
    {
        var format = FormatFor(category);
        return format != null ? string.Join(", ", format.Copies) : string.Empty;
    }

    /// <summary>Removes the format the old list holds for a category.</summary>
    public bool Drop(PrintCategory category)
    {
        return _formats.Remove(category);
    }

    /// <summary>Clears the old list.</summary>
    public void ClearAll()
    {
        _formats.Clear();
    }
}

/// <summary>
/// The paper layout the old list kept for one category: the name the paper
/// room used for it, the sheet size it was run on, and the number of copies
/// that went into the run.
/// </summary>
public sealed record PrintTemplate(string Name, string SheetSize, params string[] Copies)
{
    /// <summary>How many copies the old layout called for.</summary>
    public int CopyCount => Copies == null ? 0 : Copies.Length;

    /// <summary>True when the layout asked for more than one copy.</summary>
    public bool RunsMultipleCopies => CopyCount > 1;

    /// <summary>Read out the way the paper room would say it.</summary>
    public override string ToString() =>
        Name + " / " + SheetSize + " / " + CopyCount + " cop" + (CopyCount == 1 ? "y" : "ies");
}