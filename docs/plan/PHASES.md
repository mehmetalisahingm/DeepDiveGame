# Mehmet, Utku ve Mert — faz planı

Plan sürümü: 1.4 — 31 Ağustos 2026. Görsel/ses sorumlusu Mert; diğer ortak sorumluluklar Mehmet'te. Hafif P0, P2/P3 oynama testleri ve temel his katmanı korunur.

Bu belge görev ve kapsamın kaynağıdır. Güncel durum [STATUS.md](STATUS.md), bağlantılar [CONTRACTS.md](CONTRACTS.md), birleştirme düzeni [WORKFLOW.md](WORKFLOW.md) içindedir.

## Kim hangi alanı üstleniyor?

| Kişi | Görev kodu | Ana alan |
|---|---|---|
| Mehmet | A | Oyuncu, dalış, ekipman kullanımı ve temel oturum bağlantısı |
| Utku | B | Sualtı bölgesi, canlılar, avlanma ve kamera hedefinin değerlendirilmesi |
| Mert | C | Kasaba, oturum ekranları, envanter, ekonomi, ilerleme ve kayıt |

Bu bir başlangıç atamasıdır; deneyim seviyeleri ve haftalık saatler henüz bilinmiyor. Herkes kendi alanının kodunu, gerekli arayüzünü ve co-op testini birlikte teslim eder. Bütün ağ kodu Mehmet'e, bütün görsel üretim Utku'ya veya bütün test Mert'e bırakılmaz.

Kullanıcının son ataması: **Mert görsel/ses üretimi, kaynak seçimi ve tutarlılığın sahibidir. Mehmet takvim/kapasite, devam/ayrılma planı, ürün hedefi, tasarımda son karar, bütçe/servis takibi ve birincil netcode incelemesinin sahibidir.** Ayrıntılı sınırlar [P0_MEETING](P0_MEETING.md) içindedir. Bu ortak sorumluluklar mevcut geliştirme işlerini devretmez: herkes Mert'in sağladığı görsel/sesi kendi sistemine bağlar ve test eder. Mert'in üretim yükü de dengelenir; her varlığı sıfırdan kendisinin yapması şart değildir.

İş yükü küçük alt görevlerle ve gerçek çalışma süresiyle dengelenir. Erken bitiren kişi aynı fazın incelemesini/testini veya kayıtlı bir destek işini alır. Hiç kimse tek başına sonraki faza geçmez.

## İlk sürüm ve kararlar

Çekirdek oyun: küçük kasabada hazırlan → dalışa git → avla ve kaydet → oksijen bitmeden dön → gelir elde et → ekipman geliştir → tekrar dal.

P0'da aşağıdaki çalışma kararları ve [kısa toplantı gündemi](P0_MEETING.md) toplam 45–60 dakikalık görüşmede ele alınır. Görsel/ses üretimi, kapasite/ayrılma, ürün hedefi, bütçe, tasarım yetkisi ve insan netcode sorumluluğu oyun kodundan önce konuşulur; öneriler ekip kararı sayılmaz. Teknik ayrıntılar ihtiyaç duyulan fazdan önce netleşir; P0'da tüm geleceğin API tasarımı yapılmaz. Kaynak konsept yalnızca dalış ekipmanları dükkânını adlandırıyor; diğer hizmetlerin adları varsayımdır.

| Kimlik | Başlangıç kararı | Sınır |
|---|---|---|
| D01 | Windows PC, birinci şahıs, URP ile stilize atmosfer | İlk sürümde ek platform yok |
| D02 | Unity 6.3 LTS; kesin yama ve gereken paketler kilitli | Sonraki paketler ortak kararla eklenir |
| D03 | Bir ev sahibi ve en çok üç katılımcı; kritik sonuçları ev sahibi doğrular | Solo aynı mantığı yerelde kullanır; Relay zorunlu değildir |
| D04 | Kamera canlı/olay, süre, görüş ve kadraj üzerinden puanlanan kayıt üretir | Gerçek video dosyası ve video paylaşımı yok |
| D05 | Ortak para/ekipman havuzu; kişisel oksijen ve taşıma çantası | Satın alma/tahsis ev sahibinde doğrulanır |
| D06 | Kampanya ev sahibinin bilgisayarında saklanır | Kayıt sahibi host yokken aynı kampanya sürdürülemez; başka host ayrı kampanya açabilir. Bulut kayıt ve ilerleme aktarımı yok |
| D07 | Sağlık/oksijen bitince dalış boyunca pasif kalınır; güvenli dönmeyen av/çekim kaybolur | Yaşayanlar devam eder; herkes pasifse dalış kapanır; kurtarma yok |
| D08 | Yeni katılım kasabada; ev sahibi koparsa menüye dönüş ve son sağlam kayıt | Dalışa geç katılma, otomatik yeniden bağlanma ve ev sahibi devri yok |
| D09 | Üç küçük hizmet: ekipman, av alımı, görüntü değerlendirme | Tek ortak işlem altyapısı; son iki ad kullanıcıdan verilmiş sayılmaz |
| D10 | İlk oynanabilir sürüm: küçük hazırlık alanı ve tek dalış bölgesi | İkinci bölge bu planın zorunlu teslimi değildir; P3'te eğlence değerlendirilir |
| D11 | Başlangıçta tek zıpkın, kamera ve tek tüp yükseltmesi | Palet ve çanta yükseltmesi P4'te |

