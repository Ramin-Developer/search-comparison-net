# TODO / Follow-up Tasks

Tracked follow-up work, kept so each branch stays focused on a single theme. The **Completed**
section is a running audit trail of merged work; the **Remaining backlog** section is the single
source of truth for outstanding items. Every change ships on its own branch as a PR into `main`
(including docs-only changes).

## Completed

### Performance & concurrency investigation (`perf/concurrency`, merged)
Measurement-driven pass over the search/data-generation path. All conclusions were
validated with BenchmarkDotNet (CPU + `MemoryDiagnoser`) and the full test suite.

- **CPU:** profiling showed `LinearSearch.FindItem` dominating and `ObservableCollection<int>`
  indexer overhead in the hot path. Replaced `ObservableCollection<int>` with raw `int[]`
  in `SearchBase`/`LinearSearch`/`BinarySearch`.
- **Memory:** `DataGenerator.GenerateData()` was rewritten from `HashSet<int>` rejection
  sampling to a compact `BitArray` membership bitmap (no `HashSet`, no `Array.Sort`),
  measured 3.7x-4.8x faster with sharply reduced LOH churn.
- **async/await & disposal/lifetime:** answered by measurement rather than assumption.
  `SimulationSessionBenchmarks` (repeated full sessions) reported zero retained live objects;
  `CancellationLifetimeBenchmarks` (CTS create/cancel/dispose cycle) showed only collectible
  transients. Conclusion: no eager-disposal or async restructuring is warranted; the hot path
  is pure compute and the existing `Task.Run` plumbing only serves UI responsiveness.
- Benchmarks live in `BenchmarkSuite1` (opted out of central package management).

### Solution format migration + project cleanup (`chore/slnx-migration`)
- Migrated `SearchComparisonNet.sln` to `SearchComparisonNet.slnx` via `dotnet sln migrate`;
  removed the legacy `.sln` and updated the CI workflow's restore/build/test steps.
