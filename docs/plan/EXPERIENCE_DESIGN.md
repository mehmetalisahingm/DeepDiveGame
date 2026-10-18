# DeepDive ayrıntılı oyuncu deneyimi tasarımı

13 Ekim 2026 — Plan 3.1 ayrıntısı. Bu belge çalışma tasarımıdır; uygulama, performans veya ekip kabulü kanıtı değildir. Teslim sırası [P4.7–P4.12](PLAYER_EXPERIENCE_EXPANSION.md), üretim parçaları [PRODUCTION_BLUEPRINT](PRODUCTION_BLUEPRINT.md), ileri seçenekler [FUTURE_ROADMAP](FUTURE_ROADMAP.md) içindedir.

## 1. Oyunun vermesi gereken his

Dört arkadaşın aynı kıyı kasabasında bir hayat kurması: sabah hazırlık, denizde ortak keşif ve risk, kıyıda kazancın somut karşılığı, akşam gerçek görüntüleri birlikte izleme. Oyuncu sadece menü açan veya puan biriktiren kişi değildir; nesneyi tutar, iş yapar, ortamı tanır ve arkadaşının ne yaptığını anlar.

Öncelik sırası:
1. İlk on dakikada etkileşim, hareket ve çevre anlaşılır.
2. İlk gelir kamera veya tekne gerektirmez.
3. Ekip iş bölüşebilir; solo oyuncu aynı zinciri oynar.
4. İlerleme dünyada görünür, sayılar bunu açıklar.
5. Keşif yeni karar üretir; büyüklük sadece yürüyüş süresi değildir.
6. Görsel ve ses birbirine uyar; yüzey süslemesi bozuk eylemi örtmez.

Mevcut sınırlar: bir kasaba/ev/bağlantılı deniz, dört hizmet, üç tekne, beş normal tür ve bir boss; iki hava, bir akıntı; iki ev seviyesi ve üç kasaba gelişimi. Plan bu sınırları gizlice çoğaltmaz. Açlık/borç/zorunlu günlük giriş/gerçek sosyal medya sistemi yok.

## 2. İlk oturumun oyuncu yolculuğu

Aşağıdaki gerçek dakika aralıkları ilk kullanıcı testinin tempo varsayımıdır. Zorunlu zamanlayıcı, teslim tarihi veya oyuncuya garanti değildir.

| İlk oturum bölümü | Oyuncunun gördüğü | Yapabileceği | Gözlem ölçütü |
|---|---|---|---|
| 0–2 dk | Evin açık kapısı, dört yatak, ortak depo, uzaktan kıyı işareti | Hareket et, arkadaşını gör, çıkışı bul | Yardımsız çıkış; UI yüzünden görüş kapanmıyor |
| 2–5 dk | Dükkân tabelaları, iskele yönü, yıpranmış sandal | Sandalı incele veya ücretsiz kıyı avına git | Tamir zorunlu kilit sanılmıyor |
| 5–12 dk | Dünyada görünür parça ipuçları, sığda canlı hareketi | Parça taşı/tamir et veya ilk balığı avla | Hedefi bulma, eylem ve kapasite anlaşılır |
| 12–20 dk | Tamir edilen gövde veya dolan çanta; balıkçı tezgâhı | İlk sefer/ilk satış/ekipman kararı | İlk kazanç ve yapılan işin sonucu okunuyor |
| Oturumun devamı | Ortak harita, ilk kamera, gün/hava/geri dönüş uyarısı | Daha uzağı araştır, kayıt yap veya kısa dalışları sürdür | Tek doğru oyun tarzı dayatılmıyor |
| İlk akşam | Ev ışığı, gerçek klip arşivi, yataklar, gün özeti | İzle/yayınla/depo/uyku | Ertesi gün geliri ile eldeki para ayrılıyor |

Öğretici zorunlu ekran serisi değildir. İlk ilgili etkileşimde bir kısa açıklama, gerekirse tekrar okunur pano kullanılır. Dört oyuncuya aynı büyük açıklama üst üste gösterilmez. Geç katılan kişinin bilmediği şey onun yerel ipucu geçmişinde gösterilir; kampanya görevi yeniden başlatılmaz.

Başlangıç eşyası: mevcut ücretsiz kıyı seti; ücretli kamera ayrıca alınır. Ücretsiz onarım malzemeleri yalnız sandalla ulaşılan yere koyulmaz. Sandalın incelenmesi görev hedefini görünür kılar; oyuncu önce avlanırsa yaptıkları sonradan tanınır, tekrar yaptırılmaz.

## 3. Kasaba yerleşimi ve gerçek büyüklük

### 3.1 Bölge kompozisyonu

