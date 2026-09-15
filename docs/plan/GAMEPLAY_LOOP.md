# Oyun akışı ve görünür gelişim

Plan 3.0 — 15 Eylül 2026. İnsan karakter, fiziksel kıyı dünyası, ev/gün/PC, balıkçılık/kanal ve ileri keşif döngüsüdür. **Planlanan davranıştır; uygulanmış özellik listesi değildir.** Ana plan [PHASES](PHASES.md), güncel kanıt [STATUS](STATUS.md), bağlantılar [CONTRACTS](CONTRACTS.md); yeni sistem ayrıntıları [WORLD_SYSTEMS](WORLD_SYSTEMS.md) içindedir.

## Hangi özellik ne zaman gelecek?

| Oyuncunun göreceği değişiklik | İlk zorunlu teslim | Daha sonra |
|---|---|---|
| İnsan karakter ve insan gibi yürüme/yüzme | **P3.1**: insan gövdesi, su üstü/sualtı animasyonları, dalma/çıkma geçişleri | P4.6 son kalite; temel hareket ertelenmez |
| Kamera ve zıpkının ele gelmesi; arkadaşının bunu görmesi | **P3.1**: yerel eller, uzak oyuncuda ekipman, kuşanma ve kayıt işareti | Kamera P3.2'de ayrı satın alınır, P4.3'te iki üst modele gelişir |
| Kasabadan kumsala yürüme, yüzerek av alanına gitme ve dönme | **P3.2** | P4.3/4'te aynı denizde uzak/derin kesimler |
| İnsan NPC'lerden alışveriş, balığı çantayla götürüp satma | **P3.2**: üç fiziksel hizmet | P4'te mevcut hizmetlerin katalog/sanat/ilerleme bağlantısı genişler |
| İlk görev olarak sandalı onarma; önce kıyıda avlanabilme | **P3.2–P3.3**: ücretsiz parça toplama veya balık geliriyle parça alma; yakın rota | P4.3'te iki daha büyük tekne satın alınır |
| Harita ve canlı sandal konumu | **P3.3**: tekne/oyuncu/iskele | P4.1 keşif örtüsü, derinlik/işaret ve ansiklopedi temeli |
| Ortak ev, dört yatak, saat/uyku/00:00 ve gün özeti | **P4.1** | P4.5'te ev/kasaba görünür gelişimi |
| Gerçek klibi sonradan izlemek ve PC'den yayınlamak | **P4.2**: ortak arşiv, oynatıcı, oyun içi kanal; ertesi gün izlenme/takipçi/gelir | P4.5 sponsorlar |
| Büyük kamera ve daha büyük tekneler | **P4.3**: üç kamera ve üç ayrı tekne modeli/kademesi | Bir aktif tekne; parayla satın alma ve yeni rotalar |
| Yeni türler, derin keşif ve gerçek boss | **P4.4**: üç kesim, beş normal tür + bir boss, ansiklopedi | Araştırma veya av; ikinci harita/boss bu teslimde yok |
| Günlük çeşitlilik | **P4.5**: hava/gece/akıntı, sipariş/sponsor, ev-kasaba gelişimi, hafif roller | Küçük içerik bütçesi; yeni sistemler sınırsız eklenmez |
| Belirgin grafik gelişimi | **P3.1'den itibaren her ara teslimde**, P3.4 görsel kapısı | P4.6 tam dünya/sanat kabulü; P5 hata/performans düzeltmesi |

Bunlar teslim sırasıdır, takvim taahhüdü değildir. Süre ve ekip kapasitesi uydurulmaz. Mevcut P3 açık kalır; kullanıcı yalnızca planı düzenletmiştir.

## Bir oyun oturumu

Bu tam günlük akış P4.1–P4.6 ile tamamlanır; P3 kasaba/kıyı/av/kamera satın alma/sandal temelini teslim eder.

