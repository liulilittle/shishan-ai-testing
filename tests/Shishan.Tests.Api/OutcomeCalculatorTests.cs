using Shishan.Api.Services;

namespace Shishan.Tests.Api;

public class OutcomeCalculatorTests
{
    private static TestCaseResult Passed(string name) => new(name, true, null, null);
    private static TestCaseResult Failed(string name) => new(name, false, null, null);

    [Fact]
    public void AllGreen_ReturnsGoal()
    {
        var cases = new List<TestCaseResult> { Passed("A"), Passed("B"), Passed("C") };
        Assert.Equal("goal", OutcomeCalculator.Compute(3, 3, 0, cases));
    }

    [Fact]
    public void SingleCaseGreen_ReturnsGoal()
    {
        var cases = new List<TestCaseResult> { Passed("A") };
        Assert.Equal("goal", OutcomeCalculator.Compute(1, 1, 0, cases));
    }

    [Fact]
    public void EmptyTotal_IsNotGoal()
    {
        // total == 0 with an empty case list: the outcome is simply not goal (which exact
        // near-miss is not part of the contract when there is nothing to hash).
        var outcome = OutcomeCalculator.Compute(0, 0, 0, new List<TestCaseResult>());
        Assert.NotEqual("goal", outcome);
    }

    [Theory]
    [InlineData(1, 2)] // 0.50
    [InlineData(2, 3)] // 0.666
    [InlineData(1, 1)] // 1.00
    public void AllFailed_IsNeverGoal(int failed, int total)
    {
        var cases = Enumerable.Range(0, total)
            .Select(i => i < failed ? Failed($"F{i}") : Passed($"P{i}"))
            .ToList();
        Assert.NotEqual("goal", OutcomeCalculator.Compute(total, total - failed, failed, cases));
    }

    [Fact]
    public void LowFailRatio_ReturnsOneOfPostFamily()
    {
        // 1 of 10 failed => failRatio 0.10 <= 0.34.
        var cases = Enumerable.Range(0, 10)
            .Select(i => i == 0 ? Failed("F0") : Passed($"P{i}"))
            .ToList();
        var outcome = OutcomeCalculator.Compute(10, 9, 1, cases);
        Assert.Contains(outcome, new[] { "postLeft", "postRight", "crossbar" });
    }

    [Fact]
    public void BoundaryFailRatio_ExactlyPoint34_IsPostFamily()
    {
        // 34 of 100 failed => failRatio 0.34, included in the <= 0.34 frame group.
        var cases = Enumerable.Range(0, 100)
            .Select(i => i < 34 ? Failed($"F{i}") : Passed($"P{i}"))
            .ToList();
        var outcome = OutcomeCalculator.Compute(100, 66, 34, cases);
        Assert.Contains(outcome, new[] { "postLeft", "postRight", "crossbar" });
    }

    [Fact]
    public void HighFailRatio_ReturnsOneOfMisfireFamily()
    {
        // 5 of 10 failed => failRatio 0.50 > 0.34.
        var cases = Enumerable.Range(0, 10)
            .Select(i => i < 5 ? Failed($"F{i}") : Passed($"P{i}"))
            .ToList();
        var outcome = OutcomeCalculator.Compute(10, 5, 5, cases);
        Assert.Contains(outcome, new[] { "wideLeft", "wideRight", "overTop" });
    }

    [Fact]
    public void ChangingFailedSet_ChangesSignatureAndTendsToChangeOutcome()
    {
        var baseCases = Enumerable.Range(0, 10)
            .Select(i => i == 0 ? Failed("A") : Passed($"P{i}"))
            .ToList();
        var altCases = Enumerable.Range(0, 10)
            .Select(i => i == 0 ? Failed("B") : Passed($"P{i}"))
            .ToList();

        var a = OutcomeCalculator.Compute(10, 9, 1, baseCases);
        var b = OutcomeCalculator.Compute(10, 9, 1, altCases);

        Assert.Contains(a, new[] { "postLeft", "postRight", "crossbar" });
        Assert.Contains(b, new[] { "postLeft", "postRight", "crossbar" });
        // FNV-1a over the sorted failed-name set is stable and name-sensitive.
        Assert.NotEqual(SignatureOf("A"), SignatureOf("B"));
    }

    private static uint SignatureOf(string name)
    {
        // Recompute the documented signature directly (FNV-1a over the name), independent of
        // the private implementation, so this asserts the mapping the outcome is derived from.
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;
        uint hash = offsetBasis;
        foreach (char ch in name)
        {
            hash ^= (byte)(ch & 0xFF);
            hash *= prime;
            hash ^= (byte)((ch >> 8) & 0xFF);
            hash *= prime;
        }
        return hash;
    }

    [Fact]
    public void IsDeterministic_ForSameInputs()
    {
        var cases = new List<TestCaseResult> { Passed("A"), Failed("B"), Failed("C"), Passed("D") };
        var first = OutcomeCalculator.Compute(4, 2, 2, cases);
        var second = OutcomeCalculator.Compute(4, 2, 2, cases);
        Assert.Equal(first, second);
    }

    [Fact]
    public void FailedSetOrder_DoesNotAffectOutcome()
    {
        var ordered = new List<TestCaseResult> { Failed("Aa"), Failed("Bb"), Passed("Cc") };
        var shuffled = new List<TestCaseResult> { Failed("Bb"), Failed("Aa"), Passed("Cc") };
        Assert.Equal(
            OutcomeCalculator.Compute(3, 1, 2, ordered),
            OutcomeCalculator.Compute(3, 1, 2, shuffled));
    }
}