| Bölge | Oyun amacı | Görsel kimlik | Etkileşim | Görsel sınır |
|---|---|---|---|---|
| Ortak ev | Dinlenme, hazırlık, arşiv | Denizci eşyaları, sıcak ahşap, yaşanmış masa | Dört yatak, depo, PC, pano/trofe | Dar kapı ve yatak koridoru dört kişiyi kilitlemez |
| Ev sokağı | Kasabaya bağlanma | Taş/ahşap, bahçe kenarı, kıyı görüşü | Yönlendirme; kozmetik çevre | Her dekor kapısı kullanılabilir sanılmaz |
| Meydan | Ekip buluşması, hizmet kararı | Ayrı tabela/cepheler, orta görüş alanı | Ekipman, balıkçı, kayıt alıcısı | Dört UI aynı anda ekranı doldurmaz |
| Liman yolu | Deniz hazırlığı | Halat, kasa, kuru ağ ve işaretler | Tekne satıcısı, iskele, rota bilgisi | Dükkânların başka bir kopyası yaratılmaz |
| Kumsal | Ücretsiz başlangıç | Su çizgisi, dalga, sığ taban ve parça ipuçları | Onarım, yüzerek giriş/çıkış | Parçalar VFX içinde görünmez hâle gelmez |
| Yakın su | Öğrenme ve gelir | Aydınlık kum, küçük kaya/bitki | En az iki normal tür hedefi | Nadir/derin içeriği açığa çıkarmaz |
| Resif | Yeni keşif ve ekipman değeri | Kaya kemeri, bitki/sürü, belirgin demirleme | İki ek tür ve keşif hedefi | Dönüş yönü renk filtresine bağlı değildir |
| Derin/batık | Planlı risk/boss | Batık silueti, düşük ışık, açık siperler | Beşinci tür, üç boss izi, karşılaşma | Karanlık okunurluğu yok etmez |

Türlerin bölgelere 2/2/1 dağılımı çalışma önerisidir; gerçek tür/oksijen/habitat verisiyle doğrulanır. İsimler biyolojik gerçeklik iddiasıyla uydurulmaz.

### 3.2 Mesafe kararları

Mevcut oyuncu kodunda yürüyüş hızı 4 m/s'dir. 60–90 saniye kesintisiz yürüyüş 240–360 m eder. Önceki plan bu sayıyı nihai şehir boyutu olarak kilitlemez.

Yeni yerleşimde ayrı ölçülür:
- En kısa ev–iskele yolu, tabela ve dönüşlerle gerçek sürüş süresi.
- İlk oyuncunun durup baktığı tam deneyim süresi.
- Satış–ekipman–liman günlük hizmet turu; rahatlık için kompakt kalır.
- Alternatif sokak/uzun sahil keşif turu; günlük zorunlu yol değildir.
- Tahta taşırken ana yol; ağır taşıma yavaşlatması eklemeden önce tekrarın sıkıcılığı değerlendirilir.

Başlangıç çizim hedefleri: hizmetler arası 10–25 saniye; ev–iskele doğrudan yol 30–60 saniye; çevreye bakılan ilk tur 60–90 saniye; isteğe bağlı sahil/yan yol daha uzun. Sayılar birlikte test edilip ayarlanır. Hiçbiri uygulamaya başlamadan kabul edilmiş metrik değildir.

Daha dolu görünüm için en az üç ayrı kompozisyon: evden meydan, meydandan su, iskeleden kasaba. Her birinde ön/orta/arka plan ve okunur hedef vardır. Uzun dümdüz boş bir koridor veya her yerde aynı kasa tekrarı yeterli değildir.

### 3.3 Çarpışma ve yön bulma

Her oyun rotası gerçek CharacterController ile yürünür. Kontrolcü yarıçapı, skin, stepOffset ve dönüş genişliği ölçülür; sabit tasarım tahmini testin yerine geçmez. Kapılar iki oyuncu karşılaşınca tıkanmaz; dört oyuncunun yatak çıkışı birbirinin spawn'ı içine yerleşmez.

Kıyıdan çıkış yalnız ortadaki test rampasıyla sınırlanmaz; tanımlanmış geri dönüş rotası görünür ve erişilebilir kalır. Geometri değişirken su hacmi, güvenli dönüş, harita sınırı ve rota waypoint'leri birlikte kontrol edilir.

Tabela, siluet, ışık ve zemin yönü birbirini destekler. İşaret her gizli balığın yerini göstermez. Erişilemeyen sınır doğal kaya/çit/kapalı patikayla anlatılır; görünmez duvar kullanıcıya tek sınır sinyali değildir.

Normal ev–şehir–sahil yolculuğunda teleport yok. Sabah/başarısız dalışın D07 yerleşimi ayrı gün sonucu olarak açıklanır; bu istisna günlük yürüyüşü atlayan bir menü komutu yapmaz.

## 4. Sandal tamirinin ayrıntılı tasarımı

### 4.1 Nesne ve görünüm

İlk bakış: bir veya daha fazla açık gövde yarığı, kırık oturma parçası, yıpranmış bağlantılar. Sandal ile yalnız dekor amaçlı hurda ayrılır. Eksik yuvalar yakınlıkta ince işaretle görünür; uzaktan sürekli parlak kontur yok.

Tahta elde insan ölçeğinde görünür; hedefi ve yolu örtmez. Önce tek oyuncu taşıyabileceği boyutta parça seçilir. Solo oyuncuyu zorunlu iki kişi taşıma kilidine sokma. Bırakılan parça eğimli zeminden suya kayıp kaybolmamalı; serbest fizik yerine güvenli bırakma pozu kullanılabilir.