## P0 — Birlikte çalışmaya hazır olma

Amaç: kısa kurulum, çalışan örnek build ve üç kişinin değişiklik paylaşabilmesi. CI, ayrıntılı imza tabloları ve bütün oyun sözleşmeleri bu fazı bekletmez.

| Görev | Sorumlu | Yapacağı iş |
|---|---|---|
| P0-A | Mehmet | Unity/URP projesini açar; tam editör/paket sürümünü sabitler; kamera, ışık ve basit nesneli sahneyi hazırlar; Windows build alır. Repo erişimlerini sağlar; ortak kararları, kapasite/bütçe/hedef ve netcode yedek/öğrenme planını takip eder. |
| P0-B | Utku | Unity .gitignore, görünür .meta ve metin serileştirme ayarlarını düzenler; oyuncu/dünya/kasaba klasör ve sahne sahipliğini belirler; Mehmet'in ilk Unity commit'ini gereksiz dosya/referans açısından kontrol eder. |
| P0-C | Mert | Üç kişinin haftalık çalışma kapasitesini toplar; Mehmet'in takip ettiği D01–D11 ve T01–T07 kararlarını kaydeder. Görsel/ses için kısa ihtiyaç ve kaynak/üretim planı hazırlar; erişim testini koordine eder; temiz klondan projeyi açıp kurulum rehberini doğrular. |

Birlikte: herkes kendi deneme branch'ine küçük bir değişiklik pushlar; başkasının birleşmiş değişikliğini alır. Mert'in görevi erişimi koordine etmektir; yönetici izni gerektiren işlemi Mehmet yapar.

Toplantı hazırlığı da paylaşılır: Mehmet ekipman/HUD, Utku sualtı/canlı/VFX ihtiyaçlarını Mert'e verir; Mert bunları kasaba/UI ihtiyaçlarıyla birleştirir. Herkes kapasitesini ve ağ deneyimini belirtir. Sorumlular kullanıcı tarafından atandı; kaynak yöntemi, saat, bütçe tutarı ve netcode yedeği hâlâ açık. Atama uzmanlık kanıtı, satın alma veya faz kapanışı değildir.

Bitiş koşulları:
- [ ] Üç kişi aynı sürümle ortak projeyi açıyor.
- [ ] Üç kişi kendi branch'ine pushlayıp birleşmiş değişikliği alabiliyor.
- [ ] Mehmet'in ürettiği örnek Windows build'i üç bilgisayarda çalışıyor; üç ayrı build üretmek şart değil.
- [ ] .gitignore/.meta düzeni doğru; gerekli sürümler ve kısa kurulum adımları kayıtlı.
- [ ] Kişiler/kapasiteler, D01–D11 başlangıç kararları ve P1 bağlantı tipleri kısa biçimde kaydedildi.
- [ ] P0_MEETING içindeki T01–T07 için uygulanabilir çalışma kararları kaydedildi; ertelenen seçimlerin geçici sınırı, sorumlusu ve takip noktası belli. İnsan netcode sorumlusu/yedeği ve D06 sınırı açık.

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

His katmanı sınırlıdır: basit animasyon/tepki, ses ve ışık ayarı yeterlidir; nihai sanat, kapsamlı animasyon sistemi, yeni tür veya ücretli varlık şart değildir. Görsel beğeni ile kontrol/karar sorunları ayrı not edilir. CI, ilk balığın ve bu testin önüne alınmaz.

Birleştirmeyi koordine eden: Mert.
Kapalı kapsam: satış, para, geliştirme, kamera ödülü ve ikinci canlı türü. CI için yeni bir framework yazılmaz.

## P3 — Tam döngü ve eğlence testi

