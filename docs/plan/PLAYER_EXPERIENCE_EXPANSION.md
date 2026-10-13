# P4 oyuncu deneyimi ve sanat genişletmesi

13 Ekim 2026 — kullanıcı talebiyle plan revizyonu 3.1. Bu belge yeni aşamaların kapsamıdır; uygulama, süre veya tamamlanma iddiası değildir. [PHASES](PHASES.md) ana faz kaynağı, [STATUS](STATUS.md) güncel kabul kaydıdır.

## Neden yeni teslimler var?

Üç kişinin bildirimi: birlikte oynanabiliyor; sandal görevi 2/3 kaldı, dünya küçük, grafikler zayıf ve yalnız tek balık görüldü. Ortak oynanabilirlik bildirimi saklanır; nihai görsel/içerik kabulüne çevrilmez. Otomatik testler, yürüyüş yolu ve kataloglar tek başına bitmiş oyun değildir.

P4.6 teknik ve fiziksel temel kabulü açık kalır. Yeni ürün teslimleri P4.7–P4.12 olarak P5'ten önce eklenmiştir. P4.6'nın mevcut teknik açıkları kapatılmadan sonraki aşamanın oyun kodu başlamaz. Kaynak araştırması, tasarım ve planlama paralel yapılabilir. Her aşama kendi küçük gerçek oyun gösterimiyle kabul edilir.

## Her etkileşimin tasarım kontrolü

Ayrıntılı oyuncu yolculuğu, yerleşim/tempo, eylem tabloları, ekonomi, UI, kayıt ve tasarım kararları: [EXPERIENCE_DESIGN.md](EXPERIENCE_DESIGN.md). Aşama bazında alan işleri, üretim envanteri, kritik yol ve kanıt paketleri: [PRODUCTION_BLUEPRINT.md](PRODUCTION_BLUEPRINT.md). P5/P6 ayrıntıları ve P6 sonrası aday aşamalar: [FUTURE_ROADMAP.md](FUTURE_ROADMAP.md).

Her teslim aşağıdaki soruları gerçek Windows build üzerinden cevaplar:
1. Oyuncu hedefi dünyada nasıl fark ediyor; adını/yerini yalnız debug metninden mi öğreniyor?
2. Elleri, bedeni, taşıdığı nesne ve çevre ne yapıyor? Arkadaşı ne görüyor?
3. Sonuç hangi model, hareket, ses, yüzey veya ışık değişimiyle okunuyor?
4. Yaklaşırken, iptal ederken, yarıda koparken ve yeniden açarken ne oluyor?
5. Tek başına oynayan kişi aynı eylemi yapabiliyor mu?
6. UI, yapılan işin sonucunu açıklıyor mu; dünyanın yerine geçiyor mu?

Bu kontrolü alan sahibi PR'da doldurur. Her özellik için yeni mekanik icat edilmez; gereken sunum ve mevcut eylemin anlaşılması değerlendirilir.

## P4.7 — Görsel örnek ve dünya yerleşimi

**Görünen sonuç:** evin kapısından sahile giden kısa bir bölüm ilk kez hedef sanat kalitesinde görünür. Bir dükkân cephesi, kıyı, kırık sandal ve oyuncu elleri aynı görsel dili taşır.

- Mert: renk/malzeme, model ve ses referansları; ev–dükkân–sandal için tek uyumlu kaynak seti. Kaynak ve üretim listesi; tekrar kullanılabilir modüler parçalar.
- Utku: tek kasaba/tek deniz yerleşimi; ev, meydan, hizmetler, sahil, iskele, görüş çizgileri ve alternatif yaya yolları.
- Mehmet: insan ölçeği, kamera, el/nesne kadrajı, kontrolcü geçişi, co-op görünürlüğü ve etkileşim noktaları.
- Tasarım başlangıç hedefi: ev–iskele ana yolunda yaklaşık 60–90 saniye rahat yürüyüş; satış/alışveriş birbirine yakın. Bu bir teslim süresi değil, ölçülüp oynama sonucuyla değiştirilecek yol temposudur.
- İçerik taslağı: bir meydan, ana yol ve iki kısa yan bağlantı; dört hizmet cephesi; 6–10 tekrar kullanılabilir arka plan bina cephesi; açık kıyı ve bir dönüm noktası. Ulaşılamayan dekor kapıları etkileşimli gibi işaretlenmez.
- #153/#154 yerleşim ve sanat başlangıcı; #157 kamera/hareket bağlantısıdır. Ayrı dünyalar veya ikinci şehir üretilmez.

