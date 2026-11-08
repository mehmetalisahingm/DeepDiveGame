# #218 — Beş tür: gerçek 2 ve 4 süreçte görünürlük (8 Kasım 2026)

Mert · taban `codex/p4-integration` (`ba8127a`) · entegre Windows oynatıcısı (`tools/Build-P1.ps1 -Integrated`) · `tools/Test-P411-SpeciesVisibility.ps1`.

Gözlem aracıdır, hüküm vermez: her süreç canlı sahnede hangi türü kaç bireyle gördüğünü yazar; host grafik cihazıyla beş türün yakın ve uzak resmini de yazar, istemciler grafiksiz (`-nographics`) çalışır.

## Sonuç

| Koşu | Süreç raporu | Her süreçte |
|---|---|---|
| 2 süreç (host + 1 istemci) | 2 / 2 | 15 balık: `sea_bass` 3/3, `tuna` 3/3, `red_snapper` 3/3, `grouper` 3/3, `lanternfish` 3/3 görünür |
| 4 süreç (host + 3 istemci) | 4 / 4 | aynı: 15 balık, beş türün hepsi 3/3 görünür |

Host resimleri: `docs/reports/evidence/p411-species-visibility-20261108/` (beş tür × yakın/uzak). Süreç raporları aynı klasörde (`species-*.json`).

## Bu kanıtın karşıladığı / karşılamadığı (#218 kabul maddeleri)

- **Karşılanan:** "gerçek 1/2/4 süreç tür görünürlüğü" maddesinin 2 ve 4 süreç kısmı (aynı bilgisayarda, ayrı süreçler); Windows build'inde her tür için yakın/uzak resim.
- **Karşılanmayan:** ayrı bilgisayar ve insan değerlendirmesi (#164); "beş tür normal hızda siluet ve hareketle ayrılır" kararı bu resimlerle verilmez, insan bakışı gerekir; uçtan uca av → satış → ansiklopedi zincirinin beş tür için tekrar kanıtı ve eski ödenmiş avın çift ödenmemesi (#254/#267 ve ProductDay kanıtlarında tek tür için var, beşi ayrı ayrı yapılmadı).
- Başkası adına kabul yazılmadı; #218 açık kalır.
