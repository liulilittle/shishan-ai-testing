namespace Shishan.Tests.D002;

public class D002Tests
{
    private static List<OperationExecution> RecordsOf(OrderProcessService svc, string orderId, string operationCode) =>
        svc.ListExecutions(orderId)
            .Where(e => e.OrderId == orderId && e.OperationCode == operationCode)
            .ToList();

    // Own record: two orders that share an operation each start their own line
    [Fact]
    public void Start_TwoOrdersShareOperation_ReturnsOwnRecord()
    {
        var svc = new OrderProcessService();

        var recA = svc.Start("ORD-A", "OP-100", "alice");
        var recB = svc.Start("ORD-B", "OP-100", "bob");

        Assert.Equal("ORD-A", recA.OrderId);
        Assert.Equal("ORD-B", recB.OrderId);
        Assert.NotSame(recA, recB);
    }

    // One line per order and operation: a repeated start hands back the same record
    [Fact]
    public void Start_SameOrderSameOperation_ReturnsSameRecord()
    {
        var svc = new OrderProcessService();

        var first = svc.Start("ORD-S", "OP-100", "alice");
        var second = svc.Start("ORD-S", "OP-100", "bob");

        Assert.Same(first, second);
        Assert.Single(RecordsOf(svc, "ORD-S", "OP-100"));
    }

    // Own list: orders sharing an operation never see each other's lines
    [Fact]
    public void Start_SharedOperation_ListsStaySeparate()
    {
        var svc = new OrderProcessService();

        svc.Start("ORD-X", "OP-100", "alice");
        svc.Advance("ORD-X", "OP-100", "alice");
        svc.Advance("ORD-X", "OP-100", "alice");
        svc.Start("ORD-Y", "OP-100", "bob");

        var listX = svc.ListExecutions("ORD-X");
        var listY = svc.ListExecutions("ORD-Y");

        Assert.NotEmpty(listX);
        Assert.NotEmpty(listY);
        Assert.All(listX, e => Assert.Equal("ORD-X", e.OrderId));
        Assert.All(listY, e => Assert.Equal("ORD-Y", e.OrderId));
    }

    // One line per order and operation: starting together still yields one record
    [Fact]
    public async Task ConcurrentStart_SameOrder_OneRecord()
    {
        var svc = new OrderProcessService();
        const int workersCount = 32;
        using var gate = new Barrier(workersCount);

        var workers = Enumerable.Range(0, workersCount)
            .Select(i => Task.Run(() =>
            {
                gate.SignalAndWait();
                svc.Start("ORD-C", "OP-100", "worker-" + i);
            }))
            .ToArray();
        await Task.WhenAll(workers);

        var records = RecordsOf(svc, "ORD-C", "OP-100");

        Assert.Single(records);
        Assert.Equal("ORD-C", records[0].OrderId);
        Assert.Equal("OP-100", records[0].OperationCode);
    }

    // State progression: a single line moves through every state in order
    [Fact]
    public void Advance_PendingThenRunningThenCompleted()
    {
        var svc = new OrderProcessService();

        var execution = svc.Start("ORD-P", "OP-300", "alice");
        Assert.Equal(OperationState.Pending, execution.State);

        svc.Advance("ORD-P", "OP-300", "bob");
        Assert.Equal(OperationState.Running, execution.State);

        svc.Advance("ORD-P", "OP-300", "carol");
        Assert.Equal(OperationState.Completed, execution.State);
        Assert.True(execution.IsFinished);
    }

    // State progression: advancing together converges on one terminal record
    [Fact]
    public async Task ConcurrentAdvance_SingleTerminal()
    {
        var svc = new OrderProcessService();
        const int workersCount = 32;
        using var gate = new Barrier(workersCount);

        var workers = Enumerable.Range(0, workersCount)
            .Select(i => Task.Run(() =>
            {
                gate.SignalAndWait();
                svc.Advance("ORD-T", "OP-200", "operator-" + i);
            }))
            .ToArray();
        await Task.WhenAll(workers);

        var records = RecordsOf(svc, "ORD-T", "OP-200");

        Assert.Single(records);
        Assert.Equal(OperationState.Completed, records[0].State);
    }

    // Dimension: the summary an order reports equals the state of its own lines
    [Fact]
    public void Summary_DerivedFromExecutions_MatchesList()
    {
        var svc = new OrderProcessService();

        svc.Start("ORD-M", "OP-100", "alice");
        svc.Advance("ORD-M", "OP-100", "bob");
        svc.Start("ORD-M", "OP-200", "carol");

        var records = svc.ListExecutions("ORD-M");
        var summary = svc.GetSummary("ORD-M");

        Assert.Equal(records.Count(e => e.State == OperationState.Pending), summary.Pending);
        Assert.Equal(records.Count(e => e.State == OperationState.Running), summary.Running);
        Assert.Equal(records.Count(e => e.State == OperationState.Completed), summary.Completed);
    }

    // Boundary: an order nobody has opened anything for has an empty list
    [Fact]
    public void List_UnknownOrder_Empty()
    {
        var svc = new OrderProcessService();

        svc.Start("ORD-K", "OP-100", "alice");

        Assert.Empty(svc.ListExecutions("ORD-NONE"));
    }

    // Error path: an order id is needed before anything is opened
    [Fact]
    public void Start_EmptyOrderId_Throws()
    {
        var svc = new OrderProcessService();

        Assert.Throws<ArgumentException>(() => svc.Start("", "OP-100", "alice"));
        Assert.Throws<ArgumentException>(() => svc.Start(null!, "OP-100", "alice"));
    }

    // Error path: an operation code is needed before anything is opened
    [Fact]
    public void Start_EmptyOperationCode_Throws()
    {
        var svc = new OrderProcessService();

        Assert.Throws<ArgumentException>(() => svc.Start("ORD-Z", "", "alice"));
        Assert.Throws<ArgumentException>(() => svc.Start("ORD-Z", null!, "alice"));
    }
}