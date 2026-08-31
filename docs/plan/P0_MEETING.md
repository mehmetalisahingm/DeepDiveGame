# P0 toplantısı — ekip, üretim ve devam kararı

Durum: SORUMLULAR_ATANDI, toplantı ayrıntıları açık. Kullanıcı 31 Ağustos 2026'da ortak sorumlulukları Mehmet'e, ardından görsel ve sesi Mert'e verdi. Bu atama toplantının yapıldığı, uzmanlığın doğrulandığı, işlerin bittiği veya harcamanın onaylandığı anlamına gelmez. Mehmet kararları takip eder; Mert mevcut P0-C görevi kapsamında kayıt desteği verir.

## Güncel ortak sorumluluklar

| Alan | Sorumlu | Sınır |
|---|---|---|
| Görsel ve ses | Mert | Model, çevre, ekipman, VFX, UI görselleri ve ses için kaynak seçimi/üretim planı, lisans/atıf listesi ve tutarlılık kontrolü. Uygun hazır kaynak kullanabilir; hepsini sıfırdan üretmek şart değil |
| Takvim, kapasite ve devam/ayrılma planı | Mehmet | Gerçek saatleri toplatır, yükü dengeler, devir/yeniden planlamayı takip eder; başkasının zaman taahhüdünü veya onayını vermez |
| Ürün hedefi ve oyun tasarımı | Mehmet | Ekip görüşünü alır; mevcut kapsam içindeki tasarım anlaşmazlıklarında son kararı verir. Ticari hedefin ne olduğu ayrıca kaydedilir |
| Bütçe, ödeme planı ve servis/hesap takibi | Mehmet | Üst sınır, masraf paylaşımı ve kullanım kontrolünü netleştirir; bu atama harcama veya bütün giderleri kişisel olarak ödeme taahhüdü değildir |
| Birincil netcode incelemesi | Mehmet | Ağ kodunu anlama/hata ayıklama ve öğrenme planını üstlenir; yedek kişi henüz seçilmedi. Herkes kendi modülünün ağ uygulamasını ve testini teslim eder |
| P0 karar takibi ve D06 farkındalığı | Mehmet | Toplantıyı ve açık kararları takip eder; test kampanyası host'unu netleştirir. Mert kayıt desteği verir; kalıcı kayıt kodu Mert'te kalır |

Mevcut Mehmet/Utku/Mert geliştirme işleri ve dönüşümlü faz birleştirme koordinatörleri korunur. Mert görsel/ses üretimini yönetir; Mehmet oyuncuya, Utku dünyaya, Mert kasaba/UI'a entegrasyonu kendi dosyalarında yapar. Dosya sahipliği değişecekse devir açıkça kaydedilir. Mert'in sanat/ses yükü de kapasite hesabına katılır; destek işleri mevcut faz içinde bölünür.

Amaç: oyun koduna başlamadan önce kim, ne kadar zamanla, hangi hedef ve bütçeyle çalışacak sorularını cevaplamak. P0 proje/sürüm/erişim hazırlığı sürebilir; oyun geliştirme ve P1 hâlâ kapalıdır. D01–D11 ile bu gündemi toplam 45–60 dakikalık görüşmede ele alın; ayrı sunum veya imza matrisi hazırlamayın. Karar tablosundaki kısa sonuçlar yeterlidir.

## Kim ne hazırlayacak?

| Kişi | Toplantıya getireceği bilgi |
|---|---|
| Mehmet | Ortak kararların takibi; kapasite/takvim, hedef, bütçe ve netcode öğrenme/yedek planı; ekipman/HUD/ses ihtiyaçlarını Mert'e iletme |
| Utku | Kendi kapasitesi ve ağ deneyimi; balık, sualtı, batık, VFX ve ortam sesi ihtiyaçları; hazır kaynak veya basit üretim seçenekleri |
| Mert | Üç alanın görsel/ses ihtiyaçlarını birleştiren kısa kaynak/üretim planı; kendi kapasitesi ve kasaba/UI ihtiyaçları; ekip bilgileri ve karar kaydı desteği |

