# Codex Collaboration Plan

## Context
- Track actions I (the assistant) take so the human partner can see what needs to happen next.
- Focus areas so far: understand Autoredi's generator + Flowgen upgrade path, note where the Flowgen skill instructions live, and capture Autoredi usage guidance in a dedicated skill.

## Flowgen skill notes
- The Flowgen skill referenced in AGENTS is located at `/usr/local/repo/flowgen-source-generator/SKILL.md` (plus the global `~/.codex/skills/flowgen-source-generators/SKILL.md`). It highlights Flowgen v0.7+ features such as `context.Flow()`/`Flow.Create(context)` starting points, `AttributeContext` in `.Select(...)`, and the existing `.Collect()`/`.EmitAll()` pipeline for N-to-1 outputs.

## Work items
1. [x] Upgrade `Autoredi.Generators` to Flowgen 0.7.0, switch the pipeline to `context.Flow()` and the new `AttributeContext` extractors, and keep the aggregated registration builder intact.
2. [x] Capture Autoredi-focused knowledge in a new skill at `/Users/omoi/.codex/skills/autoredi/SKILL.md` and reference it from AGENTS.
3. [ ] Keep waiting for the next set of instructions from the user once this groundwork is ready; update this plan accordingly.

*This file is git-ignored because it is meant for live collaboration notes only.*
