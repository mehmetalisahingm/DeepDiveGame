# Dünya, deniz ve ilerleme için ayrı ürün teslimleri

19 Ekim 2026 — kullanıcının oynama geri bildirimiyle plan ayrıntısı. Bu belge uygulama veya kabul kanıtı değildir. P4.6/#141 ve P4.7/#159 açık kalır; daha önce verilen dar #187 uygulama izni genişletilmez. P4.9–P4.12 üst başlıkları ve mevcut issue kimlikleri korunur; aşağıdaki işler onların ayrı kabul edilen alt teslimleridir. P5'e sanat/içerik borcu taşınmaz.

## Sorun ve bugünkü kanıt sınırı

Kullanıcının bildirimi: su gerçekçi ve hareketli görünmüyor; mercan/çevre ve canlı hareketi yetersiz; derine gidince daha değerli av hissi yok; boss bulunamıyor; ev/şehir/sahil çok yakın ve küçük, ev ile dükkânlar gerçek mekân hissi vermiyor. Bu bildirim ürün kabulünün eksik olduğuna kaydedilir. Aynı haritada yürünebilmesi veya otomatik testlerin geçmesi bu itirazları kapatmaz.

`6816fba` kaynak incelemesi: EconomyManager üç mevcut türün (`sea_bass`, `tuna`, `red_snapper`) taban bedelini av başına 120 olarak kuruyor; ürün kodunda bunu tür bazında değiştiren SetPrice çağrısı bulunmadı. Bu bir derinlik/değer eğrisi teslimi değildir. Boss encounter/progression kodunun bulunması ve tarihli teknik kabul, kullanıcının yeni kampanyada bossu bulduğu veya nihai görünüm/savaş hissini onayladığı anlamına gelmez. Bildirilen su/grafik sorunları bu plan turunda yeni bir canlı oyun incelemesi yapılmış gibi sunulmaz.

Hedef: kıyı kasabasının ayrı mahalleleri ile sahile gerçek yolculuk; suya girince başka bir ortam; derinlikte yeni canlı, risk ve kazanç; keşfedilip okunarak oynanan boss. Tek bağlantılı dünya, dört hizmet, beş normal tür hedefi ve bir boss bütçesi korunur. Gerçekçilik hedefi hareket, malzeme, ölçek ve tutarlı davranıştır; fotogerçekçilik veya yeni render motoru sözü verilmez.

## Ayrı kabul edilen iş paketleri

### 25 Ekim — kullanıcıdan tek tek fikir beklemeyen tasarım takibi

Bu kayıt mevcut kapsamın somut sunum ayrıntılarıdır; yeni faz açılışı veya oyun
uygulaması değildir. Aşağıdaki ayrıntılar alan sahibinin teslimine bağlanır;
kullanıcının her birini ayrıca tarif etmesi beklenmez. İlk görünür kalite
hedefi Q01/#159'dur: ev kapısı–bir gerçek dükkân–kıyı–sandal ile ayrı kısa
su/mercan/balık örneği. Model sayısı veya CI sonucu bu örneğin yerine geçmez.

