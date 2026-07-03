# SearchComparisonNet — Whole-Solution Review

**Date:** 2025-11-12
**Scope:** Entire solution (Kernel, GUI, Tests) — read-only review, no code changed.
**Mode requested:** Read-only review first; conservative (behavior-preserving) refactor after approval.
**Build/test baseline:** Solution builds 0 warnings / 0 errors on .NET 10; test suite reports 50 passing.

---

## How to read this report

Each finding is tagged with a **severity** and an **action class**:

- **Severity:** 🔴 Critical · 🟠 High · 🟡 Medium · ⚪ Low
- **Action class:**
  - **[Safe]** — Conservative, behavior-preserving. Eligible to apply now under your "conservative" constraint.
  - **[Approval]** — Requires a behavior change or a larger refactor. **Reported & recommended only** — will NOT be applied unless you explicitly opt in per-item.

> ⚠️ **Important context for the whole report:** the test suite is **not** the safety net it appears to be — see [T-1](#t-1). Several "Critical" items are genuine bugs that the current tests cannot catch.

---

## Executive summary

The solution is small, readable, and cleanly organized (Kernel / GUI-MVVM / Tests). The .NET 10 + CPM upgrade is solid. However, the review surfaced a cluster of **correctness bugs** that are masked by **duplicated, ineffective tests**, plus a number of conservative cleanup opportunities.

**Top issues (most impactful first):**

| # | Severity | Area | Issue | Class |
|---|----------|------|-------|-------|
| [T-1](#t-1) | 🔴 | Tests | `BinarySearchTests` is a byte-for-byte copy of `LinearSearchTests` — **binary search is completely untested** | [Approval] |
| [K-1](#k-1) | 🔴 | Kernel | `LinearSearch` and `BinarySearch` end up searching **different datasets** | [Approval] |
| [G-1](#g-1) | 🔴 | GUI | Binary-search simulation draws random values from `LinearSearch` but searches `BinarySearch` over a different array | [Approval] |
| [G-2](#g-2) | 🟠 | GUI | UI-bound properties mutated from a background thread (`Task.Factory.StartNew`) | [Approval] |
| [G-3](#g-3) | 🟠 | GUI | `async void Simulate()` — unobserved exceptions crash the app | [Approval] |
| [G-4](#g-4) | 🟠 | GUI | DI container is fully configured but never used — `MainViewModel` is `new`'d | [Approval] |
| [G-5](#g-5) | 🟠 | GUI | `CancelCommand` is wired to a no-op `Cancel()` — button does nothing | [Approval] |
| [K-2](#k-2) | ✅ | Kernel | `SearchBase.NoOfEntries` has a public setter that can desync from `Data` | [Approval] |
| [K-3](#k-3) | 🟡 | Kernel | `ProblemConstants` literals look wrong (`100_00`, `5_00_000`) | [Approval] |

**Conservative cleanups available now** (full list in [§4](#4-conservative-refactor-worklist-safe)): file rename to match type, dead code/fields, modern C# idioms, converter simplification, namespace style consistency, and unused-validation removal.

---

## 1. Kernel (`SearchComparisonNet.Kernel`)

### <a id="k-1"></a>K-1 🔴 [Approval] — Linear and Binary search operate on different data
**Files:** `Models/SearchBase.cs`, `Models/LinearSearch.cs`, `Models/BinarySearch.cs`

`SearchBase` captures `Data = dataGen.Data` in its constructor. `BinarySearch` uses that as-is. But `LinearSearch`'s constructor **overwrites** it:

```csharp
public LinearSearch(IDataGenerator dataGen) : base(dataGen)
{
	NoOfEntries = dataGen.NoOfEntries;
	Data = dataGen.GenerateData();   // ← regenerates a brand-new random array
}
```

`DataGenerator.GenerateData()` produces a **new random set each call**. So even when both searches are built from the *same* `IDataGenerator`, `LinearSearch` and `BinarySearch` are searching **different arrays**. Any comparison of their results (iteration counts, found indices) is comparing apples to oranges. This is the root cause behind [G-1](#g-1).

**Recommendation:** Both searches should share the single generated dataset (`dataGen.Data`). Remove the regeneration in `LinearSearch`. *(Behavior change → approval.)*

---

### <a id="k-2"></a>K-2 🟡 [Approval] — `NoOfEntries` public setter can desync from `Data`
**File:** `Models/SearchBase.cs`

> **✅ Resolved** (PR #18, `fix/noofentries-encapsulation-k2`): `NoOfEntries` is now derived and read-only — `public int NoOfEntries => Data.Length` — and the setter was removed from `ISearch`, so it can never desync from the dataset. Production `DataGenerator` and the test `FakeDataGenerator` both keep `NoOfEntries == Data.Length`, so behavior was preserved. The finding below describes the original (pre-fix) state.

```csharp
public int NoOfEntries { get; set; }   // settable
protected ObservableCollection<int> Data;
```

`NoOfEntries` is used as the upper bound for indexing and iteration, but it's independently settable and not tied to `Data.Count`. If anything sets `NoOfEntries` out of step with `Data` (and `MainViewModel` does assign `NoOfEntries` from user input), indexing logic can read past the data or stop short.

**Recommendation:** Derive `NoOfEntries` from `Data.Count` (read-only), or make the setter private and keep them in lockstep. *(Behavior/API change → approval.)*

---

### <a id="k-3"></a>K-3 🟡 [Approval] — Suspicious numeric literals in `ProblemConstants`
**File:** `Models/ProblemConstants.cs`

```csharp
public static int MinNoOfEntries  => 100_00;     // = 10,000  — typo for 10_000 or 100_000?
public static int MaxNoOfSearches => 5_00_000;   // = 500,000 — odd digit grouping
```

`100_00` evaluates to `10000`. The inconsistent grouping (vs `500_000`, `5_000`, etc.) strongly suggests a typo. The **value** drives validation ranges, so I will **not** change it under "conservative" — only flag it.

**Recommendation:** Confirm intended values; then normalize digit separators. *(Value change → approval. Pure reformatting that preserves the value is [Safe] — see [§4](#4).)*

---

### <a id="k-4"></a>K-4 🟡 [Safe] — `ISearch.cs` defines `ISearchItem` (file name ≠ type)
**File:** `Interfaces/ISearch.cs`

The file is named `ISearch.cs` but contains `interface ISearchItem` (and there is no `ISearch`). Confusing for navigation.

**Recommendation:** Rename the file to `ISearchItem.cs`. *(Pure rename, no behavior change → safe.)*

---

### <a id="k-5"></a>K-5 ⚪ [Approval] — `NextRandomNo` lives awkwardly on the search type
**Files:** `Models/SearchBase.cs`, `Interfaces/IDataGenerator.cs`

> **✅ Resolved** (PR #19, `refactor/nextrandomno-to-generator-k5`): `NextRandomNo` was removed from `ISearch`/`SearchBase` and surfaced on `ISearchComparison`, wired in `SearchComparisonFactory` from the single shared `DataGenerator`. `MainViewModel` now draws probe values from the comparison instead of a search instance, decoupling random generation from a specific search. The finding below describes the original state.

`SearchBase` exposes `Func<int> NextRandomNo` (copied from the generator). The GUI then calls `LinearSearch.NextRandomNo()` to drive **both** simulations — coupling random-value generation to a specific search instance and contributing to [G-1](#g-1). Random generation is a `DataGenerator` concern.

**Recommendation:** Draw random probe values from the generator, not from a search instance. *(Behavior/structure change → approval.)*

---

### <a id="k-6"></a>K-6 ⚪ [Safe] — Minor modernization opportunities
**Files:** various Kernel models

- `IndexOutOfRangeError` message uses `{0}` literally (not a format placeholder) — reads oddly: `"[{0}, 9999]"`. Likely intended to be `0`. *(Message text only — confirm, then [Safe].)*
- `0 > index || index > NoOfEntries - 1` → clearer as `index < 0 || index >= NoOfEntries`.
- `GenerateData()` could use a collection expression / simplified construction.
- **`BinarySearch` recursion → iteration** (`chore/kernel-and-converter-polish`): `FindItemWithBinarySearch` was tail-recursive; converting it to an iterative `while (low <= high)` loop removes per-level call overhead and makes it structurally parallel to `LinearSearch`, while preserving the `NoOfIterations` counting semantics. *(Behavior-preserving, guarded by existing search tests → [Safe].)*
- **Overflow-safe midpoint** (`chore/kernel-and-converter-polish`): `(low + high) / 2` replaced with `low + (high - low) / 2` to avoid the classic integer-overflow midpoint bug. *(Defensive one-liner → [Safe].)*

---

## 2. GUI (`SearchComparisonNet.GUI`)

### <a id="g-1"></a>G-1 🔴 [Approval] — Binary simulation uses linear's RNG over different data
**File:** `ViewModels/MainViewModel.cs` (`SimulateBinarySearchAsync`)

```csharp
var value = LinearSearch.NextRandomNo();   // ← random source = LinearSearch
var searchItem = BinarySearch.FindItem(value);
```

Combined with [K-1](#k-1), the binary loop searches `BinarySearch`'s array for values produced by `LinearSearch`'s generator, while `LinearSearch` itself searches a *third* (its own regenerated) array. The "comparison" the whole app exists to make is not apples-to-apples.

**Recommendation:** Single shared dataset + single random source for both loops. *(Behavior change → approval; pairs with [K-1](#k-1)/[K-5](#k-5).)*

---

### <a id="g-2"></a>G-2 🟠 [Approval] — Cross-thread mutation of UI-bound state
> **✅ Applied** (branch `gui-async-threading-fixes`, PR into `main`): simulation loops moved to `Task.Run`; progress now flows through an `IProgress<double>` created on the UI thread, and `ProgressBarVisibility`/`ProgressBarValue` are toggled/reset on the UI thread in `SimulateAsync`'s `try/finally`. No UI-bound property is mutated off-thread. See [§7](#7).

**File:** `ViewModels/MainViewModel.cs`

`SimulateLinearSearchAsync` / `SimulateBinarySearchAsync` run on a background thread via `Task.Factory.StartNew`, yet set UI-bound properties from inside the loop:

```csharp
ProgressBarVisibility = Visibility.Visible;   // raises PropertyChanged off the UI thread
ProgressBarValue = (j + 1) * 100.0 / NoOfSearches;
```

WPF binding updates from a non-UI thread are not guaranteed safe and can throw or misbehave. It happens to "work" today but is fragile.

**Recommendation:** Marshal progress updates to the dispatcher, or use `IProgress<T>`/`async`+`Task.Run` with progress reporting. *(Behavior change → approval.)*

---

### <a id="g-3"></a>G-3 🟠 [Approval] — `async void Simulate()`
> **✅ Applied** (branch `gui-async-threading-fixes`, PR into `main`): `Simulate` is now `async Task SimulateAsync(CancellationToken)` driven by an `AsyncRelayCommand`, so faults are observable instead of tearing down the app. See [§7](#7).

**File:** `ViewModels/MainViewModel.cs`

```csharp
private async void Simulate() { ... }
```

`async void` means exceptions can't be awaited/caught by the caller — an unhandled exception in simulation will tear down the app. Command handlers are the classic `async void` trap.

**Recommendation:** Use an async-capable command (e.g., `AsyncRelayCommand` from CommunityToolkit.Mvvm, already referenced) returning `Task`. *(Behavior change → approval.)*

---

### <a id="g-4"></a>G-4 🟠 [Approval] — DI configured but unused
> **✅ Resolved** (verified during the `fix/haserrors-g7` backlog reconciliation): DI is now wired and used. `ServiceCollectionExtensions.AddSearchComparisonServices` registers `ISearchComparisonFactory`, `MainViewModel`, and `MainView`; `App.OnStartup` resolves `MainView` via `GetRequiredService<MainView>()`; and `MainView` receives a constructor-injected `MainViewModel` (`DataContext = mainViewModel`). The finding below describes the original (pre-fix) state.

**File:** `App.xaml.cs`

The container registers `DataParameters`, `IDataGenerator`, `LinearSearch`, `BinarySearch`, `MainViewModel`, `MainView`… but `MainViewModel`'s constructor is **parameterless** and `new`s up its own `DataGenerator`/searches inside `Simulate()`. The view almost certainly sets `DataContext = new MainViewModel()` (XAML/code), so the entire DI graph is dead weight. Also: `DataParameters` has **no parameterless constructor**, so `GetService<DataParameters>()` would fail if it were ever resolved.

**Recommendation:** Either resolve `MainViewModel` from the container (constructor-inject its dependencies) or remove the unused DI registrations. *(Behavior/structure change → approval.)*

---

### <a id="g-5"></a>G-5 🟠 [Approval] — Cancel command does nothing
> **✅ Applied** (branch `gui-async-threading-fixes`, PR into `main`): `Cancel()` now calls `SimulateCommand.Cancel()`; the token is checked each loop iteration via `ThrowIfCancellationRequested()`, and `OperationCanceledException` is handled gracefully. See [§7](#7).

**File:** `ViewModels/MainViewModel.cs`

```csharp
public RelayCommand CancelCommand { get; set; }
...
private void Cancel() { }   // empty
```

`CanCancel()` enables the button during simulation, but clicking it has no effect. Misleading UX.

**Recommendation:** Wire cancellation via `CancellationTokenSource` threaded into the simulation loops, or remove the command if cancellation isn't intended. *(Behavior change → approval.)*

---

### <a id="g-6"></a>G-6 🟡 [Approval] — Declared product-range validation is never enforced
> **✅ Resolved** (verified during the `fix/haserrors-g7` backlog reconciliation): the dead members were removed — `MinProductValue`, `MaxProductValue`, and `MaxProductError` no longer exist anywhere in the GUI, so there is no unenforced declared rule left. The finding below describes the original state.

**File:** `ViewModels/MainViewModel.cs` + `ViewModels/InputValidation.cs`

`MainViewModel` declares `MinProductValue`, `MaxProductValue`, and `MaxProductError` ("Product of No. of searches and No. of entries must be in the interval…"), but `InputValidation` has **no rule** referencing them. The product constraint the code clearly intends is unenforced; the fields/message are dead.

**Recommendation:** Add the cross-field rule in `InputValidation`, or remove the dead fields/message. *(Adding a rule = behavior change → approval; removing dead members = [Safe], see [§4](#4).)*

---

### <a id="g-7"></a>G-7 🟡 [Approval] — `HasErrors` swallows exceptions and returns `true`
> **✅ Resolved** (simplified in `fc134fa`, then trimmed to a minimal `INotifyDataErrorInfo` in PR #14; verified during the `fix/haserrors-g7` backlog reconciliation): `ViewModelBase` now reads `public bool HasErrors => false` with no try/catch, and `ViewModelBaseTests` guards this contract. The finding below describes the original (pre-fix) state.

**File:** `ViewModels/ViewModelBase.cs`

```csharp
public bool HasErrors
{
	get
	{
		try { ... return propErrorsCount != null; }
		catch { }
		return true;   // ← on ANY error, claims "has errors"
	}
}
```

An empty catch that defaults to `true` can silently put the form into a permanent error state and hides the real exception.

**Recommendation:** Remove the try/catch (the LINQ is safe on an empty dictionary) or handle specifically; default should be `false`. *(Behavior change → approval.)*

---

### <a id="g-8"></a>G-8 🟡 [Safe] — `NegativeConverter` uses a dictionary-of-actions dispatch
**File:** `Converters/NegativeConverter.cs`

A `Dictionary<Type, Action>` is built **on every call** to negate a value, then `@switch[value.GetType()]()` throws `KeyNotFoundException` (not the intended `NotImplementedException`) for unsupported types, and `ReturnNegative(null)` throws `NullReferenceException`.

**Recommendation:** Replace with a `switch` expression on the runtime type; handle null/unknown explicitly. Behavior for supported types is preserved → **[Safe]** (with a minor, beneficial improvement to the error path).

---

### <a id="g-9"></a>G-9 ⚪ [Safe] — Many non-notifying public setters / property style
**File:** `ViewModels/MainViewModel.cs`

`ProgressBarLabel`, `Entries`, `SearchItem`, `SimulateCommand`/`CancelCommand` (`get; set;`), `LinearSearchResults`, `BinarySearchResults`, etc. are plain auto-properties with public setters. Commands and results are set once; exposing setters is unnecessary surface. `ProgressBarLabel` is set without `SetProperty` (manual `OnPropertyChanged`). This is style/consistency, not a bug.

**Recommendation:** Tighten setters to `private`/`init` where set-once; standardize on `SetProperty`. *(No behavior change → safe; do only where provably set-once.)*

---

### <a id="g-10"></a>G-10 ⚪ [Safe] — Namespace & using style inconsistency
**Files:** `Converters/NumStringConverter.cs`, `Converters/NegativeConverter.cs`

Both converters use **block-scoped** namespaces and local `using` directives, while the rest of the GUI uses **file-scoped** namespaces and global usings (`Usings.cs`). Minor inconsistency.

**Recommendation:** Convert the two converters to file-scoped namespaces; drop redundant local usings already covered globally. *(No behavior change → safe.)*

---

## 3. Tests (`SearchComparisonNet.Tests`)

### <a id="t-1"></a>T-1 🔴 [Approval] — `BinarySearchTests` ≡ `LinearSearchTests` (binary search untested)
**Files:** `BinarySearchTests.cs`, `LinearSearchTests.cs`

The two files are **identical** — same `[InlineData]` set, same method name `Should_find_correct_index_with_linear_search_when_values_exist`, both exercising **`LinearSut`** and asserting on `LinearSut.FindItem(...)`. `BinarySut` is never used. So:

- **`BinarySearch` has zero real coverage.** ([K-1](#k-1)/[G-1](#g-1) bugs would not be caught.)
- The reported "50 tests" is **25 linear-search cases run twice**.
- Assertion argument order is reversed: `Assert.Equal(actual, expected)` (expected/actual swapped) — cosmetic, but produces misleading failure messages.

**Recommendation:** Make `BinarySearchTests` actually test `BinarySut`; add "value-not-found" cases for both; fix the method name and assertion order. *(New coverage / behavior verification → approval, but **strongly recommended** as the prerequisite safety net for any Kernel refactor.)*

---

### <a id="t-2"></a>T-2 🟡 [Safe] — Test naming & assertion-order cleanup
**Files:** `BinarySearchTests.cs`, `LinearSearchTests.cs`

Independent of [T-1](#t-1): the binary file's test name claims "linear search," and both use `Assert.Equal(actual, expected)` (swapped). Renaming and fixing argument order don't change pass/fail outcomes for currently-correct cases.

**Recommendation:** Rename `BinarySearchTests` method to reflect binary search; use `Assert.Equal(expected, actual)`. *(No behavior change to outcomes → safe.)*

---

### <a id="t-3"></a>T-3 ⚪ [Safe] — `TestBase` exposes broad mutable surface
**File:** `TestBase.cs`

`DataParameters`/`LinearSut`/`BinarySut` are public settable properties on the base; fixtures rarely need public setters.

**Recommendation:** Make them `protected`/`get`-only. *(No behavior change → safe.)*

---

## <a id="4"></a>4. Conservative refactor worklist ([Safe] only)

These are the items eligible to apply **now** under your conservative constraint. Each is behavior-preserving; I'll group them, build (0/0), and run tests after each group.

**Kernel**
1. **K-4** — Rename `Interfaces/ISearch.cs` → `Interfaces/ISearchItem.cs` (file rename only).
2. **K-6** — `0 > index || index > NoOfEntries - 1` → `index < 0 || index >= NoOfEntries`; tidy `GenerateData` construction. *(Value-preserving.)*
3. **K-3 (reformat only)** — Normalize digit separators **without changing values** (leave the actual numbers exactly as-is until you confirm [K-3](#k-3)).

**GUI**
4. **G-8** — Rewrite `NegativeConverter.ReturnNegative` as a `switch` expression; correct null/unknown handling.
5. **G-10** — File-scoped namespaces + remove redundant local usings in both converters.
6. **G-9** — Tighten set-once auto-property setters (commands, results) to `private`; standardize on `SetProperty` where trivially safe.
7. **G-6 (dead-member removal only)** — If you don't want the product rule wired up, remove the unused `MinProductValue`/`MaxProductValue`/`MaxProductError`. *(If you DO want it enforced → that's [G-6 Approval].)*

**Tests**
8. **T-2** — Fix assertion order (`expected, actual`) and rename the binary test method.
9. **T-3** — Tighten `TestBase` property accessors.

> **Recommended pairing:** Apply **T-1 (Approval)** — give `BinarySearchTests` real binary coverage — *before or alongside* any Kernel cleanup, so the refactor has a genuine regression net. I'll only do this if you opt in.

---

## 5. What I will NOT touch without explicit opt-in ([Approval] items)

These are the highest-value fixes but involve behavior changes, so they're out of scope for "conservative" unless you say so, per item:

- **K-1 / G-1** — make both searches use one shared dataset + one random source (fixes the core correctness bug).
- **T-1** — real binary-search tests (recommended prerequisite for the above).
- **G-2** — marshal UI updates to the dispatcher / `IProgress<T>`.
- **G-3** — `async void` → `AsyncRelayCommand`.
- **G-4** — use (or remove) the DI container.
- **G-5** — implement (or remove) Cancel via `CancellationToken`.
- **G-6** — enforce the product-range validation rule.
- **G-7** — fix `HasErrors` fallback.
- **K-2** — make `NoOfEntries` consistent with `Data`.
- **K-3 (values)** — correct `100_00` / grouping if they're typos.

---

## 6. Suggested decision

Pick one:

- **A — Safe-only:** Apply the [§4](#4) conservative worklist; leave all [Approval] items as documented recommendations. (Fastest, zero behavior change.)
- **B — Safe + the critical safety net:** Do [§4](#4) **plus** T-1 (real binary tests) **plus** K-1/G-1 (fix the shared-data bug). Most valuable; small, well-contained behavior changes covered by the new tests.
- **C — Custom:** Tell me which [Approval] items to include; I'll fold them into the worklist.

Whichever you choose, changes go on a new branch `refactor/solution-review` and ship as a PR into `main`.

---

## <a id="7"></a>7. Change log & status

A running record of which findings have been actioned, in which PR, and what remains.

### Completed

| PR / branch | Findings addressed | Notes |
|---|---|---|
| **#2** `refactor/solution-review` *(merged)* | §4 [Safe] worklist (K-4, K-6, K-3 reformat, G-8, G-9, G-10, T-2, T-3) **+** T-1 **+** K-1/G-1 | Decision **B**. Shared-data bug fixed; real binary-search tests added (50 → 56 tests). |
| **`gui-async-threading-fixes`** *(this PR)* | **G-3** (`async void` → `AsyncRelayCommand`/`Task`), **G-2** (cross-thread UI → `Task.Run` + `IProgress<T>`, UI-thread reset), **G-5** (no-op Cancel → real `CancellationToken` cancellation) | Decision **A** for this batch. Behavior changes, build-verified (0 warnings). See "Testing decision" below. |
| **`chore/kernel-and-converter-polish`** | **K-6** (extends): `BinarySearch` recursion → iterative loop + overflow-safe midpoint. Plus converter test coverage (relates to **G-8**) and a `NumStringConverter.ConvertBack` `InvariantCulture` fix. | Tier 1 polish. Behavior-preserving; `NumStringConverter`/`NegativeConverter` unit tests added in the `net10.0-windows` `ViewModelTests` project. |
| **`code-cleanup`** | **C-1** (read-only `ISearch` indexer + removed `IndexOutOfRangeError`/setter bounds-check), **C-2** (`GenerateData()` removed from `IDataGenerator`, now `private`), **C-3** (`IDataGenerator.NoOfEntries` get-only). | API-surface tightening. Behavior-preserving; fakes and `DataGenerationBenchmarks` updated; full suite green (115 tests). |

### Testing decision for `gui-async-threading-fixes` (Option A)

Automated VM-level tests were **deliberately deferred** for this PR. Rationale:
- The test project is `net10.0` and references **Kernel only**; `MainViewModel` lives in the GUI project (`net10.0-windows`, depends on WPF `Visibility`).
- Meaningful tests would require re-targeting the test project to `net10.0-windows`, adding a GUI `ProjectReference`, **and** a DI/seam refactor of `MainViewModel` (it self-constructs `DataGenerator`/searches and references `Visibility` directly).
- The highest-value fix — **G-2 UI-thread marshaling** — cannot be meaningfully unit-tested headlessly: `Progress<T>` marshaling needs a real UI `SynchronizationContext`, which xUnit does not provide.
- The Kernel search loops the simulation drives are already covered by the existing **56 tests**.

**Manual verification checklist** (until automated coverage lands): run the app → start **Simulate** with a large entry/search count → click **Cancel** mid-run → expect: UI stays responsive, progress bar resets to 0/hidden, no crash, **Simulate** re-enables, results from the cancelled run are not shown.

### Outstanding work

Outstanding items from this review (the remaining **[Approval]** findings and the deferred
test-infrastructure options **B**/**C**) are now tracked as the single actionable backlog in
[`TODO.md`](../../TODO.md). The findings and rationale above remain the reference for *why* each
item exists; `TODO.md` is the source of truth for *what is scheduled next*.

---

## Cleanup follow-ups (C-1 .. C-8)

A second, focused API-surface/consistency pass over the Kernel (and a couple of GUI/cosmetic
notes), surfaced while surveying the code on `code-cleanup`. These are all behavior-preserving.
`C-1`..`C-3` are the highest-value (they remove dead or leaked public surface) and are **done** on
`code-cleanup` (full suite green: 115 tests); `C-4`..`C-8` are captured for later reference.

| ID | Sev | Area | Finding |
|---|---|---|---|
| [C-1](#c-1) | 🟡 | Kernel | `ISearch` indexer exposes a **dead mutable setter**; only the getter is used (by tests) |
| [C-2](#c-2) | 🟡 | Kernel | `IDataGenerator.GenerateData()` leaks an internal detail onto the public contract |
| [C-3](#c-3) | 🟡 | Kernel | `IDataGenerator.NoOfEntries` has a public setter but is only assigned once (ctor) |
| [C-4](#c-4) | 🟢 | Kernel | `DataGenerator.Random` is `public` but is not on the interface and not used externally |
| [C-5](#c-5) | 🟢 | Kernel | `SearchItem` / `ISearchItem` use mutable `{ get; set; }` on produced-once result objects |
| [C-6](#c-6) | 🟢 | GUI | `NumStringConverter` / `NegativeConverter` share `IValueConverter` null-guard boilerplate |
| [C-7](#c-7) | 🟢 | Kernel | `ProblemConstants` uses expression-bodied `=>` for pure constants (recomputes per access) |
| [C-8](#c-8) | 🟢 | Kernel | Lone `#region IDataGenerator` in `DataGenerator.cs` is inconsistent with the rest of the Kernel |

### <a id="c-1"></a>C-1 🟡 — `ISearch` indexer has a dead, unsafe setter

`ISearch.this[int index] { get; set; }` (implemented in `SearchBase`) is only referenced by tests
(`SearchTestsBase`, `SearchEquivalenceTests`), and those uses are **reads** — they translate a
known index to the value stored there so the search can then be asserted against a random dataset.
No production code, and no test, ever *writes* through the indexer. The setter therefore only adds
a mutable seam into the shared `Data` array plus the bounds-check and the `IndexOutOfRangeError`
message that exists solely to serve it.

**Fix (C-1, implemented on `code-cleanup`):** make the indexer read-only (`{ get; }`), and remove
`SearchBase.IndexOutOfRangeError` and the setter's bounds-check. The legitimately-used getter stays,
so the two consuming tests need no changes; only `FakeSearch`'s empty indexer setter is dropped.

### <a id="c-2"></a>C-2 🟡 — `GenerateData()` leaks an implementation detail

`IDataGenerator.GenerateData()` is called exactly once, internally, from the `DataGenerator`
constructor (`Data = GenerateData()`). Exposing it on the interface forces every implementer
(including both test fakes) to re-declare it and invites callers to regenerate data out of band.

**Fix (C-2, implemented on `code-cleanup`):** make `GenerateData()` a `private` method of
`DataGenerator` and remove it from `IDataGenerator`. The data-generation benchmark constructs a
fresh generator to measure a generation instead of re-invoking the method through the interface.

### <a id="c-3"></a>C-3 🟡 — `IDataGenerator.NoOfEntries` setter is unnecessary

`NoOfEntries` is assigned once, in the `DataGenerator` constructor, and never mutated afterward. The
public setter is inconsistent with the read-only direction the rest of the Kernel took (cf. **K-2**,
which removed the `SearchBase.NoOfEntries` setter).

**Fix (C-3, implemented on `code-cleanup`):** change the interface member to `{ get; }` and set the
value via the constructor in `DataGenerator` (fakes derive it from the supplied data length).

### <a id="c-4"></a>C-4 🟢 — `DataGenerator.Random` should be private

`DataGenerator.Random` is a `public` property that is not part of `IDataGenerator` and is not read
by any caller. It should be `private` so the type's public surface reflects only its contract.

### <a id="c-5"></a>C-5 🟢 — result objects could be immutable

`SearchItem`/`ISearchItem` expose `{ get; set; }`, but every `SearchItem` is fully populated via an
object initializer inside `FindItem` and never mutated afterward. Converting the setters to
`init` (or making `SearchItem` a `record`) would make the produced-once nature explicit. This touches
the interface, so it is grouped as a deliberate follow-up rather than a drive-by change.

### <a id="c-6"></a>C-6 🟢 — converter boilerplate duplication

`NumStringConverter` and `NegativeConverter` repeat similar `IValueConverter` null-guard/boilerplate.
A shared helper or base could remove the duplication; low priority and needs a close read of both
converters before committing to a shape.

### <a id="c-7"></a>C-7 🟢 — `ProblemConstants` constants use `=>`

Members like `MinNoOfEntries => 10_000` are expression-bodied properties, so they recompute on each
access and read oddly for values that are conceptually constants. `const` / `static readonly` is more
idiomatic. **Note:** the actual value `10_000` is intentional (see **K-3**) and must not change.

### <a id="c-8"></a>C-8 🟢 — stray `#region` in `DataGenerator`

`DataGenerator.cs` wraps most of the class in a single `#region IDataGenerator`. No other Kernel file
uses regions; removing it is a small consistency win.

---

## Code-style automation & member ordering

The repository standardizes on a set of code-style conventions enforced through the root
[`.editorconfig`](../../.editorconfig). This section records what is automated, what is **not**, and
how to apply the automatable parts through Visual Studio **Code Cleanup**.

### What IS automated (via `.editorconfig` + Code Cleanup)

- **File-scoped namespaces** — `csharp_style_namespace_declarations = file_scoped:warning`. Already
  applied across the codebase.
- **Modern C# language features** — a `[*.cs]` "Modern C# language features" block sets these at
  `:suggestion` so they show as subtle dots and are fixable by Code Cleanup: primary constructors
  (C# 12), collection expressions (`[ ... ]`), pattern matching / switch expressions / extended
  property & `not` patterns, index & range operators, UTF-8 string literals, method-group
  conversions, `static` local/anonymous functions, tuple swap, and readonly-struct preferences.
- **Global usings** — each project keeps a single `Usings.cs`; inline `using` directives are removed
  and the global list is sorted alphabetically. `dotnet_sort_system_directives_first = false`
  matches that ordering.

Primary constructors are set as a **preference only** (`:suggestion`). Converting existing types is
a reviewed, per-type change (constructor bodies can carry validation/side-effects and field-init
ordering matters), not a blanket "fix all".

### Setting up the Code Cleanup profile

`Tools > Options > Text Editor > C# > Code Style > Formatting`, then **Configure Code Cleanup**
(the broom icon at the bottom of the editor, or `Ctrl+K, Ctrl+E`). Add these fixers to Profile 1:

- Apply file-scoped namespace preferences
- Apply expression/block body preferences
- Apply language/framework type preferences (`var`, predefined types)
- Apply object/collection initializer preferences
- Apply `using` directive placement preferences + Sort using directives
- Remove unnecessary usings
- Apply pattern-matching / index-and-range / null-checking preferences

Then run `Ctrl+K, Ctrl+E` per file (recommended) or **Analyze > Code Cleanup > Run Code Cleanup on
Solution**. Prefer per-file runs so diffs stay reviewable.

### What is NOT automated: member ordering

The desired convention is **public members first, then ordered by relevance** (a readability-first
layout). **Neither `.editorconfig` nor Visual Studio Code Cleanup can reorder type members** — Roslyn
has no member-layout rule, by design, because arbitrary reordering can change semantics
(field-initializer order, `static` dependencies) and "relevance" is subjective.

Options, with the honest tradeoffs:

| Option | Automated? | Tradeoff |
|---|---|---|
| **Manual / AI-assisted reorder** *(current choice)* | Semi | Matches the exact "public-first, by relevance" intent; done per file on request. No tooling to maintain. |
| **StyleCop.Analyzers `SA1201`/`SA1202`** | Yes (has code fix, Code Cleanup can apply) | Enforces the **standard** .NET order (const → field → constructor → property → method, then public→private within each). This is *not* "public methods first" and *not* relevance-based — adopting it means abandoning the custom rule. |
| **ReSharper / Rider "Type Layout"** | Fully configurable | Only tool that supports arbitrary layouts, but requires a JetBrains license and lives in its own layout XML, not `.editorconfig`. The user is not using ReSharper. |

**Recommendation (no ReSharper):** keep member ordering **manual / AI-assisted** to preserve the
custom "public-first, by relevance" layout. Adopt StyleCop `SA1201/SA1202` only if the team decides
the *standard* order is acceptable in exchange for full automation. Do not mix both — they encode
different orderings.
