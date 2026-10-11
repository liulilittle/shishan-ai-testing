namespace Shishan.Domain.K003;

/// <summary>
/// Raised when a request does not match a sheet the office keeps, or is
/// written in a way the office cannot take on.
/// </summary>
public sealed class InspectionRuleException : Exception
{
    public InspectionRuleException(string message) : base(message)
    {
    }

    public InspectionRuleException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// The wording the office uses when it turns a request away.
/// </summary>
public static class InspectionMessages
{
    /// <summary>A sheet number is needed before anything can be filed.</summary>
    public const string SheetNumberRequired = "A sheet number is required.";

    /// <summary>An item code is needed before a row can be written.</summary>
    public const string ItemCodeRequired = "An item code is required.";

    /// <summary>A hand-in needs the time it was received.</summary>
    public const string HandInTimeRequired = "A hand-in needs a time.";

    /// <summary>The wording for a sheet the office has not opened.</summary>
    public static string UnknownSheet(string inspectionNo) =>
        "No sheet numbered " + inspectionNo + " is kept by the office.";

    /// <summary>The wording for a sheet number the office already keeps.</summary>
    public static string AlreadyOpen(string inspectionNo) =>
        "A sheet numbered " + inspectionNo + " is already kept by the office.";

    /// <summary>The wording for a submission with no rows at all.</summary>
    public const string NoRowsGiven = "The submission carries no rows to record.";
}

/// <summary>
/// Takes a sheet's rows and turns them into the plain text a workshop person
/// can pin on the wall. The office printed these pages by hand before the wall
/// board was used, and keeps the wording for the record.
/// </summary>
public static class InspectionReportBuilder
{
    /// <summary>Lays a page out as a wall text for one sheet.</summary>
    public static string BuildSheetPage(
        InspectionSheet sheet,
        InspectionDetail detail,
        int width)
    {
        if (sheet == null || detail == null)
        {
            return string.Empty;
        }

        var lines = new List<string>
        {
            "Inspection " + sheet.InspectionNo,
            "Updated " + detail.UpdatedAt.ToString("s"),
            ""
        };

        foreach (var row in detail.Rows ?? Array.Empty<InspectionItem>())
        {
            lines.Add(FormatRow(row, width));
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Reads one row as a fixed-width line of the wall page.</summary>
    public static string FormatRow(InspectionItem row, int width)
    {
        if (row == null)
        {
            return string.Empty;
        }

        var prefix = row.IsSetAside ? "x " : string.Empty;
        var code = row.Code ?? string.Empty;
        var reading = row.Reading ?? string.Empty;
        var line = prefix + code + "  " + reading;

        if (line.Length > width)
        {
            return line.Substring(0, Math.Max(0, width));
        }

        return line;
    }

    /// <summary>Counts the widest row a set of rows would print.</summary>
    public static int WidestRow(IEnumerable<InspectionItem> rows, int fallback)
    {
        var widest = fallback;
        if (rows == null)
        {
            return widest;
        }

        foreach (var row in rows)
        {
            var length = (row.Code ?? string.Empty).Length + (row.Reading ?? string.Empty).Length;
            if (length > widest)
            {
                widest = length;
            }
        }

        return widest;
    }

    /// <summary>The number of lines a wall page would take for the given rows.</summary>
    public static int PageLineCount(InspectionDetail detail)
    {
        if (detail == null || detail.Rows == null)
        {
            return 0;
        }

        return detail.Rows.Count;
    }

    /// <summary>The heading a wall page starts with for a sheet.</summary>
    public static string PageHeading(InspectionSheet sheet) =>
        "Inspection " + (sheet == null ? "unknown" : sheet.InspectionNo);

    /// <summary>True when a wall page would be left blank for the detail.</summary>
    public static bool WouldBeBlank(InspectionDetail detail) =>
        detail == null || detail.Rows == null || detail.Rows.Count == 0;
}

/// <summary>
/// Combines two sets of rows into one set for a wall page, taking the reading
/// from the later set where both sets name the same item.
/// </summary>
public static class MergeRows
{
    /// <summary>Combines the earlier and the later row sets, later wins.</summary>
    public static IReadOnlyList<InspectionItem> Combine(
        IReadOnlyList<InspectionItem> earlier,
        IReadOnlyList<InspectionItem> later)
    {
        var merged = new List<InspectionItem>();
        if (earlier != null)
        {
            foreach (var row in earlier)
            {
                if (row != null)
                {
                    merged.Add(row);
                }
            }
        }

        if (later == null)
        {
            return merged;
        }

        foreach (var row in later)
        {
            if (row == null)
            {
                continue;
            }

            var replaced = false;
            for (var index = 0; index < merged.Count; index++)
            {
                if (string.Equals(merged[index].Code, row.Code, StringComparison.Ordinal))
                {
                    merged[index] = row;
                    replaced = true;
                    break;
                }
            }

            if (!replaced)
            {
                merged.Add(row);
            }
        }

        return merged;
    }

    /// <summary>True when the later set names every item the earlier set names.</summary>
    public static bool Covers(
        IReadOnlyList<InspectionItem> earlier,
        IReadOnlyList<InspectionItem> later)
    {
        if (earlier == null || earlier.Count == 0)
        {
            return true;
        }

        if (later == null)
        {
            return false;
        }

        foreach (var row in earlier)
        {
            var named = false;
            foreach (var candidate in later)
            {
                if (string.Equals(candidate.Code, row.Code, StringComparison.Ordinal))
                {
                    named = true;
                    break;
                }
            }

            if (!named)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The codes named by a set of rows, in order.</summary>
    public static IReadOnlyList<string> NamedCodes(IReadOnlyList<InspectionItem> rows)
    {
        var codes = new List<string>();
        if (rows == null)
        {
            return codes;
        }

        foreach (var row in rows)
        {
            if (row != null && !string.IsNullOrEmpty(row.Code))
            {
                codes.Add(row.Code);
            }
        }

        return codes;
    }
}