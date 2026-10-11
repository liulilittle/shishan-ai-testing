# Shitcode-Style AI Problem Bank · Topics Detail

> 中文版: [docs/TOPICS.zh.md](./TOPICS.zh.md) · Chinese version: [docs/TOPICS.zh.md](./TOPICS.zh.md)

Each topic: business scenario · symptom · expected behavior · reproduction & verdict.

---

## D-001 · Shared Business Number, Progress Goes to the Wrong Document

- **Tier/Kind**: DIAMOND · Approval
- **Scenario**: A business number may be shared by several documents, each with its own apply number. Approval progress has both a list and a detail view.
- **Symptom**: Two documents use the same business number; querying one document's progress mixes the other document's content into the list and detail, and the progress does not match.
- **Expected**: No matter which document you query, the list and detail show only that document's own latest progress, nothing from another document.
- **Dimensions**:the query returns only the current document · the two views agree · the progress is the latest
- **Reproduce**: `dotnet test tests/Shishan.Tests.D001` → 4 failed / 6 passed
  - Steps: register two documents sharing one business number, advance them, then query one document via list and detail
  - Expected behavior: the list contains only the current document; both query views agree; after advancing, queries immediately show the latest state

## D-002 · Same Process on Multiple Orders, Execution Mixed Up

- **Tier/Kind**: DIAMOND · Order
- **Scenario**: The same process appears on several downstream orders, and each order's execution must be recorded and advanced separately. Several people often operate at the same time.
- **Symptom**: Two orders use the same process; after several people operate at the same time, execution records are attributed to the wrong order and duplicated, and the order summary disagrees with actual execution.
- **Expected**: Records belong only to their own order; an operation leaves exactly one correct record; the execution list and the order summary agree.
- **Dimensions**:records belong to their own order · one operation leaves one record · the list and the summary agree
- **Reproduce**: `dotnet test tests/Shishan.Tests.D002` → 5 failed / 5 passed
  - Steps: register the same process on two orders, have multiple people advance it, then query the execution list and order summary
  - Expected behavior: each order's records belong to it alone; one operation yields one record; list and summary agree

## D-003 · Rejected and Resubmitted, the Review Is Mixed Up

- **Tier/Kind**: DIAMOND · Review
- **Scenario**: A task goes through a two-stage review and may be rejected and resubmitted, starting a new review round.
- **Symptom**: A task without complete prerequisite materials can be submitted; after rejection, resubmission and several people reviewing at the same time, the stage and verdict get mixed up, and the detail still carries the previous round's conclusion.
- **Expected**: Only tasks with complete prerequisite materials can be submitted; each stage yields exactly one valid result; the detail shows only the current round.
- **Dimensions**:prerequisites complete before submit · each stage yields one result · the detail shows only the current round
- **Reproduce**: `dotnet test tests/Shishan.Tests.D003` → 4 failed / 6 passed
  - Steps: submit a task with incomplete materials; reject and resubmit; have several people review the same task; then open the detail
  - Expected behavior: incomplete tasks cannot be submitted; a resubmission starts a new round without the old conclusion; each stage yields one result

## D-004 · Posting and Reversing, the Ledger Is Mixed Up

- **Tier/Kind**: DIAMOND · Ledger
- **Scenario**: The ledger keeps a balance and flows per entry, and a posted flow can be reversed. Different entries may share a name; the number is what actually tells them apart.
- **Symptom**: After posting and reversing on two same-named but different-numbered entries, the balance, effective flows and flow list disagree, and the reversal may even hit another entry.
- **Expected**: A reversal affects only this entry's effective flows; one entry posted once with the same voucher leaves one effective flow; the balance always equals the net effective flow; the flow list shows only this entry's latest effective flows.
- **Dimensions**:reversal affects only this entry · one voucher leaves one flow · the balance equals the net flow · the list shows only the latest
- **Reproduce**: `dotnet test tests/Shishan.Tests.D004` → 6 failed / 5 passed
  - Steps: post to two same-named entries, reverse one of them, post the same voucher once more, then check the balance and flows
  - Expected behavior: reversal affects only this entry; the same voucher posts once; balance equals the net flow; the list shows the latest content

## D-005 · Filing and Correcting, Accounts Got Crossed

- **Tier/Kind**: DIAMOND · Archive
- **Scenario**: The archive governance line covers intake & filing, round-by-round correction and cleanup, plus a board showing each archive's current state. Archives may share a name; the number is the unique identifier.
- **Symptom**: Simultaneous filing produces duplicate archives; a correction lands on another same-named household; two places read different governance rounds; the board card is crossed and stale, and the old card still shows after cleanup.
- **Expected**: Under any circumstances a number is on record exactly once; a correction lands only on the specified number; the two round readings agree; board cards stay with their own archive, refresh promptly, and disappear after cleanup.
- **Dimensions**:one number means one archive · correction hits only the specified number · both round readings agree · the board is accurate and timely
- **Reproduce**: `dotnet test tests/Shishan.Tests.D005` → 6 failed / 9 passed
  - Steps: file the same number at the same time; correct another same-named household; clean up and check the board
  - Expected behavior: one number means one archive; correction hits only the specified number; both round readings agree; the board is accurate and timely, and the card is removed after cleanup

## K-001 · Stocktake Recheck, Three Numbers Disagree