- Removed an empty `Properties\` folder include from `Kernel.csproj`.
- Fixed `<TargetFrameworks>` -> `<TargetFramework>` in `BenchmarkSuite1.csproj`.
- Removed the now-dead `global using System.Collections.ObjectModel;` (`ObservableCollection`
  was eliminated from the Kernel during the `int[]` refactor).
- Confirmed modern C# defaults are already centralized in `Directory.Build.props`
  (`LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`, `EnforceCodeStyleInBuild=true`)
  and the solution builds warning-free with all 56 tests passing.

### GUI project review (`refctor/gui`, merged via PR #14)
Focused pass over `SearchComparisonNet.GUI` for readability, DRY, and modernization.

- Removed the unused `NuGet.Configuration` package reference from `SearchComparisonNet.GUI.csproj`
  (confirmed no usage anywhere in the GUI source).
- Audited the remaining package references (`CommunityToolkit.Mvvm`, `FluentValidation`,
  `Microsoft.Extensions.DependencyInjection`) - all are in active use.
- Confirmed the WPF/build settings are minimal and correct for `net10.0-windows`.
- Solution builds warning-free with the full test suite (132 tests) passing.

### Extend test coverage and xUnit v3 idioms (`test/expand-coverage`, merged via PR #15)
Broadened automated coverage beyond the search algorithms and adopted xUnit v3 features.

- Added `DataGeneratorTests` (output invariants: exact count, in-range values, strictly
  ascending, unique; plus `NextRandomNo` range and single-element datasets).
- Added `InputValidationTests` (FluentValidation rules for entries/searches: required,
  non-integer, out-of-range, in-range).
- Added `ViewModelBaseTests` locking in the current `INotifyDataErrorInfo` no-op contract.
- Added `MainViewModelCancellationTests` for cancel/retry edges around `SimulateCommand`.
- Fixed a latent break: PR #14 removed the dead `LinearSearchResults`/`BinarySearchResults`
  from `MainViewModel` but left `MainViewModelSimulationTests` referencing them, so the test
  project could not compile from a clean build; updated it to the average-iteration +
  `IsSearchEnabled` contract.
- Adopted `TheoryData<T>`, `Assert.Multiple`, and `[ClassData]`/`[MemberData]`.
- Validated with a clean full run: 185 tests passing, 0 warnings.

### Backlog reconciliation against current code (`fix/haserrors-g7`)
Audited every open review finding against the current source (the review was written at a
50-test baseline and predates several merged PRs). Three findings were already resolved by
earlier work but had never been crossed off:

- **G-7** (`HasErrors` swallowed exceptions and returned `true`) - resolved: `ViewModelBase`
  now reads `public bool HasErrors => false` with no try/catch (simplified in `fc134fa`, then
  trimmed to minimal `INotifyDataErrorInfo` in PR #14). `ViewModelBaseTests` guards this contract.
- **G-4** (DI container configured but unused) - resolved: `ServiceCollectionExtensions` registers
  the graph, `App.xaml.cs` resolves `MainView` via `GetRequiredService<MainView>()`, and
  `MainView` receives a constructor-injected `MainViewModel` (`DataContext = mainViewModel`).
- **G-6** (declared product-range validation never enforced) - resolved: the dead
  `MinProductValue`/`MaxProductValue`/`MaxProductError` members no longer exist anywhere in the GUI.

`K-3 (values)` is a settled no-op: `ProblemConstants.MinNoOfEntries => 10_000` carries an explicit
"value preserved per decision" comment. The corresponding finding sections in
`solution-review.md` were annotated as resolved to keep the audit trail accurate.

### Kernel encapsulation fixes (`fix/noofentries-encapsulation-k2` PR #18, `refactor/nextrandomno-to-generator-k5` PR #19)
Closed the two remaining open code-level findings from the review:

- **K-2** - `SearchBase.NoOfEntries` is now derived and read-only (`public int NoOfEntries => Data.Length`),
  so it can never desync from the dataset; the setter was removed from `ISearch`. Production
  `DataGenerator` and the test `FakeDataGenerator` both keep `NoOfEntries == Data.Length`, so behavior
  was preserved.
- **K-5** - random probe generation moved off the search type onto the shared generator. `NextRandomNo`
  was removed from `ISearch`/`SearchBase` and surfaced on `ISearchComparison`, wired in
  `SearchComparisonFactory` from the single shared `DataGenerator`. `MainViewModel` now draws probes
  from the comparison instead of a search instance.
- Both shipped behavior-preserving with the full suite green (185 tests passing, 0 failing).

### Tier 1 - Kernel & converter polish (`chore/kernel-and-converter-polish`)
Small, low-risk, behavior-preserving cleanup that also lifted coverage on previously-untested units:

- **Iterative binary search** - `BinarySearch.FindItem` was converted from recursion to an iterative
  `while` loop, preserving the exact `NoOfIterations` counting semantics (guarded by the existing
  search-equivalence and edge-case tests). Removes per-level call overhead and mirrors `LinearSearch`.
- **Overflow-safe midpoint** - `(low + high) / 2` replaced with `low + (high - low) / 2`.
- **Converter unit tests** - added `NumStringConverterTests` and `NegativeConverterTests` in the
  `net10.0-windows` `ViewModelTests` project (both converters were previously at 0% coverage).
- **`ConvertBack` culture consistency** - `NumStringConverter.ConvertBack` now parses with
  `CultureInfo.InvariantCulture`, matching `Convert`.
- Shipped with the full suite green (224 tests: 115 Kernel + 109 ViewModel, 0 failing, 0 skipped).

### GUI single-value UX + layout consolidation (`refactor/gui`, merged via PR #24)
Two-PR split off the earlier cleanup work. PR #1 (`code-cleanup` -> `main`) merged; this shipped as PR #24.
Focused on single-value search UX, dataset preview, shared layout styling, and a package bump.

- **Single-value search UX** - the lookup is now explicit (on-demand via `SearchCommand`, triggered by
  the Search button or Enter in the Target Value box) instead of running implicitly on every keystroke.
  A value absent from the dataset yields `TargetIndex = -1` and a "Not Found" tooltip on the Target
  Index box (`ToolTipService.ShowOnDisabled="True"`, tooltip returns `null` when there is nothing to show).
- **Enter-to-Simulate** - a window-level `KeyBinding` runs `SimulateCommand` (respects `CanSimulate`).
- **Dataset preview row** - `MainView` shows a compact preview of the sorted dataset: the first,
  middle, and last groups joined by `", ..., "`, each group listing
  `SimulationConstants.DataSampleValueCount` (default 3) comma-separated values. Reads via the
  `ISearch` indexer so it never exposes the underlying array.
- **Simulation constants extracted** - `SimulationConstants.cs` (Kernel) now centralizes
  `DataSampleValueCount` and `ProgressReportIntervalMs`.
- **App-wide control height** - a single shared `ControlHeight` (`system:Double`, currently `16`) in
  `Views/MyDictionary.xaml` is applied through `ButtonStyle`, `TextBoxStyle`, and `TextErrorStyle`, so
  every button and textbox lines up at one height, tunable in one place.
- **Package update** - `BenchmarkDotNet` bumped `0.15.2` -> `0.15.8` in `BenchmarkSuite1.csproj`
  (that project opts out of central package management). All CPM-managed packages are already latest.
- **Pre-merge GUI cleanup (safe, non-behavioral)** - removed a redundant `Height="Auto"` on an inner
  `Grid` in `StatisticsControl.xaml`; fixed a `VerticalAlignment="center"` casing typo in
  `StatusControl.xaml`; made `MainView.MainViewModel` get-only. Verified `global using
  System.Windows.Markup` is still required (by `NegativeConverter : MarkupExtension`).
- Shipped behavior-preserving with the full suite green (228 tests passing, 0 failing).

## Remaining backlog

> Single source of truth for outstanding work. These items originate from the code review in
> [`docs/review/solution-review.md`](docs/review/solution-review.md); see that document for the
> full findings and rationale behind each one. Every change ships on its own branch as a PR into
> `main` (including docs-only changes).

### Approval items (code-level findings)

All code-level findings from the review are now resolved (K-2 and K-5 shipped in PRs #18 and #19;
G-4/G-6/G-7 and K-3 were reconciled earlier). No open code findings remain.

### Polish backlog (lower priority, for later reference)

Non-blocking readability/robustness follow-ups captured so they are not lost:

- **`NumStringConverter.Convert` culture on parse** - `long.TryParse(text, ...)` /
  `double.TryParse(text, ...)` in `Convert` parse `value.ToString()` without an explicit culture.
  Consider pinning `InvariantCulture` on those parses too for full symmetry (deliberately left out
  of Tier 1 to keep that change minimal and approved-scope only).
- **K-6 leftovers** - the `IndexOutOfRangeError` message `{0}` literal and the
  `0 > index || index > NoOfEntries - 1` -> `index < 0 || index >= NoOfEntries` clarity tweak
  remain open under K-6. *(Superseded by **C-1**: the setter and `IndexOutOfRangeError` are being
  removed outright, which retires these leftovers.)*

### API-surface & consistency cleanup (C-1 .. C-8)

> A focused Kernel/GUI cleanup pass surfaced while surveying the code on `code-cleanup`. See
> [`docs/review/solution-review.md`](docs/review/solution-review.md#cleanup-follow-ups-c-1--c-8)
> for the full rationale behind each item. All behavior-preserving.

**Shipped on `code-cleanup` (highest value - dead / leaked public surface, full suite green: 115 tests):**

- **C-1** *(done)* - made the `ISearch` indexer read-only; only the getter is used (by tests), the
  setter was dead. Also removed `SearchBase.IndexOutOfRangeError` and the setter bounds-check, and
  dropped the no-op `FakeSearch` indexer setter.
- **C-2** *(done)* - removed `GenerateData()` from `IDataGenerator` and made it `private` in
  `DataGenerator` (it is called once, internally, from the constructor). `FakeDataGenerator` no longer
  re-declares it, and `DataGenerationBenchmarks` now drives a generation via construction.
- **C-3** *(done)* - made `IDataGenerator.NoOfEntries` get-only (assigned once in the constructor),
  matching the read-only direction taken by K-2.

**Also shipped on `code-cleanup` (Kernel polish batch, behavior-preserving):**

- **C-4** *(done)* - made `DataGenerator.Random` `private` (it is not on `IDataGenerator` and is not
  read by any caller); the public members stay above it to preserve the public-first layout.
- **C-5** *(done)* - made `SearchItem`/`ISearchItem` result objects immutable by switching the
  setters to `init`. Every `SearchItem` is fully populated via an object initializer in `FindItem`
  and never mutated, so all creation sites compile unchanged.
- **C-7** *(done)* - converted `ProblemConstants` expression-bodied members to `const` (the seven
  int limits and the four plain-string messages) and `static readonly` for the two interpolated
  range messages (they reference the `const` ints, so they cannot be `const`). The intentional
  `10_000` value from K-3 is unchanged, and the message text is byte-identical so the value-based
  assertions in `InputValidationTests` still hold.
- **C-8** *(done)* - removed the lone `#region IDataGenerator` in `DataGenerator.cs`.

