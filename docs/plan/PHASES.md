# Mehmet, Utku ve Mert — faz planı

Plan sürümü: **3.0 — 15 Eylül 2026**. Kullanıcının yeni isteği: yaşayan kıyı dünyası; ilk sandal görevi ve alternatif kıyı avı; ayrı satın alınan kamera; canlı tekne konumlu keşif haritası; ortak ev, yataklar, gün/saat ve gün özeti; gerçekten izlenebilir klipler ve ev PC'sinden oyun içi kanal; daha büyük tekneler; ansiklopedi, boss, sponsorlar, kasaba gelişimi ve günlük çeşitlilik.

**P3 açık; P4–P6 kilitli.** Bu revizyon plan değişikliğidir; uygulama başlangıcı, ekip onayı veya faz kapanışı değildir. P0/P1/P2'nin tarihsel kabul kayıtları değiştirilmez. P3'ün eski teknik kapanış adayı yeni kapsamı tamamlamış sayılmaz.

Bu belge görev ve kapsamın kaynağıdır. Güncel durum [STATUS.md](STATUS.md), bağlantılar [CONTRACTS.md](CONTRACTS.md), birleştirme düzeni [WORKFLOW.md](WORKFLOW.md) içindedir.

Oyuncunun baştan sona yapacakları ve ara teslimler: [GAMEPLAY_LOOP.md](GAMEPLAY_LOOP.md). Model, animasyon, çevre ve ses teslimleri: [ASSET_PLAN.md](ASSET_PLAN.md).

Ev/gün, video/kanal, harita, tekne ve boss davranışları: [WORLD_SYSTEMS.md](WORLD_SYSTEMS.md). **Bu dosya bütün fazların ana planıdır**; yan belgeler burada tanımlanan işlerin ayrıntılarıdır.

## Kim hangi alanı üstleniyor?

| Kişi | Görev kodu | Ana alan |
|---|---|---|
| Mehmet | A | Oyuncu, dalış, ekipman kullanımı ve temel oturum bağlantısı |
| Utku | B | Sualtı bölgesi, canlılar, avlanma ve kamera hedefinin değerlendirilmesi |
| Mert | C | Kasaba, oturum ekranları, envanter, ekonomi, ilerleme ve kayıt |

Bu bir başlangıç atamasıdır; deneyim seviyeleri henüz bilinmiyor. Herkes kendi alanının kodunu, gerekli arayüzünü ve co-op testini birlikte teslim eder. Bütün ağ kodu Mehmet'e, bütün görsel üretim Utku'ya veya bütün test Mert'e bırakılmaz.

Kullanıcının son ataması: **Mert görsel/ses üretimi, kaynak seçimi ve tutarlılığın sahibidir. Mehmet takvim, devam/ayrılma planı, ürün hedefi, tasarımda son karar, bütçe/servis takibi ve birincil netcode incelemesinin sahibidir.** Ayrıntılı sınırlar [P0_MEETING](P0_MEETING.md) içindedir. Bu ortak sorumluluklar mevcut geliştirme işlerini devretmez: herkes Mert'in sağladığı görsel/sesi kendi sistemine bağlar ve test eder. Mert'in üretim yükü de dengelenir; her varlığı sıfırdan kendisinin yapması şart değildir.

İş yükü küçük alt görevlerle ve gerçek çalışma süresiyle dengelenir. Erken bitiren kişi aynı fazın incelemesini/testini veya kayıtlı bir destek işini alır. Hiç kimse tek başına sonraki faza geçmez.

## İlk sürüm ve kararlar

Tam oyun döngüsü: ortak evde uyan → harita/hava/görevlere bak → dükkânda hazırlan → kumsaldan yüz veya tekneyle çık → avla, yeni tür keşfet ve eldeki kamerayla klip çek → geri dön, çantadaki balığı balıkçıya sat → ev PC'sinde klipleri izle/seç/yayınla → ekipman/tekne/ev geliştir → birlikte uyu veya 00:00'da günü kapat → özet ve ertesi sabah kanal sonuçları → yeni keşif ve boss hedefi.

**P3** bu dünyanın oynanabilir kıyı temelidir. **P4.1–P4.6** yeni tam oyun kimliğini tamamlar; bütün özellikleri P3'e sıkıştırmaz. **P5** doğrulama/denge/performans, **P6** teslimdir. Sayılar ve saat ölçeği başlangıç tasarımıdır; test edilmeden süre/kalite garantisi verilmez.

Aşağıdaki kararlar güncel kapsamdır. [P0 toplantı gündemindeki](P0_MEETING.md) çözülmemiş kaynak/bütçe ve devam kararları ilgili iş başlamadan ele alınır; toplantı yapılmış sayılmaz. Plan 3.0, önceki gerçek video ve büyük tekne dışlamalarını aşağıdaki sınırlarla değiştirir. Hizmet/eşya/boss adları çalışma adlarıdır; fiyat/mesafe/tempo testle ayarlanır.

