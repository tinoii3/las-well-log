# PetroRes Well Log Tool

Loads LAS files, conditions the data, and renders a log plot per well
(GR | resistivity on log scale | density + neutron porosity).

## Build and run

Requires Windows, .NET SDK, and .NET Framework 4.8 targeting pack.

```
dotnet build
dotnet test
dotnet run --project src/WellLogs.Cli -- <inputFolder> [outputFolder]
```

Outputs in the output folder: `plots/*.png`, `conditioning_report.md`
(per-file details), `summary.csv` (one row per file).

The brief mentions 50 files; the set I received contained 203 .las files
in one folder, so all were processed.

## Result on the supplied data

- 203 files: 202 loaded, 1 unsupported, 0 failed; 182 plots written
- ~12 s total, ~400 MB peak memory (parallel processing)
- The 20 loaded files without plots are mostly mud logs and other
  non-wireline files with none of the four curve types, or with the
  curve present but empty (reported as CURVE_MISSING / CURVE_SKIPPED_EMPTY).

## Design

- `WellLogs.Core` (netstandard2.0): model, LAS reader, conditioning. No UI dependency.
- `WellLogs.Plotting`: OxyPlot rendering, separate so the plot library is replaceable.
- `WellLogs.Cli`: batch driver and reports.
- Reader never throws: every file yields a `LoadResult` (Ok / OkWithWarnings /
  Unsupported / Failed) plus a list of issues.
- Columnar storage: one `double[]` per curve, NaN for missing. The file is
  streamed line by line; each well is loaded, conditioned, plotted and
  released, with only a small summary kept, so memory does not grow with
  the number of wells. Wells are processed in parallel; PNG encoding is
  serialized because GDI+ is not safe for concurrent use.
- Conditioning produces a new `ConditionedWell` containing only the four
  plotted curve types; the raw `Well` is not mutated.

## Data-conditioning decisions

Principle: the header is a hint, the data is the truth.

- **Parsing**: section type is decided by the first letter after `~`.
  Wrapped data is read as a token stream cut into rows of N values.
  WRAP=NO files whose data is actually wrapped are auto-detected.
  Lines in `~Curve` with unusual mnemonics (spaces, empty) are still
  accepted, because skipping one shifts every later column.
- **Missing values**: common sentinels (-999.25, -999, -9999, -99999) plus the
  header NULL, if distinctive. A header NULL such as 0 is not applied (0 is a
  valid measurement) and a warning is recorded. Comparison uses a tolerance.
- **Depth**: rows with invalid depth removed; descending order reversed;
  non-monotonic data stably sorted; duplicate depths removed (first kept);
  irregular sampling and header STEP mismatch are reported. Data is not
  resampled or interpolated, because that would invent data.
- **Units**: depth converted to meters. Porosity to v/v (from % or from
  values that are clearly percent despite the unit). Density to g/cc.
  Conductivity is converted to resistivity (R = 1000/C) only when no
  resistivity curve exists.
- **Curve selection**: each curve gets a score from unit, description words
  and mnemonic. Mud, flushed-zone, micro, correction and difference curves
  are excluded. Among resistivity candidates, deeper investigation is
  preferred. If the best candidate has no valid values, the next is used.
  All choices are recorded in the report.
- **Out-of-range values**: values outside physical limits are set to missing
  and counted. Long runs of identical values (possible tool saturation) are
  reported but left unchanged.
- **Rows with no data** in every selected curve at the start or end are trimmed.
- **Plots**: fixed standard scales (GR 0-150, resistivity 0.2-2000 log,
  density 1.95-2.95, neutron 0.45 to -0.15). Samples outside the displayed
  scale are hidden, not deleted.

## Known limitations

- Files with array/waveform curves (multiple values per depth, e.g. `WFA_FAST[0]`)
  are reported as unsupported rather than guessed at.
- True LAS 3.0 (`~Log_*` sections) is not supported; files that declare 3.0 but
  use the 2.0 layout are read normally.
- Curve classification is heuristic. The name tables and exclusion words in
  `CurveClassifier.cs` are the extension point; unusual names may be missed
  and show as CURVE_MISSING.
- Depth is treated as measured depth; no TVD conversion.
- Comma-delimited files with trailing commas may misalign in wrapped mode.
- No point decimation when plotting; very large wells render slowly.
- Resistivity candidates with equal scores are ordered by trailing digits in
  the name, which is a guess.
- Not tested on non-Windows platforms (OxyPlot.WindowsForms / System.Drawing).

## Use of AI

I used one AI tool, Claude.

What I used it for:
- Reading the assignment and planning the project structure.
- Analysing real sample LAS files I pasted in, to find the messy cases
  (NULL declared as 0, wrapped data, descending depth, array curves, fake
  "LAS 3.0" files, label/value swaps in the header).
- Drafting most of the code: model, LAS reader, conditioning, plotting, CLI,
  and the unit tests.

What I did myself:
- Set up the solution, built it, and ran all tests (104 passing).
- Ran the program on the full dataset and reported results back. This found
  real bugs the AI had not predicted, e.g. curve names containing spaces
  in `~Curve` caused column misalignment and 6 files failed to load; fixed
  by accepting those names in that section only.
- Inspected the generated plots and the conditioning report.
- Decided what to keep, and pushed back on a hard-coded label list, which
  was replaced by a general heuristic.

I read and can explain the code, in particular LasDataParser, NullHandler,
DepthNormalizer and CurveClassifier.