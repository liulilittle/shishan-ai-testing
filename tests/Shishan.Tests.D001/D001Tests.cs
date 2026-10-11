namespace Shishan.Tests.D001;

public class D001Tests
{
    private static readonly DateTime _base = new(2026, 10, 8, 9, 0, 0, DateTimeKind.Utc);

    private static ApprovalDocument Make(string businessNo, string applyNo, string title, string nodeCode = "N1", string nodeName = "submitted", DateTime? time = null)
    {
        var doc = new ApprovalDocument(businessNo, applyNo, title);
        doc.AddNode(nodeCode, nodeName, time ?? _base);
        return doc;
    }

    // Error path: a file must be given before anything is entered
    [Fact]
    public void Register_NullDocument_Throws()
    {
        var svc = new ApprovalProgressService();

        Assert.Throws<ArgumentNullException>(() => svc.Register(null!));
    }

    // Own column: a business number with one file shows exactly that file
    [Fact]
    public void List_SingleDocument_ReturnsOne()
    {
        var svc = new ApprovalProgressService();

        svc.Register(Make("BN-010", "AP-110", "First file", "N1", "submitted"));
        svc.Register(Make("BN-020", "AP-120", "Second file", "N2", "reviewing"));
        svc.Register(Make("BN-030", "AP-130", "Third file", "N3", "approved"));

        var first = svc.ListProgressByBusinessNo("BN-010", "AP-110");
        var second = svc.ListProgressByBusinessNo("BN-020", "AP-120");
        var third = svc.ListProgressByBusinessNo("BN-030", "AP-130");

        var one = Assert.Single(first);
        Assert.Equal("AP-110", one.ApplyNo);
        Assert.Equal("N1", one.NodeCode);

        var two = Assert.Single(second);
        Assert.Equal("AP-120", two.ApplyNo);
        Assert.Equal("N2", two.NodeCode);

        var three = Assert.Single(third);
        Assert.Equal("AP-130", three.ApplyNo);
        Assert.Equal("N3", three.NodeCode);
    }

    // Shared business number: querying one apply must not bring in the others
    [Fact]
    public void List_SharedBusinessNo_ReturnsOnlyRequested()
    {
        var svc = new ApprovalProgressService();

        svc.Register(Make("BN-001", "AP-100", "Alpha", "N1", "submitted"));
        svc.Register(Make("BN-001", "AP-200", "Beta", "N5", "done"));
        svc.Register(Make("BN-001", "AP-300", "Gamma", "N2", "reviewing"));

        var first = svc.ListProgressByBusinessNo("BN-001", "AP-100");
        var second = svc.ListProgressByBusinessNo("BN-001", "AP-200");
        var third = svc.ListProgressByBusinessNo("BN-001", "AP-300");

        var one = Assert.Single(first);
        Assert.Equal("AP-100", one.ApplyNo);
        Assert.Equal("N1", one.NodeCode);

        var two = Assert.Single(second);
        Assert.Equal("AP-200", two.ApplyNo);
        Assert.Equal("N5", two.NodeCode);

        var three = Assert.Single(third);
        Assert.Equal("AP-300", three.ApplyNo);
        Assert.Equal("N2", three.NodeCode);
    }

    // Missing apply number: a business number with files must yield nothing for an unknown apply
    [Fact]
    public void List_MissingApplyNo_ReturnsEmpty()
    {
        var svc = new ApprovalProgressService();

        svc.Register(Make("BN-004", "AP-900", "Delta", "N3", "approved"));
        svc.Register(Make("BN-004", "AP-910", "Epsilon", "N1", "submitted"));

        Assert.Empty(svc.ListProgressByBusinessNo("BN-004", "AP-NOT-HELD"));
        Assert.Empty(svc.ListProgressByBusinessNo("BN-004", "AP-0000"));
    }

    // Unknown business number: nothing is shown for a business the desk has not met
    [Fact]
    public void List_UnknownBusinessNo_ReturnsEmpty()
    {
        var svc = new ApprovalProgressService();

        svc.Register(Make("BN-005", "AP-140", "Zeta", "N1", "submitted"));

        Assert.Empty(svc.ListProgressByBusinessNo("BN-NONE", "AP-140"));
        Assert.Empty(svc.ListProgressByBusinessNo("BN-EMPTY", "AP-NO"));
    }