| Oyuncunun tarif etmesini beklemeyeceğimiz ayrıntı | Mevcut iş / sahip | Oyunda görülecek kanıt |
| --- | --- | --- |
| Ölçek, ortak malzeme paleti, zemine oturma/gölge, gündüz/gece okunurluğu | Q01, #171 Mert; #172 Utku; #173 Mehmet | Aynı kadrajda yürüyüş; kapı, insan, el ve su birlikte tutarlı |
| Tahtaya gerçekten uzanan eller; kavrama, yuva/çivi teması, darbe ve bırakırken el geri dönüşü | #187 Mehmet + #189 Mert | Kendi ve arkadaş kamerasında aynı eylem; final kit ile temas ayrıca kabul edilir |
| Başarılı her darbenin çivi/ahşap/ses karşılığı; son tahtada hasarlı bölümün kapanması | #189 Mert, #188 Utku, #187 Mehmet | Darbe sayacı kapalıyken de geçici iş ve tamamlanmış tahta anlaşılır |
| Kamera/zıpkın geçişi, avı alma ve NPC'ye teslimde beden/nesne sürekliliği | Q02/#161; Mehmet, Mert rig/ses, Utku hedef | Ekipman havada belirmez; ret/iptalde teslim olmuş gibi davranmaz |
| Şehrin ön/orta/arka planı; ev sokağı, meydan ve sahile açılan görüş; yön veren tabelalar | Q03/Q04, #153/#162; Utku + Mert | Işınlanmadan dolu yaya turu; uzatılmış boş yol şehir sayılmaz |
| Dükkânın işini anlatan tartı/buzlu kasa, ekipman rafı, medya masası, tersane araçları; halat/ağ/kasa gibi sınırlı çevre seti | Q05/#162; Mert kaynak, Utku yerleşim, Mehmet erişim | Hizmet adı yazmasa da mekânın işi anlaşılır; giriş/tezgâh yolu açık kalır |
| Evde dört kişiye yer, tanınır yatak alanları, ekipman/depo/PC ve kazanılmış gelişimin rafta/ışıkta görünmesi | Q06/#162; Mert, Mehmet kullanım | Gelişim yalnız sayaç değildir; yeni oturumda aynı görünüm ve engelsiz geçiş |
| Kum/taş/ahşap ayak sesleri; kıyı dalgası; su çizgisinde ses ve görüntü değişimi, nefes/kabarcık | Q07/Q08, #163; Utku + Mert, Mehmet kamera | Kesintisiz kara→su→derinlik→yüzey kaydı ve dinleme |
| Balıkta kuyruk/yüzgeç, dönüş/kaçış; mercan/kaya/batıkta farklı siluet ve derinlik kimliği | Q09/Q10/#163; Utku + Mert | Kayarak giden renk kopyaları yerine tanınır model/hareket; dekor balığı avlanabilir tür sayılmaz |
| Derine gitmenin kazanç/risk karşılığı; bossu önceden hissettiren fiziksel iz, arena silueti ve saldırı işaretleri | Q11/Q12/#163; Utku, Mert ekonomi/ses, Mehmet erişim | Yeni kampanyadan gerçek av/satış ve ipuçlarıyla boss bulma; debug koordinatı gerekmez |
| Kamera hedefini örtmeyen HUD; REC/ret/çanta dolu/yanlış yuva gibi gerçek durumu açıklayan geri bildirim | Q01/Q02/Q14; Mehmet + Mert | Farklı çözünürlükte okunur; host reddini başarı diye göstermez |

Bu listenin üretim sahibi alan sahibidir; tasarım ve entegrasyon kontrolünde
Mehmet'in son karar rolü korunur. Mert'in bütün varlıkları tek başına sıfırdan
üretmesi varsayılmaz. Mevcut uyumlu kaynaklar ve küçük kit devri kullanılır.
Ücretli kaynak veya yeni mekanik gerekiyorsa mevcut işin içine gizlenmez.
Kabul kanıtı yoksa durum açık yazılır; bu satırlar uygulanmış gibi işaretlenmez.

