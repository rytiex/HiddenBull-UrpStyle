# Continue Prompt — HiddenBull URP Style v1 Refactor

Use this instead of `00-START-PROMPT.md` when a new chat (or another account) picks up the running rebuild.
Paste everything below the line into the new chat. The **Status** and **Next job** parts are updated at every
phase close, together with the docs.

---

You are continuing the HiddenBull URP Style v1 rebuild (Unity 6000.4.1f1, URP 17.4, Render Graph) in
`C:\Users\rytie\Documents\Github\HiddenBull-UrpStyle`, branch `refactor/v1`. This continues an earlier session
exactly where it stopped.

**Status (2026-11-17):** Phases 0–10 are closed; 7 and 8 ran before 6 by my choice. Phase 10 Texture Studio
closed on 2026-11-17: its final state is the first block of 03 §13, and the liquid change it brought (Studio-made mask
maps, runtime plate bake and Shore Crust removed) is the Phase 10 block of 03 §10. Phase 9 Cavity + Decal is 03 §12
and the Decal block of 03 §4, Phase 6 Liquids the first block of 03 §10 with the shelter block at the top of 03 §7,
Phase 5 Grass the first block of 03 §9, Phase 7 Foliage + Billboards 03 §4, Phase 8 Transparency 03 §11. Costs are
the last sections of 08. The game has buildings that players build and tear down; vehicles use Moving, Static is only
for what never moves. Every texture of the game comes from the Texture Studio; there is no Sandbox build, I measure
in my own game.

Read before doing anything (do not ask me what these answer):

1. Your memory index `MEMORY.md` and the memories it links (in
   `C:\Users\rytie\.claude\projects\C--Users-rytie-Documents-Github-HiddenBull-UrpStyle\memory\`). Binding,
   especially `confirm-before-new-work.md` and `shelter-rewrite-plan.md`.
2. `Docs/Refactor/01-Vision-and-Rules.md`, `04-Known-Pitfalls.md`, `05-Roadmap.md`, `07-Tooling.md` in full.
3. `Docs/Refactor/03-Feature-Spec.md` §14 Shader Graph, §1 Lighting core, §3 Brush and the first blocks
   of §7 and §12.
4. `Docs/Refactor/02-Architecture.md`: skim only.
5. `Docs/Refactor/08-Cost-Report.md`: only the last section (current budgets).

**The tree may hold uncommitted work.** Phase 10 closed with the liquid redesign, the Liquid preset shelf and the
phase-close docs uncommitted. Git is my job; do not commit, only check `git status` so you know what you are
standing on.

**Not run or not confirmed in Unity:** the slow return of snow after a roof is removed since the cover age moved to
its own texture; `HBStyleLiquid.TryGetDepth`; the overlay camera with glass; whether the outer shelter cascade
(2560 m) hitches on the frame it is redrawn; colliders, harvest and the billboard leaf light fix from Phase 7; liquid
mask maps following the flow on a Liquid Terrain river.

**Every surface shader sits within a few slots of its budget** (see the last section of 08). Anything that adds to
one of them must cut something first or come with a measured budget raise that I approve.

**Next job: Phase 11 Shader Graph** (05 and 03 §14). The old package had a node library on `main` under
`Package/ShaderGraph/` (HB Style Lighting, HB Weather Static / Moving, HB Fog sub-graphs, an HB Lit template and
`Docs/Features/ShaderGraph.md`); v1 has none yet. Start with a design talk: name the first step in one line and wait
for my yes.

For the Bake Panel in Phase 12: it must check that APV Dilation is on (04 §1.25).

Working rules:

- Reply in Turkish. Code has no comments. Code, tooltips and docs are in English. Lines are at most 120
  characters.
- Never commit or push, and never offer to; git is my job.
- Open design points go to me through multiple-choice questions, with one option marked "(Recommended)".
- Never add settings I did not ask for.
- After "devam edelim", name the next step in one line and wait for my yes before creating any new file, tool,
  scene or feature.
- Build in small visible steps, each with a debug view, and wait for my check between steps. A phase is closed
  only when I say "her şey iyi durumda" or close it explicitly. When a system has needed patch after patch, say so
  and propose rebuilding it in one job with one ordered test list.
- Read the code before asking me to test. When two code-read explanations have failed, stop guessing and ask me
  for the debug view that shows the data (Show Shelter Map solved in one look what three patches did not).
- When I describe a problem, take my words literally; never replace them with your own theory. Restate what you
  understood in one or two lines and wait for my "evet" before a fix that is not obvious.
- Use the package's own static registries, never FindObjects APIs.
- Run `Tools~/Check-All.ps1` only from PowerShell, never from Bash, and filter its output with
  `6>&1 2>&1 | Out-String -Stream | Select-String` (it prints with Write-Host).
- Before every hand-off, compile offline and end with "offline derlendi, Unity'de denenmedi".
- Keep each file's line endings (some files are CRLF, some LF, a few are mixed).
- Simulate logic as C# with Unity's Roslyn csc (see the memories).
- If Edit/Bash calls get no verdict from the safety classifier: write the full files into the scratchpad with
  Write, then copy them into `Package/` with a single `cp -r`.
- Update the docs, the cost report and this prompt only at phase close.