| Görev | Sorumlu | Yapacağı iş |
|---|---|---|
| P3-A | Mehmet | Kamera açma/kayıt kontrolü, kadraj ve kayıt başladı/bitti geri bildirimini yapar; tüpün oksijen etkisini uygular. Kayıt yükledikten sonra ekipman etkisinin doğru dönmesini test eder. |
| P3-B | Utku | Canlı/olay tanıma, görüş/mesafe/süre kontrolü, çekim kalitesi ve basit özel olayı yapar; geçerli hedef bilgisini kamera UI'ına verir ve olayın temel görsel/ses işaretini ekler. Tekrar ödül testlerine destek olur. |
| P3-C | Mert | Av satışı, görüntü geliri, ortak para, dükkân, tüp yükseltmesi ve kaydetme/yüklemeyi yapar. Satış/alışveriş/ödül sonuçlarını anlaşılır basit geri bildirimle gösterir. |

Bağlantı: Mehmet çekim isteği üretir → Utku geçerli kayıt/kaliteyi verir → Mert güvenli dönmüş kayda para öder. Mert alınmış ekipmanı bildirir → Mehmet dalgıca etkisini uygular. Mert'in kayıt/ekonomi test yükünü Mehmet ve Utku somut test görevleriyle paylaşır.

Teknik bitiş:
- [ ] Hazırlan → avla → kaydet → dön → sat/değerlendir → tüp geliştir → tekrar dal döngüsü tek bölgede çalışıyor.
- [ ] Aynı av/çekim/istek iki kez para üretmiyor; eşzamanlı alışveriş parayı eksiye düşürmüyor.
- [ ] Görüş dışındaki veya engel arkasındaki hedeften geçerli çekim gelmiyor.
- [ ] Yeni tüp doğru oyuncuya doğru kapasiteyi veriyor; tekrar bildirim bonusu katlamıyor.
- [ ] Yeniden açılışta son tamamlanmış kayıt geri geliyor; av/para/ekipman çoğalmıyor.
- [ ] Solo ve dört oyuncuda tam döngü doğrulandı.
- [ ] P2 his katmanı korunuyor; kayıt, satış ve yükseltme sonuçları test oyuncusuna açıkça bildiriliyor. Nihai cila bu fazın şartı değil.

P4 öncesi oyun testi:
- Üçünüz 20–30 dakikalık ortak deneme yaparsınız; mümkünse ekip dışından birkaç kişi de oynar.
- Mehmet yüzme/nişan/kamera kullanımındaki sürtünmeyi; Utku keşif/av davranışlarını; Mert gelir/yükseltme temposunu gözlemler.
- P2 sorularını kısaca yeniden kontrol edip asıl şu sorulara bakarsınız: kamera avlanmadan farklı ve değerli bir seçenek mi; kazanç/yükseltme tekrar dalma isteği yaratıyor mu; birlikte oynamanın faydası hissediliyor mu?
- Teknik testin geçmesi tek başına P4'ü açmaz. Üçünüz çekirdek döngünün ilerlemeye değer olduğuna karar verirsiniz.
- Sonuç zayıfsa P3'te en çok iki kısa düzeltme turu planlanır; hâlâ çözülmüyorsa kapsam/tasarım birlikte yeniden değerlendirilir. Sonsuz rötuş veya yeni bölgeyle sorunu örtme yok.

Birleştirmeyi koordine eden: Mehmet.
Kapalı kapsam: gerçek video, ikinci dalış bölgesi, bulut kayıt ve geniş içerik üretimi.

## P4 — Tek bölgeyi tamamla

Ön koşul: P3 teknik ve oyun değerlendirmesi geçti. İkinci harita yapılmaz.

| Görev | Sorumlu | Yapacağı iş |
|---|---|---|
| P4-A | Mehmet | Bir palet ve bir çanta yükseltmesini uygular; avcı hasarını oyuncuya bağlar; mevcut ekipman ve etkileşim geri bildirimini tamamlar. |
| P4-B | Utku | Aynı dalış bölgesinin çevre/ışık/sesini ve keşif noktalarını tamamlar; canlıları toplam beş türe çıkarır: üç yaygın, bir nadir, bir avcı. |
| P4-C | Mert | Küçük kasabada üç hizmet noktasını aynı altyapıya bağlar; üç basit görev, jurnal ve tek bölgedeki ilerleme/kayıt verisini tamamlar. |

İçerik sınırı: tek dalış bölgesi, küçük kasaba, toplam beş tür, mevcut tek özel olay, üç görev ve tüp/palet/çanta için birer yükseltme. Tek zıpkın ve kamera korunur. Aynı bölgenin sığ/derin kısımları olabilir; ikinci sahil veya ayrı dalış bölgesi açılmaz.

Bitiş koşulları:
- [ ] Beş tür, avcı ve olay 1 ve 4 oyuncuda doğru çalışıyor.
- [ ] Üç görev ve üç hizmet noktası mevcut döngüye bağlı; ödül tekrar verilmiyor.
- [ ] Ekipmanların açıklamaları ve gerçek etkileri aynı.
- [ ] Mevcut bölgede P3 döngüsü hâlâ çalışıyor; içerik ve kaynak/lisans listesi tamam.
- [ ] P5 ölçümü için referans bilgisayar/çözünürlük/kalite ve hedef kare süresi bu aşamada kaydedildi.

