namespace Shishan.Domain.D001;

/// <summary>
/// Turns approval files into the plain readings the desk shows, so the list
/// and the detail screens do not have to know the raw values.
/// </summary>
public static class ProgressMapper
{
    /// <summary>Reads one file's current step.</summary>
    public static ProgressView ToView(ApprovalDocument doc)
    {
        if (doc == null)
        {
            throw new ArgumentNullException(nameof(doc));
        }

        return ToView(doc, doc.LatestNode ?? ProgressNode.None);
    }

    /// <summary>Reads one file at a given step.</summary>
    public static ProgressView ToView(ApprovalDocument doc, ProgressNode? node)
    {
        if (doc == null)
        {
            throw new ArgumentNullException(nameof(doc));
        }

        var step = node ?? ProgressNode.None;
        return new ProgressView(doc.ApplyNo, doc.BusinessNo, step.Code, step.Name, step.Time);
    }

    /// <summary>
    /// Reads the current step of the file raised under an apply number, from
    /// the files that serve a business number.
    /// </summary>
    public static IReadOnlyList<ProgressView> ToViews(IEnumerable<ApprovalDocument> documents, string applyNo)
    {
        var views = new List<ProgressView>();
        if (documents == null)
        {
            return views;
        }

        foreach (var doc in documents)
        {
            if (!string.Equals(doc.ApplyNo, applyNo, StringComparison.Ordinal))
            {
                continue;
            }

            views.Add(ToView(doc));
        }

        return views;
    }

    /// <summary>Reads the current step of every file the desk is showing.</summary>
    public static IReadOnlyList<ProgressView> ToViews(IEnumerable<ApprovalDocument> documents)
    {
        var views = new List<ProgressView>();
        if (documents == null)
        {
            return views;
        }

        foreach (var doc in documents)
        {
            views.Add(ToView(doc));
        }

        return views;
    }

    /// <summary>Reads only the files that have taken at least one step.</summary>
    public static IReadOnlyList<ProgressView> ToStartedViews(IEnumerable<ApprovalDocument> documents)
    {
        var views = new List<ProgressView>();
        if (documents == null)
        {
            return views;
        }

        foreach (var doc in documents)
        {
            if (doc.HasStarted)
            {
                views.Add(ToView(doc));
            }
        }

        return views;
    }

    /// <summary>Reads only the files that have not taken a step yet.</summary>
    public static IReadOnlyList<ProgressView> ToUnstartedViews(IEnumerable<ApprovalDocument> documents)
    {
        var views = new List<ProgressView>();
        if (documents == null)
        {
            return views;
        }

        foreach (var doc in documents)
        {
            if (!doc.HasStarted)
            {
                views.Add(ToView(doc));
            }
        }

        return views;
    }

    /// <summary>The first of a run of views, or a blank view when the run is empty.</summary>
    public static ProgressView FirstOrBlank(IReadOnlyList<ProgressView> views)
    {
        if (views != null && views.Count > 0)
        {
            return views[0];
        }

        return ProgressView.Blank;
    }

    /// <summary>Keeps the views that carry a step.</summary>
    public static IReadOnlyList<ProgressView> KeepStepped(IEnumerable<ProgressView> views)
    {
        var result = new List<ProgressView>();
        if (views == null)
        {
            return result;
        }

        foreach (var view in views)
        {
            if (view.HasStep)
            {
                result.Add(view);
            }
        }

        return result;
    }

    /// <summary>Reads the current step of one file for the detail screen.</summary>
    public static ProgressView ToDetail(ApprovalDocument doc) => ToView(doc);
}