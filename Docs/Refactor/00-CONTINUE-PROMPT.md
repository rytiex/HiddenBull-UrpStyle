# Continue Prompt — HiddenBull URP Style v1 Refactor

Use this instead of `00-START-PROMPT.md` when a new chat (or another account) picks up the running rebuild.
Paste everything below the line into the new chat. The **Status** and **Next job** parts are updated at every
phase close, together with the docs.

---

You are continuing the HiddenBull URP Style v1 rebuild (Unity 6000.4.1f1, URP 17.4, Render Graph) in
`C:\Users\rytie\Documents\Github\HiddenBull-UrpStyle`, branch `refactor/v1`. This continues an earlier session
exactly where it stopped.

**Status (2026-11-10):** Phases 0–5, 7 and 8 and the polish batch before Phase 6 are closed; 7 and 8 ran before 6
by my choice. Phase 5 Grass final state is the first block of 03 §9, Phase 7 Foliage + Billboards is 03 §4, Phase 8
Transparency is 03 §11 (sorted forward transparency replaced MBOIT, coloured glass shadows, blurred refraction,
Transparency quality levels). The polish batch rebuilt Receive Weather Moving (03 §7), added grass Blade Detail and
texture cards (03 §9) and the Scene view bake banner with Auto Bake (03 §8). Costs are the last sections of 08.
Colliders, harvest, the billboard leaf light fix and the overlay camera with glass were not run or tested in Unity.
The game has buildings that players build and tear down; vehicles use Moving, Static is only for what never moves.

Read before doing anything (do not ask me what these answer):

1. Your memory index `MEMORY.md` and the memories it links (in
   `C:\Users\rytie\.claude\projects\C--Users-rytie-Documents-Github-HiddenBull-UrpStyle\memory\`). Binding,
   especially `water-system-plan.md` and `confirm-before-new-work.md`.
2. `Docs/Refactor/01-Vision-and-Rules.md`, `04-Known-Pitfalls.md`, `05-Roadmap.md`, `07-Tooling.md` in full.
3. `Docs/Refactor/03-Feature-Spec.md` §10 Liquids, the first (final state) blocks of §8 Terrain and §9 Grass, and
   the memory `water-system-plan.md`.
4. `Docs/Refactor/02-Architecture.md`: skim only.
5. `Docs/Refactor/08-Cost-Report.md`: only the last section (current budgets).

**Phase 6 Liquids is in progress (2026-11-10).** Committed so far: terrain liquids, foam, caustics, pools,
cutters, underwater, ripples, SSR (`a20c5fa`..`7139c18`). On top of that, uncommitted work in the tree: the
shelter liquid roof (rain and snow stop at the water level, the cutter cut mask in the shelter map). The tree was
reset on 2026-11-10 to the exact state of 00:11 UTC, because the rounds after it went wrong. A copy of the tree
before that reset is in
`C:\Users\rytie\AppData\Local\Temp\claude\a5d0939c0293cfef\61e7a00b-7560-4e58-98f0-07281820c594\scratchpad\before-revert`
(it may be gone; it is only a reference). The memory `liquid-old-look-port.md` "Roof round 2..5" notes describe
that reverted code, not the tree.

**Next job: the six open liquid points, one at a time.** They are in my words below; take them literally. Before each
one, write in one or two lines what you understood and wait for my "evet". One change per point, then my check.

1. With baked light, snow on the terrain shows dark wavy stripes (the sand layer's ripples). Without a bake there
   are none. The same kind of APV problem happened on the terrain before and we solved it; use the same solution.
   Tried alone and failed: snow flattening the layer normal, snow fading bakedTones, snow lifting the layer AO.
2. Rain and snow are cut exactly at the water line, even a bit below it. They must end a bit above the water and
   soft, and this must also work in the far shelter map.
3. Cutter: the water's top surface also has an underside. The cutter does not cut that underside, so from inside
   the cut water (inside the cutter, below the level) the water surface is visible. That is the only problem.
   Lead (not tested): the camera "LiquidCut" depth pass in `TerrainLiquid.shader` is `Cull Back`.
4. Cutter: when the screen is under water, rain and snow inside the cutter disappear while I move.
5. Cutter: add a "roof" bool (Cut Out only) so moving things like boats do not carry rain and snow down. It worked
   in the reverted code: HBStyleLiquidCutter.roof, shelter cut mask .b, the top depth roof ignores .b, the level
   pass counts it.
6. Objects and the water floor do not get wet. It worked in the reverted code: a shore band in HB_ShelterCover
   (shore = saturate(1 - (y - level) / 0.6), wet += shore, snow *= 1 - shore) plus HBWeather.gloss = 1 - sunk so
   the ground under water darkens but does not shine.

After these: movement trails, then optimisation (Measure), then Phase 6 close.

**Phase 6 Liquids** (05, 03 §10; old liquids in commit `3d4a06c`, see 06). Build each step with a debug view,
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
- Before every hand-off, compile offline and end with "offline derlendi, Unity'de denenmedi".
- Keep each file's line endings (some files are CRLF, some LF).
- When I describe a problem, take my words literally; never replace them with your own theory.
- Simulate logic as C# with Unity's Roslyn csc (see the memories).
- If Edit/Bash calls get no verdict from the safety classifier: write the full files into the scratchpad with
  Write, then copy them into `Package/` with a single `cp -r`.
- Update the docs, the cost report and this prompt only at phase close.
