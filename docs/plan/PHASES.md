# Üç kişilik geliştirme ve faz planı

Plan sürümü: 1.0 — 31 Ağustos 2026.

Bu belge kapsamı ve faz kapılarını tanımlar. Güncel görev durumu yalnızca [STATUS.md](STATUS.md) dosyasından izlenir. İş akışı [WORKFLOW.md](WORKFLOW.md), teknik bağlantılar [CONTRACTS.md](CONTRACTS.md) içindedir.

## Ürün sınırı

Kullanıcıdan gelen çekirdek: 1–4 oyuncu, küçük sahil kasabası, üç dükkân, ekipman hazırlığı, dalış bölgeleri, canlı/olay görüntüleme, avlanma, çanta ve oksijen kısıtı, satış, görüntü geliri ve daha derine ilerleme.

Gönderilen konsept üç dükkândan yalnızca dalış ekipmanları dükkânını adlandırıyor. Diğer iki dükkânın ayrıntıları verilmiş kabul edilmez. Aşağıdaki çalışma varsayımları bu eksikleri görünür kılar; ürünün doğrulanmış özellikleri gibi sunulmaz.

## P0'da kesinleştirilecek çalışma varsayımları

P0 kabulünde A/B/C bu kararları inceleyip STATUS karar tablosuna kaydeder. Kullanıcının daha sonraki açık tasarım kararı önceliklidir. Karar değişirse etkilenen fazın yükü ve sözleşmesi de güncellenir.

| Kimlik | Önerilen ilk sürüm kararı | Neden / sınır |
|---|---|---|
| D01 | Windows PC, birinci şahıs kamera, URP ile stilize atmosfer | Platform ve görsel üretim kapsamını sınırlamak |
| D02 | Unity 6.3 LTS; kesin yama ve paket sürümleri P0'da sabit | Aynı projeyi üç bilgisayarda tekrarlanabilir açmak |
| D03 | Bir ev sahibi + en çok üç katılımcı; kritik oyun durumu ev sahibinde | Solo aynı oyun mantığını yerelde kullanır; solo için Relay gerekmez |
| D04 | İlk sürümde gerçek video dosyası yerine canlı/olay, süre ve kadraj üzerinden puanlanan çekim kaydı | Sonradan oynatılabilir video, kodlama ve ağdan video paylaşımı kapsam dışı |
| D05 | Oturuma ait ortak para ve ekipman havuzu; dalgıca ait oksijen ve taşıma çantası | Ortak ekonomi için satın alma ve ekipman tahsisi ev sahibi tarafından doğrulanır |
| D06 | Kampanya kaydı ev sahibinin bilgisayarında; misafirler o kampanyaya katılır | Hesaplar arası ilerleme aktarımı ve bulut kayıt ilk sürümde yok |
| D07 | Oksijeni/sağlığı biten oyuncu dalışın kalanında pasif kalır; güvenli dönmemiş av ve çekim ödülü kaybolur | Yaşayanlar devam eder; herkes pasifse dalış biter. Sonraki kasaba aşamasında ekip geri gelir. Kurtarma mekaniği yok |
| D08 | Oturuma yeni katılım yalnızca kasabada; ev sahibi koparsa menüye dönülür, son kalıcı kontrol noktası korunur | Dalış ortasında katılım, yeniden bağlanıp kaldığı yerden sürdürme ve ev sahibi devri yok |
| D09 | Üç hizmet rolü önerisi: ekipman satışı, av alımı, görüntü değerlendirme | İkinci/üçüncü dükkânın kullanıcı tarafından verilmiş adı değildir. Ortak dükkân altyapısı kullanılacak |
| D10 | İlk sürüm içerik tavanı: küçük kasaba, toplam iki dalış bölgesi, beş canlı türü ve bir özel görüntülenebilir olay | P4 detaylandırır; faz kapanınca yeni içerik kendiliğinden eklenmez |
| D11 | İlk prototipte tek zıpkın, kamera ve oksijen kapasitesi geliştirmesi | Palet ve çanta geliştirmeleri P4'e kadar kapalı |

## Ekip dengesi