Keser, vida/çivi ve tahta etkileşimde tutarlı bulunur: çalışma seti sandal yanında görünür; kuşanınca ele gelir, bitince yerine döner. Tamir aleti ücretsiz temel görev aracıdır; yeni satın alma/crafting kapısı değildir.

### 4.2 Adım adım akış

| Adım | Girdi ve kontrol | Yerel sunum | Arkadaşın gördüğü | Kalıcı sonuç |
|---|---|---|---|---|
| İncele | Hedefe yaklaş, etkileş | Kırık yer/eksik kalem/ipucu | Oyuncu sandala dönük | Görev zaten açık olabilir; ek ödül yok |
| Al | Yakın ve görünür uygun parçaya E | El uzanır, parça tutuş noktasına gelir | Aynı taşıyıcı/nesne | Host tek sahip; parça henüz katkı değil |
| Taşı | Yürü, gerektiğinde bırak | Kontrollü el salınımı | Gövde/nesne tutuşu | Konum değil tekil sahiplik önemlidir |
| Yerleştir | Uygun yuvaya bak/E | Parça yuva pozuna oturur | Yuva meşgul, oyuncu çalışıyor | Rezervasyon geçici |
| Çak | Kısa tutma veya doğrulanmış darbeler | Keser, temas, çivi, ses/talaş | Darbe ve parçanın sabitlenmesi | Son kabul edilen adımda tek katkı |
| Bitir | Gerekli adımlar tamam | Alet bırakılır, el serbest | Açık gövde kapanır | Mask/revision + save; eski isteğe tekrar ödeme yok |
| İptal | Uzaklaş/bırak/Closing/kopma | Tutarlı el geri dönüşü | Çalışma biter | Tamamlanan katkı korunur; yarım işlem kayıp yaratmaz |

Tuşlar varsayılan çalışma tasarımıdır; mevcut input standardı ve yeniden bağlama planı ile kesinleşir. Her çivi için hassas piksel nişanı veya gerçek çivi fiziği zorunlu değildir. Animasyon kısa ve kesilebilir; kontrolü uzun süre kilitlemez.

### 4.3 Her tahtada görünür ilerleme

Kullanıcının beklentisi yalnız son katkıda sıçrayan sandal değildir. Bir gövde onarım grubunda çalışma önerisi olarak üç tahta yaması bulunur; her yerleştirilen/sabitlenen tahta görünür olur.

İki katman ayrılır:
- Geçici çalışma pozu: parça yuvasına oturur, çivi/darbe ilerler. Kesilebilir, henüz katkı değildir.
- Kalıcı tamamlanan alt parça: tahtası sabitlenmişse modelde kalır. Tasarım tercih edilirse stable alt-slot kimlikleriyle kaydedilir; sadece ana 3-bit mask bunu temsil edemiyorsa şema tasarlanır.

Bu, önceki planın katkı başına tek transaction yaklaşımını ayrıntılandırır. Varsayılan düşük maliyet seçeneği üç ana grubun her birinde bir görünür sabit parça/tahta ve 2–3 kısa darbedir. Üç gövde yaması ayrı kalıcı ilerleme gerektiriyorsa aşağıdaki R02 kararı alınmadan kodlanmaz. İki yöntemde de her tamamlanan tahta oyuncuya görünür karşılık verir; birden çok çivi vuruşu ayrı ekonomi ödülü değildir.

### 4.4 Eski motor/tank tutarsızlığı — R01

Mevcut başlangıç aracının adı kürekli sandal; teknik onarım kimlikleri gövde/motor/yakıt tankıdır. Sadece sanat ekleyerek tutarlı hâle gelmez.

**Önerilen yeni sunum:** başlangıç kürekli sandalında gövde tahtası, oturak/destek ve kürek bağlantısı olarak üç mantıksal tamir grubu. Eski teknik kimlikler kayıt uyumluluğu için saklanabilir; görünür ad eşlemesi kontrollü katalog göçüyle değiştirilir. Motor/yakıt görünümü satın alınan motorlu tekneye aittir.

Bu öneri mevcut P4.8 gövde/motor/tank tasarımının alternatifidir; kesinleşene kadar mevcut ID/ekonomi değişmez. Mehmet tasarımda son karar, Mert kayıt/quest, Utku world/catalog etkisini birlikte kaydeder. 2/3 eski kayıt silinmez; tamamlanan bitler seçilen yeni gruplara deterministik eşlenir, tekrar alış/satış yaratmaz. R01 çözülmeden motor/tank model üretimine girilmez.

### 4.5 Co-op, kayıt ve teknik sınırlar

Kod incelemesinde doğrulanan bağımlılık: `NetworkPlayer.HandlePickup → BoatPartClaim.TryClaimFound → TownServiceHandler.HandleFoundPart → EconomyManager.TryContributeBoatPart` hattı bulunan parçayı hemen kalıcı onarım maskesine ekliyor. Dükkânın Purchased yolu da aynı contribution işlemini çağırıyor. Bu nedenle fiziksel tamir yalnız animasyon overlay'iyle gerçek eyleme dönüşmez.

