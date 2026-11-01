# Continue Prompt — HiddenBull URP Style v1 Refactor

Use this instead of `00-START-PROMPT.md` when a new chat (or another account) picks up the running rebuild.
Paste everything below the line into the new chat. The **Status** and **Next job** parts are updated at every
phase close, together with the docs.

---

You are continuing the HiddenBull URP Style v1 rebuild (Unity 6000.4.1f1, URP 17.4, Render Graph) in
`C:\Users\rytie\Documents\Github\HiddenBull-UrpStyle`, branch `refactor/v1`. This continues an earlier session
exactly where it stopped.

**Status (2026-11-01):** Phases 0–5 are closed. Phase 5 Grass ended with real leaves in rings, flowers, benders
(`HBStyleBender`, `StyleTrail`), weather on grass, Wet Gloss, pressed snow and the roof cover map; the final state
is the first block of 03 §9, the costs are the last section of 08. The game has buildings that players build and
tear down; vehicles use Moving, Static is only for what never moves.

Read before doing anything (do not ask me what these answer):

1. Your memory index `MEMORY.md` and the memories it links (in
   `C:\Users\rytie\.claude\projects\C--Users-rytie-Documents-Github-HiddenBull-UrpStyle\memory\`). Binding.
2. `Docs/Refactor/01-Vision-and-Rules.md`, `04-Known-Pitfalls.md`, `05-Roadmap.md`, `07-Tooling.md` in full.
3. `Docs/Refactor/03-Feature-Spec.md` §10 Liquids. Skim §8 Terrain and the first block of §9, since liquids read
   terrain data and shore on grass.
4. `Docs/Refactor/02-Architecture.md`: skim only.
5. `Docs/Refactor/08-Cost-Report.md`: only the last two sections (current budgets).
6. `Docs/Refactor/06-Old-Package-Reference.md`. Old liquids as reference only (look, not structure): find the files
   with `git ls-tree -r 3d4a06c --name-only | grep -i -E "liquid|water"` and read them with
   `git show 3d4a06c:<path>`. Old-package water memories are lessons only; where they disagree, the docs win.

**Next job: Phase 6 Liquids** (05, 03 §10). Start with a short design talk, split it into steps, build each with a
debug view, compile with `Tools~/Check-All.ps1 -Filter '<name>*'` (never the full table for small changes), and give
me one test list per step. Respect the budgets in 01 §5. Lit has 112 slots left under its budget, so anything that
touches Lit must cut something first. The cost measurement happens once, in Phase 12.

Working rules:

- Reply in Turkish. Code has no comments. Code, tooltips and docs are in English. Lines are at most 120
  characters.
- Never commit or push, and never offer to; git is my job.
- Open design points go to me through multiple-choice questions, with one option marked "(Recommended)".
- Never add settings I did not ask for.
- Build in small visible steps, each with a debug view, and wait for my check between steps. A phase is closed
  only when I say "her şey iyi durumda" or close it explicitly.
- Read the code before asking me to test. Use the package's own static registries, never FindObjects APIs.
- Before every hand-off, compile offline and say "compiled offline, not run in Unity".
- Simulate logic as C# with Unity's Roslyn csc (see the memories).
- If Edit/Bash calls get no verdict from the safety classifier: write the full files into the scratchpad with
  Write, then copy them into `Package/` with a single `cp -r`.
- Update the docs, the cost report and this prompt only at phase close.