    // Shared business number: the detail by business number must be the requested apply
    [Fact]
    public void Detail_ByBusinessNo_ReturnsRequested()
    {
        var svc = new ApprovalProgressService();

        svc.Register(Make("BN-002", "AP-400", "Eta", "N5", "done"));
        svc.Register(Make("BN-002", "AP-520", "Theta", "N2", "reviewing"));
        svc.Register(Make("BN-002", "AP-630", "Iota", "N1", "submitted"));

        var first = svc.DetailByBusinessNo("BN-002", "AP-400");
        var second = svc.DetailByBusinessNo("BN-002", "AP-520");
        var third = svc.DetailByBusinessNo("BN-002", "AP-630");

        Assert.NotNull(first);
        Assert.Equal("AP-400", first.ApplyNo);
        Assert.Equal("N5", first.NodeCode);

        Assert.NotNull(second);
        Assert.Equal("AP-520", second.ApplyNo);
        Assert.Equal("N2", second.NodeCode);

        Assert.NotNull(third);
        Assert.Equal("AP-630", third.ApplyNo);
        Assert.Equal("N1", third.NodeCode);
    }

    // Own column: the detail by apply number reaches the right file
    [Fact]
    public void Detail_ByApplyNo_ReturnsRequested()
    {
        var svc = new ApprovalProgressService();

        svc.Register(Make("BN-003", "AP-700", "Kappa", "N1", "submitted"));
        svc.Register(Make("BN-003", "AP-710", "Lambda", "N5", "done"));
        svc.Register(Make("BN-003", "AP-720", "Mu", "N3", "approved"));

        var first = svc.DetailByApplyNo("AP-700");
        var second = svc.DetailByApplyNo("AP-710");
        var third = svc.DetailByApplyNo("AP-720");

        Assert.NotNull(first);
        Assert.Equal("AP-700", first.ApplyNo);
        Assert.Equal("N1", first.NodeCode);

        Assert.NotNull(second);
        Assert.Equal("AP-710", second.ApplyNo);
        Assert.Equal("N5", second.NodeCode);

        Assert.NotNull(third);
        Assert.Equal("AP-720", third.ApplyNo);
        Assert.Equal("N3", third.NodeCode);
    }

    // Same file: both ways of reading the detail must agree after a later step
    [Fact]
    public void Detail_TwoPaths_AgreeForSameDocument()
    {
        var svc = new ApprovalProgressService();
        var doc = Make("BN-004", "AP-600", "Nu", "N1", "submitted");
        svc.Register(doc);
        doc.AddNode("N2", "reviewing", _base.AddMinutes(20));

        var byBusiness = svc.DetailByBusinessNo("BN-004", "AP-600");
        var byApply = svc.DetailByApplyNo("AP-600");

        Assert.NotNull(byBusiness);
        Assert.NotNull(byApply);
        Assert.Equal(byApply.ApplyNo, byBusiness.ApplyNo);
        Assert.Equal(byApply.NodeCode, byBusiness.NodeCode);
        Assert.Equal(byApply.NodeName, byBusiness.NodeName);
        Assert.Equal("N2", byBusiness.NodeCode);
        Assert.Equal("reviewing", byBusiness.NodeName);
    }

    // Node freshness: the detail reflects the most recent step after a later step
    [Fact]
    public void Detail_AfterAdvance_ReflectsLatestNode()
    {
        var svc = new ApprovalProgressService();
        var doc = Make("BN-005", "AP-800", "Xi", "N1", "submitted");
        svc.Register(doc);
        doc.AddNode("N3", "approved", _base.AddMinutes(30));

        var view = svc.DetailByApplyNo("AP-800");

        Assert.NotNull(view);
        Assert.Equal("N3", view.NodeCode);
        Assert.Equal("approved", view.NodeName);

        doc.AddNode("N5", "done", _base.AddMinutes(45));

        var moved = svc.DetailByApplyNo("AP-800");

        Assert.NotNull(moved);
        Assert.Equal("N5", moved.NodeCode);
        Assert.Equal("done", moved.NodeName);
    }

    // Unknown apply number: no file, no detail
    [Fact]
    public void Detail_UnknownApplyNo_ReturnsNull()
    {
        var svc = new ApprovalProgressService();

        svc.Register(Make("BN-006", "AP-150", "Omicron", "N1", "submitted"));

        Assert.Null(svc.DetailByApplyNo("AP-NONE"));
        Assert.Null(svc.DetailByApplyNo("AP-4242"));
        Assert.Null(svc.FindByApplyNo("AP-NONE"));
        Assert.Null(svc.DetailByBusinessNo("BN-NONE", "AP-150"));
    }
}