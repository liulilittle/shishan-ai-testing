namespace Shishan.Tests.K005;

using System.Collections.Concurrent;

public class K005Tests
{
    private static UpstreamOrder Order(
        string id,
        string product,
        string customer,
        string requestedBy,
        int declared,
        params UpstreamItem[] lines) =>
        new(id, product, customer, requestedBy, declared, lines);

    private static SnapshotOrderService ServiceFor(UpstreamOrder order)
    {
        var service = new SnapshotOrderService();
        service.Register(order);
        return service;
    }

    // Normal path: the two entrance points for one request leave exactly one
    // copy and one work order behind.
    [Fact]
    public void Create_And_Rebuild_SameId_OneSnapshot()
    {
        var service = ServiceFor(Order("RQ-1", "Widget", "Acme", "alice", 10, new UpstreamItem("S1", 10)));

        service.CreateOrder("RQ-1");
        service.Rebuild("RQ-1");

        Assert.Single(service.ListSnapshots());
        Assert.Single(service.ListDownstreamOrders());
        Assert.Single(service.ListPrints());
    }

    // Concurrent path: many callers take the copy for one request at once and
    // exactly one copy and one work order survive.
    [Fact]
    public void ConcurrentCreate_SameUpstream_OnlyOne()
    {
        var service = ServiceFor(Order(
            "RQ-2",
            "Gadget",
            "Beta",
            "bob",
            20,
            new UpstreamItem("S1", 8),
            new UpstreamItem("S2", 12)));

        RunTogether(32, _ => service.CreateOrder("RQ-2"));

        Assert.Single(service.ListSnapshots());
        Assert.Single(service.ListDownstreamOrders());
        Assert.Single(service.ListPrints());
    }

    // Concurrent path: many callers rebuild one request at once and exactly one
    // copy and one work order survive.
    [Fact]
    public void ConcurrentRebuild_SameRequest_OnlyOne()
    {
        var service = ServiceFor(Order(
            "RQ-3",
            "Widget",
            "Gamma",
            "carol",
            6,
            new UpstreamItem("S1", 2),
            new UpstreamItem("S2", 4)));

        RunTogether(32, _ => service.Rebuild("RQ-3"));

        Assert.Single(service.ListSnapshots());
        Assert.Single(service.ListDownstreamOrders());
    }

    // Refresh path: an amendment to the upstream order is reflected in the copy.
    [Fact]
    public void Refresh_AfterEdit_UsesLatestContent()
    {
        var upstream = Order("RQ-4", "Widget", "Acme", "alice", 5, new UpstreamItem("S1", 5));
        var service = ServiceFor(upstream);
        service.CreateOrder("RQ-4");

        upstream.Amend("Widget Pro", "Gamma", 7, new[]
        {
            new UpstreamItem("S1", 3),
            new UpstreamItem("S2", 4),
        });
        service.Refresh("RQ-4");

        var copy = service.FindSnapshot("RQ-4");
        Assert.NotNull(copy);
        Assert.Equal("Widget Pro", copy!.ProductName);
        Assert.Equal("Gamma", copy.Customer);
    }

    // Refresh path: the refreshed copy carries the amount actually listed, not
    // the figure written on the heading.
    [Fact]
    public void Refresh_Quantity_EqualsActualRowSum()
    {
        var upstream = Order("RQ-5", "Gadget", "Acme", "bob", 3, new UpstreamItem("S1", 1), new UpstreamItem("S2", 2));
        var service = ServiceFor(upstream);
        service.CreateOrder("RQ-5");

        upstream.Amend("Gadget", "Acme", 99, new[]
        {
            new UpstreamItem("S1", 6),
            new UpstreamItem("S2", 7),
        });
        service.Refresh("RQ-5");

        var copy = service.FindSnapshot("RQ-5");
        Assert.NotNull(copy);
        Assert.Equal(13, copy!.Quantity);
    }

    // Refresh path: the printed paper is brought in line with the copy as well.
    [Fact]
    public void Refresh_SyncsPrint()
    {
        var upstream = Order("RQ-6", "Widget", "Acme", "carol", 5, new UpstreamItem("S1", 5));
        var service = ServiceFor(upstream);
        service.CreateOrder("RQ-6");

        upstream.Amend("Widget Pro", "Gamma", 20, new[]
        {
            new UpstreamItem("S1", 6),
            new UpstreamItem("S2", 7),
        });
        service.Refresh("RQ-6");

        var print = service.GetPrint("RQ-6");
        Assert.NotNull(print);
        Assert.Equal("Widget Pro", print!.ProductName);
        Assert.Equal("Gamma", print.Customer);
        Assert.Equal(13, print.Quantity);
    }

    // Trail path: every line left behind names the party that took the step.
    [Fact]
    public void AuditLog_Operator_NotEmpty()
    {
        var service = ServiceFor(Order("RQ-7", "Widget", "Acme", "alice", 4, new UpstreamItem("S1", 4)));

        service.CreateOrder("RQ-7");
        service.Rebuild("RQ-7");

        var trail = service.GetAuditLog("RQ-7");
        Assert.NotEmpty(trail);
        Assert.All(trail, entry => Assert.True(entry.HasOperator()));
        Assert.All(trail, entry => Assert.Equal("alice", entry.Operator));
    }