Herkes kendi hedefini, sürdürebileceği haftalık saati, okul/iş yoğunluklarını ve harcama sınırını belirtir. Kişisel mali bilgiler, ödeme bilgileri ve özel takvim ayrıntıları public repoya yazılmaz. Mehmet toplantıyı ve kararları takip eder; Mert kaydeder. Kimse başkası adına zaman, para veya faz kapanışı taahhüdü vermez.

## Toplantıda cevaplanacaklar

| Konu | Verilecek kısa karar |
|---|---|
| T01 — Görsel ve ses | Mert hangi parçayı hangi kaynaktan sağlayacak veya üretecek? Hazır kaynak, basit kendi üretimimiz veya dış destek mi? Alan sahiplerine hangi fazda teslim edecek? |
| T02 — Kapasite ve ayrılma | Gerçek haftalık saatler, öğrenme ihtiyacı, ilk çalışma dönemi ve tekrar değerlendirme zamanı nedir? Bir kişi ara verirse veya ayrılırsa devir ve devam kuralı ne? |
| T03 — Ürün hedefi | Portfolyo/eğlence, ticari hedef veya önce prototip sonra karar mı? Başarı ölçütü ve hedefi yeniden değerlendirme noktası ne? |
| T04 — Bütçe | Mehmet'in takip edeceği tek seferlik ve aylık üst sınır ne? Fiilî ödeyen, masraf paylaşımı ve harcama onayı nasıl kaydedilecek? |
| T05 — Tasarım kararı | Mehmet mevcut kapsam içindeki son kararını hangi deneme ve ekip görüşüne göre verecek? Kararın kısa kaydı nasıl tutulacak? |
| T06 — İnsan netcode sorumluluğu | Mehmet'in mevcut bilgi açığı ve öğrenme planı ne? Yedek kişi kim olacak? |
| T07 — D06 farkındalığı | Mehmet aynı kampanyanın kayıt sahibi host olmadan sürdürülemeyeceği v1 sınırını ekipçe görüşür; test kampanyasının host'u kim olacak? |

## Uygulama sınırları ve açık seçimler

Yukarıdaki sorumlu atamaları ve Mehmet'in tasarımda son söz yetkisi kullanıcı kararıdır. Aşağıdaki kaynak, süre, hedef ve bütçe seçenekleri bununla otomatik kabul edilmiş sayılmaz.

**Görsel/ses:** Mert üretim/kaynak seçimi ve tutarlılıktan sorumludur. Mehmet ekipman/dalgıç, Utku canlı/sualtı ihtiyaçlarını Mert'e verir; Mert kasaba/UI ihtiyaçlarıyla birleştirir. P2/P3 için basit kendi üretiminiz veya uygun lisanslı geçici kaynaklar önerilir. Her kaleme kaynak/üretim yöntemi ve oyuna ekleyecek alan sahibi yazılır; ücretli paket seçimi veya kapsamlı Blender/ses öğrenimi P0'ı bekletmez. P2/P3 his katmanı Mert'in kaynak desteğiyle, mevcut faz görevlerindeki alan sahipleri tarafından oyuna bağlanır ve test edilir. P4'te mevcut içerik tamamlanır; yeni sanat kapsamı açılmaz.