1. **Evde uyan:** Dört oyuncu ortak evde buluşur; harita, hava ve günün hedeflerine bakar. İlk görev bozuk sandalı onarmaktır; önce sığ suda para kazanmak serbesttir.
2. **Hazırlan ve alışveriş yap:** Temel zıpkın/tüp/çanta/palet ve tüp doldurma ücretsiz kıyı başlangıcını sağlar. **Kamera ayrıca dükkândan satın alınır**, ortak havuzdan bir oyuncuya tahsis edilir. Kime ait hangi kameranın takılı olduğu görünür; dört kameracı için dört örnek gerekir.
3. **Kumsala yürü:** Su çizgisinde insan hareketi yürüyüşten su üstü ve sualtı yüzmeye geçer. Kıyı avı sandal veya kamera edinmeyi beklemez.
4. **Sandalı onar veya rotaya çık:** Üç parçaya birlikte katkı yap, sandalı kullanılır hâle getir; haritada konumunu bul. Sonraki tekneler limandan alınır ve yeni rotalara eriştirir.
5. **Avla, keşfet ve çek:** Balığı çantaya al; yeni canlı/alanı keşfet; satın alınmış kamerayla gördüğün sahneyi kaydet. Dalgıç kamerayı gerçekten tutar, arkadaşları ekipmanı ve kayıt hâlini görür. Zıpkın ve kamera aynı anda etkin elde kullanılmaz.
6. **Zamanı ve dönüşü planla:** Oksijen, çanta, gün saati ve hava devam/dönüş kararını etkiler. Harita tekneyi, demirlemeyi ve dönüş yönünü gösterir. Tekneye çıkış tüpü doldurmaz veya avı kalıcı güvenceye almaz; güvenli dönüş kıyı/kasabadadır.
7. **Balıkçıya götür:** Dolu çantayla yaklaş, avları ve teklifi seçip sat. Dünya çantası ve UI aynı envanteri temsil eder; kıyıya çıkınca otomatik satış yapılmaz.
8. **Klibini izle ve yayınla:** Ev PC'sinde kendi çektiğin görüntüyü aç, izleyip başlık/kapak seç ve ortak oyun içi kanala yayınla. Alternatif olarak görüntü NPC'sine hemen lisansla; aynı kayıt iki gelir yolunda birden kullanılamaz.
9. **Geliş ve tekrar dal:** Aynı gün yeni dalış yapılabilir. Kamera/tüp/palet/çanta/zıpkın ve sonraki tekneler gerçek etki/görünüm kazanır. Keşifle ansiklopedi ve boss izleri açılır.
10. **Uyku ve gün özeti:** Bağlı etkin oyuncular evde yatağa girince veya 00:00'da gün kapanır. Güvenli dönmeyenin dalış yükü kaybolur; gelir, kayıp, keşif ve ilerleme özeti çıkar. Kurallar [WORLD_SYSTEMS](WORLD_SYSTEMS.md#ortak-saat-uyku-ve-0000--p41)'tedir.
11. **Ertesi sabah:** Yayınların izlenme/takipçi/geliri tek kez gelir; yeni uygun sipariş/sponsor ve keşif hedefleri görünür. Ev/trofe duvarı/liman gelişimi geçmiş çabayı gösterir.

## Dünya yerleşimi

```text
Ortak ev: dört yatak + depo + PC/kanal + trofe duvarı (P4)
                              |
Küçük kasaba: ekipman dükkânı + balıkçı + görüntü alıcısı
                              |
                       yürünebilir kumsal
                         /           \
            kıyıdan yüzme             iskele / başlangıçta bozuk sandal
                  |                           |
            sığ av alanı  <------  yakın demirleme (P3)
                                              |
                                 uzak demirleme / resif (P4)
                                              |
                                    derin demirleme / boss odağı (P4)
```

Tek bölge, tek kıyı üssü; P3 yakın, P4 resif ve derin olmak üzere toplam üç deniz demirleme noktasıdır. P4'te limana tekne satıcısı eklenir. Sahne sayısı teknik tercihtir; rota oyuncu için süreklidir. Harita gerçek dünya koordinatlarını izler; dönüş işaretleri su altında da okunur.

