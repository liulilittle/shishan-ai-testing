namespace Shishan.Tests.K001;

public class K001Tests
{
    [Fact]
    public void Convert_GramsToKilograms_Oneset()
    {
        var svc = new StocktakeService();
        svc.CreateInit(1, "SKU-A", 100m, "kg");
        svc.RecordActual("SKU-A", 120000m, "g");

        var line = svc.GetLine("SKU-A");
        Assert.NotNull(line);
        Assert.Equal(120m, line.CountedQuantity);
        Assert.Equal("g", line.CountedUnit);

        var row = Assert.Single(svc.CalculateDifference(1));
        Assert.Equal(120m, row.CountedQuantity);
        Assert.Equal(20m, row.Delta);

        Assert.Equal(120m, UnitConverter.ToBase(120000m, "g"));

        var init = svc.CreateInit(2, "SKU-B", 120000m, "g");
        Assert.Equal(120m, init.BookBaseQuantity);
    }

    [Fact]
    public void Convert_KilogramsToGrams_Oneset()
    {
        Assert.Equal(2000m, UnitConverter.FromBase(2m, "g"));
        Assert.Equal(2m, UnitConverter.FromBase(2m, "kg"));
        Assert.Equal(2m, UnitConverter.ToBase(2m, "kg"));
        Assert.Equal(2000m, UnitConverter.ToBase(2m, "ton"));
        Assert.Equal(24m, UnitConverter.ToBase(2m, "case"));
        Assert.Equal(40m, UnitConverter.ToBase(2m, "tray"));
        Assert.Equal(0.002m, UnitConverter.FromBase(2m, "ton"));
        Assert.Equal(2m, UnitConverter.FromBase(24m, "case"));
        Assert.Equal(2m, UnitConverter.FromBase(40m, "tray"));
    }

    [Fact]
    public void Difference_TwoOrdersStocktake_DeriveOnlyOwnOrder()
    {
        var svc = new StocktakeService();
        svc.CreateInit(1, "SKU-A", 100m, "kg");
        svc.RecordActual("SKU-A", 120m, "kg");
        svc.CreateInit(2, "SKU-B", 50m, "kg");
        svc.RecordActual("SKU-B", 60m, "kg");

        var rows1 = svc.CalculateDifference(1);
        var rows2 = svc.CalculateDifference(2);

        var row1 = Assert.Single(rows1);
        Assert.Equal("SKU-A", row1.SkuCode);
        Assert.Equal(20m, row1.Delta);

        var row2 = Assert.Single(rows2);
        Assert.Equal("SKU-B", row2.SkuCode);
        Assert.Equal(10m, row2.Delta);
    }

    [Fact]
    public void Audit_OnlyAffectsRequestedLine()
    {
        var svc = new StocktakeService();
        svc.CreateInit(1, "SKU-A", 100m, "kg");
        svc.CreateInit(1, "SKU-B", 50m, "kg");
        svc.RecordActual("SKU-A", 120m, "kg");
        svc.RecordActual("SKU-B", 40m, "kg");

        var audit = svc.AuditLine("SKU-A");
        Assert.Equal("SKU-A", audit.SkuCode);
        Assert.Equal(20m, audit.Delta);

        var lineA = svc.GetLine("SKU-A");
        var lineB = svc.GetLine("SKU-B");
        Assert.NotNull(lineA);
        Assert.NotNull(lineB);
        Assert.Equal(StocktakeLineStatus.Audited, lineA.Status);
        Assert.Equal(StocktakeLineStatus.Counted, lineB.Status);
        Assert.Equal(-10m, lineB.Delta);

        svc.CreateInit(2, "SKU-C", 30m, "kg");
        var lineC = svc.GetLine("SKU-C");
        Assert.NotNull(lineC);
        Assert.Equal(StocktakeLineStatus.Pending, lineC.Status);
    }

    [Fact]
    public void Reconcile_TwoOrdersDoNotInterfere()
    {
        var svc = new StocktakeService();
        svc.CreateInit(1, "SKU-A", 100m, "kg");
        svc.RecordActual("SKU-A", 40m, "kg");
        svc.CreateInit(2, "SKU-A", 50m, "kg");
        svc.RecordActual("SKU-A", 60m, "kg");

        var rows1 = svc.CalculateDifference(1);
        var rows2 = svc.CalculateDifference(2);

        var row1 = Assert.Single(rows1);
        var row2 = Assert.Single(rows2);
        Assert.Equal(-60m, row1.Delta);
        Assert.Equal(10m, row2.Delta);
    }

