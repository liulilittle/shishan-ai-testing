namespace Shishan.Domain.K004;

/// <summary>
/// The standing of a document as a whole.
/// </summary>
public enum DocumentStatus
{
    /// <summary>The document is still being walked.</summary>
    InProgress = 0,

    /// <summary>Every segment has been approved.</summary>
    Approved = 1,

    /// <summary>The document has been retired from review.</summary>
    Voided = 2,
}

/// <summary>Readable helpers for the standing of a document.</summary>
public static class DocumentStatusExtensions
{
    /// <summary>True when the document is still being walked.</summary>
    public static bool IsOpen(this DocumentStatus status) => status == DocumentStatus.InProgress;

    /// <summary>True when every segment has been approved.</summary>
    public static bool IsComplete(this DocumentStatus status) => status == DocumentStatus.Approved;

    /// <summary>True when the document has been retired from review.</summary>
    public static bool IsRetired(this DocumentStatus status) => status == DocumentStatus.Voided;

    /// <summary>The readable name of the standing.</summary>
    public static string Readable(this DocumentStatus status)
    {
        switch (status)
        {
            case DocumentStatus.Approved:
                return "Approved";
            case DocumentStatus.Voided:
                return "Retired";
            default:
                return "In progress";
        }
    }
}