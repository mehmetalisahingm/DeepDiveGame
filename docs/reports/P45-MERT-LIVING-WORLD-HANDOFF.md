# P4.5-C — Mert living world implementation / acceptance handoff

Scope: [#132](https://github.com/mehmetalisahingm/DeepDiveGame/issues/132). Base: `codex/p4-integration`. This branch contains a working-code candidate, **not** a completed Unity / multiplayer acceptance.

## Implementation

- `LivingWorldAuthority`: one deterministic daily fish order and one sponsor; ids and settled host evidence are persistent; only a verified fish sale at the real NPC or an archived, commercially valid *settled* channel post can advance goals. Client GUI cannot submit evidence or credits.
- Three fish order templates currently use existing, accessible `sea_bass`: 1 fish → 55 credits, 2 → 105, 3 → 165. A wider species pool requires Utku's trustworthy world/eligibility read model.
- Sponsor templates: first *verified* species recording (120), Gold+ commercial recording (175), verified nighttime recording (220). The night template exists but is **not eligible for selection yet**: current clip manifest has no authoritative night-capture proof. Never substitute client clock or upload title for Utku's evidence.
- Reward ids derived from day + template; committed settlement ids and spent upgrades share the existing economy campaign save. Host sale journal records each settled capture/day before the NPC transaction persists, allowing crash-safe reconciliation.
- Free role selection: CameraOperator/Hunter/Explorer/Carrier; applied through Mehmet's `CrewRoleEffectBinding`, with no stat modifier stacking. Host selection persists; guest role remains session-scoped under current D06 session-player identity, and needs verified reconnect behavior.
- Two visible home improvements (archive rack then trophy gallery) and three visible improvements at town service anchors (fish market, equipment display, harbor lights). Primitive presentation objects are placeholders for Mert art; they appear only when bought and persist in the campaign file. The archive upgrade adds **10 real shared storage slots** (40 → 50). The fish-market counter gives **10% more real fish NPC revenue**. The equipment display grants a **5% nonstacking discount** on catalog equipment and the harbor lights grant a **5% nonstacking discount** on purchased boats. These are purchase-time economy effects; existing route/world authorities are unchanged.
- Daily board is shown at the equipment shop using **B**; only host-validated role/upgrade intents are sent over NGO. Host state is mirrored to guest clients as a read-only snapshot. A fresh day is not generated until original day and media data have been restored.
- Schema v8 carries living-world state, upgrades and NPC catch journal; earlier save schema versions remain supported.

## Quick balance audit (working values, not measured gameplay balance)

| Action | Credits |
|---|---:|
| Original sea_bass sale | 120 |
| Upgraded fish market sea_bass sale | 132 |
| Shore 1 / 2 / 3 order bonuses | 55 / 105 / 165 |
| Sponsor first species / Gold / night | 120 / 175 / 220 |
| Home archive / trophy gallery | 280 / 460 |
| Fish market / equipment display / harbor lights | 240 / 330 / 420 |
| Equipment display / harbor discount | 5% of listed price (nonstacking) |

Prices must be adjusted only after real solo/1–4-player game loop timings; these are starter values with no artificial recurring fees or negative-balance path.

## Code and safety coverage

Added NUnit EditMode tests for deterministic daily template selection, verified sale replay, settled verified sponsor outcome, spoofed/unsettled recording refusal, free role changes/save, mandatory house upgrade order, actual shared-storage capacity, no repeated upgrades, and persistence-failure role rollback. **These tests are committed but have not been executed against a Unity editor yet.** No Windows player build, visual proof, or two-process co-op evidence has been executed/claimed.

## Gate checklist — keep #132 open until complete

- [ ] Unity 6000.3.23f1 EditMode suite PASS on the current combined branch.
- [ ] Windows x64 build PASS and verify the new `*.cs.meta` imports.
- [ ] Two real processes see identical objectives; a real safe-return fish sale and a real publish→settlement advance the objectives and pay exactly once.
- [ ] Save/load/rejoin/replay and disk-failure paths preserve balance, host role, depot contents, channel rights, and visible decorations.
- [ ] Confirm/repair guest role reconnect strategy with stable identity; do not persist raw NGO client IDs.
- [ ] Wire Utku's authoritative night/access eligibility before enabling nighttime sponsor or harder habitat templates.
- [x] Equipment and harbor shop purchases receive a permanent 5% economy effect without modifying route authority.
- [ ] Replace primitive placeholder decoration visuals with reviewed assets and measure the in-game effect.
- [ ] Execute economy/equipment/boat price balance pass in real gameplay, document timings and screenshot UI.

This is intentionally a **draft candidate**, not a claim of full P4.5 completion.