| Kimlik | Üst aşama | Oyunda görünen teslim | Sorumlu alan | Geçiş kanıtı |
|---|---|---|---|---|
| Q01 | P4.7 / #159 | Kara ve deniz için iki kısa hedef kalite örneği | Mert kaynak, Utku yerleşim, Mehmet kamera/ölçüm | Hareketli gündüz/gece görüntüsü; kullanıcı görsel değerlendirmesi |
| Q02 | P4.9 / #161 | Oyuncu elleri, yüzme, ekipman, av ve teslim eylemleri | Mehmet; Mert rig/ses, Utku temas | Yerel/arkadaş kamerası, iptal/ret ve 1/2/4 oyuncu |
| Q03 | P4.10 / #162 | Ölçekli yerleşim: ev mahallesi → şehir merkezi → sahil yolu → liman | Utku; Mehmet erişim, Mert mimari | Kesintisiz ölçülmüş yaya turu, harita ve dört kişi geçişi |
| Q04 | P4.10 / #162 | Ev ve şehir sokaklarının tamamlanmış dış görünümü | Mert + Utku | İnsan ölçüsü, ara sokaklar, ön/orta/arka plan, gece turu |
| Q05 | P4.10 / #162 | Dört hizmetin ayrı gerçek mekânı ve çalışan esnafı | Mert; Mehmet etkileşim, Utku erişim | Kapıdan girip gerçek işlem; boş dekor tezgâhı yeterli değil |
| Q06 | P4.10 / #162 | Ortak evin dışı, içi ve görünür gelişimi | Mert; Mehmet dört kişi kullanım | PC/depo/yatak turu, iki ev seviyesi, save/reload |
| Q07 | P4.11 / #163 | Hareketli deniz yüzeyi ve kıyı suyu | Utku + Mert; Mehmet temas sunumu | Sahilden, yüzerek ve tekne üstünden hareketli kayıt |
| Q08 | P4.11 / #163 | Suya giriş ve derine inişte ışık, görüntü, nefes ve ses | Utku + Mert; Mehmet kamera/beden | Aynı rotada yüzey→sığ→resif→derin→yüzey kesintisiz tur |
| Q09 | P4.11 / #163 | Kum, kayalık, mercan, bitki ve batık habitatları | Utku + Mert | Her habitatın yakın/uzak turu; av ve dönüş yolunu örtmeyen yerleşim |
| Q10 | P4.11 / #163 | Beş ayırt edilir balık türü ve canlı yüzme davranışı | Utku; Mert model/rig/animasyon | Siluet/kuyruk/yüzgeç, dönüş/kaçış/sürü ve erişilebilir av testi |
| Q11 | P4.11 / #163 | Derinlik, tür, risk ve kazanç zinciri | Mert ekonomi/save, Utku habitat, Mehmet erişim | Yeni kampanyadan gerçek sığ/resif/derin avları ve satış dökümü |
| Q12 | P4.11 / #163 | Bulunabilir, ayrı görünümlü ve okunur boss karşılaşması | Utku davranış, Mert yaratık/ses, Mehmet oyuncu/ağ | İpuçlarından arena bulma; araştırma ve mücadele yolları |
| Q13 | P4.11 / #163 | Üç teknenin canlı yolculuk sunumu | Mehmet; Mert araç/ses, Utku rota | Koltuk/güverte, motor/kürek, su izi, gerçek gidiş/dönüş |
| Q14 | P4.12 / #164 | Aynı pakette bütün dünya ve içerik kabulü | Üç alan ve gerçek kullanıcı değerlendirmesi | İki bilgisayarda dört süreç/üç gün; solo tur; kalite/performans/kayıt |

Q kodları yeni faz veya açılmış GitHub issue numarası değildir. İşler sırayla küçük PR/build teslimlerine ayrılır; üst issue tek görsel veya katalog testiyle kapanmaz. Her paketin sorumlusu kendi co-op bağlantısını da teslim eder.

## Q01: Kaliteyi önce hareketli örnekte sabitle

Kara örneğinde bir gerçek ev/dükkân bölümü, insan, el ve sahil geçişi; deniz örneğinde hareketli su, küçük mercan/kaya habitatı ve bir doğru animasyonlu balık bulunur. Aynı donanım/kamera/çözünürlükte önce/sonra video ve kare süresi raporu gerekir. Kuru ekran görüntüsü su veya yüzme animasyonunu kabul ettirmez.

Kara ve deniz örnekleri bağımsız değerlendirilir. Birinin beğenilmesi diğerinin bütün haritaya yayılmasına izin sayılmaz. Kullanıcının kaynaklarının hedef kaliteye yetmeyenleri manifestte eksik olarak gösterilir; sırf içe aktarıldı diye tamamlanmış sanat yazılmaz. Ücretli varlık/servis seçimi ayrıca yetki ister.

## Q03–Q06: Uzak sahil ve gerçek mekânlar

