# P4.3 — Gövde sunumu ve kalıcı Reef satın alma kapısı

5 Ekim 2026; PR #119, `p4/mehmet-final-route-fleet-integration`.

## Düzeltmeler

- Fiziksel otoritenin `HullKind` değeri artık `BoatTripPlayerSync` üzerinden host ve istemcilere taşınır. Yerel görünüm bu değeri doğuşta ve sonraki güncellemelerde uygular.
- `BoatHullPresentation` üç ayrı child model oluşturur; `ApplyHullPresentation` yalnız doğru grubu açık tutar. Motorlu gövdede motor/konsol, araştırma gövdesinde kabin/çatı/radar bulunur. Boyutlar ve koltuklar `BoatHullSeatRules` ile aynıdır. Görsel collider'lar kapalıdır; hareket otoritesi değişmez.
- Bunlar temel geometriden oluşan geçici modellerdir. Nihai tekne sanatı veya görsel kabul tamamlandı iddiası değildir.
- `VehicleFleetTests` içindeki sahte `ReefDiscovered` kaldırıldı. Konum girdisi gerçek `ExplorationCellAuthority.Tick` üzerinden 14 metre derinlikte Reef hücresi keşfeder. Gerçek `ExplorationPersistenceAdapter` ve `EconomySaveStore` kampanya dosyasına yazar.
- Satın alma testi keşif öncesi reddi, para/filonun değişmemesini, dosyadaki Reef kaydını, yeni oluşturulan otoritelerin diskten yüklenmesini, hem hücre otoritesi hem kayıt katmanında `HasDiscoveredCellInBand(Reef)` sonucunu ve ardından tek araştırma teknesi satın alımını doğrular.

## Kanıt

- Unity 6000.3.23f1 tam EditMode: **1065/1065 PASS**, sıfır hata. Yerel ham kayıt: `Logs/P43-hull-reef-tests.xml`.
- Model regresyonu: üç ayrı model, tekrarlı kurulumda çoğalmama, tür değiştirip geri dönme, doğru gövde boyutu, motor/kabin ayrımı, tek aktif grup ve görsel collider'ların kapalı olması.
- Bu testler oyuncunun denizde yürüme/yüzme girdisini veya iki ayrı bilgisayarlı seferi kanıtlamaz. Yeni `NetworkVariable` nedeniyle birlikte oynayanlar aynı build'i kullanmalıdır.
- PR #119'un motorlu/araştırma teknesiyle gerçek iki süreçli tam gidiş-dönüş kabulü ayrı açık kapıdır.

- Windows integrated build: **PASS**; 	ools/Build-P1.ps1 -Integrated, Logs/P1-build.log içinde P1_BUILD_SUCCEEDED integrated=true, Unity çıkış kodu 0.
