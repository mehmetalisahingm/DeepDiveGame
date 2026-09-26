# P4.1 fiziksel ev + gün: gerçek 2-süreç kabul (PR #92 üstünde, #88/#90)

Tarih: 2026-09-26. Taban: `codex/p4-integration` @ `4accae9` (PR #92 birleşti). Dal: `p4/mert-home-smoke`.

## Yeni smoke: `tools/Test-P1-Integrated.ps1 -Players 2 -HomeSleep`
Her süreç kendi oyuncusunu **gerçekten yürütür**, kendi yatağına **bakar** (host hedefi oyuncunun kendi görüş ışınından çözer), binding'in kendi istek yoluyla `H`'ye basar; sonra dalış sahnesinde host `P` ile ping atar.
- host `bed-0`, misafir `bed-1`; iki oyuncu da yatakta → gün tek seferde kapanır (gün 1→2, özet 1); sabah kimse oturur/uyur durumda kalmaz ve oyuncular `PrepArea`'da.
- `P` ping'i host'ta kabul edilir; **iki süreç de** ping'i görür ve `DiveRegionField` üzerinden harita koordinatına çevirebilir.
- Sonuç: host **PASS**, client1 **PASS**.

## Smoke'un bulduğu gerçek hatalar
1. **(benim, düzeltildi) Yataklar Lobby'de ama aktif-oyuncu tanımım Lobby'de boştu.** Fiziksel akış çalışıyor (yürü, nişan, `H` kabul) ama `DayEngine` `NotActive` döndürüyordu: evde uyunamıyordu. Düzeltme: saatin durması (`ClockPaused`: Lobby/sahne yükleme) ile uyku kapısının kimleri saydığı ayrıldı. Test: `APausedClockHoldsTimeStillButPlayersCanStillSleep`.
2. **(benim, düzeltildi) Yeniden doğan oyuncunun gün göstergesi bir an varsayılana dönüyordu** (misafirde gün 1→2→1→2). Yeni `EconomyPlayerSync` sunucuda doğarken güncel günü/özeti yazar (`DayLock.StateProvider`).
3. **(Mehmet, #92) `HomeInteractionContractTests.P4Home_HasExactlyFourStableBedIds` kırmızı:** `Has.Count` bir diziye (`DayIds.Beds`) uygulanamaz (`Property Count was not found`). Kod hatası değil; `Assert.That(DayIds.Beds.Count, Is.EqualTo(4))` yeter.
4. **(bilgi) İki `HomeStorageInteraction` çakışacak:** #92'nin `Bind(open)/TryOpen` sınıfı ile depo dalımın `Bind(store, retrieve)` sınıfı aynı adı taşıyor; #92 birleşince depo dalını tek sınıfta birleştireceğim.

## Kanıt
EditMode **678/678** (Mehmet'in `Has.Count` test hatası bu PR'da tek satırla düzeltildi; benim testlerim yeşil). Aynı build'de `-Day` (host+client1+yeniden açılan host) ve yeni `-HomeSleep` PASS; `-Trip`/taban #92 üstünde önceki koşuda PASS.

## Doğrulanmadı
Ortak depo fiziksel akışı (`H` depoya bakınca `StorageUnavailable`; gerçek depo #90 sahibinin bağlaması — sonraki paket), 4 süreç, ayrı bilgisayar, gün 00:00'da evde olmayanın yerleşimi.