    // Trail path: the moments on the lines never step backwards.
    [Fact]
    public void AuditLog_Timestamps_Monotonic()
    {
        var service = ServiceFor(Order("RQ-8", "Gadget", "Beta", "bob", 3, new UpstreamItem("S1", 3)));

        service.CreateOrder("RQ-8");
        service.Rebuild("RQ-8");
        service.Refresh("RQ-8");

        var trail = service.GetAuditLog("RQ-8");
        Assert.True(trail.Count >= 2);
        for (var i = 1; i < trail.Count; i++)
        {
            Assert.True(trail[i].At >= trail[i - 1].At);
        }
    }

    // Retire path: after a copy is retired, a rebuild takes it again from the
    // upstream order and exactly one copy is left behind.
    [Fact]
    public void Rebuild_AfterVoid_Recreates()
    {
        var service = ServiceFor(Order("RQ-9", "Widget", "Acme", "alice", 4, new UpstreamItem("S1", 4)));
        service.CreateOrder("RQ-9");
        Assert.NotNull(service.FindSnapshot("RQ-9"));

        service.Void("RQ-9");
        Assert.Null(service.FindSnapshot("RQ-9"));
        Assert.Null(service.GetPrint("RQ-9"));

        service.Rebuild("RQ-9");

        Assert.Single(service.ListSnapshots());
        Assert.Single(service.ListDownstreamOrders());
        Assert.NotNull(service.GetPrint("RQ-9"));
    }

    // Normal path: one request leaves one copy, one work order and one print,
    // all carrying the amount that is actually listed.
    [Fact]
    public void Create_SingleRequest_OneCopyAndOrderAndPrint()
    {
        var service = ServiceFor(Order(
            "RQ-10",
            "Gadget",
            "Acme",
            "dave",
            99,
            new UpstreamItem("S1", 6),
            new UpstreamItem("S2", 4)));

        service.CreateOrder("RQ-10");

        var copy = Assert.Single(service.ListSnapshots());
        var work = Assert.Single(service.ListDownstreamOrders());
        var print = Assert.Single(service.ListPrints());
        Assert.Equal(10, copy.Quantity);
        Assert.Equal(10, work.Quantity);
        Assert.Equal(10, print.Quantity);
        Assert.True(work.BelongsTo("RQ-10"));
        Assert.True(print.BelongsTo("RQ-10"));
    }

    // Boundary path: an unknown or empty identifier yields nothing rather than
    // an error for the reading calls.
    [Fact]
    public void UnknownSnapshot_Access_ThrowsOrEmpty()
    {
        var service = ServiceFor(Order("RQ-11", "Widget", "Acme", "alice", 1, new UpstreamItem("S1", 1)));

        Assert.Null(service.FindSnapshot("NOPE"));
        Assert.Null(service.FindSnapshot("   "));
        Assert.Empty(service.GetAuditLog("NOPE"));
        Assert.Throws<SnapshotException>(() => service.Refresh("NOPE"));
    }

    // Boundary path: reading the print for an unknown or empty identifier
    // yields null rather than an error.
    [Fact]
    public void GetPrint_Unknown_Null()
    {
        var service = ServiceFor(Order("RQ-12", "Widget", "Acme", "alice", 1, new UpstreamItem("S1", 1)));

        Assert.Null(service.GetPrint("NOPE"));
        Assert.Null(service.GetPrint(string.Empty));
        Assert.Null(service.GetPrint(null!));
    }

    // Error path: a request the desk was never told about is turned away.
    [Fact]
    public void Create_UnknownUpstream_Throws()
    {
        var service = ServiceFor(Order("RQ-13", "Widget", "Acme", "alice", 1, new UpstreamItem("S1", 1)));

        Assert.Throws<SnapshotException>(() => service.CreateOrder("NOPE"));
        Assert.Throws<SnapshotException>(() => service.Rebuild("NOPE"));
        Assert.Throws<SnapshotException>(() => service.Void("NOPE"));
    }

    // Error path: an empty or blank identifier is turned away.
    [Fact]
    public void Create_BlankIdentifier_Throws()
    {
        var service = ServiceFor(Order("RQ-14", "Widget", "Acme", "alice", 1, new UpstreamItem("S1", 1)));

        Assert.Throws<SnapshotException>(() => service.CreateOrder(string.Empty));
        Assert.Throws<SnapshotException>(() => service.CreateOrder("   "));
        Assert.Throws<SnapshotException>(() => service.Rebuild(string.Empty));
        Assert.Throws<SnapshotException>(() => service.Refresh(string.Empty));
    }

    // Error path: telling the desk about nothing is turned away.
    [Fact]
    public void Register_AbsentOrder_Throws()
    {
        var service = new SnapshotOrderService();

        Assert.Throws<ArgumentNullException>(() => service.Register(null!));
    }

    private static void RunTogether(int threads, Action<int> body)
    {
        using var gate = new Barrier(threads);
        var workers = new Thread[threads];
        var failures = new ConcurrentQueue<Exception>();

        for (var i = 0; i < threads; i++)
        {
            var index = i;
            workers[i] = new Thread(() =>
            {
                gate.SignalAndWait();
                try
                {
                    body(index);
                }
                catch (Exception error)
                {
                    failures.Enqueue(error);
                }
            })
            {
                IsBackground = true,
            };
            workers[i].Start();
        }

        foreach (var worker in workers)
        {
            worker.Join();
        }

        if (!failures.IsEmpty)
        {
            throw new AggregateException(failures);
        }
    }
}