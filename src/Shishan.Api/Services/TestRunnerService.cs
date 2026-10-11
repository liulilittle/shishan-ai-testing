using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Shishan.Api.Services;

public sealed record TestCaseResult(string Name, bool Passed, string? Message, string? Error);

public sealed record VerifyResult(
    string TopicCode,
    string TopicTitle,
    int Total,
    int Passed,
    int Failed,
    List<TestCaseResult> Cases,
    bool AllGreen,
    string Outcome);

public sealed record StreamOutcome(VerifyResult? Result, string? Error);

/// <summary>Pure, deterministic outcome calculation for the shot animation.</summary>
public static class OutcomeCalculator
{
    public static string Compute(int total, int passed, int failed, IReadOnlyList<TestCaseResult> cases)
    {
        if (total > 0 && failed == 0 && passed == total)
            return "goal";

        double failRatio = total > 0 ? (double)failed / total : 1.0;

        var failedNames = cases
            .Where(c => !c.Passed)
            .Select(c => c.Name)
            .OrderBy(n => n, StringComparer.Ordinal);

        uint signature = Fnv1a(string.Join("\n", failedNames));
        int pick = (int)(signature % 3);

        if (failRatio <= 0.34)
        {
            return pick switch
            {
                0 => "postLeft",
                1 => "postRight",
                _ => "crossbar",
            };
        }

        return pick switch
        {
            0 => "wideLeft",
            1 => "wideRight",
            _ => "overTop",
        };
    }

    private static uint Fnv1a(string value)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;
        uint hash = offsetBasis;
        foreach (char ch in value)
        {
            hash ^= (byte)(ch & 0xFF);
            hash *= prime;
            hash ^= (byte)((ch >> 8) & 0xFF);
            hash *= prime;
        }
        return hash;
    }
}

public sealed class TestRunnerService
{
    private const string NoResultsMsg = "No test results were produced (the build may have failed).";
    private const string BuildFailedMsg = "Build failed; no test results were produced.";

    private static readonly Regex CaseLineRegex =
        new(@"^\s*(Passed|Failed|Skipped)\s+(.+?)\s*\[", RegexOptions.Compiled);

    private static readonly Regex ListedTestRegex =
        new(@"^\s{3,}(\S.*)$", RegexOptions.Compiled);

    private readonly string _root;

    public TestRunnerService(IWebHostEnvironment env)
    {
        _root = env.ContentRootPath;
    }

    private static string ProjFileName(string code) => $"Shishan.Tests.{code}.csproj";

    private string ResolveTestProject(string code) =>
        Path.Combine(_root, "..", "..", "tests", $"Shishan.Tests.{code}");

    private static string NormalizeCode(string topicCode) =>
        topicCode.ToUpperInvariant().Replace("-", "");

    // ------------------------------------------------------------------
    // One-shot (POST /verify) — reuse the per-case enumerator, aggregate.
    // ------------------------------------------------------------------
    public VerifyResult Run(string topicCode)
    {
        string code = NormalizeCode(topicCode);
        string testProject = ResolveTestProject(code);
        if (!Directory.Exists(testProject))
            throw new InvalidOperationException($"Test project not found: {code}");

        var outcome = StreamCore(code, testProject, _ => { }, CancellationToken.None);
        if (outcome.Error is not null)
            throw new InvalidOperationException(outcome.Error);
        return outcome.Result!;
    }