**Kabul:** aynı kamera/kadrajla önce–sonra görüntüleri, gerçek yaya turu, gündüz/gece okunabilirlik ve kare süresi ölçümü; kullanıcı hedef kaliteyi değerlendirir. Görsel örnek kabul edilmeden bütün harita aynı varlıklarla doldurulmaz.

## P4.8 — Elle ve aşamalı sandal tamiri

**Görünen sonuç:** kıyıda tahtayı/onarım parçasını bul, elde taşı, hasarlı sandala yaklaştır, belirlenmiş yerine tak ve keserle çivileri çak. Her tamamlanan katkıda sandal gerçekten düzelir.

Mevcut üç onarım kimliği korunur: gövde, motor, yakıt tankı. Gövde katkısı tahta yamaları/çivi; motor katkısı bağlantı ve oturma; yakıt tankı katkısı tank/bağlantı sunumudur. Motor/tank sessizce tahta sayılmaz. Genel crafting, serbest çivi fiziği veya sürekli tekne bakım borcu eklenmez.

**Ayrıntılı incelemede açık karar:** kürekli sandal ile motor/tank kimlikleri arasında tasarım uyumsuzluğu var. EXPERIENCE_DESIGN R01 başlangıçta üç ahşap/bağlantı tamir grubunu, eski kimlikleri kayıpsız katalog eşlemesiyle korumayı önerir. Karar kesinleşene kadar mevcut onarım semantiği değişmez. Her tahtada görünür/kalıcı ilerlemenin grup veya alt-slot olacağı R02 ile kararlaştırılır; bunlar alınmış ekip kararları değildir.

- Mert: kırık gövde ve her katkı için mesh parçaları; taşıma nesnesi, keser, çivi ve darbe sesleri. Başlangıç model görünümü yamalı, eksik ve kullanılmaz; son görünüm sağlam ama aynı sandal kimliğini korur.
- Mehmet: yerel iki el tutuşu, kaldırma/taşıma/yerleştirme/darbe/bırakma, uzak oyuncu sunumu ve host doğrulaması. Kısa basılı tutma/tekrarlı eylem; başlangıç taslağı katkı başına 2–3 kontrollü darbe, hissi oynama testiyle ayarlanır. Serbest fizik simülasyonu gerekmez.
- Utku: erişilebilir fiziksel parça yerleşimi, hasarlı sandal çevresinde dört oyuncu yaklaşma alanı; görsel ipucu, çalışma noktası ve çarpışmalar.
- Taşıma sunumu yalnız onarım parçaları için başlar. Tahtayla zıpkın/kamera aynı anda kullanılmaz; oyuncuya görünür bırak/iptal yolu sağlanır.
- Her mantıksal katkı tek transaction'dır. Son doğrulanmış darbe tüketim/katkı/kayıt sınırıdır; sunum darbesi para/ödül yaratmaz. Öncesinde iptal/kopma parçayı silmez. İki kişi aynı yuvada çalışınca host tek sahip/tek katkı doğrular.
- Üç tamamlanmış katkı kalıcı maskeden gösterilir; 0/3, her bir 1/3 ve her bir 2/3 kombinasyonu doğru görünür. Eski 2/3 kampanya sıfırlanmaz. Kısmi darbe animasyonu kalıcı onarım diye kaydedilmez.
- Dükkândan alınan parça aynı yuvaya aynı tamir yoluyla bağlanır. Ücretsiz kıyı yolu ve mevcut ekonomi korunur.
- Prep/Return'da fiziksel toplama kararı #157 içinde uygulanır; Closing, pasif oyuncu ve uzak etkileşim reddi korunur. #156'daki yön/metre paneli dünya ipuçlarını destekler.

**Kabul:** solo ve iki süreçte gerçek girişlerle taşıma→yerleştirme→darbe→model değişimi; dört oyuncu görünürlük; eşzamanlı katkı, yarıda iptal/kopma ve diskten 2/3 devam; son katkı sonrası binme/rota. Birinci ve üçüncü kişi videoları gerekir.

