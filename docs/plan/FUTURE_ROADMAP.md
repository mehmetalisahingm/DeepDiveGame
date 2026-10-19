# DeepDive ileri ürün yol haritası

13 Ekim 2026 — ileriye dönük ayrıntılı seçenekler. Bu belge P4/P5/P6'nın sınırlarını değiştiren uygulama izni değildir. [Deneyim tasarımı](EXPERIENCE_DESIGN.md) mevcut oyunun ayrıntısı, [üretim planı](PRODUCTION_BLUEPRINT.md) öncelikli iş sırasıdır. Aşağıdaki P7–P10 adları aday plan aşamalarıdır; aktif faz veya yayın takvimi değildir.

## 1. Ufuklar ve başlangıç şartları

19 Ekim [dünya kalite teslimleri](WORLD_QUALITY_DELIVERIES.md) uzak sahilli şehir/ev/dükkân, hareketli su, mercan habitatı, balık animasyonu, derinlikte av değeri ve bulunabilir bossu mevcut P4 ürün borcu olarak ayrıntılandırır. Bu eksikler P7–P10 aday genişlemelerine veya P5'te genel polish işine ertelenmez. P4.9–P4.12 dört küçük iş veya kısa takvim anlamına gelmez.

| Ufuk | Ürün amacı | Başlangıç kapısı | Tamamlanma sonucu |
|---|---|---|---|
| Şimdi: P4.6–P4.12 | Oynanabilir temeli dolu ve hissedilir oyuna dönüştür | Fazın mevcut teknik ve ürün kapıları | Gerçek fiziksel dünya, grafik/ses/eylem ve içerik kabulü |
| Sonra: P5 | Hata, denge, performans, kayıt ve co-op güvenilirliği | Genişletilmiş P4 + main/build kabulü | Kritik/yüksek hata yok, hedef donanım ve ağ stres kanıtı |
| P6 | Bağımsız bilgisayarda çalışan teslim paketi | P5 kapanışı | Sabit commit, tekrar build, rehber/atıf/bilinen kusurlar |
| P7 adayı | İlk oyuncu verisine göre deneyim ve çeşitlilik iyileştirme | P6 + gerçek ürün değerlendirmesi ve kapsam kararı | En baskın sorunun küçük ölçülebilir çözümü |
| P8 adayı | Tek seçilmiş içerik genişlemesi | P7 kanıtı + bütçe/kayıt/üretim fizibilitesi | Bir yeni alan veya bir içerik paketi, gerçek döngüye bağlı |
| P9 adayı | Sosyal/yaratıcı uzun vadeli kişiselleştirme | Temel çeşitlilik/ilerleme kabulü | Oyuncunun kimliğini gösteren sınırlı sistem |
| P10 adayı | Daha gelişmiş denizcilik veya kampanya altyapısı | Ayrı teknik prototip ve ürün/bütçe kararı | Seçilen tek ağır sistem, regresyon ve eski kayıt uyumu |

P6 teslimi mağazada yayın kararı değildir. Ücretli servis, public yayın ve yeni platform ayrıca yetki/ürün kararıdır. Aday aşamalar için şu an issue açılmaz; önce mevcut ürün kabulü. Oyuncu verisi varsayılmaz veya oyun dışı analiz servisi kurulmaz.

## 2. P5'i detaylandırma

Mevcut #147–#150 korunur. P5'e yeni ada, yeni boss veya crafting sokulmaz.

- 1/2/4 oyuncu gerçek döngü; en az iki bilgisayarda dört süreç/30 dakika ve üç dalış/üç gün.
- 150 ms ek RTT/%2 paket kaybı; test koşulları kayıtlı. Ağ düzeni/araç seçimi ayrıca uygulamada belirlenir.
- Tamir son darbesi, iki kişinin aynı yuvayı istemesi, satış/yayın/tekne satın alma, Closing ve kayıt yazarken kesinti.
- Bozuk/yarım klip, arşiv tavanı, kesilen transfer, disk hata/red; kazanç veya eski kampanya sessizce sıfırlanmaz.
- Capture açıkken dört kişi grafik ve disk maliyeti; standart kalite seçenekleriyle hedef donanım değerlendirmesi.
- Ücretsiz ilk kazanç, av/kanal tur maliyeti, solo ve ekip ilerleme; oyuncuyu içerik azlığını grind ile kapatmaya zorlamama.