- Ortak ev şehrin yerleşim mahallesindedir. Şehir merkezinde meydan, hizmet sokakları ve ayrı bina kütleleri; arada okunur bir sahil yolu; kıyıda kumsal, çalışma alanı ve liman vardır. Ev, bütün esnaflar ve su birkaç adımlık aynı test alanında toplanmaz.
- Ölçek çalışmasının ilk adayı ev–iskele ana yolunda 300–500 m gerçek yürünebilir yol, mevcut 4 m/sn hızla yaklaşık 75–125 saniye kesintisiz yürüyüştür. Bu sayı tasarım adayıdır, kabul edilmiş ölçü veya süre taahhüdü değildir. Eski 30–60 saniyelik kısa rota varsayımının yerini bu ölçülecek aday alır. Şehir merkezi içindeki satış–ekipman turu kısa kalır; liman hizmeti kıyıda kalabilir.
- Önce gerçek kontrolcüyle ölçek blokajı kabul edilir, sonra ayrıntılı model yerleşir. Şehir çapı yalnız bir boş koridor uzatılarak büyütülmez. Ev sokağı, meydan, bina araları, sahile inerken görüşün açılması ve geri dönüşte tanınır şehir silueti ayrı kompozisyonlardır.
- Uzun yol gerçek av/satış döngüsünde ölçülür. İlk gelir, küçük çanta, gündeki tekrar sayısı ve solo oyun angaryaya dönüşürse yol/tempo birlikte düzeltilir; normal yolculuğa teleport veya ücretsiz ekonomik fixture eklenmez. 00:00 kapanışı için dönüş uyarısı gerçek en uzun erişilebilir rota süresiyle değerlendirilir.
- Balıkçı: girilebilir çalışma alanı, tartı, av sergisi/soğutma-depolama sunumu ve gerçek teslim. Ekipmancı: iç raflar, elde incelenebilir ölçek, mevcut kataloğa bağlı ürünler. Medya hizmeti: görünür çalışma masası/ekran ve gerçek mevcut hizmet. Tekne satıcısı: liman ofisi veya tersane çalışma mekânı, tekne/rota kataloğu. Hepsi kendi tabelası/cephe/ışık/ses kimliğini taşır.
- Evde dışarıdan okunur çatı/kapı/pencere, içeride yaşanır odalar, dört yatak, ortak depo, PC, ekipman ve trofe alanı vardır. Kapıdan girme/çıkma, dört kişinin iş yapması ve gece aydınlatması gerçek build'de değerlendirilir. İki ev seviyesi ve üç kasaba gelişimi yalnız HUD sayacı değil ortam farkı üretir.
- Normal ev–şehir–sahil akışı yürüyerek kesintisizdir. D07 gün sonucu yeniden yerleştirmesi günlük yolculuğu atlayan bir menüye dönüştürülmez. Harita sınırı, route/berth, safe return, hizmet/yatak/PC anchor'ları ve eski save kimlikleri geometriyle birlikte taşınır.

## Q07–Q09: Su ve habitatlar

**Yüzey:** uzaktan düz boyalı levha gibi görünmeyen dalga/normal hareketi; bakış açısına uygun yansıma ve saydamlık; sığdan derine renk/geçirgenlik; kıyı köpüğü, dalga vurması ve ses. Sakin/rüzgârlı günler görünüm/hareket/ses farkı verir. Sabit rota tekne çevresinde hareketle ilişkili su izi ve sıçrama vardır. Görsel dalga, yüzen oyuncunun veya dört koltuğun fizik/otorite konumunu ikinci kez taşımaz.

**Giriş/iniş:** su çizgisinin kameradan geçişi, kırılma/ışık ve ses değişimi, yakın kabarcık/nefes; sığda görünen taban ve ışık desenleri; aşağı indikçe yönlü ışığın azalması, değişen renk/ses ve yer yer parçacık. Derinlik hissi yalnız bütün ekranı mavi/siyah yapmakla sağlanmaz. Kayıt klibi gerçek oyun kamerasındaki aynı ortamı içerir. Yüzey çizgisi, clipping, şeffaf nesne sırası ve farklı kalite ayarları test edilir.

**Habitat:** kıyıda kum/küçük kayalar ve kontrollü bitki; resifte farklı mercan biçimleri, büyük kaya/kemer, bitki/sünger kümeleri ve küçük balık hareketi; derinde farklı taban, batık/yarık ve arena işaretleri. Her yüzey aynı mercanla döşenmez; hedef coğrafya/sanat yönüne uygun set seçilir. Bitkide hafif su hareketi olur, sert kaya veya mercan gövdesi rastgele sallanmaz. Mercan/bitki collider ve görüşü avı erişilemez hâle getirmez; dekor sürüleri avlanabilir tür sayısı diye raporlanmaz.

Kabul: yüzey/giriş/üç derinlik kesimi gündüz ve gece aynı gerçek rotada görüntülenir; su altı/su üstü ses geçişi dinlenir. Dört süreçte balık, oyuncu ve tekne görünürlüğü ile gerçek clip capture açık kare süresi ölçülür. Hedef donanım henüz seçilmediyse performans kabulü boş bırakılır.