## Üç hizmet ve çantanın anlamı

| Yer | Etkileşim | Hostun doğrulayacağı sonuç |
|---|---|---|
| Dalış ekipmanı dükkânı / insan satıcı | Yaklaş → ürünü/etkisini incele → oyuncuya tahsis et → satın al | Doğru NPC/mesafe/aşama, fiyat, ortak bakiye ve tekil ekipman sahipliği |
| Balık alıcısı / insan satıcı | Çantayla yaklaş → avları seç → toplam teklifi gör → sat | Güvenle getirilmiş, erişim hakkı olan tekil avları tüket; karşılığını bir kez öde |
| Görüntü değerlendirme noktası / insan görevli | Yaklaş → uygun kayıtları/kalitelerini gör → teslim et | Dalış/tür-olay için tek ödül kuralı, güvenli dönüş ve daha önce ödenmemiş kayıt |

P4.2'de ev PC'si aynı kaydın oyun içi kanal alternatifini açar; NPC'ye lisanslanmış kayıt kanalda tekrar kazandıramaz. P4.3'te dördüncü NPC hizmeti limandaki tekne satıcısıdır; üç farklı gövde ve aktif araç seçimi burada yapılır.

NPC'lerin ayakta bekleme, oyuncuya dönme ve kısa alışveriş hareketi vardır. Serbest dolaşan NPC AI'ı, uzun konuşma ağacı veya her hizmete ayrı büyük iç mekân gerekmez. Tezgâh, tabelası ve ulaşılabilir insan satıcısıyla gerçek bir yer olarak okunması zorunludur.

Güvenli dönüşte avın geçici dalış kaydı kalıcı **satış bekleyen eşya** hâline gelir; görünür çanta/taşıma listesinde kalır. Satılmayan av para değildir ve ağırlık sınırına dâhildir. Oyuncu sonraki dalışa taşırsa taşınan av D07 kayıp riskine girer; eski güvenli kopya ikinci satış kaynağı olarak tutulmaz. Kaydı garantiye alınmış görüntü adayları sonraki dalıştan ayrı bekler.

Bir oyuncu erken kıyıya dönerse avı kilitlenir, oyuncu `Returned` olur ve aynı dalışa yeniden giremez. Tüm dalgıçlar dönene/pasif olana kadar NPC satış ekranı bekleme nedenini gösterir. Ekip tamamlandığında tek kontrol noktası oluşturulur ve satış açılır; canlı dalgıçlar erken dönen oyuncu yüzünden ışınlanmaz.

Kopma/yeniden açma sonrası sahipsiz bekleyen av/kayıt ortak kampanya emanetine geçer; kasabada çantaya tekil olarak teslim alınabilir. Oturumluk guest bağlantı ID'si kalıcı insan kimliği sayılmaz. Kimin ne taşıdığı ve ortak emanet aynı itemInstanceId üzerinde tek sahiplikle tutulur.

## Başlangıç sandalının onarımı ve yolculuk

**P3.3 ilk kullanım:** Bir ahşap sandal, bir iskele, bir yakın demirleme, dört sabit koltuk. Sandal yeni kampanyada `Broken` durumundadır; ilk ana görev budur. Üç onarım kalemi: tahta parça, halat ve sızdırmaz yama; çalışma adlarıdır. Parçalar kasaba/kumsalda ücretsiz bulunur veya yakın su avının geliriyle satın alınır; kamera/sandal/derin dalış gerektirmez. Her parça ayrı onarım noktası ve görsel değişiklik üretir. Genel üretim/alet sistemi kurulmaz.