P5 kabul listesinde yeni sanat açığıyla teknik bug ayrılır. Mevcut görsel yön kabul edilmediyse P4'e geri dön; genel “polish” adıyla eksik dünyayı saklama.

## 3. P6 ve ilk bağımsız oyuncu paketi

Paket: bütün exe/data/dll/medya bağımlılıkları; sürüm/commit/build hash ve temiz bilgisayar testi. Unity/Git kurmadan oynamak mümkün olmalı. İndirilebilir artifact veya paket yöntemi bu aşamada somut teslimdir; build'in başarılı üretilmesi indirilebilir oyun bulunduğu anlamına gelmez.

Kısa oyuncu rehberi:
- Oda/host kampanyası, host yokken devam sınırı.
- Yürüme/yüzme/etkileşim/ekipman; tamir aleti ve bırak/iptal.
- Ücretsiz av, sandal onarım yolu, kamera satın alma.
- Satış ile kanal tek hak; gün ve 00:00 sonuçları.
- Kayıt/klip konumu, arşiv sınırı ve silme/bozuk dosya bildirimi.
- Sorun raporu için sürüm/build, host/guest, görev ve son eylem.

Güncelleme testinde eski 2/3 kayıt ve gelişmiş kampanya açılır; source kontrolündeki sahne veya prefab cache'ine bağımlı olmaz. Ekip erişimi/lisans atıfları dosya paketinde doğrulanır.

## 4. P7 adayı — Önce gerçek baskın sorunu seçmek

İlk sürümden sonra aynı anda altı özellik açma. Kısa ürün değerlendirmesi:
- Oyuncu ilk gelir ve ilk seferi açıklama almadan buluyor mu?
- Hangi eylem eğlenceli, hangisi tekrar eden angarya?
- Tek deniz hâlâ keşfedilecek yer veriyor mu; yoksa türler sadece sayısal koleksiyon mu?
- Av ve kamera iki anlamlı yol mu?
- Arkadaşın ne yaptığı ve ortak kazancın kime/nereye gittiği okunuyor mu?
- Evi geliştirmek görsel/oyun değeri veriyor mu?
- Tamir yeniden oynarken çok uzun mu, birlikte yapmak anlamlı mı?
- En zayıf donanımda görüntü/girdi/bağlantı sürdürülebilir mi?

Veri yöntemi: gönüllü oynama ve test kaydı, kişisel veri istemeyen yerel ölçüm, gözlem/görüşme. Küçük ekip testine oyuncu kitlesi istatistiği muamelesi yapılmaz.

Bir baskın sorun seçilip önce küçük değişiklik yapılır: yoğunluk/rota işareti, görev seçimi, kısa eylem temposu, ekonomi veya UX. Sonuç değişmiyorsa daha büyük içerik eklemek ertelenir.

## 5. P8 adayı — İçerik genişleme seçenekleri

Aşağıdaki paketlerden önce yalnız birisi seçilir. Hepsini zorunlu faz kapsamına eklemek üç kişilik ekibi taşıyamayacağı üretime sokar.

### 5.1 Aynı denizde bir yeni keşif alanı

Örnek adaylar: kelp benzeri yoğun bitki alanı veya daha büyük batık. Yeni alan sadece dekor değil bir karar üretir: görüş/saklanma, alternatif kayıt açısı, yeni rota/dönüş temposu.

Gerekenler: sınır/rota/harita/keşif kimliği, çevre kit'i, erişim ve oksijen ekonomisi, tür/olay bağlantısı, kamera içerik çeşitliliği. Önce mevcut türlerden anlamlı habitat kullan; yeni tür gereksinimini ayrı değerlendirme.

Risk: mevcut save keşif grid'i ve routeId/habitat bağları bozulabilir; oyuncu eski görevde erişemeyeceği hedef alabilir. Migration/test olmadan ship edilmez.

### 5.2 Yeni tür ve davranış paketi

