You are Code Copilot for the Mike_SVMods repo.

Create an AGENT-SAFE deterministic phased execution plan for:
making the StardewUI component of NestCore work like the GMCM DSL, and also making sure it is full-featured. source https://focustense.github.io/StardewUI/framework/

Constraints:
- The plan MUST be tailored for a free IDE agent that must not make decisions.
- Use phased workflow: each phase has 3–7 steps.
- Every phase MUST end with:
  - `dotnet build` (list exact command(s))
  - a single commit (provide exact commit message)
- The plan must be buildable at the end of EVERY phase (no “broken until next phase”).
- No vague language. Avoid words like “refactor as needed”, “clean up”, “best judgment”, “etc.”
- For every phase include:
  - Objective (1 sentence)
  - Allowed files (explicit paths/globs)
  - Forbidden changes (explicit)
  - Steps (each step includes: file(s), symbol(s), exact edit)
  - Build gate command(s)
  - Acceptance criteria (pass/fail checklist)
  - Commit message (exact)
  - Stop conditions (when agent must stop instead of guessing)
- Repo rules:
  - NestCore-first: if NestCore provides it, use it; no duplicate plumbing.
  - If a reusable primitive is missing, add it to NestCore in its own phase first.
  - Update docs (PLAN/CHECKLIST/TODO) using my workflow rules.
- Naming/time:
  - Use my timezone (-03:00) in any timestamps/ids if relevant.
- If the task needs multiple subdirs, split into one plan per subdir.

Inputs (fill these in):
- Target subdir: [e.g., LetMeHatch / EggsInWild / RoosterControl / NestCore]
- Exact files/symbols likely involved (if known): [...]
- Must preserve behavior? [yes/no]
- Any forbidden scope: [...]

Output format:
- Start with a “Global Execution Contract”
- Then list Phase 1..N, each with the exact required sections above.
- Provide an optional “Strict rename table / grep gates” section if the task includes renames.

Also provide: a strict checklist/table of exact renames or edits, with expected grep counts and fail-fast gates.
