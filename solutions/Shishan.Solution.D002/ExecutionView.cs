namespace Shishan.Domain.D002;

/// <summary>
/// The floor-friendly read of one operation line for one order. It carries the
/// wording the shop uses rather than the raw values held behind it.
/// </summary>
public sealed record ExecutionView(
    string OrderId,
    string OperationCode,
    string OperationName,
    string OperatorName,
    string Standing,
    bool Finished);

/// <summary>
/// The commissioned operations the shop recognises, held with the wording the
/// operators use for each of them and the place each takes in the line.
/// </summary>
public sealed record OperationDefinition(string Code, string Name, int Sequence);