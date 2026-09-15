# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

This is a greenfield ASP.NET Core Razor Pages app (`pmsa`, .NET 10) still at the `dotnet new webapp` scaffold stage — `Pages/Index`, `Privacy`, and `Error` are the untouched defaults, and `Program.cs` wires up nothing beyond `AddRazorPages()`. Everything of substance in the repo so far is the *planning* toolchain described below. The product itself is defined by `docs/product/story-map.md` and `docs/specs/` once those exist.

## Commands

```bash
dotnet run                      # http://localhost:5203 (+ https://localhost:7223 with --launch-profile https)
dotnet build
dotnet watch run                # hot reload during page development
```

There is no test project yet. When adding one, create it as a sibling directory with its own `.csproj` (the repo has no `.sln`; `dotnet run` works because the working directory contains exactly one project file). Run a single test with `dotnet test --filter FullyQualifiedName~SomeTest`.

## Planning workflow

The repo encodes a three-stage flow from idea to code. Follow it rather than jumping straight to implementation when the user describes a new feature:

1. **`/story-mapping`** — the `story-mapping` agent (`.claude/agents/story-mapping.md`) interviews the user and writes `docs/product/story-map.md` with globally numbered stories (001, 002, …). It writes only markdown, never code.
2. **`/spec <story>`** — the `spec` skill (`.claude/skills/spec/SKILL.md`) walks through problem discovery → requirements → Gherkin scenarios → domain model → NFRs and writes `docs/specs/NNN-<slug>.md` using `.claude/skills/spec/spec-template.md`. **The `NNN` prefix must be the story number from the story map** — that number is the thread linking map → spec → implementation.
3. **Implementation** — driven by the spec's functional requirements and acceptance scenarios.

`docs/architecture/` holds an arc42 skeleton (sections 01–12, currently empty templates). Specs are meant to *reference* these sections rather than duplicate them: project-wide quality requirements belong in `10-quality-requirements.md`, per-feature NFRs in the spec. Fill in the relevant arc42 section when an architectural decision is made, and log it in `09-architecture-decisions.md`.

## Issue tracking

Issues live in **https://github.com/GUBO-VRB/pmsa** (`owner: GUBO-VRB`, `repo: pmsa`). That is the only repository to touch — never infer a different one from the local remote or the working directory.

Always go through the **github MCP server** for issue work; this overrides the general guidance to reach for the `gh` CLI. The relevant tools:

- `mcp__github__issue_write` — create (`method: "create"`) and update (`method: "update"`)
- `mcp__github__issue_read` — fetch a single issue
- `mcp__github__list_issues` / `mcp__github__search_issues` — browse and search; search first to avoid duplicates
- `mcp__github__add_issue_comment` — comment
- `mcp__github__sub_issue_write` — parent/child links

Set `state_reason` whenever closing an issue. Creating or commenting on an issue is outward-facing — confirm with the user first unless they asked for it in the current turn.

## Conventions

- Root namespace is `pmsa` (lowercase); page models live in `pmsa.Pages`.
- `Nullable` and `ImplicitUsings` are enabled — no `using` blocks for the common BCL/ASP.NET namespaces, and annotate reference types honestly.
- Static assets go through `MapStaticAssets()`/`WithStaticAssets()` (the .NET 9+ fingerprinting pipeline), so reference them via `_Layout.cshtml`'s existing `~/` links rather than hand-written hashed paths.
- There is no `.gitignore`, so `obj/` build artifacts are currently tracked. Adding one (and `git rm -r --cached obj`) is a reasonable early cleanup, but check with the user first since it rewrites what's committed.
