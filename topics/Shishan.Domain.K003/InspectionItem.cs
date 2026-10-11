namespace Shishan.Domain.K003;

/// <summary>
/// One measured line on an inspection sheet. A line names the inspected item
/// and keeps the reading that was taken for it on the floor. A line may be
/// set aside, which takes it off the page shown for the sheet.
/// </summary>
public sealed record InspectionItem(string Code, string Value, bool Voided)
{
    /// <summary>Opens a line for a reading taken on the floor.</summary>
    public static InspectionItem Measure(string code, string value) =>
        new(NormalizeCode(code), value ?? string.Empty, false);

    /// <summary>Opens a line that is set aside from the moment it is written.</summary>
    public static InspectionItem SetAside(string code, string value) =>
        new(NormalizeCode(code), value ?? string.Empty, true);

    /// <summary>True when the line was left without an item name.</summary>
    public bool HasNoCode => string.IsNullOrEmpty(Code);

    /// <summary>True when the line still shows on the page of the sheet.</summary>
    public bool Shown => !Voided;

    /// <summary>The reading taken for the item, ready to be read aloud.</summary>
    public string Reading => string.IsNullOrEmpty(Value) ? "-" : Value;

    /// <summary>A copy of the line holding a fresh reading.</summary>
    public InspectionItem WithValue(string value) =>
        new(Code, value ?? string.Empty, Voided);

    /// <summary>A copy of the line with the set-aside mark put on or taken off.</summary>
    public InspectionItem WithVoid(bool voided) =>
        new(Code, Value, voided);

    /// <summary>True when two lines name the same inspected item.</summary>
    public bool SameItem(InspectionItem other) =>
        other != null && string.Equals(Code, other.Code, StringComparison.Ordinal);

    /// <summary>True when the line is set aside.</summary>
    public bool IsSetAside => Voided;

    /// <summary>The item code without the spaces a keyboard may leave around it.</summary>
    public string CleanCode => NormalizeCode(Code);

    /// <summary>The wording a workshop person sees for the line.</summary>
    public string PageText =>
        (Shown ? "" : "x ") + CleanCode + " " + Reading;

    /// <summary>Normalises the item code an operator typed on the floor.</summary>
    public static string NormalizeCode(string code) =>
        (code ?? string.Empty).Trim();
}
