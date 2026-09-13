# P3-A entegrasyon teslimi — 13 Eylül 2026

Aktif faz P3; bu kayıt faz kapanışı veya ekip onayı değildir.

## Teslim

- Oyun kodu: `937dad822a9f91fe2b19ab43482dcd2180d90078`; son smoke sürücüsü: `5c1a9ea48dfa484ce2ba2c9efef2f8d18829e705`. #38 kamera/kadraj/tüp, #40 çekim değerlendirmesi ve #41 host Composition bağlantısı birlikte çalışır.
- #39'un ortak para ve alışveriş sonucu göstergesi entegrasyona alındı; Mert'in mevcut kodu değiştirilmedi. `NetworkDiver` davranış düzeni değiştiği için protokol `DeepDive-P3-3` oldu. Eski build'ler aynı oturuma kabul edilmez.
- Gerçek oyuncu bakışı hostta doğrulanır. Dalış bitince görünüm bağlantıları temizlenir; kayıtlar özet üzerinden değerlendirilir. Ödeme sistemi yokken kayıtlar sıfır krediyle tüketilmez.
- Yerel build mevcut klasörde güncellendi: `Builds/P1-Integrated/DeepDiveGame-P1.exe`. Sahne üretim script'i çalıştırılmadı, ZIP oluşturulmadı.

## Doğrulama

- Unity `6000.3.23f1`, tüm EditMode testleri: **244/244 PASS**, 0 başarısız/atlanan. Yerel kanıt: `Logs/P3-final-integration-tests.xml`.
- Windows build: **PASS**, `Logs/P1-build.log` içindeki `P1_BUILD_SUCCEEDED`.
- Tüp testleri doğru oyuncuyu, temel kapasiteyi, aynı ekipmanı tekrar uygulayınca bonusun/oksijenin çoğalmamasını kapsar; diskten yükleme testi değildir.
- İlk dört oyunculu koşu (`Logs/P1-integrated-20260913-230652-194-4`) başarısız sayıldı: tek misafir çekime atanmasına rağmen test çekim yapmayan diğer misafirlerden de Start/Stop bekliyordu. Hostta Gold kayıt/güvenli dönüş oluştu, istisna yoktu. Smoke kontrolü rol ayrımıyla düzeltildi; oyun kuralları gevşetilmedi. Av testine misafirin kendi UI verisindeki gerçek çanta sayısı/ağırlığı replikasyonu kontrolü eklendi.
- Son dört oyunculu koşu: **PASS**, `Logs/P1-integrated-20260913-231037-669-4`. Host + üç misafir, yeniden katılma, hareket, sahne geçişleri, tek misafirin gerçek çekim isteği ve yüzeye güvenli dönüşü geçti. Beşinci oyuncu `RoomFull`, dalış sırasında katılım `WrongPhase` ile reddedildi. Bu, dört kişinin aynı anda kayıt yaptığı veya internet üzerinden oynadığı test değildir.
- Son iki oyunculu av koşusu: **PASS**, `Logs/P1-integrated-20260913-231037-959-2`. Misafir zıpkın/toplama isteği gönderdi; balık/av/host envanteri zinciri ve misafirin kendi çanta UI verisine sayı/ağırlık replikasyonu geçti. Test sağlayıcısı veya sahte av kullanılmadı.
- Tekrarlama: `./tools/Build-P1.ps1 -Integrated`, ardından farklı portlarla `./tools/Test-P1-Integrated.ps1 -Players 4 -Port 28777 -Record` ve `./tools/Test-P1-Integrated.ps1 -Players 2 -Port 29777 -Hunt`.

## Kalan bağımlılıklar

- **Mert:** Gerçek `RecordingResult` ödeme API'si ve kalite 1–4 fiyatları, gerçek alışveriş isteği/UI, save/load. `EconomyManager.PriceFor` sadece av fiyatıdır.
- **Mehmet:** Ödeme API'si teslim edilince `RecordingWorldBinding.SetPaymentHandler(...)` üzerinden bağla; yalnız gerçek kredi işlemi `Accepted` döndürsün. Director'ın `IsPayable` kontrolünü tekrar etme. Save/load teslim edilince yeniden açılan oyunda doğru oyuncunun tüp etkisini doğrula.
- **Utku:** Özel olay ve temel görsel/ses işareti.
- **Ekip:** Gerçek ödeme/yükseltme/yükleme dahil tam döngü, ayrı bilgisayar/internet ve ortak 20–30 dakikalık oynama testi. Yerel otomatik testler bunların yerine geçmez. Guest çanta göstergesinin insan tarafından görsel kontrolü bu kayıtta yapılmış sayılmaz.

#34 bağımlı kontroller bitene kadar açık kalır; P4 açılmaz.
