# P4.4-A — boss network gameplay / encounter authority (#121)

Tarih: 2026-10-06. Branch: `p4/mehmet-deep-boss-network`. Stacked base: Mert #125 / `p4/mert-deep-progression-save`.

## Uygulama
- `BossEncounterAuthority`: transient lifecycle `Locked → Available → Active → Completed`, host-owned health ve player-scoped hit idempotency.
- Progression gate doğrudan Mert'in `BossProgression.IsAvailable/IsCompleted` read seam'ini tüketir; ikinci progression/save authority yoktur.
- `BossEncounterActor : NetworkBehaviour, IHarpoonTarget`: mevcut `NetworkPlayer` zıpkın raycast'i boss'a doğrudan çalışır. Yeni RPC veya ikinci combat yolu yoktur.
- Phase / health / revision server-write `NetworkVariable` ile bütün süreçlere aynalanır.
- Boss sıfıra indiğinde completion yalnız `DeepProgressionEvidence.TryCompleteBoss` kabul ederse kalıcı tamamlanmış sayılır.
- Save/completion reddedilirse boss 1 HP ile Active kalır; yeni geçerli hit completion yazımını tekrar deneyebilir.
- Bitmemiş encounter abort/despawn olduğunda health kalıcılaştırılmaz; `Available + MaxHealth` durumuna normalize edilir.
- `BossEncounterRuntime` Utku #122 host arena trigger'ına RPC'siz `TryActivate/Abort` seam'i verir.

## Kanıt / test kapsamı
`BossEncounterAuthorityTests`:
- progression kilidi ve lifecycle,
- yanlış encounter kimliği,
- mevcut `HarpoonHit` ile damage,
- aynı player/request replay'inde ikinci damage olmaması,
- iki oyuncunun aynı request numarasının bağımsız olması,
- persistence ACK gelmeden completion sayılmaması,
- failed completion sonrası retry,
- abort sonrası transient health temizliği,
- NaN/0 damage reddi,
- provider rebind sırasında aktif encounter'ın yanlış gerilememesi.

## Açık kabul
- Bu branch gerçek deep arena/spawn geometry üretmez; #122 Utku alanıdır.
- Gerçek scene `BossEncounterActor` yerleşimi/spawn'ı #122 ile bağlandıktan sonra #124 fixture'sız 2-process smoke çalıştırılmalı.
- P4.3 (#119/#107/#108) kapanmadan final merge/kapanış yapılmaz.
- Bu oturumda Unity runner sonucu henüz yok; CI veya proje makinesi sonucu görülmeden PASS iddiası yoktur.
