# #311 school alarm: 8 m vs 4 m (real Windows player, 2 processes, local, muted)

Each configuration: 4 × `Test-P46-ProductDay.ps1 -Players 2 -SharedHunt -Mute` (guest films the sea bass the host hunts)
and 1 × `-Players 2 -HabitatHunt -Mute` (host-side school sampling).

| Configuration | Shared hunt | Guest clip (s) | Sea bass fleeing share | School spread (m) | Speed (m/s) |
|---|---|---|---|---|---|
| Integration before the fix (8 m, alarm relayed by fleeing mates) | 3/4 PASS (1 FAIL: guest clip hit its 15 s limit) | 6.9, 7.0, 13.6, 15.0 | **0.842** | 6.86 | 0.52 |
| Fix, 8 m (alarm only from a fish that sees the diver) | **4/4 PASS** | 7.8, 8.1, 7.1, 6.7 | 0.041 | 2.01 | 1.21 |
| Fix, 4 m | **4/4 PASS** | 6.3, 6.9, 6.3, 7.7 | 0.047 | 2.24 | 1.20 |

Finding: two schoolmates kept each other running (each saw the other fleeing), so the school almost never stopped and
the hunt dragged on. With the alarm raised only by a fish that sees the diver, both radii hunt the same and keep a tight
school; 4 m gives no measurable gain over 8 m. The branch keeps 8 m; the permanent value is Mehmet's call.