| Kimlik | Başlangıç kararı | Sınır |
|---|---|---|
| D01 | Windows PC, birinci şahıs; URP ile insan oranlı, stilize kıyı/sualtı atmosferi | Yerelde eller/ekipman; diğer oyuncuda tam insan gövdesi ve animasyon. Ek platform yok |
| D02 | Unity 6.3 LTS; kesin yama ve gereken paketler kilitli | Sonraki paketler ortak kararla eklenir |
| D03 | Bir ev sahibi ve en çok üç katılımcı; kritik sonuçları ev sahibi doğrular | Solo aynı mantığı yerelde kullanır; Relay zorunlu değildir |
| D04 | Kamera ayrı satın alınan tekil ekipmandır; tutan oyuncunun gerçek bakışından çekim yapar. P3 değerlendirme; P4.2 izlenebilir klip ve ev PC'sinden ortak kanala yayın | Yayın oyun içi kurgusal platformdadır. Gerçek sosyal medya hesabına yükleme yok; klip gerçek oyun görüntüsü/sesidir, yalnız puan kartı değildir |
| D05 | Ortak para/ekipman havuzu; kişisel oksijen ve taşıma çantası | Satın alma/tahsis ev sahibinde doğrulanır |
| D06 | Kampanya ev sahibinin bilgisayarında saklanır | Kayıt sahibi host yokken aynı kampanya sürdürülemez; başka host ayrı kampanya açabilir. Bulut kayıt ve ilerleme aktarımı yok |
| D07 | Sağlık/oksijen bitince dalış boyunca pasif kalınır; güvenli dönmeyen av/çekim kaybolur | P4'te 00:00'da hâlâ güvenli alana dönmeyenler aynı kayıp kuralına tabidir. Kalıcı ekipman/tekne silinmez; kurtarma mekaniği yok |
| D08 | Yeni katılım kasabada; ev sahibi koparsa menüye dönüş ve son sağlam kayıt | Dalışa geç katılma, otomatik yeniden bağlanma ve ev sahibi devri yok |
| D09 | P3 üç hizmet: ekipman, balıkçı, görüntü alıcısı. P4.3'te limanda tekne satıcısı; P4.2'de evde ortak PC | Aynı çekim ya NPC'ye lisanslanır ya kanalda gelir üretir; iki yoldan ödeme yok. Balık avı ve çekim ayrı emektir; ikisi birlikte kazanabilir |
| D10 | Bir kıyı kasabası, ortak ev, kumsal/iskele ve tek bağlantılı deniz bölgesi | P3 kıyı/yakın demirleme; P4 sığ/resif/derin kesim ve boss odağı. İkinci ada/harita yok |
| D11 | P3 ücretsiz kıyı av seti ve dükkândan alınan temel kamera; mevcut tüp kataloğu. P4 kameraya iki, palet/çanta/zıpkına birer yükseltme | Ekipman tekil ve tahsislidir. Dört kişinin kamera kullanması için dört kamera gerekir; ilk kazanç için kamera şart değildir |
| D12 | P3'te insan rig'i, yürüme/yüzme geçişleri, birinci şahıs elleri ve ağda görünen kuşanılmış ekipman | Kapsamlı karakter yaratma, yüz performansı veya sinematik animasyon sistemi yok; temel insan hareketi zorunlu |
| D13 | İlk ana görev sandalı üç parçayla onarmaktır; oyuncu önce yakın suda avlanıp para kazanmayı da seçebilir | Parçalar kıyıdan bulunur veya balık gelirinden alınır; ücretsiz erişim yolu korunur. Solo/co-op mümkündür; genel crafting yok |
| D14 | P3 tamir edilen sandal; P4.3 satın alınan motorlu tekne, ardından küçük araştırma teknesi | Üç ayrı gövde/model; hepsi dört kişilik. Ortak sahiplik, aynı anda denizde bir aktif araç; limanda değişim. Sabit rotalı seyahat korunur; serbest dümen/fizik simülasyonu yok |
| D15 | P4'te derinlik ve kıyıdan uzaklık ayrı ilerler; kamera/tüp/palet/çanta ve sandal birbirini tamamlar | Daha büyük kamera derinlik izni değildir. Erişim, dönüş oksijeni, canlı/ışık ve taşıma kararıyla anlamlılaşır |
| D16 | P3.3 harita ve canlı sandal/oyuncu/iskele ikonları; P4.1 ortak keşif örtüsü, işaret/ping, derinlik ve ansiklopedi temeli | Her balığın canlı konumu gösterilmez; keşfedilen habitat ve son gözlem bilgisi gösterilir |
| D17 | P4.1 dört yataklı ortak ev, ortak saat, gün sonu ve ertesi sabah | Bağlı etkin oyuncuların hepsi evde uyursa erken kapanış; aksi hâlde 00:00 zorunlu kapanış. Kopan/pasif kişi uykuyu kilitlemez |
| D18 | P4.2 ortak PC: klip arşivi/oynatma, başlık/kapak, oyun içi kanal ve ertesi gün izlenme/takipçi/gelir | Tek yayın/klip, tek ödeme/gün; aynı medyayı tekrar adlandırıp para kazanma yok. Oyun dışı ekran/mikrofon kaydı yok |
| D19 | P4.4 üç kesimde beş normal tür + bir ayrı boss; ansiklopedi ve söylenti → iz → keşif → mücadele/çekim zinciri | Bir gerçek boss karşılaşması; solo ve 1–4 ölçeklemesi; öldürme veya araştırma ilerlemesi mümkün. Sonsuz boss/ada üretimi yok |
| D20 | P4.5 sponsor/sipariş, gece/hava/akıntı, görünür ev-kasaba gelişimi ve hafif ekip rolleri | Küçük sabit içerik bütçesi; zorunlu sınıf/XP ağacı, açlık sistemi veya sürekli bakım borcu yok |

