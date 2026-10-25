# P4 ara-kabul erteleme defteri

25 Ekim 2026 — açık kullanıcı kararı: “geçmişte bekleyen testleri ve aşamaları
kapat … en son gelişme olunca tamamlayacağız … aşamalarda ilerleyelim”.

## Geçerli karar

- P4.6/P4.7'nin eski kabul bekleyişleri **ertelenerek idari kapatılır**;
  ürünün veya bütün testlerin tamamlandığı anlamına gelmez. GitHub kapanış
  nedeni `not_planned` kullanılır; eski kabul kutuları PASS diye işaretlenmez.
- Bu kayıtlar P4.7–P4.11 kapsamındaki mevcut üretimi bekletmez. Aktif öncelik
  P4.8 tamir/görsel kit; P4.7'den taşınan kit/yerleşim üretimi devam eder.
  Ortak eylemler, kasaba/ev ve canlı deniz küçük paketlerle ilerler. Gerçek
  kod/varlık girdileri ve ortak dosya sahipliği korunur; her paketten sonra
  tekrar ayrı insan/ayrı PC ara-faz kabulü aranmaz.
- Ertelenen ortak, çok günlük, ayrı bilgisayar ve son görsel/ses kabulleri
  P4.11 üretimi sonrası **P4.12 / #164** içinde tek sabit gelişmiş Windows
  build'inde yapılır. Eski oynanabilirlik bildirimi yeni build kabulü olmaz.
- Yeni değişikliğin derlenmesi ve anlamlı teknik doğrulaması sürer. Son özel
  tamir tekrarındaki hareket sıçraması ve eksik raporlar **#207** açık kusurudur;
  erteleme bunu PASS yapmaz.
- P5/P6, P4.12 son ürün kabulü ve P4 main/build kapanışından sonra açılır.
  Bu karar yeni ada/boss/mekanik, ücretli kaynak veya yayın izni değildir.

## İdari kapatılan kaynak kayıtlar ve taşınan işler

| Eski kayıt | Kapanış biçimi | Korunan üretim / son kontrol |
| --- | --- | --- |
| #138 — ev–şehir blokajı | Eski temel kabulü ertelendi | Yeni şehir ölçeği #153/#162; D01 |
| #140 — sahil/rampa/iskele | Eski temel kabulü ertelendi | Çalışma alanı #188, dünya/rota #163; D02 |
| #141 — P4.6 ortak kabul | Ara-kabul bekleyişi idari kapalı | Nihai ortak kabul #164; D03/D06/D07 |
| #156 — sandal 2/3 yönlendirme | Eski doğrulama kaydı ertelendi | Fiziksel parça/onarım #187/#188/#189; D04 |
| #157 — hareket/ağ ortak kabul | Eski kapsamın kabulü ertelendi | Eylemler #161, büyüyen dünya #162/#163; D01/D06 |
| #159 — P4.7 hedef örnek kapısı | Ayrı ara-faz kabulü ertelendi | Sanat/örnek alan #171/#172 ve #153/#154; D05 |
| #173 — kamera/ölçek kabulü | Eski kit üzerinden kabul ertelendi | Yeni el/kamera bağlantısı #187/#161; D05 |

#171/#172, #187/#188/#189, #153/#154/#155 ve #160–#163 üretim işleri
**açık kalır**. Üretilmemiş model/çevre/ses, büyük şehir, beş kaliteli tür veya
boss sunumu bu idari kapatmayla teslim edilmiş sayılmaz. #159'un kapatılması
#171/#172'nin işini silmez.

## Son gelişmiş sürümde tamamlanacak kabul listesi — #164

- [ ] **D01:** Nihai ev–şehir–dükkân–sahil–iskele–su–PC–uyku turu; gerçek
  kontrolcü ve 1/2/4 oyuncu, yeni geometri/kapı/kamera/harita, normal yolda
  teleport olmadan. Kaynak: #138/#157; üretim #153/#162.
- [ ] **D02:** Son kıyı/rampa/çarpışma, tekneye binme/inme ve yakın rota/dönüş;
  aynı dünya koordinatı ve yükleme sonrası tutarlılık. Kaynak: #140/#157.
- [ ] **D03:** En az iki bilgisayarda dört süreçle üç oyun günü; gerçek üç
  kişinin değerlendirmesi, gündüz/gece/hava ve aynı sürüm kapanış kaydı.
  Kaynak: #141.
- [ ] **D04:** Üç ücretsiz parçanın bulunabilirliği, 0/3→3/3, üç ayrı eski 2/3
  kaydı, ipucu ve satın alma alternatifi; kampanya sıfırlamadan devam.
  Kaynak: #156; üretim #187/#188/#189.
- [ ] **D05:** Son sanat kitiyle insan/kapı/tezgâh/kamera ölçeği, el/nesne/çivi
  teması, birinci/üçüncü kişi, gündüz/gece, konfor/okunurluk ve donanımı
  belirtilmiş kare süresi. Kaynak: #159/#173; üretim #171/#172/#187/#161.
- [ ] **D06:** Aynı parça/yuva yarışı, iptal, guest kopma/rejoin, aktif host
  kaybı, 00:00 ve kayıt reddi; tekil malzeme/ödeme/katkı ve doğru geç katılım.
  #207 tekrarının nihai sonucu da aynı kaynak/build'e bağlanır.
- [ ] **D07:** Gerçek klip/PC oynatma/yayın, ev/rol/hava/görev/tekne/boss
  gelişimi, güvenli dönüş ve diskten devam; tekrar ödeme/ilerleme kaybı yok.
- [ ] **D08:** Q01–Q14 son görsel/ses/içerik/tempo kabulü: gerçek dükkânlar,
  ev gelişimi, hareketli su/habitat, beş tür, derinlik/değer ve bulunabilir boss.

P4.11 üretimi birleşince #164'te sabit commit/build, donanım, katılımcı,
senaryo, gerçek sonuç ve açık kusur yazılır. Yeni başarısız sonuç eski PASS
ile değiştirilmez. Nihai kabul ve P5/P6 başlangıcı ayrıca kaydedilir.
