namespace Shishan.Domain.D001;

/// <summary>
/// Holds the approval files the office has entered and the lookups the desk
/// uses to find them again. Many files can sit under one business number,
/// while every file keeps an apply number of its own.
/// </summary>
public sealed class ProgressIndex
{
    private readonly List<ApprovalDocument> _documents = new();
    private readonly Dictionary<string, List<ApprovalDocument>> _byBusinessNo = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ApprovalDocument> _byApplyNo = new(StringComparer.Ordinal);

    /// <summary>How many files the office holds.</summary>
    public int Count => _documents.Count;

    /// <summary>
    /// Adds a file to the office's holdings. The desk reads the file back by
    /// its business number and by its apply number.
    /// </summary>
    public void Add(ApprovalDocument doc)
    {
        if (doc == null)
        {
            throw new ArgumentNullException(nameof(doc));
        }

        _documents.Add(doc);

        if (!_byApplyNo.ContainsKey(doc.ApplyNo))
        {
            _byApplyNo.Add(doc.ApplyNo, doc);
        }

        if (!_byBusinessNo.TryGetValue(doc.BusinessNo, out var files))
        {
            files = new List<ApprovalDocument>();
            _byBusinessNo.Add(doc.BusinessNo, files);
        }

        files.Add(doc);
    }

    /// <summary>
    /// The file that serves a business number and was raised under an apply
    /// number, when the office holds it.
    /// </summary>
    public ApprovalDocument? ByBoth(string businessNo, string applyNo)
    {
        var wantedBusiness = ApprovalCatalog.NormalizeBusinessNo(businessNo);
        var wantedApply = ApprovalCatalog.NormalizeApplyNo(applyNo);

        if (!_byBusinessNo.TryGetValue(wantedBusiness, out var files))
        {
            return null;
        }

        foreach (var doc in files)
        {
            if (string.Equals(doc.ApplyNo, wantedApply, StringComparison.Ordinal))
            {
                return doc;
            }
        }

        return null;
    }

    /// <summary>The file raised under an apply number.</summary>
    public ApprovalDocument? ByApplyNo(string applyNo)
    {
        var wanted = ApprovalCatalog.NormalizeApplyNo(applyNo);
        return _byApplyNo.TryGetValue(wanted, out var doc) ? doc : null;
    }

    /// <summary>
    /// Every file that serves a business number, in the order the office
    /// entered them.
    /// </summary>
    public IReadOnlyList<ApprovalDocument> DocumentsUnder(string businessNo)
    {
        var wanted = ApprovalCatalog.NormalizeBusinessNo(businessNo);
        var matches = new List<ApprovalDocument>();
        foreach (var doc in _documents)
        {
            if (string.Equals(doc.BusinessNo, wanted, StringComparison.Ordinal))
            {
                matches.Add(doc);
            }
        }

        return matches;
    }

    /// <summary>True when the office holds any file for a business number.</summary>
    public bool Serves(string businessNo)
    {
        var wanted = ApprovalCatalog.NormalizeBusinessNo(businessNo);
        foreach (var doc in _documents)
        {
            if (string.Equals(doc.BusinessNo, wanted, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True when the office holds a file raised under an apply number.</summary>
    public bool Holds(string applyNo)
    {
        var wanted = ApprovalCatalog.NormalizeApplyNo(applyNo);
        return _byApplyNo.ContainsKey(wanted);
    }

    /// <summary>How many files serve a business number.</summary>
    public int CountUnder(string businessNo)
    {
        var wanted = ApprovalCatalog.NormalizeBusinessNo(businessNo);
        var count = 0;
        foreach (var doc in _documents)
        {
            if (string.Equals(doc.BusinessNo, wanted, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>How many apply numbers sit under a business number.</summary>
    public int ApplyCountUnder(string businessNo)
    {
        var wanted = ApprovalCatalog.NormalizeBusinessNo(businessNo);
        var applyNumbers = new List<string>();
        foreach (var doc in _documents)
        {
            if (string.Equals(doc.BusinessNo, wanted, StringComparison.Ordinal)
                && !applyNumbers.Contains(doc.ApplyNo))
            {
                applyNumbers.Add(doc.ApplyNo);
            }
        }

        return applyNumbers.Count;
    }

    /// <summary>Every file the office holds, in the order they were entered.</summary>
    public IReadOnlyList<ApprovalDocument> All()
    {
        var result = new List<ApprovalDocument>(_documents);
        return result;
    }

    /// <summary>Every business number the office serves, in reading order.</summary>
    public IReadOnlyList<string> BusinessNumbers()
    {
        var numbers = new List<string>();
        foreach (var doc in _documents)
        {
            if (!numbers.Contains(doc.BusinessNo))
            {
                numbers.Add(doc.BusinessNo);
            }
        }

        numbers.Sort(StringComparer.Ordinal);
        return numbers;
    }

    /// <summary>Every apply number the office holds, in reading order.</summary>
    public IReadOnlyList<string> ApplyNumbers()
    {
        var numbers = new List<string>();
        foreach (var doc in _documents)
        {
            if (!numbers.Contains(doc.ApplyNo))
            {
                numbers.Add(doc.ApplyNo);
            }
        }

        numbers.Sort(StringComparer.Ordinal);
        return numbers;
    }

    /// <summary>The apply numbers raised under a business number.</summary>
    public IReadOnlyList<string> ApplyNumbersUnder(string businessNo)
    {
        var wanted = ApprovalCatalog.NormalizeBusinessNo(businessNo);
        var numbers = new List<string>();
        foreach (var doc in _documents)
        {
            if (string.Equals(doc.BusinessNo, wanted, StringComparison.Ordinal))
            {
                numbers.Add(doc.ApplyNo);
            }
        }

        return numbers;
    }

    /// <summary>The first file the office entered for a business number.</summary>
    public ApprovalDocument? FirstUnder(string businessNo)
    {
        var wanted = ApprovalCatalog.NormalizeBusinessNo(businessNo);
        foreach (var doc in _documents)
        {
            if (string.Equals(doc.BusinessNo, wanted, StringComparison.Ordinal))
            {
                return doc;
            }
        }

        return null;
    }

    /// <summary>The most recent file the office entered for a business number.</summary>
    public ApprovalDocument? LastUnder(string businessNo)
    {
        ApprovalDocument? found = null;
        var wanted = ApprovalCatalog.NormalizeBusinessNo(businessNo);
        foreach (var doc in _documents)
        {
            if (string.Equals(doc.BusinessNo, wanted, StringComparison.Ordinal))
            {
                found = doc;
            }
        }

        return found;
    }

    /// <summary>
    /// Removes a file from the office's holdings, leaving the other files
    /// untouched. Returns true when a file was taken off.
    /// </summary>
    public bool Withdraw(string applyNo)
    {
        var doc = ByApplyNo(applyNo);
        if (doc == null)
        {
            return false;
        }

        _documents.Remove(doc);
        _byApplyNo.Remove(ApprovalCatalog.NormalizeApplyNo(applyNo));
        if (_byBusinessNo.TryGetValue(doc.BusinessNo, out var files))
        {
            files.Remove(doc);
            if (files.Count == 0)
            {
                _byBusinessNo.Remove(doc.BusinessNo);
            }
        }

        return true;
    }

    /// <summary>Removes every file the office holds.</summary>
    public void Clear()
    {
        _documents.Clear();
        _byBusinessNo.Clear();
        _byApplyNo.Clear();
    }
}