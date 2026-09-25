# CLAUDE.md

Development practices for working in this repo. Applies to every change, human- or agent-authored.

## Test-Driven Development

Every behavior change follows red-green-refactor:

1. **Red** — write a failing test for the behavior you're about to add or fix. Run it. Confirm it fails for the reason you expect, not for an unrelated compile error.
2. **Green** — write the minimum implementation to make that test pass. Run the full suite (`dotnet test`), not just the new test.
3. **Refactor** — clean up implementation or test code with the suite green throughout. Re-run after each change.

No implementation code is written before its test exists and has been run red. This holds for bug fixes too: reproduce the bug as a failing test before touching the fix.

A test always ships in the same commit as the behavior it covers — never a separate "add tests" commit trailing behind.

Use the `superpowers:test-driven-development` skill and `mattpocock-skills:tdd` skill for the mechanics of this loop.

## Verification

```
dotnet build   # TreatWarningsAsErrors is on repo-wide (Directory.Build.props) — a warning fails the build
dotnet test
```

Both must pass clean before a change is considered done. There is no CI yet (tracked in [ROADMAP.md](ROADMAP.md)) — this is the only verification gate today.

## Commit and documentation conventions

- **[Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/), strictly** — every commit message follows the spec, no exceptions: `<type>[optional scope]: <description>`, scope naming the affected area — `fix(pipes): ...`, `feat(validation): ...`, `refactor(configuration): ...`, `test(validation): ...`, `docs: ...`, `chore: ...`. Match scope names to the folders under `src/` (`pipes`, `validation`, `configuration`, `reflection`, `exceptions`) or omit scope for cross-cutting docs. A breaking change gets `!` after the type/scope and a `BREAKING CHANGE:` footer, per spec.
- Commit bodies explain **why**, not what — the diff already shows what changed. Reference the specific bug or design flaw being corrected.
- **New commits, not amendments.** Never rewrite history that's already landed.
- An **ADR** (Architecture Decision Record) goes in `docs/adr/`, numbered sequentially (`docs/adr/000N-kebab-case-title.md`), for any decision that changes an established behavior or resolves an ambiguity in the design — not for routine feature additions. Follow the existing ADRs' shape: a one-line title stating the decision, then prose covering the problem, the change, and why the alternative was rejected.
- Domain vocabulary (canonical terms, deliberately avoided terms) lives in [CONTEXT.md](CONTEXT.md) — the single source of truth. Update it when a change introduces or retires a concept; don't restate definitions elsewhere.
- [ROADMAP.md](ROADMAP.md) tracks only outstanding gaps. Remove an entry the commit that closes it; don't leave it for a separate cleanup pass.

## Project structure

- `src/conduit` — core: `IConduit`, `IRequest<TResponse>`, `IRequestHandler<,>`, pipe/stage machinery, configuration builders.
- `src/conduit.common` — shared low-level helpers (`IEnvironment`, `HashUtil`, extensions) with no dependency on `conduit` core.
- `src/conduit.logging` — logging abstraction (`ILog`) and console implementation.
- `src/conduit.validation` — optional validation package (`ModelValidator<,>`, `RuleBuilder`, `ValidationStage`). Depends on `conduit` core; core must never depend back on it — cross the boundary only through marker interfaces (`IValidationPipeStage`, `IPassthroughException`), per [ADR-0007](docs/adr/0007-configuration-interfaces-own-their-real-surface.md).
- `conduit.tests` — one xunit project for everything, mirroring `src`'s folder structure by area (`Validation/`, `Stages/`, `Handlers/`, `Logging/`, `Helpers/`). Put a new test in the folder matching what it covers, not next to unrelated tests.

Stack: .NET 10 (`net10.0`), nullable reference types on, `LangVersion` preview. Test stack: xunit, FluentAssertions, Moq.

## AGENTS.md

[AGENTS.md](AGENTS.md) points here. This file is the single source of truth for how to work in this repo — don't fork guidance into AGENTS.md itself.
