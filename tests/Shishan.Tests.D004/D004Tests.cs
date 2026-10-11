namespace Shishan.Tests.D004;

public class D004Tests
{
    private const string SharedName = "Cash Account";
    private const string NumberA = "ENT-0001";
    private const string NumberB = "ENT-0002";

    // Two entries may share a name; each keeps its own record.
    private static (LedgerEntry, LedgerEntry) OpenSameNamedEntries(LedgerService svc)
    {
        var a = svc.OpenEntry(NumberA, SharedName);
        var b = svc.OpenEntry(NumberB, SharedName);
        return (a, b);
    }

    // Post the same voucher many times at once, over a few waves.
    private static async Task PostConcurrentlyAsync(LedgerService svc, LedgerEntry entry, string voucherId, decimal amount, int threads = 32, int waves = 3)
    {
        for (var w = 0; w < waves; w++)
        {
            using var gate = new Barrier(threads);
            var tasks = new Task[threads];
            for (var i = 0; i < threads; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    gate.SignalAndWait();
                    svc.Post(entry, voucherId, amount);
                });
            }

            await Task.WhenAll(tasks);
        }
    }

    // Two entries that share a name stay apart: each keeps its own flows and value.
    [Fact]
    public void OpenEntry_TwoEntriesSameName_KeptApart()
    {
        var svc = new LedgerService();
        var (a, b) = OpenSameNamedEntries(svc);

        Assert.NotSame(a, b);
        Assert.Equal(NumberA, a.Number);
        Assert.Equal(NumberB, b.Number);
        Assert.Equal(SharedName, a.Name);
        Assert.Equal(SharedName, b.Name);

        svc.Post(a, "V-A1", 150m);

        var flowA = Assert.Single(svc.GetFlows(a));
        Assert.Equal("V-A1", flowA.VoucherId);
        Assert.Equal(150m, flowA.Amount);
        Assert.Empty(svc.GetFlows(b));
        Assert.Equal(150m, svc.GetBalance(a));
        Assert.Equal(0m, svc.GetBalance(b));
    }

    // Same name, different numbers: flows stay with their own entry.
    [Fact]
    public void Post_TwoEntriesShareName_FlowsStayWithinOwnNumber()
    {
        var svc = new LedgerService();
        var (a, b) = OpenSameNamedEntries(svc);

        svc.Post(a, "V-A01", 100m);
        svc.Post(b, "V-B01", 250m);

        var flowA = Assert.Single(svc.GetFlows(a));
        var flowB = Assert.Single(svc.GetFlows(b));

        Assert.Equal("V-A01", flowA.VoucherId);
        Assert.Equal(100m, flowA.Amount);
        Assert.Equal("V-B01", flowB.VoucherId);
        Assert.Equal(250m, flowB.Amount);
        Assert.Equal(100m, svc.GetBalance(a));
        Assert.Equal(250m, svc.GetBalance(b));
    }

    // Dimension: reversal semantics - reversing one entry must never touch a same-named neighbour.
    [Fact]
    public void Reverse_SharedVoucher_OnlyReversesRequestedEntryFlow()
    {
        var svc = new LedgerService();
        var (a, b) = OpenSameNamedEntries(svc);

        svc.Post(a, "V-SHARED", 100m);
        svc.Post(b, "V-SHARED", 200m);

        svc.Reverse(b, "V-SHARED");

        Assert.Empty(svc.GetFlows(b));
        Assert.Equal(0m, svc.GetBalance(b));
        var remainingA = Assert.Single(svc.GetFlows(a));
        Assert.Equal("V-SHARED", remainingA.VoucherId);
        Assert.Equal(100m, remainingA.Amount);
        Assert.Equal(100m, svc.GetBalance(a));
    }

    // Dimension: account-actual consistency - balance always equals the net of valid flows.
    [Fact]
    public void Balance_AfterPostAndReverse_EqualsNetOfValidFlows()
    {
        var svc = new LedgerService();
        var entry = svc.OpenEntry(NumberA, SharedName);

        svc.Post(entry, "V-1", 100m);
        svc.Post(entry, "V-2", 50m);
        svc.Post(entry, "V-3", -30m);

        svc.Reverse(entry, "V-2");
        svc.Reverse(entry, "V-1");

        var flows = svc.GetFlows(entry);
        var net = flows.Sum(f => f.Amount);
        var flow = Assert.Single(flows);

        Assert.Equal("V-3", flow.VoucherId);
        Assert.Equal(-30m, flow.Amount);
        Assert.Equal(net, svc.GetBalance(entry));
    }

    // Dimension: account-actual consistency - reversing before posting again keeps
    // the balance equal to the net of the effective flows.
    [Fact]
    public void Balance_AfterReverseThenPost_EqualsNetOfValidFlows()
    {
        var svc = new LedgerService();
        var entry = svc.OpenEntry(NumberA, SharedName);

        svc.Post(entry, "V-1", 100m);
        svc.Reverse(entry, "V-1");
        svc.Post(entry, "V-2", 50m);
        svc.Post(entry, "V-3", -30m);

        var flows = svc.GetFlows(entry);
        var net = flows.Sum(f => f.Amount);

        Assert.Equal(2, flows.Count);
        Assert.Equal(new[] { "V-2", "V-3" }, flows.Select(f => f.VoucherId));
        Assert.Equal(net, svc.GetBalance(entry));
    }

    // Dimension: flow list freshness - the list reflects only the entry's current valid flows.
    [Fact]
    public void GetFlows_AfterReversal_ReflectsOnlyCurrentValidFlows()
    {
        var svc = new LedgerService();
        var entry = svc.OpenEntry(NumberA, SharedName);

        svc.Post(entry, "V-1", 100m);
        svc.Post(entry, "V-2", 50m);
        svc.Reverse(entry, "V-2");

        var flow = Assert.Single(svc.GetFlows(entry));
        Assert.Equal("V-1", flow.VoucherId);
        Assert.Equal(100m, flow.Amount);
    }

    // Boundary: amounts falling exactly on the second place read away from zero.
    [Fact]
    public void MoneyMath_RoundsHalfAwayFromZero()
    {
        Assert.Equal(0.13m, MoneyMath.Round(0.125m));
        Assert.Equal(-0.13m, MoneyMath.Round(-0.125m));
        Assert.Equal(2.13m, MoneyMath.Round(2.125m));
        Assert.Equal(3.13m, MoneyMath.Round(3.125m));
        Assert.Equal(1.01m, MoneyMath.Round(1.005m));
    }

    // Same voucher posted many times: only one valid flow stays.
    [Fact]
    public async Task ConcurrentPost_SameVoucher_LeavesSingleValidFlow()
    {
        var svc = new LedgerService();
        var entry = svc.OpenEntry(NumberA, SharedName);

        await PostConcurrentlyAsync(svc, entry, "V-CONC", 100m);

        var flow = Assert.Single(svc.GetFlows(entry));
        Assert.Equal("V-CONC", flow.VoucherId);
        Assert.Equal(100m, flow.Amount);
        Assert.Equal(100m, svc.GetBalance(entry));
    }

    // Same voucher posted many times then reversed: ends with no valid flow.
    [Fact]
    public async Task ConcurrentPost_SameVoucher_ThenReverse_LeavesNoValidFlow()
    {
        var svc = new LedgerService();
        var entry = svc.OpenEntry(NumberA, SharedName);

        await PostConcurrentlyAsync(svc, entry, "V-CONC", 100m);

        svc.Reverse(entry, "V-CONC");

        Assert.Empty(svc.GetFlows(entry));
        Assert.Equal(0m, svc.GetBalance(entry));
    }

    // Boundary: an account the book does not know has no flows and no value.
    [Fact]
    public void GetFlows_UnknownEntry_Empty()
    {
        var svc = new LedgerService();
        var entry = svc.OpenEntry(NumberA, SharedName);
        var ghost = new LedgerEntry("ENT-9999", "Ghost Account");

        svc.Post(entry, "V-1", 100m);

        Assert.Empty(svc.GetFlows(ghost));
        Assert.Equal(0m, svc.GetBalance(ghost));
    }

    // Boundary: withdrawing a voucher that was never booked changes nothing.
    [Fact]
    public void Reverse_UnknownVoucher_NoChange()
    {
        var svc = new LedgerService();
        var entry = svc.OpenEntry(NumberA, SharedName);
        var ghost = new LedgerEntry("ENT-9999", "Ghost Account");

        svc.Post(entry, "V-1", 100m);
        svc.Reverse(entry, "V-NONE");
        svc.Reverse(ghost, "V-NONE");

        var flow = Assert.Single(svc.GetFlows(entry));
        Assert.Equal("V-1", flow.VoucherId);
        Assert.Equal(100m, flow.Amount);
        Assert.Equal(100m, svc.GetBalance(entry));
        Assert.Empty(svc.GetFlows(ghost));
    }
}