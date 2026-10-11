namespace Shishan.Tests.D005;

public class D005Tests
{
    // Registration: a new number opens a fresh archive that carries the name
    [Fact]
    public void Register_NewNumber_CreatesArchive()
    {
        var svc = new ArchiveGovernanceService();

        var archive = svc.Register("ARC-0001", "Zhao Family");

        Assert.NotNull(archive);
        Assert.Equal("ARC-0001", archive.Number);
        Assert.Equal("Zhao Family", archive.Name);
        Assert.Equal(0, archive.Round);
        Assert.Equal(1, svc.GetRegisteredCount());
    }

    // Uniqueness: a number already open is turned away and nothing is added
    [Fact]
    public void Register_DuplicateNumber_ThrowsOrIdempotent()
    {
        var svc = new ArchiveGovernanceService();
        svc.Register("ARC-0002", "Qian Family");

        Assert.Throws<ArchiveAlreadyRegisteredException>(() => svc.Register("ARC-0002", "Sun Family"));

        Assert.Equal(1, svc.GetRegisteredCount());
        Assert.Equal("Qian Family", svc.Find("ARC-0002")!.Name);
    }

    // Uniqueness under load: opening the same number together leaves one archive
    [Fact]
    public async Task ConcurrentRegister_SameNumber_OnlyOneCreated()
    {
        var svc = new ArchiveGovernanceService();
        const string number = "ARC-C";
        const int workersCount = 32;
        using var gate = new Barrier(workersCount);

        var workers = Enumerable.Range(0, workersCount)
            .Select(i => Task.Run(() =>
            {
                gate.SignalAndWait();
                try
                {
                    svc.Register(number, "Shared Family");
                }
                catch (ArchiveAlreadyRegisteredException)
                {
                }
            }))
            .ToArray();
        await Task.WhenAll(workers);

        Assert.Equal(1, svc.GetRegisteredCount());
        Assert.NotNull(svc.Find(number));
    }

    // Correction target: only the archive named by the number is corrected
    [Fact]
    public void Correct_ChangesOnlyRequestedNumber()
    {
        var svc = new ArchiveGovernanceService();
        svc.Register("ARC-0101", "Zhang Family");
        svc.Register("ARC-0102", "Zhang Family");

        svc.Correct("ARC-0102", "Li Family");

        Assert.Equal("Li Family", svc.Find("ARC-0102")!.Name);
        Assert.Equal(1, svc.Find("ARC-0102")!.Round);
        Assert.Equal("Zhang Family", svc.Find("ARC-0101")!.Name);
        Assert.Equal(0, svc.Find("ARC-0101")!.Round);
    }

    // Correction target: a neighbour that already holds the corrected name is not edited
    [Fact]
    public void Correct_TwoEntriesShareName_OnlyRequestedEdited()
    {
        var svc = new ArchiveGovernanceService();
        svc.Register("ARC-0201", "Zhang Family");
        svc.Register("ARC-0202", "Zhang Family");
        svc.Register("ARC-0203", "Li Family");

        svc.Correct("ARC-0202", "Li Family");

        Assert.Equal("Li Family", svc.Find("ARC-0202")!.Name);
        Assert.Equal(1, svc.Find("ARC-0202")!.Round);
        Assert.Equal(0, svc.Find("ARC-0201")!.Round);
        Assert.Equal(0, svc.Find("ARC-0203")!.Round);
    }

    // Board freshness: after a correction the board shows the corrected name
    [Fact]
    public void Board_ReflectsLatestNameAfterCorrect()
    {
        var svc = new ArchiveGovernanceService();
        svc.Register("ARC-0301", "Zhang Family");
        svc.Register("ARC-0302", "Li Family");

        svc.Correct("ARC-0301", "Li Family");

        var board = svc.GetBoard();
        Assert.Equal("Li Family", board.Single(c => c.HoldsNumber("ARC-0301")).Name);
        Assert.DoesNotContain(board, c => c.Name == "Zhang Family");
    }