**R11 edinme/tamir ayrımı önerisi:** satın alınmış veya bulunan parça tekil, kullanılmamış ortak onarım malzemesi olarak edinilir; ücretsiz pickup bitmiş onarım değildir. Satın alma anında bir kez ücret alınır ve sahiplik kaydedilir; son doğru tamir adımı parçayı tüketip katkıyı bir kez kaydeder. Parçayı taşımaktan vazgeçmek yeni para iadesi/satış hakkı yaratmaz; malzeme elde veya ortak emanet havuzunda kalır. Yeni save alanı varsa schema göçü mevcut completed mask'i korur, geçmiş ücretleri tekrar kesmez.

Bu çalışma önerisi R11'de kesinleşmeden mevcut pickup/purchase kodu değiştirilmez. Edinme, çalışma rezervasyonu ve contribution ayrı request alanlarıyla idempotent olmalı; aynı requestId başka türde yanlışlıkla replay olmaz. Satın alma save'i başarısızsa bakiye/sahiplik rollback olur; contribution save'i başarısızsa parça kaybolmaz. Eski 3/3 kampanya yeniden fiziksel tamire zorlanmaz.

Taşıyıcı parça ve çalışma yuvası host tarafından tekil sahiplenilir. Bir yuvada bir aktif çalışan; diğer oyuncular farklı yuvada çalışabilir, izleyebilir veya avlanabilir. Sadece birinin görev yapabildiği global kilit yok.

İstekler requestId + partId/slotId + campaign day + revision bağlamıyla doğrulanır; istemci sadece niyeti gönderir. Host mesafe, görüş, aşama, aktiflik, mevcut yük ve rezervasyonu kontrol eder. Yeni API isimleri uygulama öncesi CONTRACTS'e yazılır.

Guest koparsa yarım parça ortak emanet veya güvenli dünya bırakma noktasına döner. Host koparsa son sağlam checkpoint'ten devam edilir. Closing başlamışsa yeni çalışma alınmaz; kabul edilmiş son katkı kayıt transaction'ına girer, kısmi animasyon tamamlanmış sayılmaz.

Kontrol listesi: yeni 0/3; üç ayrı 1/3; üç ayrı 2/3; 3/3; her aşamada bırak/kop; iki kişi aynı parça; aynı request replay; kayıt sırasında disk hatası; satın alınmış parça; suya bırakma; tamir sırasında 00:00; geç katılmada doğru mesh.

## 5. Bütün etkileşimlerde ortak kalite

| Eylem | Dünyada başlangıç | El/beden | Sonuç/ses | İptal/ret |
|---|---|---|---|---|
| Ekipman al/kuşan | Dolap/raf/mağaza | Uzanma ve kısa kuşanma | Doğru kamera/zıpkın/çanta | Başka oyuncuya tahsisli eşya ikiye bölünmez |
| Av al | Gerçek yakalanmış av | Kısa tutuş/çantaya alma | Çanta kapasitesi/av geri bildirimi | Dolu çanta avı tüketmez |
| Balık sat | Balıkçı tezgâhı | Teslim/tezgâh jesti | Teklif→onay→ödeme; kısa tartı sesi | Uzak/tekrar işlem ikinci ödeme yok |
| Kamera kullan | Eldeki gerçek kamera | İki el, hedef alanı açık | Kayıt ışığı/süre/ortam sesi | Depolama doluysa başarı gösterilmez |
| Tekneye bin | İskele/koltuk erişimi | Adım/tutuş/oturma | Koltuk durumu, gövdeyle birlikte hareket | Hareket sırasında atlama/inme reddi açıklanır |
| PC aç | Fiziksel masa/ekran | Yakın kullanım | Gerçek klip/playback kontrol sahibi | Mesafe/işgal/medya eksikliği ayrı mesaj |
| Depo kullan | Dolap | Uzanma/saklama | Tekil eşya raf/envanter bağlantısı | Çifte sahiplik/kapasite kontrolü |
| Yatağa gir | Fiziksel yatak | Yatma/kalkma | Arkadaş görünümü, uyku kapısı | Başkasının yatağı/Closing doğru açıklanır |

Sunum ekonomik işlemi onaylamaz. El hareketi başladığı anda ödeme/av tüketimi gerçekleşmiş sayılmaz. Yerel niyet için küçük tepki mümkündür; host reddederse nesne ve poz tutarlı geri döner.

Kasabada oyuncuların birbirini dar yerde kalıcı kilitlememesi kontrol edilir. Kurtarma/iten fizik gibi yeni mekanik eklemeden collider ve geçiş genişliği çözülür.

## 6. Dört esnaf ve ortak ev

### 6.1 Esnaf kimliği

