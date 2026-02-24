# Testing guidelines

These are the agreed testing preferences for this repo.

## Principles
- Keep tests deterministic and fast.
- Avoid touching Stardew Valley runtime state (`Game1`, save state, content pipeline).
- Prefer small unit tests over integration tests.
- Keep assertions simple (AAA: Arrange/Act/Assert).
- Use NSubstitute only when you need to isolate an interface.

## What to test
- Catalog validation/sanitization.
- Random group resolver behavior (including recursion/cycle safety).
- Daily planning determinism (fingerprints and roll keys).
