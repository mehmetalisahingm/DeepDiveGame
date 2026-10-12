# P4.6 — birleşik kabul kanıt defteri (AÇIK)

**Durum:** Kabul verilmedi. Bu belge #141 için gerçek çalıştırma sonuçlarını ayırır; üç ekip üyesinin oynanış onayı veya tek bir kesintisiz üç günlük oyun turu yerine geçmez.

## Bağımlılıklar

- #142: fiziksel dükkânlar birleşti (commit `0b1ba6e`).
- #143 / #138: tek sahneli ev–şehir yaya zemini; düzenlemenin gerçek build ve kesintisiz yürüyüş kabulü bekleniyor.
- #144 / #140: kıyı/rampa/iskele köprüsü ve gerçek tekne dönüş kabulü bekleniyor.
- #145: yanlış rota demirleme haritası + boss sonrası ledge dönüş düzeltmeleri integration'a merge edildi (commit `566318c`); son iki süreçli rota ve boss CI sonuçları ayrıca takip edilecek.
- #146: solo, 2 ve 4 gerçek süreçte gün 1→2→3 ve save/reload, ardından gerçek dükkân/tekne/ekonomi/video regresyonları için Linux Unity kabul adayı.

## Tespit edilen ve düzeltilen hata

- [Önceki WIP CI #38260986471](https://github.com/mehmetalisahingm/DeepDiveGame/actions/runs/38260986471): Unity Linux build PASS, solo role/map smoke PASS, iki süreçli `-Shops` FAIL.
- Gerçek host/guest JSON izleri dükkâna çapraz yaklaşımda x ≈ −8.4, z ≈ −19.7 noktasından zeminin dışına çıkılıp y konumunun hızla düştüğünü gösterdi. Sonuç: `shops step 4 timeout reason=NotAtShop`.
- `ContinuousTownLayout.MainStreet` genişletildi; iki taraftaki gerçek çapraz mağaza yaklaşımı EditMode Physics.Raycast regresyonuna eklendi. Yalnız kod yazıldı; yeni koşu PASS olana kadar sorun çözüldü denmeyecek.
- Önceki #144 Linux build'i `CoastalHarborLinkTests` içinde `Unity.Netcode` assembly referansı eksik olduğundan CS0234 ile düşüyordu; test .asmdef referansı düzeltildi.

## P4.6 kapanışından önce zorunlu kapılar

1. #143 ve #144 gerçek CI sonuçları yeşil; PR'lar doğru sırada integration dalına alındı.
2. 1/2/4 oyuncu gerçek süreçte aynı fiziksel dünya ve servis konumlarında başarılı; su→rampa→kara, iskeleye yürüyüş, binme/inme, güvenli dönüş, geceler ve hava doğrulanır.
3. Sabah → ev → şehir → dükkân → sahil → iskele → deniz → geri dönüş → PC/kanal → uyku/özet zinciri fiziksel/tek dünya olarak kaydedilir. İki ayrı başarılı smoke bunu tek başına kanıtlamaz.
4. Üç ardışık oyun günü, 00:00 ve erken uyku, başarısız dalış, host ayrılması, diskten yeniden başlatma; kaydedilen oyuncu/world/görev/ekonomi/veri aynı kalır.
5. Windows gerçek build, en az iki bilgisayarda dört oyunculu deneme, gündüz/gece/hava görsel değerlendirmesi ve üç kişinin gerçek kabul kaydı.
6. P4.3/P4.4 iki süreçli CI geçmiş düzeltmelere rağmen kırmızı kalıyorsa #141 kapatılmaz.

Test fixture'ları (saat hızlandırma, etiketli para/av) gerçek oyuncu testi diye sunulmaz. Başarısız veya atlanan işlerden PASS türetilmez.
