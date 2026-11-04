# DN03-R5 — RM-06 literal-input source-ownership recovery (v2)

This follow-up overlay changes no Devtool source, toolchain, native job commands,
outputs, dependencies or GitHub Actions trust claims. It replaces the original
six native job `inputs` glob patterns with deterministic, Git-tracked source
file inventories. A job output is never also an input.

RM-06 deliberately refuses globs once any native job produces output bytes.
The previous generated workflow stopped at `DN03_R4_REFUSED: ... ownership
through globs` before writing any generated output. This overlay preserves
that fail-closed rule and fixes the consumer contract.

The `scripts/dn03_r5_source_contract.py` validator now checks all six
literal input lists and their tracked Git sources. Newly added source files
must be declared in the job input list for affected-target ownership.
The actual GitHub-hosted seven-job acceptance remains pending.