| Hizmet | Görsel araçları | Küçük çalışma hareketi | Oyuncunun kararı |
|---|---|---|---|
| Ekipman | Kamera/tüp/palet rafı, okunur tabela | Raf kontrolü/tezgâha dönme | Ne alıyorum, kime tahsis ediliyor, ne kazandırıyor? |
| Balıkçı | Tartı, kasa, buz/bez görseli | Balık kontrolü/tartı jesti | Hangi avı satıyorum, ödeme ne kadar? |
| Kayıt alıcısı | Ekran/not defteri, araştırma işaretleri | Kayıt inceleme jesti | Hemen lisans geliri mi, evden kanal beklentisi mi? |
| Tekne satıcısı | Liman çizimi, üç gövde önizlemesi | Gövde/rota gösterme | Sahiplik, aktif tekne, erişim ve dönüş süresi |

Dört görevli sınırlı uyumlu insan setinden üretilir. Tam diyalog ağacı, seslendirme veya günlük NPC takvimi mevcut teslimde yok. Çalışma animasyonu alışveriş otoritesi veya yeni saat kilidi olmaz.

Satış ekranı mevcut ürün/kapsam kuralını yansıtır. Kısmi satış yeni ekonomi özelliği gerektiriyorsa bu teslimin sanat işi içine gizlenmez; mevcut tüm bekleyen av teslimi açık anlatılır.

### 6.2 Evde ortak yaşam

Dört yatak ve depo/PC arasında geçiş; duvar/masa ekipmanın içine kamera sokmaz. Arkadaş yatağa girerken, PC'ye yaklaşırken ve depodan eşya alırken anlaşılır poz görür. Aynı PC'de kontrol sahibi ayrılırsa hak serbest kalır.

Başlangıç evinin ucuz görünmesi kötü sanat anlamına gelmez: yıpranmış ama tutarlı malzeme, az eşya, boş sergi alanı. İki gelişim kademesi sergi/raf/depo ve ışık farkı üretir. Gelişim alındığında yerinde ön/sonrası görünür; görev performansını zorunlu bakım borcuna dönüştürmez.

İlk yayın/ilk tür/boss trofesi mevcut tekil olaylardan doğar. Başarı duvarı kopya ödül üretmez, eski kayıt yüklenince iki nesne eklenmez. Tekrar üretim dünyası runtime setup kullanıyorsa idempotent nesne kimliği gerekir.

## 7. Deniz canlıları ve keşif

### 7.1 Tek tür şikâyetini çözme planı

Kodda katalog, prefab, sahne, gerçek spawn ve oyuncu erişimi beş ayrı kanıttır. Birinin varlığı diğerini garanti etmez. Ön incelemede SeaBass.asset ve günlük hedef katalogunda sea_bass/sığlık bağlamı görüldü; bütün türlerin tek olduğu teşhisi henüz yapılmadı.

#155 için veri zinciri:
1. Tür dosyalarını ve runtime üretilen türleri say; gerçek build'de sayılan türleri ayrı raporla.
2. Her speciesId için prefab/model/animasyon/binding doğrula.
3. Host spawn sayısı ve bölge/derinlik kaydı; istemci mirroring.
4. Türün oyuncunun erişebildiği derinlikte gerçekten bulunup bulunmadığı.
5. Yakalama ve klipte aynı kimlik; ansiklopedi/günlük görev kataloğu.
6. Mevcut save veya sabit sahne fixture'ının çeşitliliği engelleyip engellemediği.

Kullanıcıya debug sayacı göstermek yerine ayırt edilir tür ve habitat gerekir. Teşhis için gizli koordinatlar yalnız geliştirici kanıtında tutulur; oyuncu haritasına sızmaz.

### 7.2 Beş normal türün tasarım rolleri

| Rol | Başlangıç davranışı | Oyuncu kararı | Kanıt |
|---|---|---|---|
| Yaygın sığ tür | Kolay fark edilir; kısa kaçış | İlk av/ilk kamera denemesi | Kamera/tekne olmadan avlanır |
| İkinci kıyı türü | Farklı siluet/tempo; kaya veya bitki yakınlığı | İlk çeşitlilik ve görev seçimi | İlk oturumda uygun şansla görülür; kesin rastgele garanti yok |
| Resif sürü türü | Sınırlı sürü sunumu | Kadraj/av/kapasite tercihi | Aynı sürü çoğaltılmış kayıt hakkı değildir |
| Resif avcı türü | Okunur yaklaşma/kaçınma | Risk ve korunma | Tehlike sadece hasar sayısı değildir |
| Derin/nadir tür | Düşük ışık, planlı rota | Ekipman/oksijen/çekim tercihi | Bossla karıştırılmaz; erişim doğrulanır |

Gerçek isim ve biyoloji World catalog sahibince seçilir. Yeni normal tür sayısı beşi aşmaz. İlk kıyı çeşitliliği oyuncu ilgisi içindir; bütün ileri türleri ilk dakikada açma zorunluluğu yok.

### 7.3 Alan ve dönüş okunurluğu

Kıyı: güneşli taban, küçük kayalar, yüzeye yakın ses. Resif: katmanlı kaya/bitki ve demirleme dönüm noktası. Derin: büyük batık silueti ve siperler; düşük ışıkta hedef seçilebilir.

Oksijen/taşıma/kamera kararları ile dönüş süresi birlikte çalışır. Kamera yükseltmesi derinlik izni değildir. Harita keşif/son gözlem gösterir; balık GPS'i olmaz. Hava rota kapatıyorsa çıkıştan önce açıklanır; denizdeki ekibin geri dönüşü kapanmaz.