- Tek kişi üç parçayı da sağlayabilir; co-op oyuncular farklı parçalara katkı verebilir. Aynı parça iki kez tüketilmez, iki oyuncu aynı noktayı iki kez tamamlamaz.
- Onarım ilerlemesi, kullanılan parça kimlikleri ve son durum birlikte kaydolur. Tamirden sonra yeniden girişte sandal tekrar bozulmaz. Eksik parçayla yola çıkılmaz; eksik kalem oyuncuya yazılır.
- Yolculuk öncesi herkes kendi koltuğuna biner. İlk koltuk sefer sorumlusudur; host bu oyuncunun rota/başlatma isteğini doğrular. Yetki host oyuncusu olmakla aynı şey değildir.
- **Hızlı ilk uygulama:** Sandal belirlenmiş su rotasında görünür biçimde hareket eder; oyuncular ve ekipman sandalda kalır. P4.3 büyük tekne satın almayı ekler; serbest dümen, dalga fiziği, yakıt ve batma bu sürümde yoktur. Rota geçişi bir menü ışınlanması değildir.
- Kıyıdan dalacak oyuncu sandala binmek zorunda değildir. Seferin yolcu listesi ayrıdır; küresel dalış durumu ile tekne durumu karıştırılmaz. Bir sefer için herkesin koltuğu ve hazır olması gerekir.
- İnme/binme yalnız iskelede veya demirlemişken mümkündür. Sefer grubundan etkin bir dalgıç su altındayken dönüş başlatılmaz; eksik oyuncu adı/durumu görünür. Pasif/kopan oyuncunun engeli host tarafından temizlenir. Sağlıklı oyuncular zorla geride bırakılamaz.
- Sefer sorumlusu koparsa kalan yolculardan birine kontrol verilir; kimse yoksa sandal iskeleye dönüp kullanılabilir olur. Host kopması D06/D08 gereği oturumu sonlandırır. Otomatik iskele dönüşü oyuncu kurtarma mekaniği veya kayıp avı geri verme değildir.
- Boş sandal iskeleden çağrılabilir; demirleme yerinde etkin sefer dalgıcı varken çağrı reddedilir. Böylece araç kilitlenmez veya su altındaki ekip terk edilmez.
- İskeleye dönüş yeni DiveId yaratmaz. Her dalgıcın güvenli dönüşü ve ekibin tamamlanma kontrolü yapılır; sandal park etmek tek başına para veya kayıt ödülü üretmez.

## Aynı denizde ilerleme — P4

Aşağıdaki metreler **ilk dengeleme taslağıdır**, uygulanmış ölçüm veya kesin harita boyutu değildir. Oksijen/tempo testine göre aynı kapsam içinde ayarlanır. Bölgeyi büyütme, yeni harita üretmek değil mevcut denizde daha uzağa/derine erişmek anlamındadır.

| Kesim | Başlangıç derinlik taslağı | Ulaşım | Keşif ve karar |
|---|---|---|---|
| Kıyı / sığ | 0–8 m | Kumsaldan yüzerek; P3'ten itibaren açık | Temel av, ışıklı kum/kaya, güvenli dönüşü öğrenme ve ilk gelir |
| Resif / orta | 8–20 m | Motorlu teknenin resif demirlemesi | Daha uzun dip süresi, yeni tür/habitat; tüp/palet/çanta arasında harcama kararı |
| Uzak / derin | 20–35 m | Küçük araştırma teknesinin derin rotası | Daha az ışık, nadir canlı/avcı/olay ve batık/boss odağı; dönüş oksijeni ve kamera yeteneği önem kazanır |

P3'te yakın demirleme aynı sığ alanın başka girişidir; orta/derin içerik P4 açılmadan eklenmez. Bu derinlikler oyun ölçeğidir; gerçek dalış fizyolojisi/dekompresyon simülasyonu yapılmaz.