**Evaluated, no change (for later reference):**

- **C-6** *(no-op)* - de-duplicating `NumStringConverter`/`NegativeConverter` was assessed and left
  as-is: `NegativeConverter` already derives from `MarkupExtension` (so it cannot also inherit a
  shared converter base), and the two converters' null handling is *opposite* (`NumStringConverter`
  returns `null`, `NegativeConverter` throws `ArgumentNullException`). The only overlap is the
  interface-mandated `IValueConverter` signature, so a shared base/helper would add indirection
  without removing real duplication.

### GUI consolidation candidates (from the `refactor/gui` pre-merge review, deferred)

Layout/structure opportunities identified while reviewing the GUI for PR #24. Left out of that PR
because they affect layout or are larger refactors; captured here for a future focused pass:

- **`LabelStyle` default alignment** - the shared `LabelStyle` defaults to `HorizontalAlignment`/
  `HorizontalContentAlignment="Center"`, but most labels override to `Left`. Flipping the default to
  `Left` and only overriding the few centered headers would drop ~12 inline overrides (layout-affecting).
- **Magic-number label widths** - the repeated `Width` values (93 / 95 / 50) across `InputControl`,
  `SearchControl`, and `StatisticsControl` could become named `system:Double` constants alongside
  `ControlHeight` for consistency.
