# Codex Collaboration Plan

## Context
- Track actions I (the assistant) take so the human partner can see what needs to happen next.
- Focus areas so far: understand Autoredi's generator + Flowgen upgrade path, and note where the Flowgen skill instructions live.

## Flowgen skill notes
- AGENTS instructions now have a symlink at `/Users/omoi/Developer/@repo/_dev/Autoredi/docs/AGENTS-link.md` so I can re-open the instructions easily.
- The Flowgen skill referenced there is located at `/usr/local/repo/flowgen-source-generator/SKILL.md` (plus the global `~/.codex/skills/flowgen-source-generators/SKILL.md`). It highlights Flowgen v0.7+ features such as `context.Flow()`/`Flow.Create(context)` starting points, `AttributeContext` in `.Select(...)`, and the existing `.Collect()`/`.EmitAll()` pipeline for N-to-1 outputs.

## Work items
1. [ ] Upgrade `Autoredi.Generators` to Flowgen 0.7.0, switch the pipeline to `context.Flow()` and the new `AttributeContext` extractors, and keep the aggregated registration builder intact (this change is currently in progress).
2. [ ] Wait for the next set of instructions from the user once this groundwork is ready; the plan file will keep evolving as more tasks land here.

*This file is git-ignored because it is meant for live collaboration notes only.*