İlk dış kaynak eklenmeden kaynak bağlantısı, lisans, atıf, ekip kullanım hakkı ve public repoda ham dosya paylaşım izni kontrol edilir. Ücretsiz indirme veya satın alma tek başına bu izinlerin kanıtı değildir; pakete özgü şartlar incelenir. İzin doğrulanamıyorsa ham varlık repoya girmez, geçici kendi varlığınız kullanılır. P4'te liste tamamlanır; ilk kontrol P4'e bırakılmaz. [Unity Asset Store koşulları](https://unity.com/legal/as-terms).

**Süre ve devam:** "Bir yıldan uzun sürebilir" bir risk senaryosudur; şu anda doğrulanmış teslim tahmini yok. Kod, öğrenme, görsel/ses, inceleme ve ayrı bilgisayar testini kapasite hesabına katın. P0'da sürdürülebilir saatleri ve ilk çalışma dönemini belirleyin; P1 sonunda gerçekleşen süreyle tahmini yenileyin, P2/P3 oynama testlerinde devam kararını tekrar değerlendirin. Faz numarası hafta veya ay demek değildir.

Öneri: geçici yoklukta mevcut fazdaki belirli işi kayıtlı olarak devredin. Kalıcı ayrılıkta mevcut fazda durup kapsamı, sahipliği ve kapasiteyi yeniden değerlendirin; kalan iki kişi eski üç kişilik tamam kaydını taklit etmesin. Devredilecekler: son commit/branch, açık hatalar, build adımları, varlık kaynakları ve hesap erişimlerinin yönetimi. Kod/varlık kullanım izni, emek/atıf ve ticari hedef varsa gelir/gider paylaşımının nasıl kararlaştırılacağını da konuşun; oran veya hak devri varsaymayın. Ekip küçülürse devam için plan ve faz kapısı açıkça güncellenir.

**Hedef:** İlk somut hedef olarak P3'te tek bölgeli, tekrar oynamak isteyeceğiniz bir prototipi değerlendirmek önerilir. Bunun ticari hedefe hizmet edip etmediğini T03'te ayrıca yazın. Ticari hedef seçmek mağaza açma veya ödeme yetkisi değildir; P6 teslimi de otomatik yayın değildir. Yayın düşünülürse mağaza içeriği, içerik/yaş derecelendirmesi ve hesap/vergi işlemlerini araştıracak kişi ve zaman ayrıca belirlenir. Ücretsiz/portfolyo amacı, seçilen platformun şartlarının kontrolünü kaldırmaz. [Steamworks başlangıç koşulları](https://partner.steamgames.com/doc/gettingstarted/onboarding).

**Bütçe:** Yazılı harcama kararı olmadan ücretli paket/servis açılmaz veya satın alınmaz. Ortak kasa zorunlu değildir; kimin ödeyeceği ve paylaşım biçimi seçilir. P1'de çevrimiçi servis kullanmadan önce hesap sahibi, kullanım takibi, uyarı eşiği ve limite yaklaşınca testi durdurma işlemi net olmalı. Uyarı, otomatik harcama tavanı olarak kabul edilmez; gerçekten yapılandırılan kontrol kaydedilir. Bütçe engeli internet testini yapılmış saydırmaz.

31 Ağustos 2026'da resmî sayfalarda kontrol edilen maliyet notları; kullanım/satın alma öncesi yeniden kontrol edin:

- Relay: ücretsiz katmanda ilk 50 **aylık ortalama** eşzamanlı kullanıcı (CCU); CCU başına 3 GiB, ayda en fazla 150 GiB ücretsiz trafik belirtiliyor. Bu, her projeye koşulsuz 150 GiB veya sınırsız ücretsiz test demek değildir. Ek kullanım ücretlidir; gerçek tüketim ölçülmeden ekip maliyeti hesaplanamaz. [UGS fiyatlandırması](https://unity.com/products/gaming-services/pricing).
- Steam Direct: uygulama başına 100 USD veya karşılığı; uygulanabilir vergiler eklenebilir. Bu yalnızca başvuru ücretidir, toplam yayın bütçesi değildir ve P0 harcaması değildir. [Steam Direct ücreti](https://partner.steamgames.com/doc/gettingstarted/appfee).
- Asset, ses, diğer servis ve olası AI abonelikleri ayrı kalemlerdir; satın alma yok, tutarlar ve ödeme sorumluları henüz belirlenmedi.

**Tasarım:** Kullanıcı atamasıyla son karar Mehmet'tedir. Alan içindeki geri alınabilir ayarları sistem sahibi yapar; balık hızı–zıpkın–ödül gibi ortak kararda etkilenenler görüşünü ve kısa deneme sonucunu sunar, Mehmet mevcut kapsam içinde kararı sonuçlandırıp gerekçesini kaydeder. Önceki 2/3 oylama önerisi uygulanmaz. Görsel/ses üretim tercihlerinin sahibi Mert'tir; ortak oynanışla çatışan karar Mehmet'e gider. Maliyet, gelir/hak paylaşımı, kapsam ve faz kapısı değişikliklerinde ilgili açık karar ve mevcut onay kuralları korunur; başkasının test/onayı yerine son söz yetkisi kullanılamaz.

**Netcode:** Birincil insan sorumlusu Mehmet'tir; bu atama onun deneyimli ağ geliştiricisi olduğunun kanıtı değildir. Mehmet P0'da öğrenme açığını ve ekipten yedek kişiyi netleştirir; gerekirse P1 işine sınırlı öğrenme/inceleme alt işi konur. P1 kapanışında Mehmet gerçek projede host yetkisini, oyuncu sahipliğini ve kopma akışını açıklayıp tekrar üretilebilir bir bağlantı/sahiplik sorununu loglarla nasıl izlediğini gösterir; yedek kişi kaydı kullanarak kontrolü tekrarlar. Yeni bir eğitim fazı veya ayrı demo projesi kurulmaz. Herkes kendi modülünün ağ kodunu anlamaktan ve co-op testinden sorumludur; Codex çıktısı insan incelemesi ve gerçek testin yerine geçmez.

**Kayıt sınırı:** Kayıt sahibi host yoksa diğerleri **aynı kampanyaya** devam edemez. Başka biri host olup ayrı kampanya başlatabilir; ilerleme kendiliğinden birleşmez. Manuel kayıt taşıma, bulut senkronu ve host devri v1 özelliği değildir. Test host'u belirlemek tek kişiyi her oyunda host olmaya mecbur bırakmaz. Bu sınır D06 ve [kayıt sözleşmesinde](CONTRACTS.md) görünür kalır.

## Tek kısa karar kaydı

Mehmet açık kararları takip eder; Mert gerçek görüşme sonucunu kayda geçirir. Kullanıcı atamaları aşağıda kayıtlıdır; diğer seçimler ve üç kişinin gerçek tamamı uydurulmaz. Ayrı onay matrisi veya yedi yeni issue gerekmez. D01–D11 sonuçları mevcut [STATUS](STATUS.md) tablosunda kalır.

| Konu | Durum | Karar / sorumlu / takip zamanı / görüşme kaydı |
|---|---|---|
| T01 | SORUMLU_ATANDI | Kullanıcı: Mert. P0'da kaynak/üretim yöntemi ve alan sahiplerine teslim planı netleştirilecek |
| T02 | SORUMLU_ATANDI | Kullanıcı: Mehmet. P0'da saatler, ilk çalışma dönemi ve yokluk/ayrılma kuralı netleştirilecek |
| T03 | SORUMLU_ATANDI | Kullanıcı: Mehmet. P0'da hedef ve yeniden değerlendirme noktası kaydedilecek |
| T04 | SORUMLU_ATANDI | Kullanıcı: Mehmet. P0'da bütçe/ödeme/paylaşım netleştirilecek; servis açılmadan kullanım kontrolü doğrulanacak |
| T05 | ATANDI | Kullanıcı: mevcut kapsamda tasarımın son kararı Mehmet'te. Gerçek faz testleri ve üç kişinin tamamı korunur |
| T06 | SORUMLU_ATANDI | Kullanıcı: birincil Mehmet. P0'da yedek/öğrenme planı açık; P1'de gerçek inceleme gösterimi bekliyor |
| T07 | SORUMLU_ATANDI | Takip Mehmet'te. P0'da D06 ekip farkındalığı ve test kampanyası host'u kaydedilecek |

P0 kapanmadan her başlıkta uygulanabilir kısa çalışma kararı bulunur. Ücretli paket veya mağaza gibi ilerideki seçimler, bu arada geçerli sınır ve sorumlu/takip noktası yazılarak ertelenebilir. Örneğin "P3 testine kadar harcama yok; geçici kendi varlıklarımız; P3 sonrası ekipçe değerlendir" açık bir karar olabilir; boş bırakmak karar değildir. Ayrıntılı üretim, fiyat dengeleme ve gelecekteki özellikler P0'a çekilmez.
