namespace Shishan.Domain.K004;

/// <summary>
/// Keeps every document the desk knows about, keyed by identifier.
/// </summary>
public sealed class WorkflowStore
{
    private readonly Dictionary<string, ApprovalWorkflow> _documents = new(StringComparer.Ordinal);

    /// <summary>How many documents the desk is holding.</summary>
    public int Count
    {
        get
        {
            lock (_documents)
            {
                return _documents.Count;
            }
        }
    }

    /// <summary>True when the desk already holds a document under the identifier.</summary>
    public bool Holds(string id)
    {
        lock (_documents)
        {
            return _documents.ContainsKey(id);
        }
    }

    /// <summary>Opens a new document under the given identifier.</summary>
    public ApprovalWorkflow Add(string id)
    {
        lock (_documents)
        {
            if (_documents.ContainsKey(id))
            {
                throw new WorkflowException(WorkflowErrors.DocumentExists(id));
            }

            var document = new ApprovalWorkflow(id);
            _documents[id] = document;
            return document;
        }
    }

    /// <summary>The document held under the identifier, or an error when absent.</summary>
    public ApprovalWorkflow Require(string id)
    {
        lock (_documents)
        {
            if (!_documents.TryGetValue(id, out var document))
            {
                throw new WorkflowException(WorkflowErrors.DocumentMissing(id));
            }

            return document;
        }
    }

    /// <summary>Every document the desk is holding, in identifier order.</summary>
    public IReadOnlyList<ApprovalWorkflow> All()
    {
        lock (_documents)
        {
            var ordered = new List<ApprovalWorkflow>(_documents.Values);
            ordered.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            return ordered;
        }
    }

    /// <summary>The identifiers of every document the desk is holding.</summary>
    public IReadOnlyList<string> Ids()
    {
        lock (_documents)
        {
            var ids = new List<string>(_documents.Keys);
            ids.Sort(StringComparer.Ordinal);
            return ids;
        }
    }

    /// <summary>Reads a document the desk holds without raising an error when absent.</summary>
    public bool TryGet(string id, out ApprovalWorkflow? document)
    {
        lock (_documents)
        {
            return _documents.TryGetValue(id, out document);
        }
    }

    /// <summary>Every document the desk holds under one of the given identifiers.</summary>
    public IReadOnlyList<ApprovalWorkflow> WithIds(IEnumerable<string> ids)
    {
        if (ids is null)
        {
            throw new ArgumentNullException(nameof(ids));
        }

        var wanted = new HashSet<string>(ids, StringComparer.Ordinal);
        var found = new List<ApprovalWorkflow>();
        lock (_documents)
        {
            foreach (var pair in _documents)
            {
                if (wanted.Contains(pair.Key))
                {
                    found.Add(pair.Value);
                }
            }
        }

        found.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
        return found;
    }

    /// <summary>True when the desk holds no documents at all.</summary>
    public bool IsEmpty()
    {
        lock (_documents)
        {
            return _documents.Count == 0;
        }
    }
}