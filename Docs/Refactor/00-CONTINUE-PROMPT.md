# Continue Prompt — HiddenBull URP Style v1 Refactor

Use this instead of `00-START-PROMPT.md` when a new chat (or another account) picks up the running rebuild.
Paste everything below the line into the new chat. The **Status** and **Next job** parts are updated at every
phase close, together with the docs.

---

You are continuing the HiddenBull URP Style v1 rebuild (Unity 6000.4.1f1, URP 17.4, Render Graph) in
`C:\Users\rytie\Documents\Github\HiddenBull-UrpStyle`, branch `refactor/v1`. This continues an earlier session
exactly where it stopped.

**Status (2026-11-02):** Phases 0–5, 7 and 8 are closed; 7 and 8 ran before 6 by my choice. Phase 5 Grass final
state is the first block of 03 §9, Phase 7 Foliage + Billboards is 03 §4, Phase 8 Transparency is 03 §11 (sorted
forward transparency replaced MBOIT, coloured glass shadows, blurred refraction, Transparency quality levels). Costs
are the last sections of 08. Colliders, harvest, the billboard leaf light fix and the overlay camera with glass were
not run or tested in Unity. The game has buildings that players build and tear down; vehicles use Moving, Static is
only for what never moves.

Read before doing anything (do not ask me what these answer):

1. Your memory index `MEMORY.md` and the memories it links (in
   `C:\Users\rytie\.claude\projects\C--Users-rytie-Documents-Github-HiddenBull-UrpStyle\memory\`). Binding,
   especially `polish-batch-2026-11-02.md` and `confirm-before-new-work.md`.
2. `Docs/Refactor/01-Vision-and-Rules.md`, `04-Known-Pitfalls.md`, `05-Roadmap.md`, `07-Tooling.md` in full.
3. `Docs/Refactor/03-Feature-Spec.md` §7 Weather (the Moving parts: drops and flakes, Weather Body, step 5) and the
   Weather point of §4.
4. `Docs/Refactor/02-Architecture.md`: skim only.
5. `Docs/Refactor/08-Cost-Report.md`: only the last section (current budgets).

**Next job: the polish batch before Phase 6** (agreed with me on 2026-11-02, in this order, each one confirmed by me
before it starts):

1. Done (closed 2026-11-03): the Receive Weather Moving rebuild, final state in 03 §7 (Weather Body slots, own rain
   and snow clocks, fixed cell kinds, beads shrink one by one, bead plane from the smooth normal). The per-face
   memory for flipped bodies was dropped.
2. Next: Grass "Blade Detail" per quality level: fewer leaves per tuft and triangles per leaf, leaves widened to keep
   coverage.
3. Custom grass textures as alpha-cut cards per terrain layer, through the same GPU placement and brush shading.

Then **Phase 6 Liquids** (05, 03 §10; old liquids in commit `3d4a06c`, see 06). Build each step with a debug view,
compile with `Tools~/Check-All.ps1 -Filter '<name>*'` (never the full table for small changes), and give me one
test list per step. Respect the budgets in 01 §5. Lit has 95 slots left under its budget (the Moving triplanar
row 64), so anything that touches Lit must cut something first. The cost measurement happens once, in Phase 12.

Working rules:

- Reply in Turkish. Code has no comments. Code, tooltips and docs are in English. Lines are at most 120
  characters.
- Never commit or push, and never offer to; git is my job.
- Open design points go to me through multiple-choice questions, with one option marked "(Recommended)".
- Never add settings I did not ask for.
- After "devam edelim", name the next step in one line and wait for my yes before creating any new file, tool,
  scene or feature.
- Build in small visible steps, each with a debug view, and wait for my check between steps. A phase is closed
  only when I say "her şey iyi durumda" or close it explicitly.
- Read the code before asking me to test. Use the package's own static registries, never FindObjects APIs.
- Run `Tools~/Check-All.ps1` only from PowerShell, never from Bash.
- Before every hand-off, compile offline and say "compiled offline, not run in Unity".
- Simulate logic as C# with Unity's Roslyn csc (see the memories).
- If Edit/Bash calls get no verdict from the safety classifier: write the full files into the scratchpad with
  Write, then copy them into `Package/` with a single `cp -r`.
- Update the docs, the cost report and this prompt only at phase close.