## P4.9 — Eller, beden ve ortak eylemler

**Görünen sonuç:** oyuncu nesneye gerçekten uzanır; ekipmanı kuşanır, kamera/zıpkın tutar, tekneye oturur, avı teslim eder. Arkadaşı aynı eylemi anlar.

- Öncelik: ekipman kuşanma, av alma/çantaya koyma, NPC teslimi, tekne binme/inme. P4.8'in el/nesne seti tekrar kullanılır.
- Mehmet animasyon geçişi/input/ağ sunumu, Mert uyumlu rig/nesne/ses, Utku dünya hedef ve collider bağlantısı.
- Yerel ve uzak oyuncu aynı authoritative sonuçtan sürülür; ses/animasyon ödül otoritesi değildir. Reddedilen işlem teslim edilmiş gibi görünmez.
- Kısa eylemlerde kontrol geri dönüşü, ekipman iptali, clipping ve kamera rahatsızlığı değerlendirilir. Uzun zorunlu sinematikler eklenmez.

**Kabul:** aynı eylemin yerel kamera ve arkadaş bakışından videosu; reddedilen/yarıda iptal edilen işlem; 1/2/4 oyuncu ve ilk gün ücretsiz av akışı.

## P4.10 — Yaşanan kasaba ve ortak ev

**Görünen sonuç:** dört esnafın tanınır çalışma alanı ve hareketi vardır; oyuncu eve girip yatağına, depoya ve PC'ye fiziksel olarak erişir. Kazandığı gelişimler ortamda görünür.

- Mert: dükkân içi/tezgâh ayrıntıları, balık tartma/teslim sunumu, ekipman sergisi, küçük NPC çalışma/selamlama döngüleri, ev eşyaları ve UI düzeni.
- Utku: ana cadde/yan yolları P4.7 setiyle doldurma; bitki, taş, sahil geçişi, iskele ve yön bulma.
- Mehmet: kapı/tezgâh/PC/yatak erişimi, oyuncu çarpışması ve arkadaşın eylem sunumu.
- NPC çalışma döngüleri kozmetiktir; yeni NPC takvimi, açlık veya mağaza açık-kapalı ekonomi sistemi yok. Tek ev, dört hizmet, iki ev seviyesi ve üç kasaba iyileştirmesi bütçesi korunur.
- Ayak sesi taş/ahşap/kum/su yüzeyine göre değişir. Kıyıda dalga/rüzgâr, dükkânda çalışma sesi, evde daha sakin ortam; duvar/mesafe geçişleri kulağa anlaşılır.
- Gelişmiş ev, depo/sergi/ışık farkıyla; dükkân/iskele yükseltmesi kendi yerinde görünür. Satın alma sadece metin sayacı değildir.

**Kabul:** gerçek satış/alışveriş/PC/uyku turu; gece ve gündüz; UI çakışma kontrolü; save/load sonrası aynı gelişim ve co-op görünüm. Grafik ve ses kullanıcı tarafından değerlendirilir.

## P4.11 — Canlı deniz, keşif ve yolculuk sunumu

**Görünen sonuç:** kıyı, resif ve derin kesim ayrılır; türler şekil/boyut/davranışla tanınır. Deniz tabanı, bitkiler, su ışığı ve ses derinliği anlatır.

- Utku: #155 tek tür teşhisi; beş normal tür ve ayrı boss için gerçek spawn/habitat/erişim haritası. Sürü davranışı gerekiyorsa mevcut tür sayısını artırmadan sınırlı sunum; amaç sadece rastgele balık çoğaltmak değildir.
- Mert: uyumlu canlı/bitki/kaya/batık kaynakları, su malzemesi, ses ve efekt seti.
- Mehmet: kamera ve zıpkın sunumu, suya giriş/çıkış, tekne koltuk/güverte/ağ görünürlüğü.
- Su yüzeyi ve sualtı arasında ışık/ses geçişi; bitki hareketi, kontrollü partikül ve okunur siluet. Sis, balıkları veya dönüş yolunu gizleyecek kısayol değildir.
- Sandal/motorlu/araştırma teknesi ayrımı gövde, koltuk, motor/su izi/ses üzerinden okunur; sabit rota korunur. Yeni serbest dümen, batma ve yakıt ekonomisi yok.
- Bossun üç saldırı hazırlığı, darbe ve zayıf nokta görsel/sesle okunur; sağlık sayacı tek anlatım değildir.