## Tam oyun için içerik bütçesi

- Bir ortak ev, dört yatak, bir PC, bir ortak depo ve trofe/fotoğraf duvarı; ev için iki görünür gelişim kademesi.
- P3'te üç, P4'te dört NPC hizmeti; ekipman dükkânı, balıkçı, iskele için birer görünür kasaba iyileştirmesi.
- Üç satın alma/sahiplik kademesinde tekne: sandal → motorlu tekne → küçük araştırma teknesi; tek aktif araç, bir kıyı limanı ve üç deniz demirleme noktası (yakın/resif/derin).
- Tek denizde üç derinlik kesimi; beş normal tür + bir boss. Bir özel biyolüminesans olayı korunur.
- Dört ana görev zinciri: sandal onarımı, ilk kamera, ilk kanal yayını, boss araştırması/mücadelesi. Üç balık siparişi ve üç sponsor şablonu; aynı anda bir sipariş ve bir sponsor.
- Üç kamera modeli/kademesi, mevcut tüp kademeleri, palet/çanta/zıpkına birer yükseltme; dört değiştirilebilir hafif rol.
- İki hava durumu (sakin/rüzgârlı), gündüz/gece ışığı ve bir yerel akıntı alanı. Tam fırtına/deniz simülasyonu yok.

Yeni tam oyun kapsamı önceki üç görev/tek sandal sınırının yerini alır. P3'ün ilk oynanabilir kapsamı korunur; içerik bütçesi P4 açılmadan üretilmez. Ek fikirlerin sınırı WORLD_SYSTEMS içindedir.

## P0 — Birlikte çalışmaya hazır olma

Amaç: kısa kurulum, çalışan örnek build ve üç kişinin değişiklik paylaşabilmesi. CI, ayrıntılı imza tabloları ve bütün oyun sözleşmeleri bu fazı bekletmez.

| Görev | Sorumlu | Yapacağı iş |
|---|---|---|
| P0-A | Mehmet | Unity/URP projesini açar; tam editör/paket sürümünü sabitler; kamera, ışık ve basit nesneli sahneyi hazırlar; Windows build alır. Repo erişimlerini sağlar; ortak kararları, bütçe/hedef ve netcode yedek/öğrenme planını takip eder. |
| P0-B | Utku | Unity `.gitignore`/`.gitattributes`, görünür `.meta` ve metin serileştirme düzenini sağlar; güncel projeyi indirip aynı Unity sürümünde açar ve ortak Windows build'ini çalıştırır. |
| P0-C | Mert | Güncel projeyi temiz klondan aynı Unity sürümüyle açar, ortak Windows build'ini çalıştırır ve üç kişinin kısa kurulum sonucunu kaydeder. Görsel/ses sorumluluğu sonraki fazlarda sürer. |

Birlikte: ortak temel main'e birleşir. Mehmet, Utku ve Mert güncel depoyu indirir, Unity `6000.3.23f1` ile açar ve aynı örnek Windows build'ini çalıştırır. P0'da karşılıklı PR incelemesi ve herkesin deneme branch'i pushlaması zorunlu değildir.

P0_MEETING gündemi referans olarak korunur; P0 kapanışını bekletmez. Kaynak yöntemi, bütçe ve netcode yedeği gibi açık kararlar ilgili özelliğe başlamadan netleştirilir. Atama uzmanlık kanıtı veya satın alma izni değildir.

Bitiş koşulları:
- [x] Ortak Unity temeli main'e birleşti.
- [x] Üç kişi güncel depoyu indirip Unity `6000.3.23f1` ile açtı.
- [x] Mehmet'in ürettiği örnek Windows build'i üç kişi de çalıştırdı; üç ayrı build üretmek şart değil.
- [x] `.gitignore`/`.meta` düzeni, gerekli sürüm ve kısa kurulum adımları depoda kayıtlı.

Birleştirmeyi koordine eden: Mehmet.
Kapalı kapsam: gerçek co-op, balık AI, oksijen, ekonomi ve oyun özellikleri. Branch koruması P1'e, otomatik build P2'ye planlanır. Büyük ikili varlık henüz yoksa LFS kurulumu ilk böyle varlık eklenmeden önce yapılır.

## P1 — Aynı oturumda hareket

| Görev | Sorumlu | Yapacağı iş |
|---|---|---|
| P1-A | Mehmet | Oda oluşturma/katılma/ayrılma, oyuncu oluşumu, yürüme/yüzme ve hareket senkronizasyonunu yapar. |
| P1-B | Utku | Basit hazırlık alanı ve tek sualtı test bölgesi; zemin/su çarpışmaları ve giriş/çıkış noktalarını hazırlar. |
| P1-C | Mert | Oda/oyuncu listesi, hazır olma ekranı ve hazırlık → dalış → dönüş durumunu yapar; Mehmet'in yönetici desteğiyle basit branch korumasını kurup dener. |

