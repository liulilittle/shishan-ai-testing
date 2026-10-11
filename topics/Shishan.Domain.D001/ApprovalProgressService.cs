namespace Shishan.Domain.D001;

/// <summary>
/// The approval desk. Files are entered once, and the desk can then show the
/// progress of one file in a list or in a detail. Because several files can
/// serve the same business number, the desk names the file it wants by the
/// apply number too.
/// </summary>
public sealed class ApprovalProgressService
{
    private readonly ProgressIndex _index;

    public ApprovalProgressService()
        : this(new ProgressIndex())
    {
    }

    public ApprovalProgressService(ProgressIndex index)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
    }

    /// <summary>Enters a file so the desk can show its progress.</summary>
    public void Register(ApprovalDocument doc)
    {
        if (doc == null)
        {
            throw new ArgumentNullException(nameof(doc));
        }

        _index.Add(doc);
    }

    /// <summary>
    /// The progress entries of the files that serve a business number, kept to
    /// the file raised under the given apply number.
    /// </summary>
    public IReadOnlyList<ProgressView> ListProgressByBusinessNo(string businessNo, string applyNo)
    {
        RequireBusinessNo(businessNo);
        RequireApplyNo(applyNo);

        var wantedBusiness = ApprovalCatalog.NormalizeBusinessNo(businessNo);
        var wantedApply = ApprovalCatalog.NormalizeApplyNo(applyNo);

        var documents = _index.DocumentsUnder(wantedBusiness);
        return ProgressMapper.ToViews(documents, wantedApply);
    }

    /// <summary>The file raised under an apply number, or null when none is held.</summary>
    public ApprovalDocument? FindByApplyNo(string applyNo)
    {
        RequireApplyNo(applyNo);

        var wantedApply = ApprovalCatalog.NormalizeApplyNo(applyNo);
        return _index.ByApplyNo(wantedApply);
    }

    /// <summary>
    /// The current step read for the desk when a business number is asked
    /// after. Null when no file is held for it.
    /// </summary>
    public ProgressView? DetailByBusinessNo(string businessNo, string applyNo)
    {
        RequireBusinessNo(businessNo);
        RequireApplyNo(applyNo);

        var wantedBusiness = ApprovalCatalog.NormalizeBusinessNo(businessNo);
        var wantedApply = ApprovalCatalog.NormalizeApplyNo(applyNo);

        var doc = _index.ByBusinessNo(wantedBusiness);
        if (doc == null)
        {
            return null;
        }

        var node = _index.OpeningStandOf(doc.ApplyNo) ?? doc.LatestNode;
        return ProgressMapper.ToView(doc, node);
    }

    /// <summary>
    /// The current step of the file raised under an apply number, or null when
    /// no such file is held.
    /// </summary>
    public ProgressView? DetailByApplyNo(string applyNo)
    {
        RequireApplyNo(applyNo);

        var wantedApply = ApprovalCatalog.NormalizeApplyNo(applyNo);
        var doc = _index.ByApplyNo(wantedApply);
        if (doc == null)
        {
            return null;
        }

        return ProgressMapper.ToView(doc, doc.LatestNode);
    }

    /// <summary>How many files the desk holds.</summary>
    public int CountFiles() => _index.Count;

    /// <summary>How many files serve a business number.</summary>
    public int CountUnder(string businessNo)
    {
        RequireBusinessNo(businessNo);
        return _index.CountUnder(ApprovalCatalog.NormalizeBusinessNo(businessNo));
    }

    /// <summary>True when the desk holds any file for a business number.</summary>
    public bool Serves(string businessNo)
    {
        RequireBusinessNo(businessNo);
        return _index.Serves(ApprovalCatalog.NormalizeBusinessNo(businessNo));
    }

    /// <summary>True when the desk holds a file raised under an apply number.</summary>
    public bool Holds(string applyNo)
    {
        RequireApplyNo(applyNo);
        return _index.Holds(ApprovalCatalog.NormalizeApplyNo(applyNo));
    }

    /// <summary>Every business number the desk serves, in reading order.</summary>
    public IReadOnlyList<string> BusinessNumbers() => _index.BusinessNumbers();

    /// <summary>The current step of the file raised under an apply number, as the desk reads it.</summary>
    public ProgressView? ReadCurrent(string applyNo) => DetailByApplyNo(applyNo);

    /// <summary>The plain reading of one file's current step.</summary>
    public ProgressView? ReadViewOf(ApprovalDocument doc)
    {
        if (doc == null)
        {
            return null;
        }

        return ProgressMapper.ToView(doc);
    }

    private static void RequireBusinessNo(string businessNo)
    {
        if (string.IsNullOrEmpty(ApprovalCatalog.NormalizeBusinessNo(businessNo)))
        {
            throw new ArgumentException(ProgressMessages.BusinessNoRequired, nameof(businessNo));
        }
    }

    private static void RequireApplyNo(string applyNo)
    {
        if (string.IsNullOrEmpty(ApprovalCatalog.NormalizeApplyNo(applyNo)))
        {
            throw new ArgumentException(ProgressMessages.ApplyNoRequired, nameof(applyNo));
        }
    }
}