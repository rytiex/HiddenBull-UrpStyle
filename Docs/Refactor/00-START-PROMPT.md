# Start Prompt — HiddenBull URP Style v1 Refactor

Paste everything below the line into the new chat.

---

You are continuing work on **HiddenBull URP Style**, a Unity 6 / URP 17 render package, in this repository
(`C:\Users\rytie\Documents\Github\HiddenBull-UrpStyle`). The current package works but grew heavy and patchy.
We are rebuilding it **from scratch on a new branch** with a clean, shared, performance-first architecture.
The old code stays in git history as a reference only.

Before you write or plan anything, read these documents in order. They contain every decision, requirement
and lesson you need. Do not ask me about things these documents already answer.

1. `Docs/Refactor/01-Vision-and-Rules.md` — goals, priorities, how I want you to work. **Binding.**
2. `Docs/Refactor/02-Architecture.md` — the target architecture and module boundaries.
3. `Docs/Refactor/03-Feature-Spec.md` — what every system must do (keep / change / new).
4. `Docs/Refactor/04-Known-Pitfalls.md` — bugs we already paid for, Unity/URP traps, rejected approaches.
5. `Docs/Refactor/05-Roadmap.md` — phase order, deliverables and checkpoints.
6. `Docs/Refactor/06-Old-Package-Reference.md` — map of the old code: what to reuse, what to drop.
7. `Docs/Refactor/07-Tooling.md` — offline shader and C# compile recipes you must run before every hand-off.

Then:

- Reply in **Turkish**. Code (without comments), tooltips and documentation are in **English**.
- Give me a short summary (max ~15 lines) of how you understood the plan and list anything that genuinely
  contradicts itself across the documents. Do not restate the documents.
- Create the branch `refactor/v1` (I commit and push myself; never commit or push).
- Start **Phase 0** from the roadmap with its design discussion. Open design points go to me as
  multiple-choice questions with one option marked "(Recommended)".

Hard rules, repeated here because they matter most:

- **Optimisation is the first priority.** Measure shader cost offline (instruction slots / texture reads) and
  state budgets. No feature lands without its cost.
- **Shared building blocks.** One lighting core, one brush, one atmosphere, one weather module — every shader
  composes them; no shader carries features it does not use.
- **Root-cause fixes only.** No speculative patches, no stacking tweaks. If a visual target is missed twice,
  stop and name the missing component.
- **Ask about my scene setup instead of inferring it** from asset files.
- **Never add settings or rules I did not ask for.** If a side effect needs a decision, ask.