Boss üç saldırı hazırlığına farklı beden/ses işareti verir; saldırı sonrası kısa fırsat. Solo oyuncu çekim ve mücadeleyi sırayla yapabilir. Her bir iz dünyada anlam taşır; yalnız üç aynı parlayan düğme dizisi olmaz.

## 8. Günler, ekonomi ve ilerleme

### 8.1 İlk günlerden sonra amaç

| Dönem | Çalışma hedefi | Ana seçim | Görünür gelişim |
|---|---|---|---|
| İlk gün | Kıyıyı öğren, ilk gelir/onarım | Av mı tamir mi, kamera şimdi mi? | İlk parça, ilk av, ilk ekipman |
| Erken günler | İlk yayın ve yakın sefer, ikinci tür | Hemen satış mı kanal beklentisi mi? | Gerçek arşiv, gün sonucu, sandal |
| Orta ilerleme | Resif, daha iyi ekipman/motorlu tekne | Kapasite, risk, ortak kaynak | Yeni gövde/rota, habitat, sergi |
| İleri ilerleme | Derinlik/boss araştırması veya mücadele | Dönüş planı ve kamera/av odağı | Araştırma/ganimet trofesi, araştırma teknesi |
| Tamamlanan ana zincir sonrası | Kayıt kalitesi, koleksiyon, farklı gün koşulları | Ekibin kendi hedefi | Dolu ansiklopedi/kanal/ev geçmişi |

Dönemler belirli gün sayısına kilitlenmez. İlk günde bütün ileri araçları bedava fixture ile görmek ekonomik kabul değildir. Sonsuz endgame içeriği vaat edilmez; tamamlanan kampanyanın ne sunduğu P4.12'de açık değerlendirilir.

### 8.2 Fiyat/gelir/tempo çalışması

Fiyatlar mevcut katalogdan çıkarılır, örnek tasarım sayıları gizlice kod yerine geçmez. Ölçüm hesabı:

- Net av turu = satış + doğrulanmış sipariş ödülü − gerçek harcama.
- Net medya günü = gerçekleşmiş yayın geliri + uygun sponsor − gerçek harcama.
- Hedef yükseltme turu = fiyat / medyan pozitif net gelir; solo ve ekip ayrı.
- Fiilî karar süresi = gidiş + keşif/av + dönüş + teslim; yürüyüş verimsizliği ayrı.
- Kanal bekleyen gelir net cüzdana yazılmaz; aynı klip NPC ve kanalda çift ödenmez.

Beş kullanıcı rotası ölçülür: solo yalnız av, solo kamera, iki kişi karışık, dört kişi ortak, başarısız dalış sonrası toparlanma. Ekip sayısıyla gelir sınırsız çarpılmamalı; ortak havuz/kamera tahsisi ve günlük kapasiteyle gerçek ölçülür.

Günlük sipariş/sponsor tek birer aktif görevdir. World uygunluğu erişilen tür/habitat/tekne/rota/havadan hesaplanır. Uygun hedef yoksa boş açık durum gösterilir; imkânsız görev doldurulmaz. Yalnız sahte skor veya panel düğmesi hedef tamamlamaz.

### 8.3 Tekrarı azaltma

Altı şablonun başka başlıkla sürekli aynı hedefe dönüşmesi incelenir: üç gün görev/habitat/koşul ve seçilen gelir yolu kaydı. Yeni şablon çoğaltmadan önce mevcut çeşitlilik, gece ve hava gerçek seçimi değiştiriyor mu görülür.

Uyku, arkadaşını beklemeye zorlayan belirsiz kilit değildir: kim etkin, kim uyuyor, denizde kim kaldı anlaşılır. Kopan/pasif kişi gün kapanışını kilitlemez. 22/23 uyarıları ve uzak rota dönüş süresi yeterince erken gösterilir.

## 9. Görsel üretim ve ses

### 9.1 Sanat kalite sözleşmesi

İnsan oranlı stilize kıyı, sıcak kuru ortam ve turkuazdan koyu maviye deniz. Siluet/renk/ayrıntı düzeyi ev, karakter, balık ve ekipmanda tutarlı. Rastgele ücretsiz paketlerin karışımı hedef değildir.

Her ana varlık teslimi: insan ölçeği; pivot/yön; oyuncu/animasyon tutuş noktası; collider; material referansı; gereken yakın/uzak detay; kaynak/lisans/.meta; gerçek gündüz/gece görüntüsü. LOD/doku boyutu sayıları hedef donanımda ölçülür; gelişi güzel kesin bütçe verilmez.

Tahta ve keser küçük varlık olsa bile oyuncu kamerasına çok yakındır; yakın malzeme/kenar/tutuş kontrolü gerekir. Arka plan cepheleri az geometriyle çeşitlenebilir; ana yol hero nesnelerinin önceliği daha yüksek.

Su: kıyı köpüğü/sınır, yüzey saydamlığı/renk, sualtı sis/ışık/kabarcık geçişi. Tam akışkan simülasyonu gerekmez. Post-process ve bloom, zayıf modelleri veya hedef okunurluğunu örtmez.

