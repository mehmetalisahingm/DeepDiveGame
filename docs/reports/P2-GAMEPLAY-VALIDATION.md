# P2 kapanış ve oynanış doğrulaması — 11 Eylül 2026

Durum: **KAPALI — kullanıcı faz kabulüyle**.

P2 kod teslimleri `main` üzerinde birleşti. Doğrulanan oyun kodu commit'i `7ebdc4ae6ce4188758eddfb7bce9ea321031efa8`.

## Doğrulananlar

- GitHub Actions `Unity P2 tests and Windows build` koşusu #8, attempt 2, aynı `main` commit'i üzerinde tamamen PASS oldu.
- EditMode test adımı PASS.
- Mevcut Windows build yöntemi PASS; proje Windows build üretebildi.
- Kullanıcı manuel olarak oyunun açıldığını ve balığın vurulup/yakalanabildiğini doğruladı.
- Önceki gerçek iki-süreç testinde misafir oyuncu mevcut input/RPC yolu ile hareketli balığı vurdu, öldürdü ve topladı; host tarafındaki çantaya tek av eklendi ve av iki tarafta despawn oldu.
- Envanter testleri aynı `captureId`'nin iki oyuncuya birden yazılmasını engelliyor; dolu çantada reddedilen av claim edilmeden kalıyor; güvenli dönüş, kayıp/güvenli av ayrımı, lobiye dönüşte çanta temizliği ve yeni dalışta eski claim durumunun sıfırlanması test ediliyor.
- Dalgıç kuralları oksijen tüketimi, sıfır oksijende pasif durum, hasar/pasiflik sıfırlaması, request replay/rate gate ve temel pickup/harpoon geri bildirimlerini kapsıyor.
- `SafeReturnZone` için sınır/ölçek geometrisi EditMode testleriyle doğrulanıyor.

## Kabul notu

`docs/plan/PHASES.md` P2 için ayrıca final head üzerinde 1/2/4 oyunculu kabul ve en az iki kişinin 10–15 dakikalık insan oynama testini ister. Final head için ayrı bir 4 oyunculu insan oturumu ve 10–15 dakikalık ortak his testi kayıtlı değildir. Kullanıcı mevcut manuel oynanış, önceki iki-süreç çalışma testi, birleşik EditMode PASS ve Windows build PASS kanıtlarını yeterli kabul ederek P2'yi kapatma kararı verdi. Bu eksik deneyim doğrulaması gizlenmez; P3 başlangıcındaki ilk ortak smoke/playtest'te tekrar kontrol edilir.

## Kalite kontrol — bloklamayan teknik borç

- `SafeReturnZone.Update()` her karede sahne araması ve oyuncu taraması yapıyor. Dört oyunculuk P2 için bloklayıcı görülmedi; ileride component caching veya trigger tabanlı akışla sadeleştirilebilir.
- `InventoryPlayerSync` NetworkVariable'larında owner-only okuma izni açıkça tanımlı değil. Çanta verisi hassas değil ve owner UI yalnız kendi objesinde çiziliyor; yine de kapsülleme P3/P5'te sıkılaştırılabilir.
- Çanta göstergesi prototip `OnGUI` ile çiziliyor. Nihai UI/cila işi değildir; sonraki fazlarda kalıcı UI sistemine taşınabilir.
- `InventoryPlayerSync` için doğrudan canlı guest-replication odaklı ayrı bir test görünmüyor; mevcut birleşik CI ve önceki co-op çalışma kanıtına ek olarak P3 ilk ortak testinde guest çanta göstergesi özellikle kontrol edilir.

Bu maddeler P2 çekirdek sözleşmesini bozan blocker olarak değerlendirilmedi; performans/cila ve test sertleştirmesi olarak sonraki fazlara taşındı.

## Sonuç

P2'nin çekirdek hedefi — avla, taşı, güvenli dönüş için gerekli oyuncu/av/çanta sözleşmelerinin birleşmesi — tamamlandı. CI ve Windows build yeşil. P2 kapatıldı; P3 çalışması açılabilir.