    // ------------------------------------------------------------------
    // Streaming (GET /verify/stream) — summary aggregated from per-case results.
    // ------------------------------------------------------------------
    public async Task<StreamOutcome> StreamAsync(string topicCode, Action<TestCaseResult> onCase, CancellationToken ct)
    {
        string code = NormalizeCode(topicCode);
        string testProject = ResolveTestProject(code);
        if (!Directory.Exists(testProject))
            return new StreamOutcome(null, $"Test project not found: {code}");

        return await Task.Run(() => StreamCore(code, testProject, onCase, ct), ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds once, lists the test cases, then runs each case in its own <c>dotnet test</c>
    /// invocation so <paramref name="onCase"/> fires as soon as that case finishes (observable
    /// per-case gaps). The summary is aggregated from the per-case results, never from a
    /// single-case TRX (each per-case run rewrites its own unique TRX).
    /// </summary>
    private StreamOutcome StreamCore(string code, string testProject, Action<TestCaseResult> onCase, CancellationToken ct)
    {
        string projFile = Path.Combine(testProject, ProjFileName(code));
        if (!File.Exists(projFile))
            return new StreamOutcome(null, $"Test project file not found: {projFile}");

        // 1) Build once (restore on the cold path).
        var build = RunProc(testProject, ct, "build", projFile, "-c", "Debug", "-v", "quiet");
        if (build.TimedOut)
            return new StreamOutcome(null, "Build timed out after 3 minutes.");
        if (build.Cancelled)
            return new StreamOutcome(null, null);
        if (build.ExitCode != 0 && build.Output.Contains("error ", StringComparison.OrdinalIgnoreCase))
            return new StreamOutcome(null, BuildFailedMsg);

        // 2) List the test cases (no build: the DLL already exists).
        var list = RunProc(testProject, ct, "test", projFile, "--no-build", "--list-tests", "-v", "quiet");
        if (list.TimedOut)
            return new StreamOutcome(null, "Test discovery timed out after 3 minutes.");
        if (list.Cancelled)
            return new StreamOutcome(null, null);

        var names = ParseListedTests(list.Output);
        if (names.Count == 0)
            return new StreamOutcome(null, NoResultsMsg);

        // 3) Run each case in its own invocation; emit as soon as each completes.
        string trxDir = NewTrxDir(code);
        var cases = new List<TestCaseResult>(names.Count);
        for (int idx = 0; idx < names.Count; idx++)
        {
            string name = names[idx];
            if (ct.IsCancellationRequested)
                return new StreamOutcome(null, null);

            var run = RunProc(testProject, ct, "test", projFile, "--no-build", "-v", "quiet",
                "--filter", $"FullyQualifiedName={name}",
                "--logger", $"trx;LogFileName=result_{idx:D3}.trx",
                "--results-directory", trxDir);

            TestCaseResult rec;
            if (run.TimedOut)
            {
                rec = new TestCaseResult(name, false, "Timed out after 3 minutes.", null);
            }
            else if (run.Cancelled)
            {
                return new StreamOutcome(null, null);
            }
            else
            {
                rec = SingleResult(name, run);
            }

            cases.Add(rec);
            try { onCase(rec); } catch { /* client gone; keep going to settle the summary */ }
        }

        if (cases.Count == 0)
            return new StreamOutcome(null, NoResultsMsg);

        // Enrich per-case detail from each run's own TRX when available.
        MergeTrxDetails(cases, trxDir);

        var (total, passed, failed) = Tally(cases);
        bool allGreen = total > 0 && failed == 0 && passed == total;
        return new StreamOutcome(
            new VerifyResult(code, "", total, passed, failed, cases, allGreen,
                OutcomeCalculator.Compute(total, passed, failed, cases)),
            null);
    }

    // ------------------------------------------------------------------
    // Per-case result: one dotnet test run for exactly one case.
    // ------------------------------------------------------------------
    private TestCaseResult SingleResult(string name, ProcResult run)
    {
        string consoleName = ShortName(name);
        foreach (var line in EnumerateLines(run.Output))
        {
            var m = CaseLineRegex.Match(line);
            if (!m.Success) continue;
            if (!NamesMatch(consoleName, m.Groups[2].Value.Trim())) continue;
            return new TestCaseResult(name, m.Groups[1].Value.Equals("Passed", StringComparison.OrdinalIgnoreCase), null, null);
        }

        // Fallback: parse the console summary line ("Failed! ... Passed: N").
        if (TryParseSummary(run.Output, out _, out int passed, out int failed) || failed > 0 || passed > 0)
        {
            if (failed == 0 && passed >= 1)
                return new TestCaseResult(name, true, null, null);
            if (failed >= 1)
                return new TestCaseResult(name, false, null, null);
        }

        return new TestCaseResult(name, run.ExitCode == 0, null, null);
    }

    private static IEnumerable<string> EnumerateLines(string s)
    {
        using var reader = new StringReader(s);
        string? line;
        while ((line = reader.ReadLine()) is not null)
            yield return line;
    }

    private static bool NamesMatch(string a, string b) =>
        a.Equals(b, StringComparison.Ordinal)
        || a.EndsWith("." + b, StringComparison.Ordinal)
        || b.EndsWith("." + a, StringComparison.Ordinal);

    private static string ShortName(string fullyQualified) =>
        fullyQualified.Split('.').LastOrDefault() ?? fullyQualified;

    // ------------------------------------------------------------------
    // Process helpers
    // ------------------------------------------------------------------
    private static ProcResult RunProc(string workingDir, CancellationToken ct, params string[] args)
    {
        var psi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (string a in args)
            psi.ArgumentList.Add(a);
        // Force English UI so result lines are parseable regardless of host locale.
        psi.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
        psi.Environment["VSLANG"] = "1033";
        psi.Environment["DOTNET_NOLOGO"] = "1";

        using var p = new Process { StartInfo = psi };
        var stdout = new System.Text.StringBuilder();
        var stderr = new System.Text.StringBuilder();
        p.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        if (!p.Start())
            return new ProcResult(1, "", "Failed to start dotnet");

        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        bool canceled = false, timedOut = false;
        try
        {
            if (!p.WaitForExit(180000))
            {
                timedOut = true;
                try { p.Kill(entireProcessTree: true); } catch { }
                p.WaitForExit(5000);
            }
        }
        catch (Exception)
        {
            // Process died on its own; fall through to settle on whatever we captured.
        }

        if (ct.IsCancellationRequested && timedOut == false && p.HasExited == false)
            canceled = true;

        try { p.WaitForExit(2000); } catch { }

        string combined = stdout.ToString() + "\n" + stderr.ToString();
        return new ProcResult(p.HasExited ? p.ExitCode : 1, combined, null) { TimedOut = timedOut, Cancelled = canceled };
    }

    private sealed record ProcResult(int ExitCode, string Output, string? Error)
    {
        public bool TimedOut { get; init; }
        public bool Cancelled { get; init; }
    }

    // ------------------------------------------------------------------
    // Listing / TRX / console parsing
    // ------------------------------------------------------------------
    private static List<string> ParseListedTests(string output)
    {
        var names = new List<string>();
        bool inList = false;
        foreach (string line in EnumerateLines(output))
        {
            if (!inList)
            {
                if (line.Contains("The following Tests are available", StringComparison.OrdinalIgnoreCase))
                    inList = true;
                continue;
            }

            if (line.Length == 0) continue;
            var m = ListedTestRegex.Match(line);
            if (!m.Success)
            {
                // A line that is not indented ends the list block.
                if (line.Length > 0 && !char.IsWhiteSpace(line[0]))
                    break;
                continue;
            }

            string name = m.Groups[1].Value.Trim();
            if (name.Length == 0) continue;
            if (name.Contains('(') || name.Contains("Total tests") || name.StartsWith("Test run", StringComparison.OrdinalIgnoreCase))
                continue; // Theory rows and trailers.
            names.Add(name);
        }
        return names;
    }

    private static VerifyResult ParseTrx(string code, string trxPath)
    {
        XDocument doc = XDocument.Load(trxPath);
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

        var cases = new List<TestCaseResult>();
        var counters = doc.Descendants(ns + "Counters").FirstOrDefault();
        int total = counters != null ? (int?)counters.Attribute("total") ?? 0 : 0;
        int passed = counters != null ? (int?)counters.Attribute("passed") ?? 0 : 0;
        int failed = counters != null ? (int?)counters.Attribute("failed") ?? 0 : 0;

        foreach (var tr in doc.Descendants(ns + "UnitTestResult"))
        {
            string name = (string?)tr.Attribute("testName") ?? "";
            string outcome = (string?)tr.Attribute("outcome") ?? "Failed";
            bool ok = outcome.Equals("Passed", StringComparison.OrdinalIgnoreCase);

            string? msg = null, err = null;
            var msgEl = tr.Descendants(ns + "Message").FirstOrDefault();
            var errEl = tr.Descendants(ns + "StackTrace").FirstOrDefault();
            if (msgEl != null) msg = msgEl.Value.Trim();
            if (errEl != null) err = errEl.Value.Trim();

            cases.Add(new TestCaseResult(name, ok, msg, err));
        }

        bool allGreen = total > 0 && failed == 0 && passed == total;
        return new VerifyResult(code, "", total, passed, failed, cases, allGreen,
            OutcomeCalculator.Compute(total, passed, failed, cases));
    }

    private static void MergeTrxDetails(List<TestCaseResult> cases, string trxDir)
    {
        if (!Directory.Exists(trxDir)) return;

        var byName = new Dictionary<string, TestCaseResult>(StringComparer.Ordinal);
        foreach (string file in Directory.GetFiles(trxDir, "result_*.trx"))
        {
            try
            {
                var trx = ParseTrx("", file);
                foreach (var c in trx.Cases)
                    byName[c.Name] = c;
            }
            catch
            {
                // Unparseable TRX: keep the console-derived record.
            }
        }

        if (byName.Count == 0) return;
        for (int i = 0; i < cases.Count; i++)
        {
            if (byName.TryGetValue(cases[i].Name, out var rich))
                cases[i] = rich with { Passed = cases[i].Passed };
        }
    }

    private static bool TryParseSummary(string output, out int total, out int passed, out int failed)
    {
        total = MatchCount(output, @"总计[:：]\s*(\d+)", @"Total:\s*(\d+)");
        passed = MatchCount(output, @"通过[:：]\s*(\d+)", @"Passed:\s*(\d+)");
        failed = MatchCount(output, @"失败[:：]\s*(\d+)", @"Failed:\s*(\d+)");
        return total > 0 || passed > 0 || failed > 0;
    }

    private static int MatchCount(string output, string cnPattern, string enPattern)
    {
        foreach (string pattern in new[] { cnPattern, enPattern })
        {
            var m = Regex.Match(output, pattern);
            if (m.Success)
                return int.Parse(m.Groups[1].Value);
        }
        return 0;
    }

    private static (int total, int passed, int failed) Tally(List<TestCaseResult> cases)
    {
        int passed = cases.Count(c => c.Passed);
        return (cases.Count, passed, cases.Count - passed);
    }

    private static string NewTrxDir(string code)
    {
        string dir = Path.Combine(Path.GetTempPath(), $"shishan_trx_{code}_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