- A/B/C kişilerin takma adıdır; gerçek isim ve hesap eşleştirmesi P0'da yapılır.
- Her fazda kişi başına bir ana iş paketi vardır. Her pakete uygulama, test, içerik ve inceleme süresi dahildir.
- Görev sayısı veya commit sayısı eşitlik ölçüsü değildir. Her faz açılırken paketler 0,5–2 çalışma günlük alt işlere bölünür ve aynı tahmin yöntemiyle ölçülür.
- Benzer zaman ayıran kişiler için planlanan yük farkı yaklaşık %20'yi aşarsa işler başlamadan destek alt görevleri yeniden dağıtılır.
- Faz kapasitesinin yaklaşık %30'u ortak test, hata düzeltme ve birleştirmeye ayrılır; kalan kapasite yeni teslimlere ayrılır.
- Takvim tahmini, üçünüzün haftalık ayıracağı süre ve P0 sonuçları görülmeden verilmez.
- Erken bitiren kişi başka faza geçmez. Sırayla diğer kişinin PR'ını inceler, ortak test yapar ve devredilen alt görevi alır.

## Fazların sırası

| Faz | Ortak sonuç | Birleştirme sorumlusu | Sonraki fazı açan kanıt |
|---|---|---|---|
| P0 | Üç bilgisayarda açılan ortak proje ve kilitlenmiş sözleşmeler | A | Temiz kurulum/build + ortak karar ve araç kayıtları |
| P1 | 1–4 oyuncunun aynı hazırlık/dalış alanına bağlanması | B | Gerçek bağlantı, hareket ve sahne geçiş testi |
| P2 | Oksijen süresi içinde avlanıp çantayla geri dönme | C | Avın tek sahipliği, çanta ve başarısız dalış testleri |
| P3 | Av + çekim → gelir → geliştirme → yeniden dalış | A | Uçtan uca döngü ve kayıt doğrulaması |
| P4 | Sınırları belli içerik ve bölge ilerlemesi | B | İki bölge ve tanımlı içerik envanterinin kabulü |
| P5 | Bağlantı, kayıt, performans ve oynanış sağlamlığı | C | Hata/regresyon ve ölçülmüş performans raporu |
| P6 | Tekrarlanabilir teslim adayı | A | Sabit commit'ten kurulup test edilen paket |

Birleştirme sorumlusu bütün işleri yazmaz; inceleme, çakışma çözümü, ortak test ve raporu koordine eder. Sonraki teslim döngüsünde dönüşüm B'den devam eder.

## P0 — Ortak temel ve kararlar

Amaç: herkesin aynı projede, aynı sözleşmelerle ve aynı araçlarla çalışabilmesi.

| Görev | Sorumlu | Teslim |
|---|---|---|
| P0-A | A | Unity/URP proje iskeleti; kesin editör/paket listesi; başlangıç build'i; temel sahne ve modül sınırları |
| P0-B | B | Sürüm kontrolü kuralları; `.gitignore`, uygun Git LFS/merge ayarları, `.meta` düzeni; ilk boş test alanı; varlık kaynak/lisans kayıt şablonu |
| P0-C | C | Uzak repo ve ekip erişimi düzeni; görev/PR şablonları; build kontrolünün kurulması ve denenmesi; ortak veri/saklama sözleşmelerinin taslağı |

Ortak çalışma: A/B/C tasarım varsayımlarını, ortak veri tiplerini, adlandırmayı, hata sonuçlarını ve saat kapasitesini birlikte kesinleştirir. Hesap/lisans gerektiren araç ayarları mevcut yetkiyle yapılır; eksik erişim tamamlanmış sayılmaz.

Kabul koşulları:

