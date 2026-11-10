# Contributing

This project is kept coherent by [Anchors](https://github.com/co2-lab/anchors). The
section below says how work flows here; it was seeded by `anchors init` from
`anchors.yaml`, and the configuration stays the source of truth.

Running and testing the project needs only Docker, as the README says. Anchors is needed
only to run the gates that guard a commit.

## Working with Anchors

### The order of the work

The spec is the anchor: a change starts in the spec (`*.spec.md`), which states the
rules; the feature (`*.feature`) turns each rule into scenarios; the tests prove the
scenarios; the code realises them. A bug fixed without a rule and a scenario is a bug
the next change can bring back — write the rule, then the scenario, then the test.

Where each piece lives here:

- spec: `**/*.spec.md`
- feature: `**/*.feature`
- test: `**/*.test.*`
- beside its spec: the code at `{{dir}}/{{name}}.{{ext}}`; the feature at `{{dir}}/{{name}}.feature`; the test at `{{dir}}/{{name}}.test.{{ext}}`

### Layers

Anchors does not care how the folders are arranged, only that the layers are kept
apart and that each file respects the layer it is in. Layers are the project's own
decision — for example: entry points, use cases, domain, repositories,
infrastructure, presentation. Those names are illustration, not a layout to fit.

The code layers here are `core`, `infrastructure` and `host`, declared in `anchors.yaml`.

### Day to day

- `anchors status` — where the project stands, and the next step.
- `anchors next` — claim the next task from the local queue (`.anchors/tasks/`).
- `anchors work <stage> --for <target>` — the prompt of one task: what to read, where each piece goes, how to verify.
- `anchors check --changed <file>` — run the gates over what a change reaches.
- `anchors test` — run the test suites and ingest their results into the map.
- `anchors touch` — bump `updated_at` in the headers of the files that changed.
- `anchors install-hooks` — once per clone: the pre-commit runs the gates over what is staged.
- `anchors progress` — the only writer of `anchors.progress.md`. Work the plan does not list, found on the way and done for it — a fix, a missing helper, a test the plan forgot — is recorded before it is done: `anchors progress add task <demand|stage> "<what>" --weight <n> --unplanned`. The plan is not rewritten for it; the progress counts it, marked so it is seen.

### Long commands and agents: watch them

**Strongly recommended:** never leave a long command or an agent working unwatched. A
suite that hangs, a build that dies or an agent stuck on a prompt looks like work in
progress until someone notices — usually much later.

- Launch every long command — a test suite, a mutation run, a build, a script, an agent's
  task — through `anchors monitor run -- <command>`, in the background: its process, its
  output and its exit are recorded in `.anchors/runs/`. Anchors' own long commands
  (`anchors test`, `mutation`, `check`, `verify`) record themselves.
- Keep `anchors monitor` running while they work (`--until-done` to stop when they end):
  one line when a run starts, stalls, dies or ends, and a heartbeat with what is running.
  React to each line instead of waiting blind.
- Working with a coding agent: `anchors install-hooks --agent` records the long commands
  it sends to the background, so the monitor sees them too.
- An agent that watches the monitor runs it with `--level action`: only what calls for a
  reaction goes out, and each line it sends wakes a turn. No `| grep` of your own.
- With `--summary` (or `monitor.summary: true`) the monitor also posts the progress summary
  when it changes — the `▸ Progress` line and the indented ones under it —, after a line
  addressed to the agent. **Agent: copy that summary to the user verbatim, in a code block,
  the total line included — without the line meant for you.**

### What blocks and what informs

A failure of these gates bars the commit: `spec-complete`, `spec-has-code`, `header-valid`, `unit-complete`, `no-test-proof-real`, `rule-fulfilled`, `code-cataloged`, `rule-types`, `dependency-honored`, `obligation-honored`, `plan-seeds-valid`, `plan-source-declared`, `docs-fresh`, `plan-doctrine-exists`, `spec-doctrine-exists`, `feature-spec-match`, `test-feature-match`, `revision-orphans`, `flag-scenario-exists`, `doctrine-not-duplicated`, `spec-realizes-doctrine`, `failure-declared`, `docs-covered`, `doc-required`, `doc-self-contained`, `plan-revised`, `plan-change-justified`, `phase-ordered`, `parent-valid`, `phase-exists`, `feature-not-empty`, `scenario-asserts`, `test-traceable`, `layer-boundary`, `count-honored`, `domain-declared`, `rule-uses-declared`, `rule-uses-implemented`, `contract-impact`, `rule-uses-resolve`, `presentation-exhaustive`, `presentation-conflict`, `presentation-copy-single-source`, `presentation-observable`, `single-test-per-unit`, `header-layer-declared`, `examples-match`, `open-questions-resolved`, `ref-resolves`, `code-reference-valid`, `cross-unit-citation`, `spec-feature-match`, `no-secret-leaked`, `guide-checklist`, `evidence-fresh`, `feature-test-match`, `scenario-identity`, `scenario-letter-declared`, `nav-matches-spec`, `nav-symmetric`, `nav-reachable`, `placeholder-filled`, `rule-implemented`, `section-required`, `region-pair-honored`, `updated-at-atual`, `freeze-consistent`, `frozen-touched`, `progress-intact`, `guidance-linked`, `guidance-honored`, `progress-honest`.
These only inform: `tests-green`, `line-coverage`, `coverage-delta`, `branch-coverage`, `mutation-score`, `scenario-coverage`, `dependency-vulnerable`, `no-duplication`, `sbom-generated`, `spellcheck`, `license-compatible`, `circular`, `deadcode`.
Never commit with `--no-verify`: the hooks are where this agreement is enforced. A gate
that fails is fixed, or waived in the commit message with its reason
(`[skip-<rule>@<CODE>: why]`).
A divergence (⚠) and a pending item (?) inform unless `severity` in `anchors.yaml`
says otherwise; `anchors check --show-drift` lists them.

### Guides

`anchors guide` prints the guides for writing each piece.
