# P4.3 / P4.4 kabul ve kapanış — 8 Ekim 2026

Kullanıcı iki ara teslimin gerçek testlerinin tamamlanmasını, kapanmasını ve sonraki görevlerin açılmasını istedi. PR [#129](https://github.com/mehmetalisahingm/DeepDiveGame/pull/129), kabul edilen head `60e6306182dd171a68714d3d6f8a5528d5a4482a` ile `codex/p4-integration`a birleşti (`515f5b6`). Merge sonrası kaynak ağacı aynı: `git diff --stat 60e6306 origin/codex/p4-integration` boş.

## Kanıt

[CI 37699987867](https://github.com/mehmetalisahingm/DeepDiveGame/actions/runs/37699987867) ve [CI 37699982093](https://github.com/mehmetalisahingm/DeepDiveGame/actions/runs/37699982093) içinde üç kontrol SUCCESS: Unity EditMode/Windows build, gerçek iki süreçli filo/iki rota, gerçek iki süreçli boss/ikinci host restore. Yerel son paket 1097/1097 EditMode ve Windows build geçti; tam runtime kapanışı son yeşil CI artefaktlarına dayanır.

37699987867 artefaktları indirilip JSON'ları okundu: `Logs/P43-P44-accepted-ci`. CI merge-aday artefakt soneki `590ff2fae1525f318a51f18b3b36bc8d2fe1cd68`.

| Akış | Ham kayıt klasörü | Sonuç |
|---|---|---|
| Filo/gerçek Reef/satın alma/aktif seçim/restore | P1-integrated-20261007-231727-660-2 | host, guest, host-reload PASS |
| Motorlu/resif tam sefer | P1-integrated-20261007-232058-101-2 | host/guest PASS; doğru gövde, tripDone true |
| Araştırma/derin tam sefer | P1-integrated-20261007-232522-452-2 | host/guest PASS; doğru gövde, tripDone true |
| Gerçek boss/yeniden açılış | P44-boss-20261007-231744-529 | host/guest/host-reload PASS |

Bütün JSON errors listeleri boş. Boss hostta iki farklı kabul edilmiş saldırgan, gerçek kuru güvenli dönüş ve tek kalıcı completion var. İkinci host `bossReloadPass=true`: üç iz/discovery/completion geri gelir, Active transient encounter geri gelmez.

## Düzeltmeler

Tamirli kampanyada tüketilmiş parçaları tekrar toplama denemeleri kaldırıldı; repair/hidden-parts kontrolü korunur. Aktif araştırma teknesi seçimi gerçek satıcı oturumuyla yapılır. Büyük gövdede demirde iniş tekrar binme menziliyle uyumludur. Boss testinde iki oyuncu ayrı atış konumlarına ulaşır, yenilgiden sonra kuru dönüş alanına yürür. World'ün tekrarlı aktivasyonu yenilen boss'un bekleyen güvenli dönüş kaydını silmez. Test, ürünün önceden gözlemlediği gerçek tür kanıtını okuyabilir; tamamlanan gerçek oturum sonrası temiz kapanır.

## Kapanış

#107/#108 ve #121/#122/#123/#124 completed kapatıldı; #109 önceden kapalıydı. Eski PR #127, #129 tarafından karşılandı. P4.5 aktif: Mehmet #130, Utku #131, Mert #132; koordinatör Utku.

Bu kanıt tek makinede/CI'da iki gerçek süreçtir. Ayrı bilgisayar/internet, nihai görsel kalite, 1–4 oyuncu ürün dengesi ve P4.6 üç günlük ortak değerlendirme ayrıca doğrulanır. Diğer ekip üyelerinin manuel oynanış onayı yazılmadı. P4.6/P5/P6 açılmadı.