- [ ] A, B ve C temiz klondan aynı editör yamasıyla projeyi açtı ve Windows build aldı.
- [ ] Paket kilit dosyası ve gerekli `.meta` dosyaları takipte; üretilen önbellekler ve sırlar depoda değil.
- [ ] CONTRACTS kabul edildi; sahte/test sağlayıcıları sonraki kişilerin bağımsız çalışmasını sağlayacak şekilde tanımlandı.
- [ ] D01–D11 kararları, isim/hesap eşleştirmesi ve faz kapasitesi kaydedildi.
- [ ] Uzak repo, özellik PR incelemesi ve faz kapanış kuralları yapılandırıldı ve test edildi. Plan/hesap sınırı varsa uygulanabilir alternatif açıkça kaydedildi.
- [ ] Otomatik build kontrolü çalıştırıldı; mümkün değilse neden ve üç kişinin kabul ettiği tekrarlanabilir manuel kontrol kaydedildi. Otomasyon yapılmış gibi sunulmadı.
- [ ] İlk sualtı görünümü için seçilecek varlıkların URP uyumu ve kullanım lisansı kontrol edildi; henüz varlık satın alınmadıysa satın alınmış sayılmadı.

Kapalı kapsam: oyuncu mekaniği, internet üzerinden co-op, balık AI, gerçek ekonomi ve diğer fazların oyun özellikleri.

## P1 — Co-op ve sahne iskeleti

Ön koşul: P0 kapalı ve ana dalda doğrulanmış.

| Görev | Sorumlu | Teslim |
|---|---|---|
| P1-A | A | Oturum oluştur/katıl/ayrıl; en çok dört oyuncu; temel yürüme/yüzme; oyuncu sahipliği ve hareket senkronizasyonu |
| P1-B | B | Basit kasaba ve tek dalış test alanı; zemin/su çarpışmaları; giriş/çıkış noktaları; oyuncuların aynı alanı görmesi için sahne içerik testi |
| P1-C | C | Oturum ve hazır olma ekranı; oyuncu listesi; oturum durumunun UI'ı; ağla uyumlu hazırlık → dalış → kasaba geçişi ve bağlantı hata ekranı |

Bağımsız çalışma: B'nin test alanı yerelde açılır. C, A'nın oturumu bitmeden sözleşmeye uyan test oturum sağlayıcısını kullanır; faz kabulünde gerçek sağlayıcıya geçilir.

Kabul koşulları:

- [ ] Solo modda hazırlık alanı ve dalış geçişi çalışır; zorunlu bulut oturumu yoktur.
- [ ] Farklı bilgisayarlarda iki oyuncu internet üzerinden bağlanır ve birbirini görür.
- [ ] Dört ayrı oyun süreci, en az iki fiziksel bilgisayara dağıtılarak bağlanır; üçünüzün biri dördüncü süreci çalıştırabilir.
- [ ] Bütün oyuncular aynı sahne/dalış durumuna geçer; çift oyuncu oluşmaz ve geçişte oyuncu kaybolmaz.
- [ ] Dalış sırasında yeni katılma isteği kontrollü reddedilir; beşinci oyuncu kabul edilmez.
- [ ] Katılımcı ayrılması listeyi günceller; ev sahibi ayrılması kalanları anlaşılır mesajla menüye döndürür.

Kapalı kapsam: oksijen kaybı, avlanma, satış, kamera puanı ve ayrıntılı harita görselleri.

## P2 — Dalış, av ve taşıma

Ön koşul: P1'in bağlantı ve sahne geçişi testleri hâlâ geçiyor.

| Görev | Sorumlu | Teslim |
|---|---|---|
| P2-A | A | Oksijen/sağlık altyapısı; zıpkın kullanımı ve etkileşim isteği; dalış durum göstergeleri; oksijen bitince pasif oyuncu davranışı |
| P2-B | B | Bir yaygın balık türü; temel yüzme/kaçma; ev sahibinde vurulma ve av sonucu; tekil av nesnesi ve güvenli toplama isteği |
| P2-C | C | Oyuncu çantası ve ağırlık sınırı; av ekleme doğrulaması; güvenli dönüş envanteri; başarısızlık/ayrılma durumunda geçici avların temizliği; dalış özeti |

Bağımsız çalışma: A test hedefiyle zıpkını doğrular; B sabit test hasarıyla balığı dener; C örnek CaptureResult ile çantayı dener. Faz kabulünde test sağlayıcıları gerçek oyun yolunda kullanılmaz.

Kabul koşulları:

- [ ] 1, 2 ve 4 oyunculu oturumlarda balık avlanıp toplanabilir ve güvenli dönüşte av korunur.
- [ ] İki kişi aynı avı aynı anda almaya çalışınca yalnızca bir çantaya eklenir.
- [ ] Çanta doluyken toplama reddedilir; yerdeki av kaybolmaz ve ağırlık sınırı aşılmaz.
- [ ] Oksijen ev sahibinde doğrulanır; oksijen bittiğinde ilgili oyuncu fazın belirlenen başarısızlık davranışına geçer.
- [ ] Bir oyuncu pasifken yaşayanlar devam eder; herkes pasifse dalış kapanır.
- [ ] Kopan/pasif oyuncunun güvenli dönmemiş avı tekrar oluşturulmaz; diğer oyuncuların çantası etkilenmez.
- [ ] Arka arkaya iki dalışta önceki dalıştan oyuncu, av veya sayaç kalmaz.

Kapalı kapsam: av satışı, gerçek para, geliştirme, çekim ödülü ve ikinci canlı türü.

## P3 — Kamera, ekonomi ve tam oyun döngüsü

Ön koşul: P2'nin av/çanta ve başarısız dalış sonuçları tutarlı.

| Görev | Sorumlu | Teslim |
|---|---|---|
| P3-A | A | Kamera kullanım/kayıt kontrolü ve kadraj göstergesi; doğrulanacak çekim isteği; ekipman kuşanma ve satın alınmış tüp kapasitesinin dalgıca uygulanması |
| P3-B | B | Canlı/olay hedefi tanıma; görüş engeli, mesafe ve süreye dayalı kalite değerlendirmesi; bir özel olay; dalış içi tekrar kaydının ödül açısından filtrelenmesi |
| P3-C | C | Av satışı, çekim değerlendirme, ortak para, tek dükkân altyapısı; başlangıç tüpü + bir yükseltme; kampanya kontrol noktası kaydı ve tekrar ödeme engeli |

Yük dengesi: C'nin işlemsel kayıt/test yükü tahminde yüksek çıkarsa A kayıt/yükleme testlerini, B satış ve tekrar ödül testlerini devralır. Ürün kodu sahipliği korunur, test katkısı ayrıca görev olarak kaydedilir.

Kabul koşulları:

- [ ] Hazırlan → dal → avla → kaydet → dön → avı sat → çekimi değerlendir → tüp geliştir → tekrar dal döngüsü tamamlanır.
- [ ] Güvenli dönmüş av ve çekim ayrı ayrı, yalnızca bir kez paraya dönüşür.
- [ ] Aynı dalışta aynı tür/olay için tanımlı tekrar kuralı uygulanır; spam kazancı oluşmaz.
- [ ] Duvar arkasındaki veya sınır dışındaki hedef üzerinden geçerli çekim elde edilemez.
- [ ] Eşzamanlı satın almada ortak para eksiye düşmez; aynı para iki kez harcanmaz.
- [ ] Yeni tüp bir sonraki dalışta doğru kapasiteyi verir; diğer oyuncunun değeri yanlış değişmez.
- [ ] Oyun kapatılıp açıldığında son tamamlanmış kontrol noktasındaki para, depo, ekipman ve ilerleme geri gelir.
- [ ] Satış/kayıt isteği tekrarlandığında av, para veya geliştirme çoğalmaz.
- [ ] 1 ve 4 oyuncuda tam döngü geçer; en az iki ayrı bilgisayar kullanılır.

Kapalı kapsam: gerçek video dosyaları, bulut kayıt, kişisel hesap ekonomisi, yeni bölge ve sınırsız ekipman içeriği.

## P4 — Sınırlı içerik ve derinlik ilerlemesi

Ön koşul: P3 tam döngüsü bütünleşmiş ana dalda çalışıyor.