    // Board after opening: a fresh archive is matched by its card
    [Fact]
    public void Board_AfterRegister_ContainsMatchingCard()
    {
        var svc = new ArchiveGovernanceService();

        var archive = svc.Register("ARC-0701", "Chen Family");

        var board = svc.GetBoard();
        Assert.Single(board);
        Assert.Equal("ARC-0701", board[0].Number);
        Assert.Equal("Chen Family", board[0].Name);
        Assert.Equal(archive.Number, board[0].Number);
        Assert.Equal(archive.Name, board[0].Name);
    }

    // Reading: a number that was never opened comes back as none
    [Fact]
    public void Find_UnknownNumber_ReturnsNull()
    {
        var svc = new ArchiveGovernanceService();
        svc.Register("ARC-0801", "Zhou Family");

        Assert.Null(svc.Find("ARC-9999"));
    }

    // Reading: the ledger shows the corrected name after the correction
    [Fact]
    public void Find_AfterCorrect_ReturnsUpdated()
    {
        var svc = new ArchiveGovernanceService();
        svc.Register("ARC-0802", "Zhou Family");

        svc.Correct("ARC-0802", "Zhao Family");

        Assert.Equal("Zhao Family", svc.Find("ARC-0802")!.Name);
        Assert.Equal(1, svc.Find("ARC-0802")!.Round);
    }

    // Registration again and again: the same number never opens twice
    [Fact]
    public void RepeatedRegister_NoDuplicate()
    {
        var svc = new ArchiveGovernanceService();
        svc.Register("ARC-0901", "Wu Family");

        Assert.Throws<ArchiveAlreadyRegisteredException>(() => svc.Register("ARC-0901", "Wu Family"));
        Assert.Throws<ArchiveAlreadyRegisteredException>(() => svc.Register("ARC-0901", "Wu Family"));

        Assert.Equal(1, svc.GetRegisteredCount());
        Assert.Single(svc.GetBoard());
    }

    // Same name twice: two archives keep their own cards and do not cross
    [Fact]
    public void DuplicateName_DoesNotCollide()
    {
        var svc = new ArchiveGovernanceService();
        svc.Register("ARC-1001", "Wang Family");
        svc.Register("ARC-1002", "Wang Family");

        Assert.Equal(2, svc.GetRegisteredCount());
        Assert.NotNull(svc.Find("ARC-1001"));
        Assert.NotNull(svc.Find("ARC-1002"));

        var board = svc.GetBoard();
        Assert.Equal(2, board.Count);
        Assert.Contains(board, c => c.HoldsNumber("ARC-1001"));
        Assert.Contains(board, c => c.HoldsNumber("ARC-1002"));
    }

    // Counting: the desk reports every number that has been opened
    [Fact]
    public void GetRegisteredCount_CountsAllRegistrations()
    {
        var svc = new ArchiveGovernanceService();
        svc.Register("ARC-1201", "Zheng Family");
        svc.Register("ARC-1202", "Yang Family");
        svc.Register("ARC-1203", "Xie Family");

        Assert.Equal(3, svc.GetRegisteredCount());
        Assert.Equal(3, svc.GetBoard().Count);
    }

    // Error path: correcting a number that was never opened is turned away
    [Fact]
    public void Correct_UnknownNumber_Throws()
    {
        var svc = new ArchiveGovernanceService();
        svc.Register("ARC-1301", "Xu Family");

        Assert.Throws<ArchiveNotFoundException>(() => svc.Correct("ARC-9999", "Never Registered"));
    }

    // Error path: a number is needed before an archive is opened
    [Fact]
    public void Register_EmptyNumber_Throws()
    {
        var svc = new ArchiveGovernanceService();

        Assert.Throws<ArgumentException>(() => svc.Register("", "Some Family"));
        Assert.Throws<ArgumentException>(() => svc.Register(null!, "Some Family"));
    }

    // Error path: a household name is needed before an archive is opened
    [Fact]
    public void Register_EmptyName_Throws()
    {
        var svc = new ArchiveGovernanceService();

        Assert.Throws<ArgumentException>(() => svc.Register("ARC-1401", ""));
        Assert.Throws<ArgumentException>(() => svc.Register("ARC-1401", null!));
    }
}