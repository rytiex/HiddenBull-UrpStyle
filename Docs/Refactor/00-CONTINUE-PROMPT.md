# Continue Prompt — HiddenBull URP Style v1 Refactor

Use this instead of `00-START-PROMPT.md` when a new chat (or another account) picks up the running rebuild.
Paste everything below the line into the new chat. Keep the **Status** and **Next job** parts current at every
step change.

---

You are continuing the HiddenBull URP Style v1 rebuild (Unity 6000.4.1f1, URP 17.4, Render Graph) in
`C:\Users\rytie\Documents\Github\HiddenBull-UrpStyle`, branch `refactor/v1`. This continues an earlier session
exactly where it stopped.

**Status (2026-10-27):** Phases 0–4 are closed, and the fog fix (full-screen atmosphere pass with Fog Paint +
Silhouette) is confirmed. Phase 5 Grass is designed, including its optimisation plan (03 §9); nothing of the
grass is built yet.

Read before doing anything (do not ask me what these answer):

1. Your memory index `MEMORY.md` and the memories it links (in
   `C:\Users\rytie\.claude\projects\C--Users-rytie-Documents-Github-HiddenBull-UrpStyle\memory\`). Binding.
2. `Docs/Refactor/01-Vision-and-Rules.md`, `04-Known-Pitfalls.md`, `05-Roadmap.md`, `07-Tooling.md` in full.
3. `Docs/Refactor/03-Feature-Spec.md` §9 Grass (the "Decided 2026-10-27" and "Optimisation" blocks). Skim §8
   Terrain, since grass reads terrain data (`HBStyleTerrainLayer`, the terrain data store, the Brushes tab).
4. `Docs/Refactor/02-Architecture.md`: skim only.
5. `Docs/Refactor/08-Cost-Report.md`: only the last two sections (current budgets).
6. Old grass as reference only (look, not structure): find the files with
   `git ls-tree -r 3d4a06c --name-only | grep -i grass` and read them with `git show 3d4a06c:<path>`
   (StyleGrass.compute kernel Place, StyleGrassPass.cs, StyleGrassVertex.hlsl for the gust sweep).

**Next job: step 5.1 of Grass**: generation, drawing, per-layer grass and the Grass brush, with a density debug
view. Follow the optimisation block in 03 §9 (cached per-cell placement, per-blade work, no shadow casting,
near depth-first drawing). Start with a short design talk on the open points: the grass map format on the
terrain data, the per-layer grass fields, cell size, and the budgets (01 §5: Grass ≤ 2000 slots / 30 reads,
≤ 2 ms Ultra meadow). Then build it, run `Tools~/Check-All.ps1`, write the cost into 08, and give me one test
list. After my check: 5.2 look, 5.3 flowers + benders, 5.4 weather on grass.

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
- At every step change, update the Status and Next job parts of `Docs/Refactor/00-CONTINUE-PROMPT.md`.