| Görev | Sorumlu | Teslim |
|---|---|---|
| P4-A | A | Bir palet ve bir çanta kapasitesi yükseltmesinin uygulanması; avcı hasarının oyuncuya etkisi; ekipman geri bildirimi, kamera/zıpkın kullanım iyileştirmeleri |
| P4-B | B | Toplam iki küçük dalış bölgesi; toplam beş tür (üç yaygın, bir nadir, bir avcı); mevcut özel olayın yerleşimi; sualtı ışık/ses/görünürlük ve çarpışma geçişi |
| P4-C | C | Küçük kasabanın üç onaylanmış hizmet noktası; veriyle tanımlanan üç görev; ikinci bölge açılma koşulu; dükkân/jurnal/ilerleme UI'ı; yeni içeriğin kayıt sürümü |

İçerik tavanı:

- Toplam iki dalış bölgesi: başlangıç ve daha derin/tehlikeli alan.
- Toplam beş canlı türü; her tür için ayrı karmaşık AI yazılmaz, ortak davranışlar veriyle ayarlanır.
- Toplam bir özel görüntülenebilir olay; P3'teki olay yeniden kullanılır.
- Toplam üç görev: belirli avı getir, belirli türü kaydet, nadir türü/olayı kaydet.
- Başlangıç ekipmanlarına ek olarak her biri tek kademeli tüp, palet ve çanta yükseltmesi; tek zıpkın ve tek kamera korunur.
- Üç dükkân/hizmet noktası aynı işlem altyapısını kullanır. Büyük kasaba, NPC diyalog ağı veya açık dünya yapılmaz.

Yük dengesi: B'nin çevre/varlık üretimi tahminde taşarsa hazır ve lisansı uygun modüler varlıklar seçilir; A ses/etkileşim yerleşimi, C sahne kontrolü ve içerik veri girişini devralır. Yardım görevleri başlamadan sahipleri kaydedilir.

Kabul koşulları:

- [ ] İçerik envanteri yukarıdaki sınırlarla eşleşir ve kaynak/lisans kayıtları vardır.
- [ ] İkinci bölge yalnızca kararlaştırılan ilerleme şartıyla açılır; kilit UI ve ev sahibi doğrulamasında tutarlıdır.
- [ ] Beş tür, avcı saldırısı ve özel olay 1 ve 4 oyuncuda doğru görünür/işler.
- [ ] Üç görev ödülü tekrarlı teslim veya kayıt yükleme ile çoğaltılamaz.
- [ ] Yeni ekipmanlar gerçek davranışı değiştirir; UI açıklaması ve uygulanan değer aynıdır.
- [ ] İki bölgede de P3 tam döngüsü tamamlanır.

Kapalı kapsam: ek tür/bölge/görev, prosedürel dünya, araç/tekne sürüşü, üs inşası, üretim sistemi ve kurtarma sistemi.

## P5 — Sağlamlaştırma ve dengeleme

Ön koşul: içerik tavanı tamamlandı. Bu fazda yeni özellik veya içerik eklenmez.

| Görev | Sorumlu | Teslim |
|---|---|---|
| P5-A | A | Bağlantı kaybı, gecikme, oyuncu sahipliği ve giriş kontrolleri; oyuncu tarafı performans; mevcut hareket, kamera ve ekipman kullanımındaki hataların düzeltmeleri |
| P5-B | B | Canlı AI ve grafik profili; çarpışma/görüş/av davranışı hataları; sahne performansı ve okunabilirlik düzeltmeleri |
| P5-C | C | Kayıt bütünlüğü, bozuk kayıt/yedek davranışı, eşzamanlı ekonomi işlemleri, UI hata durumları ve gelir/fiyat dengesi |

Kabul koşulları:

- [ ] 1, 2 ve 4 oyunculu bütün temel döngüler regresyon listesinden geçer.
- [ ] Dört süreç en az iki bilgisayarda 30 dakikalık oturum ve arka arkaya en az üç dalış tamamlar; çökme/oyuncu kaybı/çoğaltma görülmez.
- [ ] 150 ms ek gidiş-dönüş gecikmesi ve %2 paket kaybı benzetiminde kritik envanter/para tutarlılığı korunur; ölçüm yöntemi raporda yazılıdır.
- [ ] Menü, hazırlık, dalış, dönüş, satış ve kayıt sırasında ayrılma senaryoları ayrı ayrı denenir.
- [ ] Bozuk veya yarım yazılmış kayıt sessizce sıfırlanmaz; yedek/uyarı davranışı doğrulanır.
- [ ] P0'da seçilen referans bilgisayar, çözünürlük ve kalite ayarında hedef kare süresi ölçülür; test sahnesi, oyuncu/canlı sayısı ve p95 kare süresi raporlanır.
- [ ] Önerilen performans hedefi 1080p'de 60 FPS'tir. Donanım/kalite profili P0'da onaylanır; düşük sonuç hedefi sonradan gizlice düşürerek PASS yapılmaz.
- [ ] Açık kritik/yüksek önem hatası yoktur. Küçük kusurlar üç kişi tarafından açıkça kabul edilip listelenmiştir.

