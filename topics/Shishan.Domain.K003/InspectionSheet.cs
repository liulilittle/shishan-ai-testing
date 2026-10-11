namespace Shishan.Domain.K003;

/// <summary>
/// One inspection sheet kept by the office. The sheet carries a header with
/// the number, the time it was last updated and the set of rows the sheet
/// holds. The page shown for the sheet is expected to carry exactly the rows
/// most recently handed in.
/// </summary>
public sealed class InspectionSheet
{
    private readonly List<InspectionItem> _heldRows = new();

    /// <summary>Opens a sheet with the given header time and rows.</summary>
    public InspectionSheet(
        string inspectionNo,
        IReadOnlyList<InspectionItem> initialRows,
        DateTime openedAt)
    {
        InspectionNo = inspectionNo;
        OpenedAt = openedAt;
        UpdatedAt = openedAt;

        if (initialRows != null)
        {
            foreach (var row in initialRows)
            {
                if (row != null)
                {
                    _heldRows.Add(row);
                }
            }
        }
    }

    /// <summary>The number the office reads the sheet by.</summary>
    public string InspectionNo { get; }

    /// <summary>When the sheet was opened in the office.</summary>
    public DateTime OpenedAt { get; }

    /// <summary>When the sheet was last updated on the header.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>The rows the sheet holds, in the order they were written.</summary>
    public IReadOnlyList<InspectionItem> HeldRows => _heldRows;

    /// <summary>How many rows the sheet holds.</summary>
    public int RowCount => _heldRows.Count;

    /// <summary>True when the sheet holds no rows at all.</summary>
    public bool IsEmpty => _heldRows.Count == 0;

    /// <summary>True when the sheet holds a row for the given item code.</summary>
    public bool Carries(string code)
    {
        var clean = InspectionItem.NormalizeCode(code);
        foreach (var row in _heldRows)
        {
            if (string.Equals(row.Code, clean, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Puts the given rows on the sheet, replacing whatever was held.</summary>
    public void TakeRows(IReadOnlyList<InspectionItem> rows)
    {
        _heldRows.Clear();
        if (rows == null)
        {
            return;
        }

        foreach (var row in rows)
        {
            if (row != null)
            {
                _heldRows.Add(row);
            }
        }
    }

    /// <summary>
    /// Writes a row onto the sheet. When the sheet already holds a row for the
    /// same item, the reading is replaced; otherwise the row is added at the
    /// end of the held set.
    /// </summary>
    public bool KeepRow(InspectionItem row)
    {
        if (row == null)
        {
            return false;
        }

        for (var index = 0; index < _heldRows.Count; index++)
        {
            if (string.Equals(_heldRows[index].Code, row.Code, StringComparison.Ordinal))
            {
                _heldRows[index] = row;
                return true;
            }
        }

        _heldRows.Add(row);
        return true;
    }

    /// <summary>Notes on the header that the sheet was updated at the given time.</summary>
    public void NoteUpdate(DateTime updatedAt)
    {
        if (updatedAt > UpdatedAt)
        {
            UpdatedAt = updatedAt;
        }
    }

    /// <summary>The reading held for the given item code, if one is held.</summary>
    public InspectionItem? LastReading(string code)
    {
        var clean = InspectionItem.NormalizeCode(code);
        for (var index = _heldRows.Count - 1; index >= 0; index--)
        {
            if (string.Equals(_heldRows[index].Code, clean, StringComparison.Ordinal))
            {
                return _heldRows[index];
            }
        }

        return null;
    }

    /// <summary>How many of the held rows are set aside.</summary>
    public int SetAsideCount()
    {
        var count = 0;
        foreach (var row in _heldRows)
        {
            if (row.IsSetAside)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>How many of the held rows still show on the page.</summary>
    public int ShownCount() => _heldRows.Count - SetAsideCount();

    /// <summary>True when the sheet was last updated before a given time.</summary>
    public bool LastUpdatedBefore(DateTime moment) => UpdatedAt < moment;

    /// <summary>True when the sheet was last updated after a given time.</summary>
    public bool LastUpdatedAfter(DateTime moment) => UpdatedAt > moment;

    /// <summary>The time the sheet was last updated, read as a plain value.</summary>
    public DateTime HeaderTime => UpdatedAt;

    /// <summary>A copy of the held rows, safe to hand out on its own.</summary>
    public IReadOnlyList<InspectionItem> CopyOfRows() => new List<InspectionItem>(_heldRows);

    /// <summary>Every item code held on the sheet, in written order.</summary>
    public IReadOnlyList<string> Codes()
    {
        var codes = new List<string>();
        foreach (var row in _heldRows)
        {
            if (string.IsNullOrEmpty(row.Code))
            {
                continue;
            }

            codes.Add(row.Code);
        }

        return codes;
    }

    /// <summary>True when the sheet was opened for the given number.</summary>
    public bool IsNumbered(string inspectionNo) =>
        string.Equals(InspectionNo, InspectionItem.NormalizeCode(inspectionNo), StringComparison.Ordinal);

    /// <summary>The plain wording the office uses for the sheet.</summary>
    public string Describe() =>
        "sheet " + InspectionNo + ", updated " + UpdatedAt.ToString("s") +
        ", " + _heldRows.Count + " rows";
}