    [Fact]
    public async Task ConcurrentAudit_SameLine_RecordsOnce()
    {
        var svc = new StocktakeService();
        svc.CreateInit(1, "SKU-X", 100m, "kg");

        const int workers = 32;
        using var barrier = new Barrier(workers);
        var tasks = Enumerable.Range(0, workers)
            .Select(i => Task.Run(() =>
            {
                barrier.SignalAndWait();
                svc.RecordActual("SKU-X", 10m, "kg");
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        var line = svc.GetLine("SKU-X");
        Assert.NotNull(line);
        Assert.Equal(10m, line.CountedQuantity);

        var row = Assert.Single(svc.CalculateDifference(1));
        Assert.Equal(10m, row.CountedQuantity);
        Assert.Equal(-90m, row.Delta);
    }

    [Fact]
    public void Difference_ShowsOnlyNetDelta()
    {
        var svc = new StocktakeService();
        svc.CreateInit(1, "SKU-A", 100m, "kg");
        svc.CreateInit(1, "SKU-B", 200m, "kg");
        svc.RecordActual("SKU-A", 130m, "kg");
        svc.RecordActual("SKU-B", 190m, "kg");

        var rows = svc.CalculateDifference(1);
        Assert.Collection(rows,
            rowA => Assert.Equal("SKU-A", rowA.SkuCode),
            rowB => Assert.Equal("SKU-B", rowB.SkuCode));

        var deltaA = rows.Single(r => r.SkuCode == "SKU-A").Delta;
        var deltaB = rows.Single(r => r.SkuCode == "SKU-B").Delta;
        Assert.Equal(30m, deltaA);
        Assert.Equal(-10m, deltaB);
    }

    [Fact]
    public void ZeroDifference_StillLeavesRecord()
    {
        var svc = new StocktakeService();
        svc.CreateInit(1, "SKU-A", 100m, "kg");
        svc.RecordActual("SKU-A", 100m, "kg");

        var row = Assert.Single(svc.CalculateDifference(1));
        Assert.Equal(0m, row.Delta);
        Assert.Equal(StocktakeLineStatus.Counted, row.Status);

        var audit = svc.AuditLine("SKU-A");
        Assert.Equal("SKU-A", audit.SkuCode);
        Assert.Equal(0m, audit.Delta);
        Assert.True(audit.IsBalanced);

        var signed = Assert.Single(svc.SignOffsFor(1));
        Assert.Equal(0m, signed.Delta);
    }

    [Fact]
    public void Difference_UnknownSku_ThrowsOrEmpty()
    {
        var svc = new StocktakeService();
        svc.CreateInit(1, "SKU-A", 100m, "kg");

        Assert.Throws<ArgumentException>(() => svc.RecordActual("SKU-MISSING", 10m, "kg"));
        Assert.Throws<ArgumentException>(() => svc.AuditLine("SKU-MISSING"));
        Assert.Throws<ArgumentException>(() => svc.ReadRow("SKU-MISSING"));

        Assert.Empty(svc.CalculateDifference(99));
    }

    [Fact]
    public void GetLine_Unknown_ReturnsNull()
    {
        var svc = new StocktakeService();
        svc.CreateInit(1, "SKU-A", 100m, "kg");

        Assert.Null(svc.GetLine("SKU-NOPE"));
        Assert.NotNull(svc.GetLine("SKU-A"));
    }

    [Fact]
    public void Init_RoundNo_ScopedPerOrder()
    {
        var svc = new StocktakeService();
        svc.CreateInit(1, "SKU-A", 100m, "kg");
        svc.CreateInit(2, "SKU-B", 50m, "kg");
        svc.CreateInit(1, "SKU-C", 200m, "kg");

        var rows1 = svc.CalculateDifference(1);
        var rows2 = svc.CalculateDifference(2);

        Assert.Collection(rows1,
            rowA => Assert.Equal("SKU-A", rowA.SkuCode),
            rowC => Assert.Equal("SKU-C", rowC.SkuCode));
        var rowB = Assert.Single(rows2);
        Assert.Equal("SKU-B", rowB.SkuCode);

        Assert.Equal(100m, rows1.Single(r => r.SkuCode == "SKU-A").BookQuantity);
        Assert.Equal(200m, rows1.Single(r => r.SkuCode == "SKU-C").BookQuantity);
        Assert.Equal(50m, rowB.BookQuantity);
    }
}