Bağlantı: Mehmet ağ üzerinden sahne yüklemeyi sağlar; Mert ne zaman geçileceğini belirleyen oturum durumunu yönetir. Utku'nun alanı bu ortak akışta yüklenir. Mert başlangıçta örnek oturum verisiyle çalışabilir.

Bitiş koşulları:
- [ ] Solo çalışıyor; en az iki farklı bilgisayardan internet üzerinden birlikte oynanabiliyor.
- [ ] Dört ayrı oyun süreci en az iki bilgisayarda bağlanıyor; beşinci oyuncu reddediliyor.
- [ ] Oyuncular birbirini görüyor, hareket ediyor ve aynı dalış alanına geçiyor.
- [ ] Dalışta yeni katılım reddediliyor; katılımcı/ev sahibi ayrılması kontrollü sonuçlanıyor.
- [ ] Özellik PR'ları bir başka kişinin incelemesinden geçiyor; basit branch koruması doğrulandı veya erişim engeli ve uygulanacak manuel kural açıkça kaydedildi.
- [ ] Mehmet gerçek projede host yetkisi/sahiplik/kopma akışını ve loglarla hata izlemeyi gösterdi; T06'da belirlenecek yedeği kontrolü tekrarladı. Sonuç aynı faz kaydında; P0'da uzmanlık varsayılmadı.

Birleştirmeyi koordine eden: Utku.
Kapalı kapsam: oksijen tüketimi, avlanma, satış, kamera puanı ve ayrıntılı harita.

## P2 — Avla, taşı, geri dön

| Görev | Sorumlu | Yapacağı iş |
|---|---|---|
| P2-A | Mehmet | Oksijen/sağlık, zıpkın, etkileşim, dalgıç göstergeleri ve pasif kalmayı yapar. Yüzme/nişan hissini ayarlar; temel atış/vuruş geri bildirimi ve düşük oksijende nefes/uyarı sesi ekler. |
| P2-B | Utku | Tek balığın yüzme/kaçma/vurulma davranışını, basit vurulma tepkisini ve tekil av nesnesini yapar; temel sualtı sisi/ışığını ekler. Oynanabilir av döngüsünden sonra mevcut build komutunu CI'a bağlar; hesap/lisansta Mehmet destek olur. |
| P2-C | Mert | Çanta, ağırlık sınırı, av ekleme, güvenli dönüş, kayıpta temizleme ve dalış özetini yapar. Av alındı/çanta dolu/geri dönüldü durumlarına anlaşılır temel UI geri bildirimi ekler; kısa oynama testini koordine eder. |

Bağlantı: Mehmet zıpkın/toplama isteği gönderir → Utku vurulma ve avı doğrular → Mert çantaya ekler. Çanta doluyken avı yok etmezsiniz. Bu sözleşme P2 işlerine başlamadan kesinleşir.

Bitiş koşulları:
- [ ] 1, 2 ve 4 oyuncuda balık avlanıp taşınabiliyor ve güvenli dönülebiliyor.
- [ ] Aynı avı iki kişi aynı anda alınca yalnızca bir çantaya ekleniyor.
- [ ] Dolu çanta avı kaybettirmiyor; oksijen/pasif oyuncu ve herkesin başarısızlığı doğru işliyor.
- [ ] Art arda iki dalışta eski av, oyuncu veya sayaç kalmıyor.
- [ ] Yüzme ayarı, vuruş tepkisi, oksijen nefes/uyarısı, çanta bildirimi ve basit sualtı sisi/ışığı test öncesinde mevcut; geri bildirimsiz boş sahne üzerinden tasarım kararı verilmiyor.
- [ ] Otomatik build gerçek commit üzerinde denendi. Lisans/erişim engeli varsa nedeni ve sorumlusu kaydedildi; ekipçe doğrulanmış manuel build komutu kullanılıyor. Yapılmayan CI başarılı sayılmıyor.

P3 öncesi kısa oynama testi (10–15 dakika):
- Yüzmek, nişan almak ve avlanmak anlaşılır ve keyifli mi?
- Oksijen devam etmekle geri dönmek arasında anlamlı bir karar yaratıyor mu?
- Çanta kapasitesi hangi avı alma/bırakma kararını değiştiriyor mu?
- Mehmet kontrol hissini, Utku av/çevre tepkisini, Mert kapasite/oksijen kararlarını kaydeder. En az iki oyuncu birlikte dener; üçünüz sonuçları tek kısa kayıtta değerlendirirsiniz.
- Kontrol veya geri bildirim eksikliği varsa P3'e kamera/ekonomi eklemeden önce P2'de kısa düzeltme ve yeniden deneme yapılır. En çok iki kısa turdan sonra hâlâ sorun varsa tasarım birlikte yeniden değerlendirilir.

Bu P2'nin tarihsel his kapısıdır. P2 kabulü korunur; 15 Eylül'de istenen insan rig'i, gerçek yüzme/yürüme animasyonu ve fiziksel dünya akışı aşağıdaki P3 kapsamına eklenmiştir. Ücretli varlık veya kapsamlı animasyon sistemi şart değildir. Görsel beğeni ile kontrol/karar sorunları ayrı not edilir.

