# #157 — Kıyıda fiziksel parça toplama

14 Ekim 2026. Kaynak commit `3c91a22`; hedef `codex/p4-integration`. Bu #157'nin ilk dar teslimidir; bütün dünya/sanat/ayrı bilgisayar kabulünü kapatmaz.

## Problem ve sonuç

Sahilde görülen ücretsiz onarım parçasına Prep/Return'da E basıldığında `NetworkPlayer.HandlePickup` hedefi okumadan `DiveActive` kapısında reddediyordu. Artık host bağlı/etkin oyuncuyu, sahne yükleme ve gün kapanışını doğrular; gerçek görüş/mesafe raycast'iyle bulduğu onarım parçası dalış öncesi/sonrası da katkı olur. Catch hedefi hâlâ yalnız aktif dalışta toplanır. Tek contribution, fiyat ve kayıt yolu değiştirilmedi.

İpucu paneli artık “E: parcayi topla; dukkandan da alinir” der. Bu elle taşıma/keser/çivi tamiri değildir; o yeni P4.8 tesliminde edinme ve contribution ayrımı gerektirir.

## Doğrulama

- Tüm EditMode suite: 1248/1248 PASS. Yeni izin testleri Prep/Return part vs catch sınırını ve offline/loading/passive/Closing retlerini kontrol eder; mevcut raycast occlusion testleri korunur.
- Windows integrated build PASS. Son build gerçek runtime smoke'u için yeniden alındı.
- İki gerçek Windows süreçli `-Hunt`: host + guest PASS; değişen pickup yolunda av/catch davranışı korunur.
- Yeni `-CoastParts` ilk koşu: ürün eylemleri başarılı, 0→1→2→3; gövde Prep, motor Dive, yakıt Return. Ancak son katkıdan hemen sonra lobiye geçildiği için parts-hidden/hint-gone gözlemleri kaçtı ve koşu FAIL oldu. Test bu gözlemleri kaldırmadan, final mirror/panel kapanışı ve iki saniye gözlem beklemesiyle düzeltildi.
- Son `-Players 2 -CoastParts -Capture`: host + guest PASS. `P46_COAST_PARTS hull=Prep fuel=Return repaired=True`; sequence 0→1→2→3, duplicate hull reddi, parts-hidden ve hint-gone iki raporda doğru.

Kanıt: [coast host](evidence/p157/coast-host.json), [coast guest](evidence/p157/coast-client1.json), [hunt host](evidence/p157/hunt-host.json), [hunt guest](evidence/p157/hunt-client1.json). Runtime dizinleri `P1-integrated-20261014-132058-664-2-22177` (son kıyı koşusu) ve `P1-integrated-20261014-131617-514-2-22077` (av regresyonu).

Windows exe SHA-256: `56c28afba054092826b7a8a80ba3767daa87967f182c0c562eb2f98799175b7a`. Exe tek başına paket değildir; data/dll setiyle çalıştırıldı. Birleşmiş CI sonucu bu rapordan türetilmez.

Komutlar:

```powershell
powershell -NoProfile -File tools/Build-P1.ps1 -Integrated
powershell -NoProfile -File tools/Test-P1-Integrated.ps1 -Players 2 -CoastParts -Capture
powershell -NoProfile -File tools/Test-P1-Integrated.ps1 -Players 2 -Hunt
```

CoastParts senaryosu host+guest için iki süreç gerektirir. Gövdeyi Prep'te ve yakıt tankını Return'de host, motoru Dive'da guest gerçek yürüme/nişan/E yoluyla alır. Sahneye test parçası/katkı enjekte edilmez; teleport çağrısı eklenmedi. Son katkı sonrası collider/render gizlenmesi, ipucu kapanması ve duplicate hull reddi gereklidir.

## Sınırlar

Ayrı bilgisayar/internet, tam ev→şehir→iskele→PC→uyku turu ve dört oyuncu nihai görsel kabulü bu teslimde yapılmadı. Mevcut phase sahne geçişleri korunur. Panelin dev oturum ekranıyla görsel örtüşmesi bu kontrol ekranında hâlâ var; #154/#162 UI/görsel işi olarak açık kalır. Baştan sona #157 veya P4 kapanışı ilan edilmedi. Önceden var olan `.meta` silinmeleri bu commit'e alınmadı.