Kapalı kapsam: performans bahanesiyle yeni mimariye sınırsız geçiş, içerik genişletme, motor sürümü yükseltme ve yeni servis ekleme. Zorunlu değişiklikler gerekçe, etki ve onayla kayıt altına alınır.

## P6 — Teslim adayı

Ön koşul: P5 sağlamlaştırma kapısı geçti.

| Görev | Sorumlu | Teslim |
|---|---|---|
| P6-A | A | Sabit commit'ten temiz Windows build; sürüm/commit/ayar bilgisi; build adımları ve paket bütünlüğü kaydı |
| P6-B | B | Bağımsız kurulum ve oynanış kontrolü; kontroller/oyuncu yardım belgesi; varlık lisans/atıf son denetimi |
| P6-C | C | Başka bilgisayarda kayıt ve çevrimiçi oturum kabulü; bilinen sınırlamalar, servis/maliyet ayarları ve teslim notları |

Kabul koşulları:

- [ ] Teslim paketi faz PR'ının birleştiği ana dal commit'inden yeniden üretildi; commit ve dosya özeti raporda var.
- [ ] Oyunun geliştirici önbelleklerine ihtiyaç duymadan başka bilgisayarda açıldığı doğrulandı.
- [ ] Dağıtılacak paketle solo, iki ve dört oyunculu temel döngü geçti.
- [ ] Kimlik bilgileri/sırlar pakette ve depoda değil; gerekli servis ayarları ve kullanım sınırları belgelendi.
- [ ] Kayıt konumu, yedekleme, ev sahibi ayrılma sınırı ve desteklenen katılma zamanı kullanıcıya açıklandı.
- [ ] A/B/C teslim adayını onayladı; açık hatalar ve test sınırları teslim raporunda.

Kapalı kapsam: mağazaya yükleme, ücretli servis satın alma, herkese açık yayın ve yeni içerik. Bunlar kullanıcının ayrıca vereceği yayın/genişletme kararına bağlıdır.

## Bütün fazlarda ortak kapanış

Kişisel teslim → faz dalında birleşme → ortak kabul testleri → üç kişinin onayı → ana dala faz PR'ı → birleşmiş ana dal commit'inde doğrulama → STATUS güncelleme.

Bir adım eksikse sonraki faz kapalı kalır. Eski fazların ilgili testleri sonraki fazlarda da geçmelidir. Fazı bitirmek için başarısız test silinmez veya kapsam sessizce küçültülmez.

## Teknik referanslar

31 Ağustos 2026 tarihinde plan hazırlanırken kontrol edilen resmî kaynaklar; kesin paket uyumluluğu yine P0'da denenir:

- [Unity 6 sürüm desteği](https://unity.com/releases/unity-6/support)
- [URP/HDRP seçimi](https://docs.unity3d.com/6000.3/Documentation/Manual/choose-a-render-pipeline.html)
- [Unity co-op başlangıcı](https://docs.unity.com/en-us/multiplayer/quickstarts/casual-co-op-quickstart)
- [Multiplayer Services ile Relay](https://docs.unity.com/en-us/mps-sdk/networking/relay-servers)
- [Oturum ve oyun durumu devri sınırları](https://docs.unity.com/en-us/relay/host-migration)
- [Unity hizmet ücretleri](https://unity.com/products/gaming-services/pricing)
- [Unity Smart Merge](https://docs.unity3d.com/6000.0/Documentation/Manual/SmartMerge.html)
