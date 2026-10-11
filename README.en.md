# Shitcode-Style AI BUG-Fixing Problem Bank

> 中文版见 [README.md](./README.md) · Chinese version: [README.md](./README.md)

An AI problem bank for **testing whether an AI can actually fix real-world business BUGs**. Each problem is a "shit mountain" implementation of a real business chain: written like normal production code, but with reproducible defects deliberately planted. The AI's job is to locate and fix these BUGs; success is decided by **unit tests (expected behavior)**.

- Tiers: DIAMOND (D-001~D-005), KING (K-001~K-005), 10 problems total
- Frontend: ASP.NET Core MVC verification arena — click "Verify" to run that problem's tests
  - All green = GOAL ⚽
  - Partial = hit the woodwork
  - All red = ball missed (not yet fixed)

## Layout

```
F:\shishan\
├── Shishan.sln
├── src/
│   └── Shishan.Api/             # ASP.NET Core MVC main project (topic pages + verify arena + data API)
├── topics/                      # Problem projects (one per problem, contain BUGs, normal code style)
│   ├── Shishan.Domain.D001/ ... # D001~D005, K001~K005
├── tests/                       # Judgment tests (one per problem, encode expected behavior)
│   ├── Shishan.Tests.D001/ ...
├── solutions/                   # Answer area (correct implementation per problem, kept separate)
│   ├── Shishan.Solution.D001/ ...
└── docs/                        # Topic docs (bilingual, cross-indexed)
```

**Answers are kept separate from problems**: `topics/` is the problem project an AI must fix (contains BUGs), `solutions/` holds the correct implementation (reference answer), `tests/` is the judgment baseline. The AI may only change `topics/`; tests decide whether the fix is correct.

## Problem List

| ID | Tier | Kind | Title | Symptom (summary) |
|---|---|---|---|---|
| D-001 | DIAMOND | Approval | Shared business number, progress goes to the wrong document | Two docs share a business number; querying one mixes in the other's progress |
| D-002 | DIAMOND | Order | Same process on multiple orders, execution mixed up | Two orders share a process; simultaneous operation mis-owns/duplicates records; summary disagrees |
| D-003 | DIAMOND | Review | Rejected and resubmitted, the review is mixed up | Incomplete tasks submit; several people reviewing at once scramble stage & verdict |
| D-004 | DIAMOND | Ledger | Posting and reversing, the ledger is mixed up | Same-name entries reverse the wrong one; balance, effective flows and list disagree |
| D-005 | DIAMOND | Archive | Filing and correcting, accounts got crossed | Simultaneous filing duplicates; correction hits a namesake; board is stale/crossed |
| K-001 | KING | Stocktake | Recheck, three numbers disagree | Ledger balance, recheck records and reconciliation disagree; rechecking at once crosses/drops |
| K-002 | KING | Print | Print gate, counts do not match | First-print no-reason window breached; counter vs ledger rows disagree; some rows lack print number |
| K-003 | KING | Inspection | Standalone maintenance, changes do not take effect | Standalone edit hits the wrong sheet; downstream never updates; detail replays old content |
| K-004 | KING | Approval | Four-stage approval, results mixed up | Simultaneous submit/reject/void loses or duplicates results, skips stages, distorts board |
| K-005 | KING | Filing | One filing, two documents | One filing yields two documents and two orders; refresh stale; recorded operator empty |

Full docs per problem (scenario / symptom / expected / dimensions / reproduction) live under [docs/](./docs/) in both languages.

## Quick Start

```bash
# 1. Build the solution
dotnet build Shishan.sln

# 2. Start the verification arena
dotnet run --project src/Shishan.Api --urls http://localhost:5088

# 3. Open http://localhost:5088/ to browse topics; click "Verify" to run tests
```

## Judgment Loop (how a correct AI fix is decided)

1. **Initial state**: `tests/` are written purely against expected behavior; `topics/` contains BUGs → tests are red.
2. **AI fix**: only edit domain code under `topics/Shishan.Domain.XXX/`.
3. **Decision**: `dotnet test tests/Shishan.Tests.XXX` — all green = fixed; any red = not fixed.

Every test project currently references the `topics/` problem version; the answer version lives in `solutions/` and can be swapped in to prove each problem is solvable (guaranteed all-green on the answer version).

## API

| Method | Path | Description |
|---|---|---|
| GET | `/api/topics` | All topic metadata |
| GET | `/api/topics/{code}` | Single topic detail |
| POST | `/api/topics/{code}/verify` | Run that topic's tests, return per-case pass/fail |

## License & Disclaimer

This repository is a self-made AI problem bank (format inspired by the like-named tournament). It exists only to test an AI's ability to fix BUGs; it is not official content, carries no copyright-attribution claim, and its results are for testing reference only.
