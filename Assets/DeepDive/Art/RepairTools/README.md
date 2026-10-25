# Repair tool art — project-authored support for #187

Original mesh/material/audio assets authored for DeepDiveGame, 25 October 2026.
No third-party models, images or recordings were downloaded or repackaged.
The source is `RepairToolArtBuilder.cs`; regenerate from
**DeepDive / P4 / Generate repair tools**. Existing asset GUIDs are preserved.

- **OakRepairPlank:** chipped/chamfered oak, physical grain/knot detail and two
  hand grip markers. Approximately 78 cm long. No rigidbody or collider.
- **CarpentersAdze:** shaped oak handle, seven grip bindings, bevelled forged
  steel head and worn steel edges. `GripPoint` is the wrist anchor;
  `ToolContact` is the hammer poll's nail contact point.
- **ForgedBoatNail:** shaped steel head, shank and tapered point; `ContactPoint`
  is the head surface. It does not own interaction or physics.
- **OakRepairPatch:** three fitted slats for an existing logical repair group;
  `FastenerSurface` defines where driven nail heads sit flush. This is not a
  replacement hull or a new repair/save schema.
- **OakNailImpact.wav:** original deterministic wood/metal impact synthesis,
  22050 Hz, mono, 16-bit, 0.16 seconds. No microphone recording.

The catalog binds these assets into the player's hands, material rack and
repair slots. Host-accepted hits drive nail seating, tool contact and sound;
visuals never grant material, money or completion. Existing slot colliders,
IDs, save transactions and world anchors remain authoritative.

User authorization permits asset support for Mehmet's work. This pack does
not assert Mert's acceptance or completion of #189's full broken/repaired
Rowboat states. Final team/visual acceptance is deferred to #164; #207's
older multi-process regression remains separately tracked.