### 9.2 Ses envanteri

| Ses ailesi | Tetikleme | Kim duyar | Kontrol |
|---|---|---|---|
| Taş/ahşap/kum/ıslak adım | Gerçek yürüyüş ve yüzey | Yerel + yakındaki arkadaş | Sabit sürüklenme sesi değil |
| Tahta alma/bırakma/çivi/keser | Kabul edilen eylem teması | Yakın 3D ses | Aynı RPC iki kez çalmaz |
| Dalga/rüzgâr/liman | Zone ve hava | Çevresel | Sınırda keskin sıçrama yok |
| Nefes/sualtı | Oyuncu su/oksijen durumu | Temelde yerel | Düşük oksijen anlaşılır, sürekli yorucu değil |
| Kamera | Kuşanma/kayıt | Yerel + yakın kısa cue | Mikrofon/masaüstü kaydı yok |
| Tekne kürek/motor/su izi | Araç/rota hareketi | Yakın/araç yolcuları | Sahip olunan aktif olmayan araç çalmıyor |
| NPC/UI/ödeme | Doğru işlem sonucu | İşlem sahibi ve gerektiğinde yakın | Müzik/ortamı bastırmıyor |
| Boss hazırlık/temas | Host evresi ve poz | Arena yakınları | Yön/tehlike okuması korunuyor |

Ses dosyalarının sayısı kalite değil; mesafe, tekrar, çeşitlilik ve miks ölçülür. Master/ortam/efekt UI kontrolleri, klibin gerçek ortam sesi ve dilsiz görsel uyarılar birlikte değerlendirilir.

## 10. UI ve erişilebilirlik

- Tek aktif etkileşim mesajı; hedef adı, tuş ve anlaşılır ret. Debug oturum/economy kutuları normal oyuncu ekranını kaplamaz.
- Parça ipucu: eksik ad, görev bağlamı, yön ve mesafe; yakın dünyada görsel ipucu. Sayacın yanında hangi parçanın tamam olduğu bellidir.
- Satış/satın alma: teklif, ortak bakiye etkisi ve tahsis. Tekrarlı butona basmak yeniden ödeme değildir.
- PC: klip hazır/aktarılıyor/bozuk/silinmiş ayrımı; sahiplik ve playback kontrolü; gerçek kapağın kaynağı açık.
- Oksijen/çanta/geri dönüş birbirini örtmez; dalışta en kritik bilgi önce.
- Tasarım hedefleri: ölçeklenebilir metin, yeniden bağlanabilir eylem, hold/toggle seçimi, azaltılabilir kamera sallanması, renk dışında şekil/ikon uyarısı, ses cue için görsel karşılık.
- Bunlardan altyapıda bulunmayanlar üretim başlamadan eforlanır; varsayılan “zaten vardır” denmez. P5'e kadar doğrulanmamış erişilebilirlik özelliği tamam sayılmaz.

## 11. Ekip ve kalıcı dünya davranışı

Rol bonusları gönüllüdür; kimse tamirci sınıfı seçmeden çivi çakamaz veya kameracı olmadan kamera kullanamaz diye kilitlenmez. Sözlü iş bölümü teşvik edilir; zorunlu aynı anda dört düğme gibi solo engelleri yok.

Host kampanyası D06 korunur. Guest ID kalıcı kişi kimliği değildir. Ortak eşya/para/tekne ve onarım kampanyaya ait; oyuncu sahipliği geçici tahsistir. Arkadaş ayrılınca ortak ilerleme kaybolmaz, guest yeni oturum kimliğiyle eski taşıma hakkını otomatik devralmaz.

Geç katılım yalnız kasabada kabul edilir. Bağlanan kişi tamamlanan tamir/ev/tekne state'ini alır; geçmiş bütün darbe animasyonlarını yeniden oynatmaz. Görüntüsü bir anda bozuk modelden onarılmış modele zıplayıp kullanıcının önünde yanlış sunum üretmemeli; snapshot kurulumu atomik yapılır.

Görsel animasyon/partikül öncelikle presentation state'tir. Dünya/ekonomi otoriteleri aynı kalır. Kalıcı işaretler kalıcı kayıttan, kısa vuruşlar doğrulanmış olaydan sürülür; oyun nesnesi sayısı birden çok setup çağrısıyla çoğalmaz.

## 12. Uygulama öncesi karar defteri