Birleştirmeyi koordine eden: Mert.
Kapalı kapsam: satış, para, geliştirme, kamera ödülü ve ikinci canlı türü. CI için yeni bir framework yazılmaz.

## P3 — İnsan karakterle kasabadan denize tam döngü

Amaç: ilk tam oynanabilir sürümde oyuncu ve arkadaşları insan gibi hareket eder, ekipman görünür, alışveriş ve yolculuk dünyada gerçekleşir. P3'teki teknik çalışma korunur; aşağıdaki yeni teslimler ayrıca yapılır. Sıra ve bağımlılıklar [oyun akışında](GAMEPLAY_LOOP.md#teslim-sırası) tanımlıdır.

| Ara teslim | Mehmet (A) | Utku (B) | Mert (C) |
|---|---|---|---|
| P3.0 — Mevcut teknik işi birleştir | Kamera/tüp/ödeme bağlantısı ve güncel regresyonlar | Özel olayın gerçek ışık/ses/çekim doğrulaması | Gerçek ekonomi/save-load ve tekrar işlem doğrulaması; yeni NPC akışına geçiş için ödeme noktası |
| P3.1 — İnsan ve elde ekipman | İnsan rig'ini harekete bağlar; yürüme, su üstü/sualtı yüzme, dalma/yükselme, eller, zıpkın/kamera tutuşu ve uzaktan görünür durum | Su çizgisi, kıyı çarpışmaları ve insan ölçüsünde giriş/çıkış; animasyon geçişlerini iki oyuncuyla test | Tek uyumlu karakter/animasyon seti ile kamera/zıpkın/tüp/çanta/palet varlıklarını sağlar; loadout görünüm verisini bağlar |
| P3.2 — Kasaba, kumsal ve mağazalar | Yakınlık/etkileşim ve elde taşıma; satın alınmış kamerayı doğru oyuncuya bağlama | Kumsal/sığ av alanı ve kıyıdan dönüş; ücretsiz ulaşılabilir tamir parçaları | Üç NPC hizmeti; çantayla satış; temel kamera ayrı ürün; ilk sandal görevi ve parça satın alma alternatifi; kalıcı bekleyen ürünler |
| P3.3 — Sandalı onar, haritadan bul ve git/dön | Tamir, ağda koltuk/binme/inme, sabit rota hareketi; onaylı oyuncu/tekne konumu | İskele/yakın demirleme/rota, dünya-harita koordinatları ve su çarpışması | Onarım/sefer/kayıt; açılabilir harita, canlı sandal/oyuncu/iskele ikonları ve dönüş işareti |
| P3.4 — Birleşik oyun ve görsel kabul | Kontrol/kamera/ekipman ve yolcu senkronizasyonu | Av/keşif/sualtı okunurluğu ve dünya bütünlüğü | NPC/satış/onarım/kayıt; görsel/ses tutarlılığı ve ortak oynama kaydı |

Bu ara teslimler yeni faz değildir; hepsi P3 içindedir. Mert kaynak setini küçük paketler hâlinde teslim eder; Mehmet ve Utku kendi sahne/prefab bağlantılarını yapar. Üç kişinin tek büyük sanat teslimini beklemesi gerekmez.

Teknik ve görünür bitiş:

- [ ] Yerel oyuncu ellerini ve kullandığı ekipmanı, diğer oyuncular insan gövdesini görür. Yürüme, su üstünde yüzme, sualtı yüzme, dalma/yükselme ve karaya çıkma geçişleri doğru; T-pose, yerde kayma veya ayakta sualtı yürüyüşü yok.
- [ ] Kamera ve zıpkın kuşanma/kaldırma/saklama hâlleri doğru elde görünür; kayıt ışığı/durumu diğer oyuncuda da okunur. Tüp, çanta ve palet doğru bağlanır; yüzme sırasında ekipman gövdeden ayrılmaz.
- [ ] Oyuncu dükkândan kumsala yürür, suya girer, sığda avlanır ve aynı kıyıya çıkar. Menü/sahne geçişi fiziksel yolun yerine geçmez; gerekiyorsa yükleme dünya üzerindeki girişe bağlanır.
- [ ] Üç NPC'nin hizmeti ve yaklaşma/etkileşim alanı bellidir. Balıklar güvenli dönüşte satılmaz; çantadaki bekleyen av alıcıya götürülür, seçilip onaylanınca azalır ve para artar. Kayıt geliri değerlendirme NPC'sinde alınır.
- [ ] Sıfır parayla temel ekipman ve kıyı avına erişilir. Yeni kampanyadaki bozuk sandal üç parçayla solo veya ortak onarılır; onarım bir kez uygulanır ve yüklemede korunur.
- [ ] İlk görev sandal onarımıdır; önce yakın suda avlanıp balıkçıya satmak serbesttir. Kamera ayrı satın alınır ve tek oyuncuya tahsis edilir; başlangıçta herkese bedava kamera verilmez.
- [ ] Harita açılır; hareket eden sandal, bağlı oyuncular, iskele ve yakın demirleme doğru konumda görünür. Sandala dönüş işareti çalışır.
- [ ] Dört koltuklu sandal iskeleden yakın demirlemeye gider ve geri gelir; yolcular bunu görür. Hareket sırasında inme, koltuk çakışması, yüzücüyü geride bırakma ve kopan yolcu durumları [sözleşmeye](CONTRACTS.md#sandal-tamiri-ve-yolculuk--p3-taslak) göre çözülür.
- [ ] Hazırlan → kıyıdan/sandalla git → avla/kaydet → fiziksel olarak geri gel → NPC'lere teslim et → tüp geliştir → tekrar dal döngüsü tamamlanır.
- [ ] Aynı av/kayıt/onarım parçası/istek iki kez sonuç üretmez; eşzamanlı alışveriş parayı eksiye düşürmez. Uzak veya yanlış NPC'de işlem reddedilir.
- [ ] Görüş dışı/engel arkasındaki hedef kayıt üretmez; yeni tüp doğru oyuncuya doğru kapasiteyi verir; tekrar bildirim bonusu katlamaz.
- [ ] Kayıt yüklenince para, bekleyen av/kayıt, ortak ekipman ve onarım tutarlıdır. Eski kayıt için şema geçişi vardır; oturumluk guest kimliği başka oyuncuya eşya tahsis etmez.
- [ ] Solo, iki oyunculu gözlem ve en az iki bilgisayarda dört süreçle tam döngü doğrulanır; yerel otomasyon ayrı bilgisayar/internet kanıtı yerine geçmez.
- [ ] [P3 görsel kabulü](ASSET_PLAN.md#p3-görsel-kabulü) gerçek build'de geçer: insan model/animasyon, elde kamera, NPC mağazaları, kumsal-su geçişi, sandal ve okunur sualtı birlikte görünür.

P4 öncesi oyun testi:

- Üçünüz ayrı bilgisayar/internet üzerinden 20–30 dakikalık ortak deneme yaparsınız; solo onarım ve dört oyuncu kontrolleri ayrıca kaydedilir.
- Oyuncu dış açıklama olmadan nerede alışveriş yapacağını, kıyıdan nasıl dalacağını, sandalı nasıl onaracağını ve avı nereye satacağını bulabiliyor mu?
- Mehmet insan hareketi/elde ekipman/kamera ve yolculuk hissini; Utku kıyı-yüzme/av/keşif okunurluğunu; Mert satış, onarım ve tekrar dalma temposunu değerlendirir.
- Kamera avlanmadan farklı ve değerli bir seçenek mi; çantayı dolu geri taşımak ve kazançla gelişmek anlamlı mı; birlikte oynamanın faydası görülüyor mu?
- Teknik test ve güzel tek ekran görüntüsü tek başına kapanış değildir. Kontrol, yolculuk veya döngü zayıfsa P3 içinde en çok iki kısa düzeltme turu yapılır; sonra kapsam/tasarım yeniden değerlendirilir.

Birleştirmeyi koordine eden: Mehmet.
P3'te henüz teslim edilmeyenler: ev/gün/keşif örtüsü P4.1; izlenebilir klip/PC/kanal P4.2; büyük tekneler P4.3; boss P4.4. Bunlar tüm plandan çıkarılmış değildir. İkinci bölge, serbest tekne simülasyonu, genel crafting ve bulut kayıt kapsam dışıdır. Temel insan/sahne sanatı P3 şartıdır.

## P4 — Ev, kanal, keşif ve ileri oyun

Ön koşul: revize P3 teknik/görsel/oyun kabulü geçti ve P4 ayrıca açıldı. **P4.1 → P4.2 → P4.3 → P4.4 → P4.5 → P4.6** sırası küçük oynanabilir teslimlerdir; önceki ara teslimin kabulü olmadan sıradakinin özelliği uygulanmaz. Koordinatör Utku, alan sahipleri korunur. Bu sıra gerçek iş yükünü görünür kılar; takvim taahhüdü değildir.

| Ara teslim | Mehmet (A) | Utku (B) | Mert (C) |
|---|---|---|---|
| P4.1 — Ev, gün ve keşif | Ev/yatak/depo etkileşimi, uyuyan oyuncu ve harita ping/konum; gün kapanışında oyuncu durumu | Gün-gece ışığı, harita keşif hücreleri/derinlik verisi; ilk tür gözlem/ansiklopedi olayları | Dört yataklı ev, ortak saat/uyku/00:00, günlük özet/kayıt, ev deposu, keşif haritası ve ansiklopedi UI |
| P4.2 — Çek, izle ve yayınla | Gerçek oyuncu kamerasından klip/ses yakalama; dosya/aktarım/oynatma ve ortak PC ekranı; kayıt performansı | Klip-hedef/kalite/yenilik doğrulaması, canlı/olay etiketleri, medya ile kayıt sonucu eşlemesi | PC arşivi, başlık/kapak/yayın kuyruğu, ortak kanal/izlenme/takipçi/gelir, NPC veya kanal tek hak seçimi, günlük ödeme/save |
| P4.3 — Kamera ve büyük tekneler | İki kamera üst modeli/etkisi; palet/çanta/zıpkın yükseltmesi; üç gövdenin koltuk/hareket/harita bağlantısı | Kamera menzil/düşük ışık denetimi; yakın/resif/derin demirleme rotaları ve çevre | Tekne satıcısı, sandal→motorlu→araştırma teknesi satın alma/sahiplik/aktif seçim; katalog/tahsis/emanet/save |
| P4.4 — Derin keşif ve boss | Avcı/boss saldırısını oyuncuya bağlar; zıpkın/kaçınma/çekim ve solo kontrol testi | Üç kesimde beş normal tür + bir boss; davranışlar, üç işaretli saldırı, zayıf nokta/iz/söylenti/araştırma ve 1–4 ölçekleme | Ansiklopedi tam açılımları, boss görev zinciri, tek sefer ödül/trofe ve kampanya ilerleme kaydı |
| P4.5 — Her gün yeni hedef | Değiştirilebilir hafif rol etkileri; akıntı/uyarı/ekip pingleri | İki hava durumu, gece canlı davranışı ve tek akıntı alanı; günlük hedef uygunluğu | Üç sipariş/üç sponsor şablonu, kanal eşikleri; iki ev seviyesi ve üç kasaba iyileştirmesi; ortak depo/sergi ve denge |
| P4.6 — Tam dünya kabulü | İnsan/ekipman/çekim/tekne ve video performansı | Kıyı-resif-derin dünya, boss/canlı, gece/hava, ışık/ses | Ev/PC/mağaza/harita/kanal/UI tutarlılığı, bütün kayıtlar ve ortak çok günlük test |

### P4 ara teslim kabulü

- [ ] **P4.1:** Aynı saat dört oyuncuda görünür; tek kişi ve dört kişi uyuyarak günü kapatır. 00:00, kopan/pasif oyuncu ve güvenli dönmeyen av için belirlenen sonuç uygulanır. Gün özeti tek oluşur; yeniden açma günü iki kez ilerletmez. Harita açılan alanı/tekneyi, ansiklopedi bulunan türü korur.
- [ ] **P4.2:** İki oyuncu farklı kameralardan gerçek klip çeker, güvenli dönüşten sonra ev PC'sinde kendi görüntülerini izler; diğerleri aynı klibi görebilir. Oyun yeniden açılınca klip oynar. Yayın ve ertesi gün izlenme/takipçi/gelir tek kez işlenir. Tek puan kartı, stok video veya yalnız thumbnail bu kapıyı geçirmez.
- [ ] **P4.2 teknik ön kapı:** Hedef Windows build'inde klip yakalama/oynatma, boyut, disk doluluğu, kesilen aktarım ve 1–4 oyuncu performansı ölçülür. Başarısızsa yöntem düzeltilir; izlenebilir video şartı sessizce silinmez. Başlangıç bütçesi WORLD_SYSTEMS'te taslaktır.
- [ ] **P4.3:** Kamera modelleri elde/uzaktaki oyuncuda/mağazada büyür ve somut yetenek değiştirir. Büyük tekne ayrı satın alınır, gövde/koltuk/rota/ambar değişir; sadece eski sandalı ölçeklemek yeterli değildir. Aynı anda yalnız bir aktif tekne vardır; yeni tekne eski sahibi/envanteri çoğaltmaz.
- [ ] **P4.4:** Sığdan derine yeni tür keşfi ve ansiklopedi ilerler. Bir gerçek boss araştırma veya öldürme yoluyla tamamlanabilir; 1–4 oyuncuda oynanabilir, zorunlu rol/eşzamanlı çok kişi kapısı yoktur. Dört oyuncu aynı ödülü çoğaltamaz; avcı tür boss sayılmaz.
- [ ] **P4.5:** Ulaşılabilir sipariş/sponsor üretilir; aynı klip/ödül/ev iyileştirmesi iki kez kazandırmaz. Gece/hava/akıntı öngörülebilir uyarı ve dönüş kararı üretir. Her rol solo oynanabilir ve kasabada değişebilir.
- [ ] **P4.6:** Sabah → av/çekim → satış/PC yayını → gelişim → uyku → özet → ertesi sabah sonuç döngüsü, en az iki bilgisayarda dört süreçle art arda üç oyun günü çalışır. Ölüm, 00:00, host ayrılması ve diskten devam birlikte denenir.
- [ ] [P4 görsel kabulü](ASSET_PLAN.md#p4-görsel-kabulü) gerçek build'de geçer. İnsan/ev/PC/harita/üç tekne/boss/gece sanatı tamamdır; kaynak listesi ve kare süresi kanıtı vardır.

P4 oyun değerlendirmesi: balıkçılık ve kanal üretimi ayrı anlamlı kazanç yolları mı; eve dönmek/uyumak sadece bekleme mi; harita yeni keşfe yöneltiyor mu; büyüyen tekne gerçek imkân açıyor mu; boss savaşı ve çekim kararı keyifli mi? Üç kişinin gerçek değerlendirmesi ve en çok iki kısa düzeltme turu kayda alınır; eksik oyunla P5'e geçilmez.

Mert bütün model/animasyon/sesi tek başına sıfırdan üretmez. Ortak kaynak setini sağlar; Mehmet insan/ekipman/tekne/medyaya, Utku dünya/canlı/bossa, Mert ev/kasaba/UI'a bağlar. Destek/devir görevleri dosya bazında kaydolur.

Kapalı kapsam: ikinci ada/harita, ikinci boss, sınırsız filo, aynı anda birden çok aktif tekne, serbest dümen/dalga simülasyonu, bina inşa sistemi, zorunlu sınıf ağacı, gerçek sosyal medya yükleme, sesli sohbet kaydı ve bulut kampanya. Ek fikir listesi zorunlu teslimlerin önüne alınmaz.

## P5 — Hataları ve performansı düzelt

Yeni özellik ve içerik yok.

| Görev | Sorumlu | Yapacağı iş |
|---|---|---|
| P5-A | Mehmet | Ağ/insan/ekipman/tekne/uyku hataları, klip yakalama/aktarma/oynatma ve disk/performans sorunlarını düzeltir. |
| P5-B | Utku | Balık/boss AI, kıyı/rota/akıntı çarpışmaları, harita keşfi, gece/hava/ışık ve sahne performansını düzeltir. |
| P5-C | Mert | Gün/kanal/tekne/ev/boss kayıtları, tekrarlı ödemeler, PC/NPC/harita UI ve balık-kanal-ilerleme dengesini düzeltir. |

Bitiş koşulları:
- [ ] 1, 2 ve 4 oyunculu temel döngü regresyonları geçti.
- [ ] En az iki bilgisayardaki dört süreçle en az 30 dakika, art arda üç dalış ve üç oyun günü tamamlandı; satış, klip/PC yayını ve günlük sonuçlar dâhil.
- [ ] Ek 150 ms gidiş-dönüş gecikmesi ve %2 paket kaybı denemesinde para/av tutarlılığı korundu; yöntem kaydedildi.
- [ ] Uyku/00:00, boss sonucu, tekne alımı, NPC satışı, klip aktarımı/yayını ve günlük ödeme sırasında kopma/disk hatası denendi; eşya/para/gün/ödül çoğalmıyor, bozuk kayıt sessizce sıfırlanmıyor.
- [ ] Kayıt açıkken 1–4 oyuncu kare süresi ve disk kullanımı ölçüldü; eksik klip anlaşılır hata verir. Arşiv silmek kanal gelirini veya kampanya kaydını bozmaz.
- [ ] P4'te sabitlenen ortamda kare süresi ölçüldü. Başlangıç hedefi 1080p/60 FPS; sonuç düşük diye hedef sessizce değiştirilmedi.
- [ ] Kritik/yüksek önem hatası yok; kabul edilen küçük kusurlar listeli.

Birleştirmeyi koordine eden: Mert.
Kapalı kapsam: yeni mekanik, yeni içerik, gereksiz motor yükseltmesi ve sınırsız mimari değişiklik.

## P6 — Teslim paketini hazırla

| Görev | Sorumlu | Yapacağı iş |
|---|---|---|
| P6-A | Mehmet | Sabit commit'ten temiz Windows build alır; sürüm/commit ve yeniden build alma adımlarını kaydeder. |
| P6-B | Utku | Paketi bağımsız bilgisayarda dener; kontroller/oyuncu rehberini ve varlık atıflarını kontrol eder. |
| P6-C | Mert | Paketle kampanya/gün/kanal ve çevrimiçi kabulü yapar; klip arşivi/sınırı, kayıt konumu, bilinen sorunlar ve servis ayarlarını belgeler. |

Bitiş koşulları:
- [ ] Dağıtılacak paket geliştirici önbelleği olmadan başka bilgisayarda çalışıyor.
- [ ] Paketle solo, iki ve dört oyunculu günlük balık/video döngüsü; harita, büyük tekne, boss ve diskten devam geçti; commit/paket özeti kayıtlı.
- [ ] Sırlar pakette/depoda değil; ev sahibi ayrılma ve kayıt sınırları açık.
- [ ] Üç kişi aynı kapanış kaydında teslimi kabul etti.

Birleştirmeyi koordine eden: Mehmet.
Kapalı kapsam: mağaza yayını, ücretli servis satın alma, public oyun yayını ve ikinci bölge. Bunlar ayrı ürün kararıdır.

## Ortak faz geçişi

Üç kişinin teslimi → faz dalında birleştirme → ortak test → tek kısa kapanış kaydı → ana dala PR → birleşmiş build kontrolü → sıradaki faz.

Ayrı imza matrisleri yoktur; test sonucu, commit/build ve Mehmet/Utku/Mert'in gerçek tamam kaydı yeterlidir. P2'de erken kontrol/av/oksijen-çanta testi, P3'te insan/ekipman, kıyı/NPC/sandal ve kamera/tam döngü testi de yapılır. Başkasının tamamını yazamazsınız. Test başarısızsa veya bir kişinin işi eksikse sonraki faz kapalıdır. P3'ün birleşmiş build kontrolünden sonra P4 için ayrıca açılış kararı gerekir.

## Teknik referanslar

Kesin sürüm uyumluluğu kurulumda denenir:
- [Unity sürüm desteği](https://unity.com/releases/unity-6/support)
- [Unity co-op başlangıcı](https://docs.unity.com/en-us/multiplayer/quickstarts/casual-co-op-quickstart)
- [Relay bağlantısı](https://docs.unity.com/en-us/mps-sdk/networking/relay-servers)
- [Unity Smart Merge](https://docs.unity3d.com/6000.0/Documentation/Manual/SmartMerge.html)
