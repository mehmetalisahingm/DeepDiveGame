# P0 toplantısı — ekip, üretim ve devam kararı

Durum: GORUSULMEDI. Bu belge gündem ve öneridir; toplantının yapıldığı, sorumluların kabul ettiği veya harcamanın onaylandığı anlamına gelmez. Sonuçların kaydı [Mert'in P0-C görevi](https://github.com/mehmetalisahingm/DeepDiveGame/issues/3) üzerinden izlenir.

Amaç: oyun koduna başlamadan önce kim, ne kadar zamanla, hangi hedef ve bütçeyle çalışacak sorularını cevaplamak. P0 proje/sürüm/erişim hazırlığı sürebilir; oyun geliştirme ve P1 hâlâ kapalıdır. D01–D11 ile bu gündemi toplam 45–60 dakikalık görüşmede ele alın; ayrı sunum veya imza matrisi hazırlamayın. Karar tablosundaki kısa sonuçlar yeterlidir.

## Kim ne hazırlayacak?

| Kişi | Toplantıya getireceği bilgi |
|---|---|
| Mehmet | Kendi kapasitesi ve ağ deneyimi; ekipman/HUD/ses ihtiyaçları; repo ve ileride kullanılacak servis hesaplarının yönetim ihtiyacı |
| Utku | Kendi kapasitesi ve ağ deneyimi; balık, sualtı, batık, VFX ve ortam sesi ihtiyaçları; hazır kaynak veya basit üretim seçenekleri |
| Mert | Kendi kapasitesi ve ağ deneyimi; kasaba/UI/ses ihtiyaçları; üç kişinin bilgilerini toplayan gündem ve kısa karar kaydı |

Herkes kendi hedefini, sürdürebileceği haftalık saati, okul/iş yoğunluklarını ve harcama sınırını belirtir. Kişisel mali bilgiler, ödeme bilgileri ve özel takvim ayrıntıları public repoya yazılmaz. Mert toplantıyı koordine eder; ekip adına tek başına karar vermez.

## Toplantıda cevaplanacaklar

| Konu | Verilecek kısa karar |
|---|---|
| T01 — Görsel ve ses | Hangi parçayı kim üretecek veya bulacak? Hazır kaynak, basit kendi üretimimiz veya dış destek mi? Görsel/ses tutarlılığını kim kontrol edecek? |
| T02 — Kapasite ve ayrılma | Gerçek haftalık saatler, öğrenme ihtiyacı, ilk çalışma dönemi ve tekrar değerlendirme zamanı nedir? Bir kişi ara verirse veya ayrılırsa devir ve devam kuralı ne? |
| T03 — Ürün hedefi | Portfolyo/eğlence, ticari hedef veya önce prototip sonra karar mı? Başarı ölçütü ve hedefi yeniden değerlendirme noktası ne? |
| T04 — Bütçe | Tek seferlik ve aylık üst sınır ne? Ödeyen, masraf paylaşımı, harcama onayı ve servis kullanımını izleyen kim? |
| T05 — Tasarım kararı | Sistemler arası kararı kim sonuçlandıracak? Anlaşmazlık nasıl çözülecek? Kapsam ve para kararları nasıl ayrılacak? |
| T06 — İnsan netcode sorumluluğu | Ağ kodunu okuyup hata ayıklamayı üstlenen kişi ve yedeği kim? Mevcut bilgi açığı nasıl kapatılacak? |
| T07 — D06 farkındalığı | Aynı kampanyanın kayıt sahibi host olmadan sürdürülemeyeceği v1 sınırı anlaşıldı mı? Test kampanyasının host'u kim olacak? |

## Başlangıç önerileri — henüz ekip kararı değil

**Görsel/ses:** P2/P3'e kadar basit kendi üretiminiz veya uygun lisanslı geçici kaynaklarla temel his katmanını kurun. Sıfırdan kapsamlı Blender, animasyon veya ses üretimi öğrenmeyi bütün oyunun ön koşulu yapmayın. Mehmet ekipman/dalgıç geri bildirimi, Utku canlı/sualtı, Mert kasaba/UI ihtiyaçlarını listeler; bu, hepsini sıfırdan kendilerinin üreteceği anlamına gelmez. Her kaleme kaynak/üretim yöntemi, yapan kişi ve oyuna ekleyecek kişi yazın; ücretli paket seçimi P0'ı bekletmesin. Mevcut P2/P3 his teslimleri değişmez.

İlk dış kaynak eklenmeden kaynak bağlantısı, lisans, atıf, ekip kullanım hakkı ve public repoda ham dosya paylaşım izni kontrol edilir. Ücretsiz indirme veya satın alma tek başına bu izinlerin kanıtı değildir; pakete özgü şartlar incelenir. İzin doğrulanamıyorsa ham varlık repoya girmez, geçici kendi varlığınız kullanılır. P4'te liste tamamlanır; ilk kontrol P4'e bırakılmaz. [Unity Asset Store koşulları](https://unity.com/legal/as-terms).

**Süre ve devam:** "Bir yıldan uzun sürebilir" bir risk senaryosudur; şu anda doğrulanmış teslim tahmini yok. Kod, öğrenme, görsel/ses, inceleme ve ayrı bilgisayar testini kapasite hesabına katın. P0'da sürdürülebilir saatleri ve ilk çalışma dönemini belirleyin; P1 sonunda gerçekleşen süreyle tahmini yenileyin, P2/P3 oynama testlerinde devam kararını tekrar değerlendirin. Faz numarası hafta veya ay demek değildir.

Öneri: geçici yoklukta mevcut fazdaki belirli işi kayıtlı olarak devredin. Kalıcı ayrılıkta mevcut fazda durup kapsamı, sahipliği ve kapasiteyi yeniden değerlendirin; kalan iki kişi eski üç kişilik tamam kaydını taklit etmesin. Devredilecekler: son commit/branch, açık hatalar, build adımları, varlık kaynakları ve hesap erişimlerinin yönetimi. Kod/varlık kullanım izni, emek/atıf ve ticari hedef varsa gelir/gider paylaşımının nasıl kararlaştırılacağını da konuşun; oran veya hak devri varsaymayın. Ekip küçülürse devam için plan ve faz kapısı açıkça güncellenir.

**Hedef:** İlk somut hedef olarak P3'te tek bölgeli, tekrar oynamak isteyeceğiniz bir prototipi değerlendirmek önerilir. Bunun ticari hedefe hizmet edip etmediğini T03'te ayrıca yazın. Ticari hedef seçmek mağaza açma veya ödeme yetkisi değildir; P6 teslimi de otomatik yayın değildir. Yayın düşünülürse mağaza içeriği, içerik/yaş derecelendirmesi ve hesap/vergi işlemlerini araştıracak kişi ve zaman ayrıca belirlenir. Ücretsiz/portfolyo amacı, seçilen platformun şartlarının kontrolünü kaldırmaz. [Steamworks başlangıç koşulları](https://partner.steamgames.com/doc/gettingstarted/onboarding).

**Bütçe:** Yazılı harcama kararı olmadan ücretli paket/servis açılmaz veya satın alınmaz. Ortak kasa zorunlu değildir; kimin ödeyeceği ve paylaşım biçimi seçilir. P1'de çevrimiçi servis kullanmadan önce hesap sahibi, kullanım takibi, uyarı eşiği ve limite yaklaşınca testi durdurma işlemi net olmalı. Uyarı, otomatik harcama tavanı olarak kabul edilmez; gerçekten yapılandırılan kontrol kaydedilir. Bütçe engeli internet testini yapılmış saydırmaz.

31 Ağustos 2026'da resmî sayfalarda kontrol edilen maliyet notları; kullanım/satın alma öncesi yeniden kontrol edin:

- Relay: ücretsiz katmanda ilk 50 **aylık ortalama** eşzamanlı kullanıcı (CCU); CCU başına 3 GiB, ayda en fazla 150 GiB ücretsiz trafik belirtiliyor. Bu, her projeye koşulsuz 150 GiB veya sınırsız ücretsiz test demek değildir. Ek kullanım ücretlidir; gerçek tüketim ölçülmeden ekip maliyeti hesaplanamaz. [UGS fiyatlandırması](https://unity.com/products/gaming-services/pricing).
- Steam Direct: uygulama başına 100 USD veya karşılığı; uygulanabilir vergiler eklenebilir. Bu yalnızca başvuru ücretidir, toplam yayın bütçesi değildir ve P0 harcaması değildir. [Steam Direct ücreti](https://partner.steamgames.com/doc/gettingstarted/appfee).
- Asset, ses, diğer servis ve olası AI abonelikleri ayrı kalemlerdir; satın alma yok, tutarlar ve ödeme sorumluları henüz belirlenmedi.

**Tasarım:** Öneri: alan içindeki geri alınabilir ayarları sistem sahibi yapar. Balık hızı–zıpkın–ödül gibi ortak kararda bir kişi öneriyi ve etkilerini yazar, etkilenenler mevcut fazda kısa deneme yapar; uzlaşma olmazsa üç kişinin oyu ile 2/3 çoğunluk uygulanabilir. Bu yöntem ancak T05'te kabul edilirse geçerlidir. Maliyet, gelir/hak paylaşımı, kapsam ve faz kapısı değişiklikleri sıradan denge oylamasına sokulmaz; ilgili açık karar ve mevcut onay kuralları korunur. Çözümsüz kararda mevcut davranış korunur ve engel kaydedilir; koordinatör olmak tek başına son söz yetkisi vermez.

**Netcode:** Mehmet'in bağlantı görevi, onun deneyimli ağ geliştiricisi olduğunun kanıtı değildir. P0'da birincil kişi, yedek ve varsa öğrenme açığı seçilir; kimse hazır değilse P1 işine sınırlı öğrenme/inceleme alt işi konur. P1 kapanışında birincil kişi gerçek projede host yetkisini, oyuncu sahipliğini ve kopma akışını açıklayıp tekrar üretilebilir bir bağlantı/sahiplik sorununu loglarla nasıl izlediğini gösterir; yedek kişi kaydı kullanarak kontrolü tekrarlar. Yeni bir eğitim fazı veya ayrı demo projesi kurulmaz. Herkes kendi modülünün ağ kodunu anlamaktan ve co-op testinden sorumludur; Codex çıktısı insan incelemesi ve gerçek testin yerine geçmez.

**Kayıt sınırı:** Kayıt sahibi host yoksa diğerleri **aynı kampanyaya** devam edemez. Başka biri host olup ayrı kampanya başlatabilir; ilerleme kendiliğinden birleşmez. Manuel kayıt taşıma, bulut senkronu ve host devri v1 özelliği değildir. Test host'u belirlemek tek kişiyi her oyunda host olmaya mecbur bırakmaz. Bu sınır D06 ve [kayıt sözleşmesinde](CONTRACTS.md) görünür kalır.

## Tek kısa karar kaydı

Mert gerçek görüşme sonucunu bu tabloya işler; önerileri kendiliğinden kabul edildiye çevirmez. Ayrı onay matrisi veya yedi yeni issue gerekmez. D01–D11 sonuçları mevcut [STATUS](STATUS.md) tablosunda kalır.

| Konu | Durum | Karar / sorumlu / takip zamanı / görüşme kaydı |
|---|---|---|
| T01 | GORUSULMEDI | Kaynak yöntemi, üretim/ekleme sahipleri ve tutarlılık sorumlusu belirlenmedi |
| T02 | GORUSULMEDI | Saatler, ilk çalışma dönemi ve yokluk/ayrılma kuralı belirlenmedi |
| T03 | GORUSULMEDI | Nihai hedef ve yeniden değerlendirme noktası belirlenmedi |
| T04 | GORUSULMEDI | Bütçe, ödeme/paylaşım, onay ve kullanım takibi sorumlusu belirlenmedi |
| T05 | GORUSULMEDI | Tasarımda son söz veya oylama yöntemi kabul edilmedi |
| T06 | GORUSULMEDI | Birincil ağ inceleyicisi, yedeği ve öğrenme planı belirlenmedi |
| T07 | GORUSULMEDI | D06 ekip farkındalığı ve test kampanyası host'u kaydedilmedi |

P0 kapanmadan her başlıkta uygulanabilir kısa çalışma kararı bulunur. Ücretli paket veya mağaza gibi ilerideki seçimler, bu arada geçerli sınır ve sorumlu/takip noktası yazılarak ertelenebilir. Örneğin "P3 testine kadar harcama yok; geçici kendi varlıklarımız; P3 sonrası ekipçe değerlendir" açık bir karar olabilir; boş bırakmak karar değildir. Ayrıntılı üretim, fiyat dengeleme ve gelecekteki özellikler P0'a çekilmez.