| Karar | Çalışma önerisi | Etki/sahip | Ne zaman gerekir? |
|---|---|---|---|
| R01 Kürekli sandal + motor/tank uyumsuzluğu | Üç ahşap/fitting tamir grubu; eski ID kontrollü eşleme | Mehmet son tasarım, Mert save, Utku world | P4.8 sanat üretiminden önce |
| R02 Her tahtanın kalıcı ilerlemesi | Önce üç ana grup/tek sabit parça; daha çok yama için alt-slot şeması | Üç alan; eski mask göçü | P4.8 API kesinleşmeden |
| R03 Taşıma input/iptal | Mevcut E + açık bırak/iptal, yeni zorunlu tuş kalabalığı yok | Mehmet, kullanıcı testi | İlk tamir prototipi |
| R04 Dünya ölçeği | Ayrı hızlı hizmet rotası/isteğe bağlı uzun tur | Utku/Mert, Mehmet kamera | P4.7 blokajı |
| R05 Beş türün gerçek kimliği | World katalog, siluet/rol/habitat tek kaynak | Utku; Mert asset | P4.11; #155 erken teşhis |
| R06 İlk hedef donanım | Ekipten gerçek GPU/CPU/RAM/çözünürlük; 1080p/60 başlangıç hedefi | Mehmet koordinasyon | P4.7 maliyet ölçümü |
| R07 Sanat kaynak yöntemi | Uyumlu mevcut/öz üretim/izinli ücretsiz set; eksiklerin listesi | Mert, Mehmet bütçe | İlk varlık ekleme |
| R08 Avcı/kanal ekonomisi | Tur ve gün verisiyle ayar; ilk kazanç ücretsiz | Mert, iki yolun testleri | P4.10–P4.12 |
| R09 Erişilebilirlik kapsamı | Kritik input/metin/kamera seçenekleri önce | Mehmet/Mert | P4.9 UI işleri |
| R10 Gelecek genişlemeler | P6 sonrası veri/kapsam kararı, ayrı kampanya uyumu | Ürün kararı Mehmet | İlk sürüm kapanışından sonra |
| R11 Pickup/satın alma bugün otomatik katkı | Edinilen malzeme ile tamamlanan tamiri ayır; fiyat bir kez edinmede | Mehmet etkileşim, Mert ekonomi/save, Utku nesne | P4.8 API/şema tasarımından önce |

Çalışma önerileri alınmış ekip kararı diye raporlanmaz. Karar kaydında tarih, seçilen seçenek, alternatifin bedeli ve etkilenen kayıt/varlık bulunur. Uygulama bu karar gerçekten alındığında başlar; varsayılan fikir ile imzalanmış sözleşme karışmaz.

18 Ekim #186 hazırlığı: [fiziksel tamir sözleşme önerisi](P48_REPAIR_CONTRACT_PROPOSAL.md), üç görünür tamir grubu, edinme/son darbe ayrımı, sekiz eski maskenin geçişi ve #187/#188/#189 dosya sınırlarını somutlaştırır. Bu belge R01/R02/R11'i tek başına kapatmaz veya #141/#159 kabulünü üretmez.

## 13. Oyuncu kabul ölçümü

Her testte aynı commit/build, host kampanya, oyuncu sayısı/donanım, yapılan tur, video/JSON ve yapılmayan sınırlar kaydedilir. Test fixture'ı ve gerçek kullanıcı girdisi ayrılır.

| Başlık | Test görevi | Geçti demek için |
|---|---|---|
| İlk yön bulma | Eve ve sahile açıklamasız erişim | Takılma/ışınlanma yok; yanlış dekor hedefleri kayıtlı |
| Sandal keşfi | Eksik parçayı isim/yönle bul | 2/3 sorunu kayıpsız çözülür |
| Fiziksel tamir | Taşı/yerleştir/çak/iptal | Model/ses/arkadaş tutarlı; katkı tam bir kez |
| Sanat | Aynı açı gündüz/gece ve hareket | Kullanıcı kalite/ölçek değerlendirir; sadece screenshot yetmez |
| Türler | Başlangıç/kilitli habitatlar | Beş tür gerçek spawn ve erişim kanıtı |
| Günlük çeşitlilik | Üç gün av/medya kararları | Erişilemeyen görev ve tekrarlı ödül yok |
| Çoklu oyuncu | İki bilgisayar/dört süreç üç gün | Kimlik, state, sonuçlar uyumlu; gerçek ekip değerlendirmesi |
| Performans | Kasaba/deniz/boss/kayıt 1–4 kişi | Kare süresi/disk raporu, donanım sabit |
| Yeniden açma | 2/3 ve gelişmiş kampanya | Kaybolan/çoğalan parça/para/trofe/klip yok |

Başlangıç ürün ölçüm hedefleri: ilk hedefi çoğu yeni oyuncunun sözlü yardım olmadan bulması, ilk gelire gereksiz kilit olmaması, tamir sırasında neyin değiştiğini sayaca bakmadan söyleyebilmesi. Küçük üç kişilik testten istatistiksel evrensel sonuç çıkarılmaz; takılma zamanı ve yorumlar kaydedilir.

P4.12 tamamlandığında “oynanıyor” ve “görsel/içerik hedefi kabul edildi” ayrı sonuçlarıyla birlikte raporlanır. Yeni oyun tasarım açığı P5'e “küçük polish” diye devredilmez.


18 Ekim #187 uygulama istisnası: kullanıcı yalnız fiziksel tamir işini şimdi yetkilendirdi. E ile kaldır/yerleştir, Q ile ortak askıya bırak, üç sol tık darbesiyle tek katkı; edinme tek başına onarım değildir. Geçici mekanik görünüm nihai #189 sanat teslimi sayılmaz; #141/#159 insan kabulü açık kalır.