## Q10: Balığın model ve hareket kalitesi

Önce bir tür uçtan uca: uygun mesh/material, kuyruk/yüzgeç yüzme döngüsü, dönüş ve hız geçişi, kaçış, darbe/ölüm/av alma; sonra kalan türler aynı disiplinle bağlanır. Gövdeyi bir bütün olarak ileri kaydırmak veya bütün balıkları aynı görünümün renk kopyası yapmak yeterli değildir. Rig ve klip uygunluğu kaynakta incelenir; genel procedural deformasyon her modele zorla uygulanmaz.

Türler normal hızda uzaktan siluet/boyut, yakında hareket ve habitatla ayrılır. Sürü uygunsa host kararını izleyen sınırlı grup hareketi kullanılır; her balığa ağır bağımsız arama açılmaz. Balık kaya/mercan/batık içine girmez, av erişilebilir kalır. Yaşayan balık state'i hostta, kozmetik kuyruk hareketi istemcide olabilir; ikinci ölüm/ödül otoritesi kurulmaz. Her türün prefab→spawn→client→av→satış→çekim→ansiklopedi zinciri ayrı doğrulanır. Balık adedi ve gerçek tür sayısı ayrı raporlanır.

## Q11: Derine gitmenin karşılığı

Tasarım yönü: sığ su ücretsiz güvenilir başlangıç geliri; resif yeni türler ve daha yüksek beklenen av değeri; derin kesim daha seyrek, daha değerli türler ve artan dönüş/oksijen riski. Çalışma dağılımı sığda iki, resifte iki, derinde bir normal tür ve ayrıca boss'tur. Türlerin isimleri/habitatı R05 ile somut veriden seçilir; mevcut ton balığı sadece görseli var diye otomatik derin tür ilan edilmez.

Bir balığı aynı derinlikte yüzdürmek onun fiyatını değiştirmez. Değer, doğrulanmış SpeciesId ve kabul edilmiş fiyat tablosundan gelir; sığ balığı aşağı taşıma, client'ın derinlik iddiası veya oyuncunun bir kez derine inmesi gelir çarpanı yaratmaz. Ağırlık kalite/ödeme formülüne alınacaksa mevcut av başı fiyat semantiği ayrıca Mert tarafından kararlaştırılır; bu belge yeni fiyat/çarpan uygulamaz.

Her tür için minimum tasarım tablosu: stable ID, okunur adı/modeli, habitat/derinlik aralığı, bolluk, hareket/kaçış, ağırlık aralığı, temel satış değeri, erişim/ekipman ve çekim karşılığı. Mevcut beş tür hedefi önce gerçekten doldurulur. Üç aynı 120 fiyatı veya beş kataloğa karşı tek habitat yeterli değildir.

Kabul: fixture'sız kampanyada sığ/resif/derin gerçek av ve kıyıya dönüş; para/satış kaydıyla tür bazında değer farkı; tur süresi, başarısızlık ve masraf dâhil net gelir. Daha derinin av başına değer farkı görünür olur, fakat risk yüzünden daha düşük net gelir çıkması saklanmaz. Solo ve ekip ayrı ölçülür. Kıyı avı kamera, tamir veya satın alınmış tekne istemez; ulaşılamayan hedef siparişi üretilmez. Eski kayıtta fiyat değişimi daha önce ödenmiş capture'ı tekrar ödetmez.

## Q12: Boss nerede ve oyuncu nasıl bulacak?

Önce yayınlanan oyuncu build'inin commit'i belirlenir; o pakette ilerleme kilidi, söylenti, üç fiziksel iz, keşif haritası, gerekli rota/tekne ve arena aktivasyonu uçtan uca denetlenir. Geliştirici testi ile açılan arena, yeni oyuncunun bossu doğal olarak bulabildiğinin kanıtı değildir. Yanlış ipucu/kilit veya eksik model varsa ürün kusuru olarak kaydolur; kullanıcıya “kodda var” cevabı kabul sayılmaz.

Boss derin batık/oyuk çevresinde normal beş türden ayrı siluet/ölçek/beden hareketine sahiptir. Uzak siluet ve çevresel iz merak oluşturur; yaklaşma/uyarı, üç farklı saldırı hazırlığı, saldırı etkisi, kaçınma/siper ve saldırı sonrası fırsat görülebilir. Zayıf nokta HUD metnine bağımlı değildir. Araştırma için geçerli gerçek çekim ve mücadele için host doğrulamalı vuruş ayrı tamamlanabilir yollardır. Yeni ikinci boss veya zorunlu çok oyunculu eşzamanlı düğme kapısı eklenmez.

