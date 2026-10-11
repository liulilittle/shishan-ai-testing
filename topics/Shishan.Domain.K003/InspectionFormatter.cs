namespace Shishan.Domain.K003;

/// <summary>
/// Turns stored sheet values into the plain wording shown on the wall page,
/// so the workshop does not need to read the raw values off the shelf.
/// </summary>
public static class InspectionFormatter
{
    /// <summary>Reads one row in floor terms, using the known item wording.</summary>
    public static string DescribeRow(InspectionItem row)
    {
        if (row == null)
        {
            return string.Empty;
        }

        var mark = row.IsSetAside ? "x " : string.Empty;
        var name = InspectionCatalog.WordingFor(row.Code);
        return mark + row.Code + " " + name + " = " + row.Reading;
    }

    /// <summary>Reads a set of rows, one per line.</summary>
    public static IReadOnlyList<string> DescribeRows(IEnumerable<InspectionItem> rows)
    {
        var lines = new List<string>();
        if (rows == null)
        {
            return lines;
        }

        foreach (var row in rows)
        {
            if (row != null)
            {
                lines.Add(DescribeRow(row));
            }
        }

        return lines;
    }

    /// <summary>Reads a whole sheet page in floor terms.</summary>
    public static string DescribeSheet(InspectionSheet sheet)
    {
        if (sheet == null)
        {
            return string.Empty;
        }

        var header = "sheet " + sheet.InspectionNo +
            " opened " + sheet.OpenedAt.ToString("s") +
            " updated " + sheet.HeaderTime.ToString("s");
        var lines = new List<string> { header };
        foreach (var row in sheet.HeldRows)
        {
            lines.Add("  " + DescribeRow(row));
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Reads a detail page in floor terms.</summary>
    public static string DescribeDetail(InspectionDetail detail)
    {
        if (detail == null)
        {
            return string.Empty;
        }

        var header = "page " + detail.InspectionNo +
            " updated " + detail.UpdatedAt.ToString("s");
        var lines = new List<string> { header };
        foreach (var row in detail.Rows ?? Array.Empty<InspectionItem>())
        {
            lines.Add("  " + DescribeRow(row));
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Reads the reading held for an item code as its plain value.</summary>
    public static string ReadingOf(InspectionItem row) =>
        row == null ? "-" : row.Reading;

    /// <summary>True when the reading is a pass mark written on the floor.</summary>
    public static bool ReadsPassed(InspectionItem row) =>
        row != null && string.Equals(
            row.Value?.Trim(),
            "pass",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the reading is a fail mark written on the floor.</summary>
    public static bool ReadsFailed(InspectionItem row) =>
        row != null && string.Equals(
            row.Value?.Trim(),
            "fail",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>How many rows of a set have not been set aside.</summary>
    public static int ShownCount(IEnumerable<InspectionItem> rows)
    {
        var count = 0;
        if (rows == null)
        {
            return count;
        }

        foreach (var row in rows)
        {
            if (row != null && row.Shown)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>How many rows of a set have been set aside.</summary>
    public static int SetAsideCount(IEnumerable<InspectionItem> rows)
    {
        var count = 0;
        if (rows == null)
        {
            return count;
        }

        foreach (var row in rows)
        {
            if (row != null && row.IsSetAside)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>The codes of the rows that have not been set aside, in order.</summary>
    public static IReadOnlyList<string> ShownCodes(IReadOnlyList<InspectionItem> rows)
    {
        var codes = new List<string>();
        if (rows == null)
        {
            return codes;
        }

        foreach (var row in rows)
        {
            if (row != null && row.Shown && !string.IsNullOrEmpty(row.Code))
            {
                codes.Add(row.Code);
            }
        }

        return codes;
    }

    /// <summary>True when every row of a set passes on the floor.</summary>
    public static bool AllPassed(IEnumerable<InspectionItem> rows)
    {
        if (rows == null)
        {
            return true;
        }

        foreach (var row in rows)
        {
            if (row != null && !ReadsPassed(row))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>True when any row of a set fails on the floor.</summary>
    public static bool AnyFailed(IEnumerable<InspectionItem> rows)
    {
        if (rows == null)
        {
            return false;
        }

        foreach (var row in rows)
        {
            if (row != null && ReadsFailed(row))
            {
                return true;
            }
        }

        return false;
    }
}