namespace Shishan.Domain.K003;

/// <summary>
/// The set of readings handed in at one time for one sheet. The sheet keeps
/// this set on its header, and the page shown for the sheet is expected to
/// carry exactly the rows most recently handed in.
/// </summary>
public sealed record InspectionSubmission(
    string InspectionNo,
    DateTime SubmittedAt,
    IReadOnlyList<InspectionItem> Rows,
    string OperatorName)
{
    /// <summary>Builds a submission for the readings taken on the floor.</summary>
    public static InspectionSubmission For(
        string inspectionNo,
        DateTime submittedAt,
        IReadOnlyList<InspectionItem> rows,
        string operatorName) =>
        new(
            InspectionItem.NormalizeCode(inspectionNo),
            submittedAt,
            rows ?? Array.Empty<InspectionItem>(),
            operatorName ?? string.Empty);

    /// <summary>Builds a submission for one line kept up to date by an operator.</summary>
    public static InspectionSubmission ForMaintenance(
        string inspectionNo,
        DateTime submittedAt,
        string operatorName,
        InspectionItem line) =>
        new(
            InspectionItem.NormalizeCode(inspectionNo),
            submittedAt,
            line == null ? Array.Empty<InspectionItem>() : new[] { line },
            operatorName ?? string.Empty);

    /// <summary>True when at least one row was handed in.</summary>
    public bool HasRows => Rows != null && Rows.Count > 0;

    /// <summary>How many rows were handed in.</summary>
    public int RowCount => Rows?.Count ?? 0;

    /// <summary>True when the submission carries nothing to record.</summary>
    public bool IsEmpty => !HasRows;

    /// <summary>The row named by the given item code, if one was handed in.</summary>
    public InspectionItem? LineWith(string code)
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

    /// <summary>Every item code handed in, in the order they were written.</summary>
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

    /// <summary>How many of the handed-in rows are set aside.</summary>
    public int SetAsideCount()
    {
        var count = 0;
        if (Rows == null)
        {
            return count;
        }

        foreach (var row in Rows)
        {
            if (row.IsSetAside)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>True when the handed-in rows all show on the page.</summary>
    public bool NothingSetAside() => SetAsideCount() == 0;

    /// <summary>True when the submission was handed in before a given time.</summary>
    public bool WasBefore(DateTime moment) => SubmittedAt < moment;

    /// <summary>True when the submission was handed in after a given time.</summary>
    public bool WasAfter(DateTime moment) => SubmittedAt > moment;

    /// <summary>The sheet number and the person together, as the desk reads them.</summary>
    public string Reference =>
        InspectionNo + "@" + (string.IsNullOrEmpty(OperatorName) ? "unlisted" : OperatorName);

    /// <summary>The plain wording the office uses for this submission.</summary>
    public string Describe() =>
        "sheet " + InspectionNo + ", " + RowCount + " rows, by " +
        (string.IsNullOrEmpty(OperatorName) ? "unlisted" : OperatorName);

    /// <summary>A copy of the submission carrying the given rows instead.</summary>
    public InspectionSubmission WithRows(IReadOnlyList<InspectionItem> rows) =>
        new(InspectionNo, SubmittedAt, rows ?? Array.Empty<InspectionItem>(), OperatorName);

    /// <summary>A copy of the submission handed in at the given time.</summary>
    public InspectionSubmission WithTime(DateTime submittedAt) =>
        new(InspectionNo, submittedAt, Rows ?? Array.Empty<InspectionItem>(), OperatorName);

    /// <summary>True when the submission was handed in for the given sheet.</summary>
    public bool ForSheet(string inspectionNo) =>
        string.Equals(
            InspectionNo,
            InspectionItem.NormalizeCode(inspectionNo),
            StringComparison.Ordinal);
}