Yalnız model sayısını artırma. Farklı yaklaşma/kaçış/sürü, farklı av veya çekim kararı, yeni gözlem bilgisi gerekir. Paket büyüklüğü bütçe/kullanıcı verisiyle belirlenir; beş→yirmi tür sözü verilmez.

Gerekenler: species kimliği, prefab/rig, spawn/limit, davranış, av/ekonomi, manifest, ansiklopedi, görev uygunluğu, kaynak ve co-op performansı.

Kabul: eski beş tür kaydı korunur; görev kataloğu gerçek erişimle filtrelenir; her yeni tür aynı modelin yalnız farklı rengi değildir.

### 5.3 İkinci efsanevi canlı

İkinci boss ancak birinci karşılaşma öğrenilebilir/tekrar oynanabilir ve teknik olarak kararlıysa. Yeni boss, sağlık artırılmış ilk boss değil; farklı saldırı hazırlığı, arena/siper ve araştırma/av seçimi.

Maliyet: büyük model/rig/animasyon, arena, AI/host, dört kişi ölçek, ödül/save, keşif zinciri ve ses. Üç kişilik ekip için yüksek risk; normal içerik boşluğunu bossla örtme.

### 5.4 İkinci kıyı/ada

Daha uzun tekne yolculuğu veya yeni kasaba/biyom fikri. P4'ün tek bölge sınırı ancak açık yeni kapsam kararıyla değişir.

Karar öncesi: bölge geçişinde oyuncu/tekne/envanter/klip/host durumu, streaming veya sahne yükleme yöntemi, dönüş ve 00:00, birden çok harita, kayıt şema maliyeti. Günlük teleport yasağını aşmak için ekran fade'ini gizli konum atlama olarak kullanma; ulaşım oyuncuya dünyada anlaşılır olmalı.

Bu seçenek aynı denizde bir yeni alana göre çok daha pahalıdır; dünya küçük şikâyetini yalnız bu paketle çözeceğim demek gereksiz gecikme yaratır.

## 6. P9 adayı — Kimlik, ev ve ortak yaratıcılık

### 6.1 Sınırlı ev kişiselleştirme

İlk aday: mevcut fotoğraf/trofe duvarında birkaç sabit slot, renk/afiş seçimi, ekip başarı rafı. Serbest bina inşa editörüyle başlamaz.

Kayıt: placementId/slotId/itemInstanceId, shared ownership, aynı slota iki istek, eski save ve UI. Co-op: tüm arkadaşlar aynı seçimi görür; tek kişi eşyayı kaybedemez veya ortak parayı gizlice harcayamaz.

Değer: oyuncu kendi gerçek klibinden kapak/fotoğraf seçer, başarısını duvarda görür. Tekil recording ve gelir hakkı dekorla çoğalmaz.

### 6.2 Daha derin NPC ilişkileri

İlk seçenek birkaç kısa görev/diyalog ve yaptığı işe tepki; daha sonra kozmetik takvim. Ticaret kapısını gece mağaza kapanmasıyla kilitlemek ayrı ekonomi kararıdır.

Gerekenler: diyalog lokalizasyonu, quest state, NPC sunum, ses/altyazı, günlük callback/save. Tam seslendirme ve onlarca NPC planlanmadan kaynak üretim eforu hesaplanır.

### 6.3 Oyun içi kanal kimliği

Sınırlı kanal afişi, arşiv/başlık düzeni ve önceki videonun dünyada sergisi. Gerçek dış hesap/sosyal medya entegrasyonu varsayılmaz.

Uzun klip montajı, dışa video aktarma ve ortak düzenleme ayrı ağır medya projeleridir. Codec/platform/disk/lisans/transfer maliyeti prototipsiz kabul edilmez. Kamera kaydı ortam sesidir; mikrofon kaydı otomatik eklenmez.

### 6.4 Birlikte iş yapma

Tamir/taşıma eylemleri iyi hissediyorsa farklı oyuncuların aynı yapıda farklı küçük işleri yapması düşünülebilir. Solo karşılığı zorunludur; iki kişinin aynı anda tutması gereken yük yerine solo taşıma seçeneği veya küçük bileşenler gerekir.

