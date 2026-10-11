namespace Shishan.Domain.K002;

/// <summary>
/// Looks over the reason noted with a sheet before it leaves the counter.
/// The counter wants a reason to be present on a repeat sheet, and the notes
/// should be readable; the checks here do not turn a sheet away, they only
/// describe the note that came with it.
/// </summary>
public static class ValidateReason
{
    /// <summary>True when a reason note is present and has letters in it.</summary>
    public static bool IsPresent(string? reason) =>
        !string.IsNullOrWhiteSpace(reason);

    /// <summary>True when a reason note is missing or blank.</summary>
    public static bool IsMissing(string? reason) =>
        string.IsNullOrWhiteSpace(reason);

    /// <summary>
    /// The note with its leading and trailing spaces taken off, or the empty
    /// wording when nothing was noted.
    /// </summary>
    public static string Trimmed(string? reason) =>
        (reason ?? string.Empty).Trim();

    /// <summary>How many characters the note has after trimming.</summary>
    public static int Length(string? reason) =>
        Trimmed(reason).Length;

    /// <summary>
    /// True when the note is short enough to write on the sheet's margin.
    /// </summary>
    public static bool FitsMargin(string? reason) =>
        Length(reason) <= 80;

    /// <summary>
    /// True when the note begins with a letter, which the counter prefers
    /// for notes it can file under the customer's name.
    /// </summary>
    public static bool BeginsWithLetter(string? reason)
    {
        var note = Trimmed(reason);
        return note.Length > 0 && char.IsLetter(note[0]);
    }

    /// <summary>
    /// The first word of the note, used when the counter files the sheet
    /// under a heading.
    /// </summary>
    public static string FirstWord(string? reason)
    {
        var note = Trimmed(reason);
        if (note.Length == 0)
        {
            return string.Empty;
        }

        var end = 0;
        while (end < note.Length && !char.IsWhiteSpace(note[end]))
        {
            end++;
        }

        return note.Substring(0, end);
    }

    /// <summary>
    /// True when the note holds no letters at all, which the counter flags
    /// as a note that cannot be filed.
    /// </summary>
    public static bool HoldsNoLetters(string? reason)
    {
        var note = Trimmed(reason);
        foreach (var character in note)
        {
            if (char.IsLetter(character))
            {
                return false;
            }
        }

        return note.Length > 0;
    }
}