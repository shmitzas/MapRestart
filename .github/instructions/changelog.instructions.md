---
description: 'How to write CHANGELOG.md entries for this plugin. Trigger phrases: changelog, release notes, new release, version bump, what changed.'
applyTo: 'CHANGELOG.md'
---

# Changelog style

The changelog **is** the release body. `.github/workflows/release.yml` pulls the
section whose heading matches `PluginMetadata.Version` in `src/MapRestart.cs` and
publishes it verbatim, so write for the person reading the GitHub release page.

## Audience

A server admin deciding two things: *do I need this update*, and *what do I re-test
after installing it*. Not a reviewer, and not your future self debugging the fix.

## Shape

```markdown
## [1.2.0] - 2026-10-07

One-line summary, only when the release has a theme or a reason to hurry.

- What changed, from the admin's side.
- Another change.
```

- **Heading is `## [x.y.z] - YYYY-MM-DD`.** The version must equal
  `PluginMetadata.Version` exactly or the release fails — that check is deliberate.
- **Newest section first**, directly under the header comment.
- The optional lead sentence earns its place when it tells someone whether this
  release is for them: *"Worth updating if you use ready tags or captains."*
- Plain `-` bullets. No `Added` / `Changed` / `Fixed` sections, no bold lead-in
  labels — that is the K4ryuu-Upkeep house style, not this one.
- Wrap around 90 columns.
- Leave the `---` + "Older releases" footer at the bottom of the file.

## Content

1. **Outcome, not mechanism.** "The map could reload mid-match" — not which property
   the predicate tested. Internals belong in code comments.
2. **Name the symptom they saw.** The command, config key, cvar, in-game tag or error
   text, in `backticks`.
3. **One bullet per change.** If a bullet needs a semicolon and a subclause, it is two
   bullets, or it is too much detail.
4. **Say when a version was folded in**, e.g. *"Includes the work versioned 1.11.3,
   which was never released on its own."*
5. Link an issue as `([#N](url))` **only when that issue exists**. Never invent one.
6. Skip anything with no user-visible effect — refactors, comments, CI. A release with
   nothing worth saying does not need a release.

## Before committing

Read the bullets back as an admin. If one explains why the bug existed at the code
level, cut it to the symptom and the fix.