| Gelişim | Görünür değişim | Oynanış etkisi ve sınır |
|---|---|---|
| Kamera 0 — temel | Dükkândan ayrı satın alınan kompakt kamera | Mevcut kadraj/görüş/süre kuralları; P4.2'de gerçek klip yakalama |
| Kamera 1 — ilk yükseltme | Daha geniş gövde, iki el tutacağı | Geçerli çekim menzili artar; kadraj/görüş/temel süre hâlâ şarttır. Tek katalog değeri değerlendirmeye iletilir |
| Kamera 2 — ikinci yükseltme | Büyük muhafaza, ışık/kol ekleri; farklı iki el pozu | Aynı derin kesimde düşük ışıkta geçerli çekim yapabilir; ışık konisi hostça doğrulanır, engelden kayıt alamaz. Otomatik yüksek kalite veya düz fiyat çarpanı değildir |
| Tüp | Mevcut seviye kataloğuna uygun ayırt edilir tüp/bağlantı | Doğru oyuncunun kapasitesini temel değerden hesaplar; id/fiyat/mevcut kayıt uyumu korunur |
| Palet / çanta | Daha uzun palet / daha kapasiteli çanta görünümü | Birer yükseltme ile yüzme/taşıma kararı değişir; katlanan bonus yok |
| Zıpkın | Bir görünür geliştirme | Boss dâhil av kullanımında somut avantaj; kadraj/hasar otoritesini atlamaz |
| Tekne | Onarılan sandal → ayrı motorlu gövde → küçük araştırma teknesi | Para/keşifle alınır, yeni rota ve sandık kapasitesi; tek aktif araç, üç sahiplik kademesi |

Uzaklık deniz yolculuğunun, derinlik dalgıcın oksijen/geri dönüş planının konusudur. Harita panosu rota durumunu, yaklaşık derinliği ve önerilen ekipmanı gösterir. Kamera satın almak derinliği tek başına açmaz; üst seviye ekipman başlangıç avını değersizleştirmez. Temel gelir/onarım ücretsiz başlangıca açık kaldığı için ölüm veya kötü alışveriş kampanyayı kilitlemez.

## Teslim sırası

1. **P3.0:** Mevcut teknik işin gerçek durumu ve regresyonu korunur. Bu, yeni P3'ü kapatmaz. Otomatik ödeme bağlantısı NPC teslimine, P4.2'de tek ticari hakla PC kanal seçeneğine dönüşür.
2. **P3.1:** Mert tek insan rig'i + minimum hareket klipleri + temel kamera/zıpkın setini sağlar; Mehmet oyuncu/ekipmana, Utku kıyı-su geçişine bağlar. İlk iki oyunculu görünür gösterim burada alınır.
3. **P3.2:** Kasaba/kumsal/üç NPC, ayrı kamera satın alma ve ilk sandal görevi bağlanır; av/kayıt ödülü NPC teslimine taşınır. Kalıcı bekleyen eşya ve eski kayıt geçişi tamamlanır.
4. **P3.3:** Üç parçayla onarım, dört koltuk, yakın rota ve canlı tekne konumlu harita bağlanır. Solo/co-op taşıma/onarım/kopma denenir.
5. **P3.4:** Tam döngü ve [görsel kabul](ASSET_PLAN.md) gerçek build'de denenir. Kısa video/görüntü ve test sonucu aynı commit ile kaydolur; üç kişinin gerçek değerlendirmesi alınır.
6. **P3 kapanışı ve ayrı P4 açılışı sonrasında:** P4.1 ev/gün/keşif → P4.2 klip/PC/kanal → P4.3 kamera/büyük tekneler → P4.4 tür/boss → P4.5 günlük çeşitlilik/ev-kasaba/roller → P4.6 bütün oyun/sanat kabulü. Her ara teslim kabul edilmeden sonraki uygulanmaz; P5 yeni özellik üretmez.

Takvim için her ara teslimin sonucu ve kalan engeli yazılır; kişilerin özel kapasite/donanım bilgisi plana alınmaz. Başka birinin sahne/prefabına destek verilecekse devir [WORKFLOW](WORKFLOW.md)'a göre kaydolur. Yeni kapsamın GitHub görevleri henüz açılmış veya ekipçe kabul edilmiş sayılmaz.