Yük desteği: Utku'nun çevre işi taşarsa Mehmet ses/etkileşim yerleşimini, Mert içerik verisi girişini devralır. Sahiplik devri kısa görev kaydına yazılır.
Birleştirmeyi koordine eden: Utku.
Kapalı kapsam: ikinci bölge, ek görev/tür, büyük kasaba, prosedürel dünya, tekne sürüşü, üs inşası ve kurtarma.

## P5 — Hataları ve performansı düzelt

Yeni özellik ve içerik yok.

| Görev | Sorumlu | Yapacağı iş |
|---|---|---|
| P5-A | Mehmet | Bağlantı kaybı/gecikme, oyuncu sahipliği, hareket/kamera/ekipman hatalarını ve oyuncu performansını düzeltir. |
| P5-B | Utku | Balık AI, çarpışma, görüş, ışık ve sahne performansını düzeltir. |
| P5-C | Mert | Kayıt/yedek/bozuk dosya davranışı, eşzamanlı ekonomi, UI hataları ve fiyat/gelir dengesini düzeltir. |

Bitiş koşulları:
- [ ] 1, 2 ve 4 oyunculu temel döngü regresyonları geçti.
- [ ] En az iki bilgisayardaki dört süreçle 30 dakika ve art arda üç dalış tamamlandı.
- [ ] Ek 150 ms gidiş-dönüş gecikmesi ve %2 paket kaybı denemesinde para/av tutarlılığı korundu; yöntem kaydedildi.
- [ ] Hazırlık, dalış, dönüş, satış ve kayıt sırasında kopmalar denendi; bozuk kayıt sessizce sıfırlanmıyor.
- [ ] P4'te sabitlenen ortamda kare süresi ölçüldü. Başlangıç hedefi 1080p/60 FPS; sonuç düşük diye hedef sessizce değiştirilmedi.
- [ ] Kritik/yüksek önem hatası yok; kabul edilen küçük kusurlar listeli.

Birleştirmeyi koordine eden: Mert.
Kapalı kapsam: yeni mekanik, yeni içerik, gereksiz motor yükseltmesi ve sınırsız mimari değişiklik.

## P6 — Teslim paketini hazırla

| Görev | Sorumlu | Yapacağı iş |
|---|---|---|
| P6-A | Mehmet | Sabit commit'ten temiz Windows build alır; sürüm/commit ve yeniden build alma adımlarını kaydeder. |
| P6-B | Utku | Paketi bağımsız bilgisayarda dener; kontroller/oyuncu rehberini ve varlık atıflarını kontrol eder. |
| P6-C | Mert | Paketle kayıt ve çevrimiçi kabul testini yapar; bilinen sorunları, kayıt konumunu ve servis ayarlarını belgeler. |

Bitiş koşulları:
- [ ] Dağıtılacak paket geliştirici önbelleği olmadan başka bilgisayarda çalışıyor.
- [ ] Paketle solo, iki ve dört oyunculu temel döngü geçti; commit ve paket özeti kayıtlı.
- [ ] Sırlar pakette/depoda değil; ev sahibi ayrılma ve kayıt sınırları açık.
- [ ] Üç kişi aynı kapanış kaydında teslimi kabul etti.

Birleştirmeyi koordine eden: Mehmet.
Kapalı kapsam: mağaza yayını, ücretli servis satın alma, public oyun yayını ve ikinci bölge. Bunlar ayrı ürün kararıdır.

## Ortak faz geçişi

Üç kişinin teslimi → faz dalında birleştirme → ortak test → tek kısa kapanış kaydı → ana dala PR → birleşmiş build kontrolü → sıradaki faz.

Ayrı imza matrisleri yoktur; test sonucu, commit/build ve Mehmet/Utku/Mert'in gerçek tamam kaydı yeterlidir. P2'de erken kontrol/av/oksijen-çanta testi, P3'te kamera/tam döngü testi de yapılır. Başkasının tamamını yazamazsınız. Test başarısızsa veya bir kişinin işi eksikse sonraki faz kapalıdır.

## Teknik referanslar

Kesin sürüm uyumluluğu kurulumda denenir:
- [Unity sürüm desteği](https://unity.com/releases/unity-6/support)
- [Unity co-op başlangıcı](https://docs.unity.com/en-us/multiplayer/quickstarts/casual-co-op-quickstart)
- [Relay bağlantısı](https://docs.unity.com/en-us/mps-sdk/networking/relay-servers)
- [Unity Smart Merge](https://docs.unity3d.com/6000.0/Documentation/Manual/SmartMerge.html)