- **`StatusControl` hardcoded sizes** - `ProgressBar Height="20"` and `TextBlock Height="10"`/`Width="45"`
  are hardcoded and unrelated to the shared `ControlHeight`; decide whether they should align.
- **`StatisticsControl` Linear/Binary duplication** - the two result blocks are ~95% identical; extract
  a small reusable UserControl or a parameterized `DataTemplate` (title + two bindings). Structural
  refactor, best as its own branch.

### Test-infrastructure options (deferred)

- **Option B** - extract the cancellation-aware iteration logic into a Kernel-side (or plain,
  `net10.0`-referenceable) helper and unit-test the G-5 cancellation contract (token honored,
  `OperationCanceledException` thrown) without WPF. Moderate effort.
- **Option C** - full VM testability: re-target the test project to `net10.0-windows`, add a GUI
  `ProjectReference`, refactor `MainViewModel` for constructor injection (pairs with G-4), and add
  a UI-`SynchronizationContext` fixture to exercise dispatcher marshaling. Largest effort.

## Suggested ordering & effort

- All open code-level findings are done: the **Tier 1 - Kernel & converter polish** branch has
  shipped, and the **C-1 .. C-8** cleanup batch is now fully resolved (C-1..C-5, C-7, C-8 applied;
  C-6 evaluated and intentionally left unchanged - see above).
- **Option B** is the next-most-valuable step (real cancellation-contract coverage without WPF).
- **Option C** is partially in place already: the test project targets `net10.0-windows` and references
  the GUI, and `MainViewModel` is exercised through a factory abstraction. What remains for full VM
  testability is a UI-`SynchronizationContext` fixture to exercise dispatcher marshaling; schedule
  deliberately.
- The **Polish backlog** items are optional follow-ups, to be picked up opportunistically.
