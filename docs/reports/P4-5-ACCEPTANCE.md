# P4.5 birleşik kabul — 9 Ekim 2026

Kapsam: #131 gece/hava/akıntı ve world uygunluğu; #132 günlük hedefler, tek ödül, rol/kayıt ve görünür ev-kasaba gelişimi. Kapanış PR'ı: #135 → `codex/p4-integration`. Bu rapor P4.6'nın ortak üç günlük kabulünün yerine geçmez.

## Tamamlanan bağlantı

Günlük seçim, `IOrderTargetWorld.CanOffer` üzerinden World'ün gerçek tür/olay tanımlarına bağlı `P45ObjectiveContentCatalog` habitat/rota verisini okur. Katalog kasabada da Resources üzerinden yüklenir; dalış sahnesi açılmadan boş hedeflerin kaydedilmesi giderildi. Tür/habitat/rota, kayıtlı tekne sahipliği, resif keşfi ve günlük hava aynı filtreye girer. Kıyı avı teknesiz kalır; bilinmeyen içerik/erişilmeyen resif ve derin hedef reddedilir. Aynı gün kayıttan geldiğinde yeniden seçim yapılmaz. Üç etkin sponsor korunur; doğrulanmış kayıt-anı gece verisi olmayan dördüncü rezerv şablon açılmadı.

Günlük pano gerçek Canvas arayüzüdür. Satın alma ve rol düğmeleri aynı host doğrulamalı isteklere gider. Ekrana sığan hedef/ilerleme/ödül, dört kalıcı gelişim ve dört rol görünür. PC, yatak ve ortak depo mevcut URP malzemelerini kullanır. Gizli Windows render'ı tüm aktif Canvas'ları yakalar; gerçek görüntü yerine sahte panel çizilmez.

## Yerel Unity/Windows kanıtı

Unity `6000.3.23f1`; tüm EditMode paketi **1143/1143 PASS**, Windows build PASS. Ürün kodu `a004502`; üstündeki değişiklik yalnız solo/dört oyunculu smoke kabul koşulunu düzeltir. Aşağıdaki JSON'lar aynı ürün kaynaklarından alınan gerçek Windows süreçleridir.

| Koşu | Sonuç | Kanıt |
|---|---|---|
| `-Players 2 -WorldConditions -Capture`, gün 1 | Host + guest PASS; aynı hava; işaretli akıntıya gir/çık; gerçek balıkta gece flag/ışık/hareket ve gündüze dönüş; gerçek günlük pano world filtresinden geçiyor | [Host](evidence/p45/world-host.json), [guest](evidence/p45/world-client1.json) |
| Aynı world koşusu, diskten gün 2 (rüzgarlı) | Host + guest PASS; gün/seed kaynaklı rüzgar ve akıntı aynası | [Host](evidence/p45/world-windy-host.json), [guest](evidence/p45/world-windy-client1.json) |
| `-Players 2 -Living -Capture` + yeni host süreci | Gerçek UI düğmesiyle kasaba satın alma + host rol seçimi; iki aynada etkiler; dört gelişim görünür; gerçek teslimlerle tek sipariş ödülü; kaydet/yükle ve süreç yeniden açma PASS | [Host](evidence/p45/living-host.json), [guest](evidence/p45/living-client1.json), [yeniden açılış](evidence/p45/living-host-reload.json) |
| `-Players 2 -Acceptance` + yeni host süreci | Gerçek 104616 bayt klip, oynatma/yayın/sponsor ve gün kapanışı; duplicate/replay ikinci ödeme yok; hash/boyut eşleşir; yeniden açıldığında klip oynar ve bakiye/gün aynıdır | [Host](evidence/p45/media-host.json), [guest](evidence/p45/media-client1.json), [yeniden açılış](evidence/p45/media-host-reload.json) |
| `-Players 1 -RoleEffects` | Solo gerçek oyuncuda rol/kapasite, katlamama, ping ve akıntı PASS | [Host](evidence/p45/roles-solo-host.json) |
| `-Players 4 -RoleEffects` | Dört süreç PASS; guest yeniden bağlanma; çapraz yetki reddi; beşinci kişi RoomFull, dalışa geç katılan WrongPhase | [Host](evidence/p45/roles-four-host.json), [guest 1](evidence/p45/roles-four-client1.json), [guest 2](evidence/p45/roles-four-client2.json), [guest 3](evidence/p45/roles-four-client3.json) |

Gece sakin yüzüş hızı **1,740 m/sn**, gündüz **1,200 m/sn**: mevcut 1,45 katsayısı gerçek FishActor'da ölçüldü. Host ve guest aynı gece durumunu ve ışık açılıp kapanmasını gördü. Living koşusunda gelişim sonrası **2850**, iki oyuncunun teslimi ve bir sipariş ödülü sonrası **3458**; yeni hostta yine **3458**. Hava aynı seed `-1412989973`, aynı sakin durumla döndü. Medya koşusunda yeniden açılış **210 bakiye / 60 kanal geliri / gün 2**; ikinci ödeme yok.

Derin rotada rüzgar yeni çıkışı reddeder, dönüşü kapatmaz: gerçek `BoatTripManager` üzerindeki EditMode kabulü ve mevcut rota regresyonları bu ayrımı sınar. World runtime koşusu doğrudan derin sefer başlatmış sayılmaz.

## Görsel kontrol

Gerçek build PNG'leri açılarak kontrol edildi: pano metinleri/düğmeler ekrana sığıyor; sipariş tamamlandığında durum/ilerleme değişiyor; ev rafı, kasaba tezgahı/dükkan ve iskele geliştirme grupları gerçek sahnede kuruluyor; gündüz/gece ışığı farklı.

![Günlük pano](media/p45-board.png)
![Ev rafı](media/p45-home.png)
![Kasaba gelişimi ve tamamlanan sipariş](media/p45-town.png)
![Gece](media/p45-night.png)
![Gündüz](media/p45-day.png)

## Kanıtın sınırı ve son kapı

Saat ve akıntı konumu, geceyi kısa koşuda sınamak için etiketli test girdisidir; balık AI, replikasyon, ışık ve oyuncu drift'i gerçektir. Living başlangıç parası ve dört av da etiketli girdidir; UI isteği, satış, ödül, gelişim, disk kaydı ve yeniden açılış gerçek ürün yollarıdır. Medya koşusu gerçek oyun kamerasından klip üretir.

D06 korunur: kampanya hostun diskindedir; host rolü kalıcıdır, ayrılan guest'in geçici client kimliği yeni kampanyaya taşınmaz. Yerel solo/iki/dört süreç ayrı bilgisayar/internet kabulü değildir. Nihai insan/canlı/çevre sanatı, oyuncuların ortak görsel/oynanış değerlendirmesi ve üç ardışık gün P4.6'da kalır. Başka ekip üyesi adına manuel onay yazılmadı.

Yerel kapılar PASS. Son PR head'inin CI ve merge sonrası birleşik kaynak/build doğrulaması GitHub kapanış kaydına bağlanacaktır; bu satır tek başına merge veya issue kapanışı iddiası değildir.