**Kabul:** yeni kampanyadan başlayarak tür erişimi/av/çekim; üç kesim gündüz/gece videosu; boss araştırma ve mücadele kanıtı; aktif tekne harita/dönüş tutarlılığı ve 1–4 oyuncu performansı.

## P4.12 — Nihai dünya ve deneyim kabulü

Aynı sabit Windows build ve kampanyada: evden çıkış → fiziksel parça/elle sandal onarımı → ücretsiz kıyı avı → satış/ekipman → tekne → tür keşfi/çekim → güvenli dönüş → PC/yayın → ev gelişimi → uyku/sonuç. Mevcut ilk gün özgürlüğü korunur; bütün zincir zorunlu öğreticiye çevrilmez.

- En az iki bilgisayarda dört süreçle üç gün; solo yolu da oynanır. Üç kişinin gerçek değerlendirmesi ve kullanıcı görsel/içerik kabulü kayıtlıdır.
- Tamamlanan eylemlerde model/hareket/ses/geri bildirim; görev ipuçları, grafik ölçeği ve içerik çeşitliliği değerlendirilir.
- 2/3 eski kayıt, yarıda eylem, host ayrılması, 00:00 ve medya/diskten devam regresyonları korunur. P4.6'nın eksikleri bu kapıda kaybolmuş sayılmaz.
- Aynı donanım/1080p hedefinde kare süresi ve dört oyuncu/kayıt etkisi raporlanır. P5'i bekleyen kusurlar sınıflanır; görsel içerik açığı küçük kusur sayılmaz.
- Kritik eksik varken P5 açılmaz; görsel yön veya tempo reddedilirse en fazla iki dar düzeltme turu yapılır, kapsam yeniden değerlendirilir.

## Çalışma sırası, sahiplik ve mevcut işler

| Aşama | GitHub teslimi |
|---|---|
| P4.7 | [#159 — görsel örnek/yerleşim](https://github.com/mehmetalisahingm/DeepDiveGame/issues/159) |
| P4.8 | [#160 — fiziksel sandal tamiri](https://github.com/mehmetalisahingm/DeepDiveGame/issues/160) |
| P4.9 | [#161 — eller/beden/ortak eylemler](https://github.com/mehmetalisahingm/DeepDiveGame/issues/161) |
| P4.10 | [#162 — kasaba/ev/ses](https://github.com/mehmetalisahingm/DeepDiveGame/issues/162) |
| P4.11 | [#163 — canlı deniz/yolculuk](https://github.com/mehmetalisahingm/DeepDiveGame/issues/163) |
| P4.12 | [#164 — nihai deneyim kabulü](https://github.com/mehmetalisahingm/DeepDiveGame/issues/164) |

#153 dünya ve #154 sanat P4.7/P4.10/P4.11 alt teslimleriyle kapanır; #155 tür doğrulaması P4.11 kanıtıdır; #156 mevcut 2/3 yönlendirme düzeltmesidir, elle tamir teslimi sayılmaz. #157 hareket ve ağ temelidir; Mehmet P4.8/P4.9 işlerine ayrıca sahiptir.

Mert'in üretimi bütün aşamaların tedarik listesiyle küçük parçalara bölünür. Önce görsel örnek ve sandal seti; sonra ortak eylemler; sonra kasaba ve deniz. Her şeyi sıfırdan tek kişiye yüklemek yerine mevcut uyumlu kaynakların yeniden kullanımı ve gerekirse açık dosya bazlı destek ataması yapılır. Satın alma/servis/kurulum izni bu plan değildir.

Sahne değişimi sırası: Utku geometri/anchor → Mehmet hareket/etkileşim → Mert sunum/servis → ortak build. Prefab sanat değişimlerinde Mert varlık/.meta seti → ilgili alan sahibi binding → co-op doğrulama. Ortak dosyayı iki kişi aynı anda düzenlemez; görevde sıra belirtilir.

Her aşamanın bağımsız hazırlığı paraleldir; uygulama öncekinden kabul alır. Takvim tarihi ve ekip tamamı uydurulmaz. P5 #147–#150 ancak genişletilmiş P4 kabulü ve main/build kapısından sonra açılır; P6 teslim kapsamı korunur.