Kabul: yeni kampanyada ipuçlarından yardım almadan bulma; solo ve co-op araştırma/mücadele videosu; yenilgi/geri çekilme/yeniden giriş ve güvenli dönüşte tek ödül; host yeniden açınca ilerleme korunur. Sadece can çubuğu/çarpışma küresi veya büyük normal balık nihai model ve davranış kabulü değildir.

## Bağımlılık, üretim ve süre

P4.6 teknik temel ve P4.7 hedef kalite kapıları korunur; #187 için mevcut dar istisna ayrıca korunur. Q01 kara/deniz örneğinin tasarımı, kaynak envanteri ve Q03 ölçek çizimi mevcut kapılar beklerken hazırlanabilir; bu belge sonraki faz kodunu açmaz. P4.9, P4.10, P4.11 üst faz uygulama sırası değiştirilmez. Su örneği P4.7'de gösterilir; bütün su/habitat uygulaması P4.11 içinde paket paket teslim edilir. Böylece suyun hedef görünümü son kabul gününe kadar belirsiz bırakılmaz.

P4.10 iç sırası Q03 ölçek → Q04 mimari ile Q05/Q06 mekân setleri → gerçek günlük tur. P4.11 iç bağımlılıkları Q07 su + Q08 geçiş → Q09 habitat → Q10 tür + Q11 değer → Q12 boss; Q13 tekne sunumu aynı route/world verisini tüketir. Her alan diğer kişinin ortak sahnesini paralel düzenlemez; parça/prefab devri ve tek bir sahne entegrasyonu planlanır.

Her Q işinin açılabilir görev metni: kullanıcı sorunu, sorumlu alan ve etkilenen dosyalar, mevcut kaynak, eksik varlık, ön koşul, minimum gerçek oyun sonucu, 1/2/4 oyuncu/kayıt etkisi, kabul videosu ve bilinen açıklar. Kaynak/model/rig maliyeti görülmeden bitiş günü veya “dört küçük iş kaldı” tahmini verilmez. Mert bütün sanatın tek başına üreticisi sayılmaz; tedarik ve entegrasyon alanlara bölünür, gerçek destek ataması kaydedilir.

## Q14: Kapanış için ürün kontrol listesi

- [ ] Ev şehrin içinde; merkez ve sahil ayrı okunur alanlar, günlük yolculuk kesintisiz ve ölçülmüş.
- [ ] Dört hizmet gerçek mekânında çalışıyor; ev içi/kapı/PC/depo/yatak ve yükseltmeler kabul edildi.
- [ ] Hareketli su, kıyı teması, suya giriş ve üç derinlik kesiminin ışık/sesi kabul edildi.
- [ ] Mercan/bitki/kaya/batık habitatları dolu, farklı ve av/geri dönüş açısından erişilebilir.
- [ ] Beş normal tür gerçekten ayrı model/hareket/habitat ve tüm ürün yollarında doğrulandı.
- [ ] Sığ/resif/derin türlerin değer/erişim/riski ve net tur geliri gerçek satışlardan görüldü.
- [ ] Boss yeni kampanyadan bulunabilir, araştırma ve savaş yolları görünür/oynanabilir.
- [ ] Sandal/diğer tekneler, eller/eylemler ve yerel/arkadaş görünümü son kaliteyle kabul edildi.
- [ ] Eski 2/3 kayıt, kopma, 00:00, clip/diskten devam ve tek ödül/ödeme korunuyor.
- [ ] Aynı sabit build'de gerçek iki bilgisayar/dört süreç/üç gün ve solo değerlendirme, kullanıcı grafik/ses/tempo kabulü ve donanımı belirtilmiş performans kanıtı var.

Bu kutuların hiçbiri bu plan değişikliğiyle işaretlenmez. Mevcut #161/#162/#163/#164 üst işleri korunur; Q alt işleri açıldığında gerçek GitHub bağlantıları ayrıca eklenir. Issue açılması, PR birleşmesi ve oyuncu kabulü ayrı kayıtlardır.