Yeni meslek/XP/kalıcı sınıf sistemi, mevcut hafif rollerden ayrı ürün kararıdır. İlk co-op iş bölümü istemli davranışla çözülür.

## 7. P10 adayı — Ağır sistemler için prototip önce

### 7.1 Serbest tekne sürüşü

Mevcut sabit rota korunurken aday prototip: kıyıda küçük kontrollü alan, düşük hız, dört yolcu, gecikmeli ağ koşulu. Önce sürüş hissi ve network cost; sonra dünya boyutu ve içerik kararı.

Yeni gereksinimler: steer/input authority, çarpışma, dalga/denge, yolcu ilişkilendirme, docking, suya düşme ve reconnect/host ayrılması. Tekne batırma, yakıt tüketimi, çok aktif araç kendiliğinden eklenmez. Eski trip state ve save migration analiz edilir.

Bu prototip oynanışta değer göstermezse ürün kapsamına alınmaz; “gerçekçi” diye temel co-op kararlılığı bozulmaz.

### 7.2 Daha fazla hava/olay

Aday önce tek ek anlamlı koşul veya mevcut hava sunumunun güçlenmesi. Şiddetli fırtına kullanıcıyı görmeden rota dışına atıyor, geri dönüşü kapatıyor veya her gün eşyayı siliyorsa ürün zarar görür.

Gerekenler: günlük deterministik seed, planlama uyarısı, güvenli dönüş, ışık/su/ses, uygun görev kataloğu ve performans. Fiziksel dalga sistemi şart değildir.

### 7.3 Daha esnek kampanya erişimi

Mevcut host sahibi D06 korunur. Dedicated server, bulut kayıt, host migration veya kampanya aktarımı yeni ürün/servis kararlarıdır.

Ön inceleme: kampanya tek sahibi/kimliği, tam snapshot ve medya dosyası bütünlüğü, katılım/kişilik/sahiplik, ücret/bakım, kötü yarım transfer ve offline davranışı. Kullanıcı isteği olmadan servis hesabı veya harcama yaratılmaz.

Bu sistemler büyük teknik/bütçe riskidir; dört kişinin oyunu birlikte oynayamamasına sebep olan temel hata varsa önce onu çöz.

## 8. Öncelik seçme yöntemi

Her aday için bir sayfa:
- Oyuncunun hangi doğrulanmış sorunu/isteğini çözüyor?
- İlk beş dakikalık gösterimde fark edilir değer var mı?
- Aynı değeri mevcut içerik/sunum iyileştirmesiyle elde edebilir miyiz?
- Solo ve co-op sonucu ne; kayıt ve günlük haklar nasıl korunuyor?
- Yeni model/animasyon/ses/sahne ve kod miktarı?
- En düşük maliyetli gerçek prototip ve durdurma ölçütü?
- Ekip kapasitesi, üretim kaynağı, servis/varlık bütçesi?
- En kötü durumda mevcut kampanya/oyuncu emeği ne olur?

Önerilen sıra, veri aksini göstermedikçe: mevcut deneyim boşlukları → aynı denizde keşif/tür paketi → sınırlı ev/kanal kimliği → ikinci alan/boss → ağır sürüş/kampanya altyapısı. Bir sonraki aşama seçilir; hepsi aynı anda uygulamaya açılmaz.

## 9. Planın ne zaman değişeceği

P4.7 kısa görsel örneği, P4.8 ilk fiziksel tamir, P4.12 üç günlük oyuncu kabulü ve P6 bağımsız build değerlendirmesi karar noktalarıdır. Her noktada eski varsayımlar ölçüme göre güncellenir.

Tarih tahmini ancak ekip gerçek süre/veri getirince yapılır. Her ileri aday için ayrı ürün kararıyla kapsam/owner/acceptance/fiyat ve kayıt etkisi kesinleşir; plan dokümanı uygulama, ücretli satın alma veya yayın izni değildir.

Oyunun sınırlı ilk sürümü tamamlanabilmelidir. Gelecek fikirlerin varlığı, mevcut grafik/tamir/balık eksiklerini belirsiz bir “sonra”ya ertelemez.

