namespace Shishan.Tests.K002;

public class K002Tests
{
    private static readonly PrintCategory[] AllCategories =
    {
        PrintCategory.Invoice,
        PrintCategory.DeliveryNote,
        PrintCategory.PackingList,
        PrintCategory.Statement
    };

    // Dimension: first-print gate - within one category the reason-free
    // opening is used exactly once, then a reason is needed
    [Fact]
    public void FirstPrint_FreeNoReason_SingleCategory()
    {
        var svc = new PrintService();

        var first = svc.RequestPrint("Invoice", "");

        Assert.True(first.Granted);
        Assert.True(first.FirstPrint);
        Assert.True(first.PrintNumber > 0);
        Assert.Equal(PrintStatuses.FirstPrinted, first.Status);

        var again = svc.RequestPrint("Invoice", "");

        Assert.False(again.Granted);
        Assert.Contains("reason", again.RejectionReason, StringComparison.OrdinalIgnoreCase);

        var withReason = svc.RequestPrint("Invoice", "reprint needed");

        Assert.True(withReason.Granted);
        Assert.False(withReason.FirstPrint);
        Assert.Equal(PrintStatuses.Reprinted, withReason.Status);

        var status = svc.GetStatus("  INVOICE  ");

        Assert.Equal(PrintStatuses.Reprinted, status.Status);
    }

    // Boundary: each category owns its own reason-free window - a second
    // category's first sheet leaves without a reason too
    [Fact]
    public void SecondCategory_FirstPrint_HasOwnWindow()
    {
        var svc = new PrintService();

        var invoice = svc.RequestPrint("Invoice", "");

        Assert.True(invoice.Granted);
        Assert.True(invoice.FirstPrint);

        var delivery = svc.RequestPrint("DeliveryNote", "");

        Assert.True(delivery.Granted);
        Assert.True(delivery.FirstPrint);
        Assert.Equal(PrintStatuses.FirstPrinted, delivery.Status);
        Assert.NotEqual(invoice.PrintNumber, delivery.PrintNumber);

        var deliveryAgain = svc.RequestPrint("DeliveryNote", "");

        Assert.False(deliveryAgain.Granted);
    }

    // Dimension: after a category's opening is used, every later sheet of
    // that category must carry a reason before it is let out
    [Fact]
    public void SubsequentPrint_RequiresReason()
    {
        var svc = new PrintService();

        var first = svc.RequestPrint("Invoice", "");

        Assert.True(first.Granted);

        var noReason = svc.RequestPrint("Invoice", "");

        Assert.False(noReason.Granted);

        var withReason = svc.RequestPrint("Invoice", "customer asked for a copy");

        Assert.True(withReason.Granted);
        Assert.False(withReason.FirstPrint);
        Assert.Equal(PrintStatuses.Reprinted, withReason.Status);

        var noReasonAgain = svc.RequestPrint("Invoice", "");

        Assert.False(noReasonAgain.Granted);
    }