- **Tier/Kind**: KING · Stocktake
- **Scenario**: A stocktake has four steps: open, recheck the count, settle the posting, and reconcile. Business keeps moving during the count. The measurement unit entered at recheck may differ from the ledger convention.
- **Symptom**: After recheck and settle, the ledger balance, recheck records and reconciliation summary disagree; several people rechecking at the same time also cross orders and drop quantity.
- **Expected**: A recheck affects only the corresponding row of this stocktake; if several people recheck the same order and the same entry, only one valid record remains; reported units convert to the ledger convention; settling reverses only the difference without losing normal movement; entries with no difference keep a settle trail; the reconciliation reflects only this order's latest valid state.
- **Dimensions**:recheck affects only this order row · one entry keeps one record · units convert to the ledger convention · difference-only reversal · the reconciliation reflects only this order
- **Reproduce**: `dotnet test tests/Shishan.Tests.K001` → 3 failed / 8 passed
  - Steps: open a stocktake, have several people recheck the same row using different units, then settle and reconcile
  - Expected behavior: recheck affects only this order's row; one row keeps one recheck record; units convert to the ledger convention; settling reverses only the difference; reconciliation agrees with the ledger

## K-002 · Print Gate, Counts Do Not Match

- **Tier/Kind**: KING · Print
- **Scenario**: There are four kinds of printable documents downstream. The first print skips the reason field; a reprint requires a reason. Every print increments a counter and leaves a ledger row with a print number, queryable by page.
- **Symptom**: After printing at the same time, the first-print no-reason window is breached, the print counter and ledger row count disagree, some rows lack a print number, and the print status leaks across document kinds.
- **Expected**: Each kind's first print has exactly one no-reason window, and simultaneous duplicate first prints pass only once; the print counter always equals the ledger row count; every row has a print number; the print status is derived per kind and reflects the latest action; the ledger covers all four kinds and pages correctly.
- **Dimensions**:each kind has one no-reason first-print window · count equals the ledger rows · every row has a print number · status independent per kind · the ledger is complete and pageable
- **Reproduce**: `dotnet test tests/Shishan.Tests.K002` → 4 failed / 6 passed
  - Steps: first-print and reprint each of the four kinds, have several people click first print at the same time, then page through the ledger
  - Expected behavior: each kind's first print has exactly one no-reason window; counter equals row count; every row has a print number; status is independent per kind; the ledger is complete and pageable

## K-003 · Standalone Maintenance, Changes Do Not Take Effect

- **Tier/Kind**: KING · Inspection
- **Scenario**: An inspection sheet is maintained both with the child order and via a standalone entry. After a maintenance is submitted, the content saved downstream must update accordingly, and the detail page reads that content.
- **Symptom**: After standalone maintenance, the change lands on another sheet; the downstream content never updates; the detail replays the pre-maintenance state and still contains rows voided in the previous round.
- **Expected**: Standalone maintenance affects only the declared sheet; after each maintenance, the content saved equals exactly what was submitted; the downstream content reliably updates to the latest; the detail always reflects the latest content; the header update time advances along with the write.
- **Dimensions**:maintenance affects only the declared sheet · the saved content is exactly the submission · the downstream updates promptly · the detail shows the latest · the header time advances
- **Reproduce**: `dotnet test tests/Shishan.Tests.K003` → 9 failed / 4 passed
  - Steps: perform standalone maintenance on one inspection sheet and submit, then open the detail and the downstream content
  - Expected behavior: maintenance affects only the declared sheet; the saved content is exactly the submitted content; downstream updates to the latest; the detail shows the latest; the header time advances

## K-004 · Four-Stage Approval, Results Mixed Up

- **Tier/Kind**: KING · Approval
- **Scenario**: A document goes through four approval stages; the middle two can be handled at the same time and the last one closes the flow. Stage-by-stage rejection and re-walk, and whole-document voiding, are supported. A board lets everyone query the current stage and step.
- **Symptom**: After simultaneous submission, rejection & re-walk and voiding, approval results are lost or duplicated, the flow stalls or skips ahead, voided documents keep advancing, and the board shows a stage that disagrees with the real state.
- **Expected**: The four stages advance uniquely and correctly under any circumstances; a downstream stage must not be stamped before a rejected upstream stage is re-walked; rejection clears downstream residue; voided documents advance no further; the board always reflects the current real stage.
- **Dimensions**:the flow advances uniquely · no downstream stamp before the upstream is re-walked · rejection clears the residue · voiding stops the flow · the board reflects the real stage
- **Reproduce**: `dotnet test tests/Shishan.Tests.K004` → 6 failed / 11 passed
  - Steps: have several people submit the same action at the same time; then reject and re-walk stage by stage and void a whole document; check the board
  - Expected behavior: each step yields one result; no downstream stamp before the upstream is re-walked; rejection clears the downstream; voiding stops the flow; the board reflects the real stage

## K-005 · One Filing, Two Documents

- **Tier/Kind**: KING · Filing
- **Scenario**: After an upstream completes, a cross-module document is built automatically and downstream orders are derived from it. Two entries can trigger the filing. The upstream keeps changing after filing, and a refresh must reflect those changes to the document and the print.
- **Symptom**: One filing yields two documents and two downstream orders; after changing the upstream, a refresh does not update the document and print; the recorded operator is empty; the order quantity differs between two places.
- **Expected**: A request ends with exactly one document and one downstream order; two entries triggering at the same time build only one; after filing, upstream changes are reflected truthfully to the document and print through a refresh; the recorded operator is not empty; the two places both show the real quantity.
- **Dimensions**:one request one document · two entries build only one · the refresh reflects the truth · the recorded operator is present · both quantities agree
- **Reproduce**: `dotnet test tests/Shishan.Tests.K005` → 5 failed / 10 passed
  - Steps: trigger filing from both entries at the same time; then change the upstream and refresh; check the document, print and downstream orders
  - Expected behavior: one request yields one document and one downstream order; simultaneous triggering from two entries builds only one; refresh reflects the changes; the recorded operator is present; both quantities agree

---