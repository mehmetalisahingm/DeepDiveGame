# P4.4-B — deep world, iz noktaları ve boss arena (#122)

Tarih: 2026-10-07. Dal: `p4/utku-deep-world-boss-arena`. Stacked base: #126 → #125 → P4.3.

## Uygulama
- Gerçek deep world içinde üç kararlı trace: `deep-trace-1..3`; sayısı `DeepProgressionIds.RequiredTraceCount` ile aynıdır.
- Gerçek arena: `arena-deep-1`, encounter: `encounter-deep-1`.
- Host yalnız gerçek `NetworkPlayer` pozisyonundan exploration cell ve `WaterDepth` bandını çıkarır; client claim'i yoktur.
- `IDeepProgressionWorld` exact trace/arena id + exact grid cell + `Deep` band zorunluluğuyla fail-closed doğrulama yapar.
- Trace noktaları söylenti öncesi gösterilmez; boss target Discovery/encounter öncesi vurulabilir değildir.
- Boss local collider yalnız mevcut `IHarpoonTarget` yolunu tüketir; health/network/save authority taşımaz.
- Tek boss transient authority mevcut `SessionNetworkAdapter` üzerinde `BossEncounterSessionBinding` ile hostta çalışır; client'lara yalnız phase/health/revision snapshot'ı aynalanır.
- Arena boşalırsa bitmemiş encounter abort edilip transient sağlık temizlenir.
- Map/progression sözleşmesine trace/arena/boss dünya koordinatı yazılmaz.

## EditMode kanıtı
`DeepEncounterWorldTests`:
- 3 trace id/pozisyonu benzersiz,
- her trace ayrı exploration cell'inde,
- trace/arena konumları gerçek Deep bandında,
- yanlış id/cell/band reddedilir,
- arena exact context ister,
- shared progression state raw `Vector3` taşımaz.

## Final kabul
- CI EditMode + Windows build sonucu bu branch'in GitHub Actions run'ında alınacak.
- Fixture'sız 2-process gerçek progression → trace → arena → boss damage/completion → reload, #124 final acceptance branch'inde çalıştırılacak.