    // Deterministic concurrency: many desks asking for the first sheet of a
    // category at the same moment must let exactly one through the opening
    [Fact]
    public async Task ConcurrentFirstPrint_OnlyOnce()
    {
        var svc = new PrintService();
        const int workers = 32;
        using var barrier = new Barrier(workers);

        var tasks = Enumerable.Range(0, workers)
            .Select(i => Task.Run(() =>
            {
                barrier.SignalAndWait();
                return svc.RequestPrint("Statement", "");
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        var granted = tasks.Select(t => t.Result).Where(r => r.Granted).ToList();

        Assert.Single(granted);
        Assert.True(granted[0].FirstPrint);
        Assert.True(granted[0].PrintNumber > 0);

        var ledger = svc.GetLedger("Statement", 1, workers);

        Assert.Equal(granted.Count, ledger.Total);
    }

    // Dimension: the printed count of a category equals its own ledger rows
    [Fact]
    public void Count_MatchesLedgerRows()
    {
        var svc = new PrintService();

        svc.RequestPrint("Invoice", "");
        svc.RequestPrint("Invoice", "reprint needed");
        svc.RequestPrint("Invoice", "");
        svc.RequestPrint("DeliveryNote", "");
        svc.RequestPrint("PackingList", "");

        foreach (var category in AllCategories)
        {
            var counted = svc.CountPrinted(category.ToString());
            var ledger = svc.GetLedger(category.ToString(), 1, 100);

            Assert.Equal(counted, ledger.Total);
            Assert.Equal(counted, ledger.Items.Count);
        }
    }

    // Dimension: every ledger row carries a positive, distinct print number
    [Fact]
    public void EachRow_HasPrintNumber()
    {
        var svc = new PrintService();

        svc.RequestPrint("Invoice", "");
        svc.RequestPrint("Invoice", "reprint needed");
        svc.RequestPrint("DeliveryNote", "");
        svc.RequestPrint("PackingList", "");
        svc.RequestPrint("Statement", "");

        var page = svc.GetLedger(PrintService.LedgerAll, 1, 100);

        Assert.Equal(page.Total, page.Items.Count);
        Assert.All(page.Items, e => Assert.True(e.PrintNumber > 0));
        Assert.Equal(page.Items.Count, page.Items.Select(e => e.PrintNumber).Distinct().Count());
    }

    // Dimension: the standing of a category derives from its own latest
    // sheet and never from another category's sheets
    [Fact]
    public void Status_DerivedFromLatestOfCategory()
    {
        var svc = new PrintService();

        var first = svc.RequestPrint("Invoice", "");

        Assert.Equal(PrintStatuses.FirstPrinted, svc.GetStatus("Invoice").Status);

        var reprint = svc.RequestPrint("Invoice", "reprint needed");

        var status = svc.GetStatus("Invoice");

        Assert.Equal(PrintStatuses.Reprinted, status.Status);
        Assert.Equal(reprint.PrintNumber, status.LastPrintNumber);
        Assert.Equal(PrintCategory.Invoice, status.Category);

        Assert.Equal(PrintStatuses.NotPrinted, svc.GetStatus("Statement").Status);
        Assert.Equal(0, svc.GetStatus("Statement").LastPrintNumber);
        Assert.False(svc.GetStatus("Statement").HasPrinted);
    }

    // Boundary: paging over all four categories keeps every row and every
    // page total consistent with the register
    [Fact]
    public void Paging_FourCategories_FullCoverage()
    {
        var svc = new PrintService();

        svc.RequestPrint("Invoice", "");
        svc.RequestPrint("Invoice", "reprint needed");
        svc.RequestPrint("Invoice", "");
        svc.RequestPrint("DeliveryNote", "");
        svc.RequestPrint("PackingList", "");
        svc.RequestPrint("Statement", "");

        const int pageSize = 2;
        var first = svc.GetLedger(PrintService.LedgerAll, 1, pageSize);

        Assert.Equal(5, first.Total);
        Assert.Equal(3, first.TotalPages);

        var collected = new List<PrintLedgerEntry>();
        for (var page = 1; page <= first.TotalPages; page++)
        {
            var paged = svc.GetLedger(PrintService.LedgerAll, page, pageSize);

            Assert.InRange(paged.Items.Count, 0, pageSize);
            collected.AddRange(paged.Items);
        }

        Assert.Equal(first.Total, collected.Count);
        Assert.Equal(first.Total, first.Items.Concat(
            svc.GetLedger(PrintService.LedgerAll, 2, pageSize).Items)
            .Concat(svc.GetLedger(PrintService.LedgerAll, 3, pageSize).Items).Count());

        foreach (var category in AllCategories)
        {
            Assert.Contains(collected, e => e.Category == category);
        }
    }

    // Boundary: an empty register and out-of-range pages come back clean,
    // and the page count works out exactly when the register fits the page
    // size
    [Fact]
    public void Paging_EmptyAndOutOfRange()
    {
        var svc = new PrintService();

        var empty = svc.GetLedger(PrintService.LedgerAll, 1, 10);

        Assert.Equal(0, empty.Total);
        Assert.Equal(0, empty.TotalPages);
        Assert.Empty(empty.Items);

        svc.RequestPrint("Invoice", "");

        var single = svc.GetLedger(PrintService.LedgerAll, 1, 10);

        Assert.Equal(1, single.Total);
        Assert.Equal(1, single.TotalPages);
        Assert.Single(single.Items);

        var beyond = svc.GetLedger(PrintService.LedgerAll, 99, 10);

        Assert.Empty(beyond.Items);
        Assert.Equal(1, beyond.Total);
        Assert.Equal(1, beyond.TotalPages);

        var zeroPage = svc.GetLedger("Invoice", 0, 10);

        Assert.Equal(1, zeroPage.Page);

        var zeroSize = svc.GetLedger("Invoice", 1, 0);

        Assert.Equal(1, zeroSize.PageSize);

        var negativeSize = svc.GetLedger("Invoice", 1, -3);

        Assert.Equal(1, negativeSize.PageSize);

        svc.RequestPrint("Invoice", "reprint one");
        svc.RequestPrint("Invoice", "reprint two");
        svc.RequestPrint("Invoice", "reprint three");

        var exact = svc.GetLedger("Invoice", 1, 2);

        Assert.Equal(4, exact.Total);
        Assert.Equal(2, exact.TotalPages);

        var secondPage = svc.GetLedger("Invoice", 2, 2);

        Assert.Equal(2, secondPage.Items.Count);
        Assert.Equal(exact.Total, firstPageTotal(exact, secondPage));
    }

    // Dimension: looking a row up by its number returns the exact row or null
    [Fact]
    public void Lookup_ByNumber_ExactOrNull()
    {
        var svc = new PrintService();

        var first = svc.RequestPrint("Invoice", "");
        var reprint = svc.RequestPrint("Invoice", "reprint needed");

        var row = svc.FindLedgerEntry(reprint.PrintNumber);

        Assert.NotNull(row);
        Assert.Equal(PrintCategory.Invoice, row.Category);
        Assert.Equal(reprint.PrintNumber, row.PrintNumber);
        Assert.Equal(PrintStatuses.Reprinted, row.Status);

        var firstRow = svc.FindLedgerEntry(first.PrintNumber);

        Assert.NotNull(firstRow);
        Assert.Equal(first.PrintNumber, firstRow.PrintNumber);

        Assert.Null(svc.FindLedgerEntry(999999));
        Assert.Null(svc.FindLedgerEntry(-1));
        Assert.Null(svc.FindLedgerEntry(0));
    }

    private static int firstPageTotal(PagedLedger firstPage, PagedLedger secondPage) =>
        firstPage.Items.Count + secondPage.Items.Count;
}