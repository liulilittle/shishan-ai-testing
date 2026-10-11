namespace Shishan.Domain.K003;

/// <summary>
/// What a sheet looks like when read out on the wall page. The page carries
/// the sheet number, the time the sheet was last updated, and the rows that
/// show for the sheet.
/// </summary>
public sealed record InspectionDetail(
    string InspectionNo,
    DateTime UpdatedAt,
    IReadOnlyList<InspectionItem> Rows)
{
    /// <summary>True when the page has at least one row to show.</summary>
    public bool HasRows => Rows != null && Rows.Count > 0;

    /// <summary>How many rows the page shows.</summary>
    public int RowCount => Rows?.Count ?? 0;

    /// <summary>True when the page is showing a full blank sheet.</summary>
    public bool IsBlank => !HasRows;

    /// <summary>True when the page shows a row for the given item code.</summary>
    public bool Shows(string code)
    {
        if (Rows == null)
        {
            return false;
        }

        var clean = InspectionItem.NormalizeCode(code);
        foreach (var row in Rows)
        {
            if (string.Equals(row.Code, clean, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True when the page shows any row that was set aside.</summary>
    public bool ShowsAnySetAside()
    {
        if (Rows == null)
        {
            return false;
        }

        foreach (var row in Rows)
        {
            if (row.IsSetAside)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The row shown for the given item code, if the page has one.</summary>
    public InspectionItem? RowShown(string code)
    {
        if (Rows == null)
        {
            return null;
        }

        var clean = InspectionItem.NormalizeCode(code);
        foreach (var row in Rows)
        {
            if (string.Equals(row.Code, clean, StringComparison.Ordinal))
            {
                return row;
            }
        }

        return null;
    }

    /// <summary>Every item code shown on the page, in display order.</summary>
    public IReadOnlyList<string> Codes()
    {
        var codes = new List<string>();
        if (Rows == null)
        {
            return codes;
        }

        foreach (var row in Rows)
        {
            if (string.IsNullOrEmpty(row.Code))
            {
                continue;
            }

            codes.Add(row.Code);
        }

        return codes;
    }

    /// <summary>The page and the given rows, compared row by row.</summary>
    public bool RowSetEquals(IReadOnlyList<InspectionItem> other)
    {
        if (other == null)
        {
            return RowCount == 0;
        }

        if (RowCount != other.Count)
        {
            return false;
        }

        for (var index = 0; index < other.Count; index++)
        {
            var expected = other[index];
            var shown = Rows == null ? null : Rows[index];
            if (!ItemEquals(shown, expected))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>True when the page matches the rows handed in for a sheet.</summary>
    public bool Matches(InspectionSubmission submission) =>
        submission != null && RowSetEquals(submission.Rows);

    /// <summary>True when the page carries nothing the sheet was not given.</summary>
    public bool OnlyRowsFrom(IReadOnlyList<InspectionItem> source)
    {
        if (source == null)
        {
            return RowCount == 0;
        }

        var known = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in source)
        {
            if (item != null && !string.IsNullOrEmpty(item.Code))
            {
                known.Add(item.Code);
            }
        }

        if (Rows == null)
        {
            return true;
        }

        foreach (var row in Rows)
        {
            if (string.IsNullOrEmpty(row.Code) || !known.Contains(row.Code))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The plain wording a workshop person reads off the page.</summary>
    public string PageText() =>
        "sheet " + InspectionNo + " at " + UpdatedAt.ToString("s") +
        ", " + RowCount + " rows";

    /// <summary>True when the two rows carry the same item and reading.</summary>
    private static bool ItemEquals(InspectionItem? left, InspectionItem right)
    {
        if (left == null)
        {
            return right == null;
        }

        return string.Equals(left.Code, right.Code, StringComparison.Ordinal)
            && string.Equals(left.Value, right.Value, StringComparison.Ordinal)
            && left.Voided == right.Voided;
    }
}
