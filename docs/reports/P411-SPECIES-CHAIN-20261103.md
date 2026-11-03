# #218 — Beş türün av → kıyıya dönüş → satış → ansiklopedi zinciri (3 Kasım 2026)

Durum: **Kanıt belgesi; kabul veya PASS değildir.** Gerçek harpoon/yüzme koşuları tek makinede yerel Windows süreçleridir; satış ve ansiklopedi ucu gerçek bileşenlerle EditMode'da, gerçek NPC'de değil.

## Bulunan ve düzeltilen boşluk

`EncyclopediaPresenter` ve `HomePcView.Label` yalnız `sea_bass`, `tuna`, `red_snapper` adlarını biliyordu. Orfoz ve fener balığı ansiklopedide ham kimlikle ("grouper") ve "Bilinmeyen tur" kategorisiyle, ev PC'sinde ham kimlikle görünecekti. Eklendi: `grouper` → "Orfoz" / "Resif baligi", `lanternfish` → "Fener baligi" / "Derin su baligi". Düzeltme geri alınınca yeni test "Orfoz" yerine "grouper" görüp kırmızı oldu (denendi), düzeltmeyle yeşil.

## Gerçek süreçte av (`Test-P1-Integrated.ps1 -Players 1 -Hunt -HuntFish <sahne balığı> -HuntExtra 60`)

Mevcut `-p2-hunt` smoke'u yalnız `Fish_SeaBass`'ı avlıyordu. `-p411-hunt-fish` ile hedef seçilebilir (varsayılan aynı), çok uzak hedef için `-p411-hunt-extra` süre ekler ve vurulana kadar 8 m'ye yüzerek yaklaşır. Gerçek girişle yüzme, harpoon, toplama (pickup) ve çanta; enjekte yakalama yok. Rapor çantadaki türü ve gramı da yazıyor. Kanıt: `evidence/p411-species-chain/hunt-*.json`.

| Hedef balık | Tür | Sonuç | Çantadaki tür / gram |
|---|---|---|---|
| Fish_SeaBass (varsayılan, 1 ve 2 süreç) | sea_bass | geçti | sea_bass, 1821 g / 2459 g |
| Fish_SeaBass_04 | tuna | geçti | tuna, 1807 g |
| Fish_SeaBass_07 | red_snapper | geçti | red_snapper, 1666 g |
| Fish_SeaBass_11 | grouper | geçti | grouper, 3384 g |
| Fish_SeaBass_12 | grouper | geçti | grouper, 4555 g |
| Fish_SeaBass_10 | grouper | **geçmedi** | yakalanamadı; 6,5 m'de takıldı |
| Fish_SeaBass_13 | lanternfish | geçti | lanternfish, 430 g |
| Fish_SeaBass_14 | lanternfish | geçti | lanternfish, 324 g |
| Fish_SeaBass_15 | lanternfish | geçti | lanternfish, 557 g |

Geçen koşularda: balık vuruldu, toplandı, istemci çantası çoğaldı, diğer 14 balık yerinde kaldı, dalgıç sağ döndü, hata yok. Gramlar türün tanımlı aralığında (orfoz 2000–6000, fener 300–900).

**Açık:** `Fish_SeaBass_10` (orfoz, ev noktası (-10, -4,5, 28)) iki ayrı koşuda 12 m'de, 8 m eşiğiyle 6,5 m'de dalgıç (-8,6, -3,1, 21,5) civarında takıldı ve hiç yakalanmadı. Neden araştırılmadı: probun düz çizgi yaklaşması bir engele mi takılıyor yoksa balık zıpkın görüşünün dışında mı kalıyor, bilmiyorum. Gerçek bir oyuncunun oraya erişip erişemediği bu koşulardan çıkarılamaz; Utku'nun resif yerleşimine bakması gerekir. Sahne, ev noktası veya engel değiştirilmedi.

## Gerçek oyuncu kamerasından kareler (kamera okunurluğu, 3 Kasım 2026)

`-Capture -HuntFish <ad>` birlikte: grafikli host'ta avlanan balığa yaklaşırken 20/14/9 m, vurulduğunda ve çantaya girdikten 1,5 sn sonra gerçek oyuncu kamerasından dünya görüntüsü (HUD yok). Kare dosyaları `evidence/p411-species-chain/player-camera/`, özet [player-camera-contact-sheet.png](evidence/p411-species-chain/player-camera-contact-sheet.png). Orfoz 11 (4104 g) ve fener balığı 13 (885 g; ampul değişikliğinden sonraki koşuda 762 g) geçti; fener balığı kareleri ampul değişikliğinden sonraki koşudan.

Resimlerden okunan (yorum): zıpkın menzilinde (yaklaşık 14–20 m) **orfoz küçük turuncu bir benek, fener balığı koyu suda neredeyse hiç görünmüyor**; kadrajdaki iki mor nokta balık değil. Taşınan av karesinde fener balığı elde okunmadı, orfozun yalnız bir parçası kadrajın köşesinde sarı görünüyor. Yani bu koşularda iki yeni türün uzaktan okunurluğu zayıf; bu, önceki 9 m geçici kamera bulgusunu doğrular ve su/sis ışık kararı Utku/Mehmet'le birlikte verilmelidir.

**Yapılan küçük sanat değişikliği (Mert):** fener balığının ampulü (`Lanternfish_Light.mat`, `SuppliedKitImport.Material`) artık emisyonlu (HDR camgöbeği), gövde koyu kaldı. Önce/sonra: [lanternfish-lure-before-after.png](evidence/p411-species-chain/lanternfish-lure-before-after.png). 2,5 m'de ampul belirgin beyaz bir nokta; 9 m'de balık küçük bir benek olarak seçiliyor, ama iki koşu farklı balık konumlarında alındı (bire bir kıyas değil), oyuncu kamerasında 14–20 m'de değişiklik görünmüyor. Okunurluk sorunu çözüldü denmez.

## Veri zinciri (EditMode, gerçek bileşenler)

- `P411SpeciesSaleTests` (P3 derlemesi; oturum + envanter + ekonomi): beş tür güvenli dönüşte kuyruğa girer (5 bekleyen, 0 ödeme); tek satış 120+120+120+180+320 öder; ikinci istek `NothingToTurnIn`; kayıt dışa aktarılıp yeni ekonomiye yüklenince aynı yakalamalar bir daha kuyruğa girmez, bakiye aynı kalır.
- `P411SpeciesChainTests` (P4): beş tür gözlem → kayıt → yakalamayla ansiklopedide doğru adla ("Levrek", "Ton baligi", "Kirmizi mercan", "Orfoz", "Fener baligi"), kendi kategorisiyle ve gözlendikleri derinlik bandıyla listelenir; beş türün beş ayrı av/teslim görseli yüklenir.
- Tam EditMode takımı: **1482 test, 1482 geçti** (#239 sonrası taban).

## Denenmedi

- Gerçek NPC'ye yürüyerek satış ve çekim (kamera) akışı yeni iki tür için; ödül ve satış kâğıdı yalnız bileşen düzeyinde.
- Ölüm/av pozu ve animasyon son kareleri; el feneri/ekipmanla uzak okunurluk (oyuncu kamerası kareleri yukarıda).
- 2 ve 4 süreçte yeni tür avı (yalnız varsayılan levrek avı 2 süreçte denendi); ayrı PC.
- Eski kayıt dosyasıyla gerçek yükleme (yalnız EditMode dışa/içe aktarma).
- Tuna'nın sığ kıyıdaki yolu ve NPC/kasa görüşü. #218, #164, #207 açık kalır.
