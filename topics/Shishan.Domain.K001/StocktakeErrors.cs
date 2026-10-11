namespace Shishan.Domain.K001;

/// <summary>
/// Raised when a request does not fit the rules the desk keeps for stocktake
/// rounds.
/// </summary>
public sealed class StocktakeRuleException : Exception
{
    public StocktakeRuleException(string message) : base(message)
    {
    }

    public StocktakeRuleException(string message, Exception inner) : base(message, inner)
    {
    }
}

/// <summary>
/// The wording the desk uses when it turns a request away.
/// </summary>
public static class StocktakeMessages
{
    /// <summary>Wording for a request without a round number.</summary>
    public const string RoundNumberRequired = "A round number is required.";

    /// <summary>Wording for a request without a product code.</summary>
    public const string SkuRequired = "A product code is required.";

    /// <summary>Wording for a request with a figure below zero.</summary>
    public const string QuantityBelowZero = "A figure cannot be below zero.";

    /// <summary>Wording for a request with an unknown counting unit.</summary>
    public const string UnitNotRecognised = "The counting unit is not recognised.";

    /// <summary>Wording for a request aimed at a row the round does not hold.</summary>
    public const string LineNotOnRound = "The round does not hold this product.";

    /// <summary>Wording for an opening of a row the round already holds.</summary>
    public const string RowAlreadyOnRound = "The round already holds this product.";

    /// <summary>Wording for a round that has not been opened.</summary>
    public const string RoundNotOpened = "The round has not been opened.";

    /// <summary>Wording for a sign-off of a row that has not been counted.</summary>
    public const string RowNotCounted = "The row has not been counted.";

    /// <summary>Wording for a request aimed at a row on a round that is closed.</summary>
    public const string RoundClosed = "The round is closed.";

    /// <summary>Wording for a request that carries an over-large figure.</summary>
    public const string FigureTooLarge = "The figure is too large to carry.";

    /// <summary>Wording for a request that carries no figure.</summary>
    public const string FigureMissing = "A figure is required.";
}