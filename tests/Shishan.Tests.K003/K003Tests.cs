namespace Shishan.Tests.K003;

public class K003Tests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 8, 0, 0);
    private static readonly DateTime T1 = T0.AddMinutes(10);
    private static readonly DateTime T2 = T0.AddMinutes(20);

    private static InspectionItem Item(string code, string value = "ok", bool voided = false)
        => new(code, value, voided);

    private static InspectionSubmission SubmitFor(string sheetId, DateTime time, params InspectionItem[] rows)
        => new(sheetId, time, rows.ToList(), "office");

    // Ownership: maintenance written for one sheet must land on that sheet only,
    // never on another sheet the office happens to keep.
    [Fact]
    public void Maintenance_OnlyAffectsRequestedSheet()
    {
        var svc = new InspectionService();
        svc.Register("A-001", new[] { Item("X1") }, T0);
        svc.Register("B-001", new[] { Item("Y1") }, T0);

        svc.SubmitMaintenance("B-001", "Y2", "2", "alice");

        Assert.True(svc.GetDetail("B-001")!.Shows("Y2"));
        Assert.False(svc.GetDetail("A-001")!.Shows("Y2"));
    }

    // Row set: the rows a sheet shows must be exactly the rows most recently
    // handed in for that sheet.
    [Fact]
    public void Detail_RowSet_EqualsSubmittedRows()
    {
        var svc = new InspectionService();
        svc.Register("A-001", new[] { Item("X1") }, T0);
        svc.Register("B-001", new[] { Item("Y1") }, T0);

        svc.Submit(SubmitFor("B-001", T1, Item("B1"), Item("B2")));

        var detail = svc.GetDetail("B-001");
        Assert.NotNull(detail);
        Assert.Equal(new[] { Item("B1"), Item("B2") }, detail.Rows);
    }

    // Freshness: after two hand-ins for the same sheet, the page must reflect
    // the later set, not the earlier one.
    [Fact]
    public void Detail_AfterSecondSubmit_ReflectsLatest()
    {
        var svc = new InspectionService();
        svc.Register("A-001", new[] { Item("X1") }, T0);

        svc.Submit(SubmitFor("A-001", T1, Item("Y1")));
        svc.Submit(SubmitFor("A-001", T2, Item("Z1")));

        var detail = svc.GetDetail("A-001");
        Assert.NotNull(detail);
        Assert.Equal(new[] { Item("Z1") }, detail.Rows);
    }

    // Freshness: a reading written by maintenance shows on the page once the
    // desk has done its rounds.
    [Fact]
    public void Maintenance_AfterSubmit_ReflectedInDetail()
    {
        var svc = new InspectionService();
        svc.Register("A-001", new[] { Item("X1") }, T0);

        svc.SubmitMaintenance("A-001", "Y1", "3", "alice");

        var detail = svc.GetDetail("A-001");
        Assert.NotNull(detail);
        Assert.True(detail.Shows("Y1"));
        Assert.Equal("3", detail.RowShown("Y1")!.Value);
    }

    // Header time: the time on the sheet header, and on the page, must move on
    // to the time of the most recent hand-in.
    [Fact]
    public void Header_Time_AdvancesOnSubmit()
    {
        var svc = new InspectionService();
        svc.Register("A-001", new[] { Item("X1") }, T0);

        svc.Submit(SubmitFor("A-001", T1, Item("X1")));
        svc.Submit(SubmitFor("A-001", T2, Item("X1")));

        Assert.Equal(T2, svc.GetSheets().Single(s => s.InspectionNo == "A-001").UpdatedAt);
        Assert.Equal(T2, svc.GetDetail("A-001")!.UpdatedAt);
    }

    // Row set: a row set aside in a hand-in must not appear on the page.
    [Fact]
    public void Detail_SupersededRows_NotCarried()
    {
        var svc = new InspectionService();
        svc.Register("A-001", new[] { Item("X1") }, T0);

        svc.Submit(SubmitFor("A-001", T1, Item("X1"), Item("V1", "n", true)));

        var detail = svc.GetDetail("A-001");
        Assert.NotNull(detail);
        Assert.False(detail.ShowsAnySetAside());
        Assert.False(detail.Shows("V1"));
    }

    // Boundary: handing in no rows clears the sheet completely.
    [Fact]
    public void EmptySubmission_ClearsRows()
    {
        var svc = new InspectionService();
        svc.Register("A-001", new[] { Item("X1") }, T0);

        svc.Submit(SubmitFor("A-001", T1, Item("M1")));
        svc.Submit(SubmitFor("A-001", T2));

        var detail = svc.GetDetail("A-001");
        Assert.NotNull(detail);
        Assert.Empty(detail.Rows);
        Assert.Equal(0, svc.GetSheets().Single(s => s.InspectionNo == "A-001").RowCount);
    }

    // Error path: handing rows in for a sheet the office does not keep is turned
    // away and must not alter any sheet that is kept.
    [Fact]
    public void Submit_UnknownSheet_Throws()
    {
        var svc = new InspectionService();
        svc.Register("A-001", new[] { Item("X1") }, T0);

        Assert.Throws<InspectionRuleException>(() =>
            svc.Submit(SubmitFor("NOPE-999", T1, Item("Q1"))));

        Assert.Equal(new[] { Item("X1") }, svc.GetDetail("A-001")!.Rows);
    }

    // Error path: maintenance for a sheet the office does not keep is turned away.
    [Fact]
    public void Maintenance_UnknownSheet_Throws()
    {
        var svc = new InspectionService();
        svc.Register("A-001", new[] { Item("X1") }, T0);

        Assert.Throws<InspectionRuleException>(() =>
            svc.SubmitMaintenance("NOPE-999", "Q1", "1", "alice"));
    }

    // Ownership: maintenance must not bring rows onto a sheet that was never
    // handed those rows, and must reach the sheet it was written for.
    [Fact]
    public void Detail_AfterMaintenanceOnly_NoPhantomRows()
    {
        var svc = new InspectionService();
        svc.Register("A-001", new[] { Item("X1") }, T0);
        svc.Register("B-001", new[] { Item("Y1") }, T0);

        svc.SubmitMaintenance("B-001", "C-2", "5", "bob");

        Assert.False(svc.GetDetail("A-001")!.Shows("C-2"));
        Assert.True(svc.GetDetail("B-001")!.Shows("C-2"));
    }

    // Boundary: reading the page of an unknown sheet yields no content.
    [Fact]
    public void Detail_UnknownSheet_ReturnsNull()
    {
        var svc = new InspectionService();
        Assert.Null(svc.GetDetail("NOPE-999"));
    }

    // Row set: a hand-in replaces whatever the sheet held before, exactly.
    [Fact]
    public void Submit_OnPrimary_ReplacesRowSetExactly()
    {
        var svc = new InspectionService();
        svc.Register("A-001", new[] { Item("X1"), Item("X2") }, T0);

        svc.Submit(SubmitFor("A-001", T1, Item("Z1")));

        var sheet = svc.GetSheets().Single(s => s.InspectionNo == "A-001");
        Assert.Equal(new[] { Item("Z1") }, sheet.HeldRows);
        Assert.Equal(new[] { Item("Z1") }, svc.GetDetail("A-001")!.Rows);
    }

    // Row set: maintenance of one item leaves the other rows of the sheet alone.
    [Fact]
    public void Maintenance_OnPrimary_PreservesEarlierItems()
    {
        var svc = new InspectionService();
        svc.Register("A-001", new[] { Item("X1"), Item("Y1") }, T0);

        svc.SubmitMaintenance("A-001", "Z1", "7", "alice");

        var detail = svc.GetDetail("A-001");
        Assert.NotNull(detail);
        Assert.Equal(new[] { Item("X1"), Item("Y1"), Item("Z1", "7") }, detail.Rows);
    }
}
