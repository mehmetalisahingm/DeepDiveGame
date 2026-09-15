# Yaşayan dünya: ev, gün, kanal, keşif ve boss

**Plan 3.0 — 15 Eylül 2026.** Kullanıcının yeni kapsamı ve onu tamamlayan sınırlı tasarım ekleridir. Tamamlanmış sistem veya teknik başarı iddiası değildir. Ana kapsam/sahiplik/kabul [PHASES](PHASES.md), başlangıç yolculuğu [GAMEPLAY_LOOP](GAMEPLAY_LOOP.md), bağlantılar [CONTRACTS](CONTRACTS.md), sanat [ASSET_PLAN](ASSET_PLAN.md) içindedir.

## Oyunun iki gelir yolu

**Balıkçılık:** Kıyıdan veya tekneyle git → avı çantada taşı → güvenle dön → balıkçıda seçip sat → hemen gelir.

**İçerik üretimi:** Kamera satın al/tahsis et → kendi elindeki kamerayla çek → güvenle dön → ev PC'sinde gerçek klibi izle → başlık/kapak seç → ortak oyun içi kanalda yayınla → ertesi sabah izlenme/takipçi/gelir.

Balıkçılık ilk günden kamera veya tekne parası istemez. Kanal uzun vadeli keşif ve riskli çekimleri değerli kılar; büyük tekne/kamera ilerlemesi yalnız tek gelir yolunu zorunlu kılmaz. Görüntü alıcısı sabit ve hemen ödenen alternatif olarak kalır. **Aynı recordingId'nin ticari hakkı NPC veya kanal için bir kez kullanılır.** Balığı satmak ile ayrıca o canlıyı kaydetmek ayrı sonuçlardır; ikisi kazanabilir.

Aynı dalış/konu için en iyi güvenli kayıt tek ticari adaydır; farklı kameracıların aynı hedefi çekmesi ikinci hak üretmez. Diğer klipler ortak arşivde izlenebilir. Bu kural hem NPC hem kanal yolunda geçerlidir.

## İlk görev ve oyuncuyu yönlendirme

İlk ana görev **“Sandalı denize indir”**. Bozuk sandal kumsalda görünür, haritada işaretlidir; eksik üç parçanın dünyadaki ipucu ve görev metni bellidir. Oyuncular kıyıda parçaları bulabilir veya önce sığda balık avlayıp satın alabilir. İlk para kazanmak için onarılmış sandal gerekmez; parçalar yalnız sandalla ulaşılabilen yerde olmaz.

Ana görev zincirleri:

1. Sandalı incele → üç parçaya katkı yap → onar → yakın rotadan dön.
2. Balıkçıya av sat → ekipman dükkânından ilk kamerayı ayrı satın al → oyuncuya tahsis et → ilk geçerli çekimi güvenle getir.
3. P4.2'de ev PC'sinde klibi izle → ilk yayını seç/onayla → uyku/gün özeti → ertesi sabah kanal sonucu.
4. P4.4'te kasaba söylentisi → üç keşif izi → boss bölgesini bul → ekipman/tekne hazırlığı → araştırma veya mücadele → güvenli dönüş ve tek görev sonucu.

Görevler oyuncuyu sırayla bilgilendirir; kıyı avını, serbest keşfi veya diğer oyuncunun kamera almasını yapay biçimde kilitlemez. Atlanmış görev adımı zaten yapılmışsa geriye dönüp aynı şeyi zorla yaptırmaz; ödülü de iki kez vermez.

## Ortak ev ve depo — P4.1

- Başlangıçta ortak bir kıyı evi, dört yatak, ortak ekipman dolabı/depo, gün/iş panosu vardır. P4.2'de kullanılabilir PC/arşiv/kanal açılır. Evin ilk kullanımı satın alma istemez.
- Oyuncular eve yürüyerek gelir; yatağa yatma, kalkma, PC'ye oturma ve depodan eşya alma arkadaşları tarafından görülür. Menü kısayolu fiziksel eve dönüş şartını atlamaz.
- Ortak para/ekipman/ev/kanal/tekne kampanyaya aittir; kişisel oksijen ve eldeki çanta oyuncudadır. Tekil eşya aynı anda iki çantada veya depoda olamaz.
- Güvenli av kasabadaki depoya bırakılabilir. Balıkçıya satmak için seçilen av yeniden çantaya alınır; taşıma kapasitesi geçerlidir. İlk sürümde balığın gün geçince bozulması eklenmez.
- Fotoğraf/trofe duvarı ansiklopedi, ilk yayın ve boss başarısıyla dolar. İki ev iyileştirmesi daha düzenli arşiv/sergi/depo görünümü verir; büyük oda inşa editörü kurulmaz.
- Bu ev oyuncunun gerçek masaüstü/dosyalarıyla bağlantılı değildir; PC oyun dünyasının etkileşimli nesnesidir.

## Ortak saat, uyku ve 00:00 — P4.1

Başlangıç taslağı: gün 08:00'da başlar, 00:00'da kapanır. Gerçek dakika/oyun saati oranı oynama testinde ayarlanır; bu saatler teslim süresi değildir. Host tek saati yönetir; bütün oyuncular aynı gün/hava ve zamanı görür. Dalış bitmesi ile gün bitmesi ayrıdır; bir günde birden fazla dalış yapılabilir.

**Erken uyku:** En az bir bağlı etkin oyuncu bulunur ve bağlı etkin oyuncuların hepsi evde yatağa girerse gün kapanır. Solo oyuncunun tek yatağı yeterlidir. Kopan, dalışta pasif olmuş veya oturumdan çıkmış kişi uyku oyunu kilitlemez. Kapanış başlamadan yataktan kalkmak hazır olmayı iptal eder; başka oyuncuyu zorla uyutma yoktur.

**00:00 zorunlu sınırı:** 22:00 ve 23:00'te zaman/dönüş uyarısı; uzun rotaya çıkışta dönüş süresi bilgisi gösterilir. 00:00'da yeni dalış, seyahat, alışveriş ve yayın isteği alınmaz; kapanış işlemi başlar.

- Evde/kasabada/güvenli kıyı alanında olanların güvenceye alınmış av ve klipleri korunur; yatağa yürüyememiş olmak tek başına eşya kaybı değildir.
- Hâlâ denizde veya dış demirlemede kalanlar o dalış için başarısız sayılır; güvenli dönmeyen av/klip D07'ye göre kaybolur. Ekipman ve sahip olunan tekne kalıcı olarak silinmez. Sonraki sabah oyuncular evde, aktif tekne limanda başlar.
- Bu, dalış içinde kurtarma değildir; günün kapanış sonucudur. 00:00'da teknede olmak kasabada güvenli dönüş sayılmaz. Oksijen/sağlık sıfırlanması aynı kayıp kurallarıyla sonuçlanır.
- Kapanışta host işlem sırasını sabitler: kabul edilmiş eylemler → dalış sonuçları → günlük özet → sıradaki güne ait kanal sonuçları → atomik kayıt → ertesi sabah. Sonradan gelen istek bir önceki güne sızamaz.
- Host ayrılırsa gün keyfî tamamlanmaz; son sağlam kontrol noktası yüklenir. Bütün oyuncular çıkmışken gün ilerlemez. Gün tekrar yüklenince aynı kazanç/özet ikinci kez oluşturulmaz.

**Gün özeti:** Gün numarası; satılan balık/adet/ağırlık; doğrudan gelir/gider/net bakiye; keşfedilen tür/alan; getirilen ve yayınlanan klip; bayılma/kayıp; onarım/görev/tekne/ev ilerlemesi. Bekleyen kanal geliri kazanılmış para gibi gösterilmez. Örnek: “Gün 12 — 7 balık satıldı — 2 yeni tür — 1 yayın sırada — 1 dalgıç geri dönemedi.” Sayılar örnektir.

Ertesi sabah aynı yayın için bir sonuç kartı gelir: izlenme, takipçi değişimi ve kesinleşen kredi. Gün özeti geçmişi ev PC'sinde/iş panosunda okunabilir.

## Gerçek klip ve ev PC'si — P4.2

**Görünür teslim:** Kamera oyuncunun elindedir. Kayıtta kendi çevirdiği açı, gördüğü canlı, kadrajı, hareketi ve oyun ortam sesi yakalanır. Eve geldiğinde bunları klip olarak yeniden izler; yalnız kalite puanı, sabit resim veya önceden hazırlanmış film yeterli değildir. Diğer oyuncular da ortak ev ekranında aynı klibi izleyebilir. Oyun dışı masaüstü, mikrofon ve sesli sohbet kaydı kapsamda yoktur.

İlk medya bütçesi, uygulama öncesi teknik deneme için taslak: **720p/24 FPS hedefi, klip başına en çok 30 saniye, oyuncu başına dalışta dört klip ve kampanyada 1 GB arşiv tavanı**. Bunlar başarılmış ölçümler değildir; capture/encode/aktarım/oynatma yaklaşımı hedef Windows build'inde denenir. Oyunun kayıt açıkken kare süresi, görüntü/ses uyumu ve dört eşzamanlı kamera etkisi ölçülür. Sorun varsa önce yöntem/bütçe gözden geçirilir; izlenebilir video şartı sessizce puan ekranına çevrilmez.

### PC akışı

1. Eve dön → PC'ye yaklaş → kayıt arşivini aç. Her satırda gerçek thumbnail, çeken oyuncunun görünen adı, gün, hedef, süre, kalite ve aktarım durumu bulunur.
2. Klibi oynat/duraklat/ileri-geri sar. Oynatma kontrolü bir oyuncudadır, diğerleri ortak ekranı görür; kontrol sahibi kalkar/koparsa kontrol serbest kalır. Oynatma dünya zamanını durdurmaz.
3. Yayınlanabilir klibi seç → başlık yaz → klip içinden kapak karesi seç → ilgi gerekçesi/tahmini sınıfı gör → yayınla. İlk sürümde kurgu/editör, çoklu klip montajı veya müzik lisanslama sistemi yoktur.
4. Kuyruktaki kayıt aynı clipId/publicationId ile tutulur. İkinci tıklama, iki kişinin PC'ye erişmesi veya oyunu yeniden açmak yeni yayın/ödül üretmez.
5. Gün bitince yayın değerlendirilir; ertesi sabah ortak kanala izlenme/takipçi ve ortak cüzdana gelir eklenir. Bunlar kurgusal oyun içi izleyici istatistikleridir; YouTube/Twitch/gerçek internet yüklemesi yapılmaz.

### Arşiv, kopma ve kalıcılık

- Kameranın üretildiği istemci ile host arasında klip ve doğrulanmış recordingId eşleşir. Host güvenli dönüşü, medya bütünlüğünü ve tek ticari hakkı doğrulamadan yayın gelirine izin vermez; istemcinin dosya adı/kalite/izlenme beyanı otorite değildir.
- Kesilmiş aktarım “hazır” görünmez; aynı clipId ile yeniden denenir. Eksik/bozuk medya anlaşılır hata verir. Güvenli dönüş metadata'sı, dosya aktarımı ve ticari durum ayrı takip edilir.
- Kayıt diski dolunca önceden bildirim verilir; başarısız yeni çekim başarı gibi gösterilmez. Mevcut favoriler sessizce silinmez. Kullanıcı oyun içinden eski klipleri silebilir; silme öncesi seçim/onay görünür.
- Arşivden dosya silmek ödenmiş yayın kimliğini/kanal geçmişini silmez ve yeniden kazanç hakkı yaratmaz. Silinmiş klip oynatılamaz, geçmiş kartında durumu belirtilir.
- Host yeniden açınca arşiv oynar ve güvenli ortak kampanya kayıtları korunur. Guest bağlantı ID'si kalıcı kişi kimliği değildir; ortak arşiv kampanya ve clipId'ye bağlıdır.
- P3'ün eski puan kayıtlarından sahte klip üretilmez. Şema geçişinde bunlar “eski araştırma kaydı” olarak NPC hattında kalabilir; gerçek görüntüsü olmayan kayıt izlenebilir/yayınlanabilir diye gösterilmez.

## Kanal ve sponsorluk — P4.2 / P4.5

Kanal puanına doğrulanmış kalite, farklı/yeni tür, görülmüş olay, anlamlı yakınlık, gece/derinlik/boss riski ve içerik çeşitliliği katkı verir. Aynı balığı peş peşe aynı açıyla çekmek giderek daha az ilgi üretir; isim/kapak değiştirmek içeriği yeni saymaz. Kör bir seviye veya rastgele büyük kredi çarpanı kullanılmaz; oyuncu sonuç kartında etkenleri görür.

Üç küçük sponsor şablonu: belirli habitatta yeni türü kaydet, koşullu gece çekimi getir, açılmış tehlikeli hedefi belirli süre görünür tut. Kesin saniye/fiyat testle ayarlanır. Sistem oyuncunun henüz ulaşamadığı tekne/derinlik/hedefi isteyen görev üretmez. Aynı anda bir sponsor bulunur; tekrar ödeme kimliği kalıcıdır. Sponsorlar P4.5'te gelir, ilk yayın için gerekmez.

Görüntü alıcısına hemen satmak ile daha yüksek ama gecikmeli kanal beklentisi arasında seçim vardır. Kanal takipçisi tekne satın almanın tek zorunlu kilidi değildir; avcı ağırlıklı ekip de gelir ve keşifle ilerleyebilir.

## Harita, keşif ve ansiklopedi — P3.3 / P4.1 / P4.4

- **P3.3 haritası:** Açılabilir dünya haritası; iskele/kasaba/bozuk veya aktif sandal/yakın demirleme ve bağlı oyuncular. Tekne seyahatte gerçek konumunda hareket eder. HUD dönüş yönü ve ekip ping'i haritayla aynı koordinatı kullanır.
- **P4.1 keşif:** Ziyaret edilmemiş deniz bölgeleri örtülüdür. Oyuncu/yolculuk ilerledikçe ortak keşif alanı açılır, kayıt sonrası korunur. Aktif tekne ve takım konumu navigasyon için görünür; bu ikonlar çevredeki gizli içerikleri kendiliğinden ifşa etmez.
- Resif, batık, demirleme, görülen boss izleri ve keşfedilmiş derinlik çizgileri işaretlenir. Rota kilidi, yaklaşık dönüş süresi ve ekipman önerisi görünür. Her balığın sürekli canlı konumu yoktur; bilinen habitat veya son gözlem gösterilir.
- Oyuncular sınırlı takım işareti bırakabilir: “dönüş”, “ilginç canlı”, “tehlike”. İşaret kimliği/yaşam süresi nettir; kopan oyuncunun sonsuz işareti kalmaz.
- **Ansiklopedi:** İlk doğrulanmış karşılaşma siluet/kategori açar; geçerli kamera kaydı adı açar; avlanmış örnek kesin ağırlık/değer bilgisini; iyi kayıt habitat/derinlik/davranış bilgisini açar. Araştırma yüzdesi ile av koleksiyonu ayrı gösterilir: avlanmadan araştırma tamamlanabilir, av ağırlık rekoru boş kalır. Boss öldürmek araştırma yüzdesi için zorunlu değildir.
- **P4.4 içerik:** Beş normal tür üç kesime dağıtılır; boss altıncı ayrı girdidir. Aynı türün başka balığını görmek ikinci keşif ödülü yaratmaz. Tür, alan ve koleksiyon ilerlemeleri ortak kampanyaya yazılır.

## Daha büyük tekneler — P4.3

| Tekne | Edinme | Görünür ve işlevsel fark |
|---|---|---|
| Ahşap sandal | P3 ilk görevle onarılır | Dört sabit oturma yeri, yakın dalış rotası; ücretsiz kıyı avı yanında ilk deniz aracı |
| Motorlu tekne | Limandaki tekne satıcısından ortak parayla alınır | Ayrı büyük gövde/koltuk düzeni ve motor sesi; resif rotası, daha kısa yolculuk, sınırlı ortak av sandığı |
| Küçük araştırma teknesi | Para + resif keşfiyle alınır | Daha geniş güverte/kabin silueti, daha büyük sandık ve ekipman rafı; derin demirleme/boss yolculuğu |

Üçü de en fazla dört oyuncu taşır; daha büyük tekne daha çok insan slotu demek değildir. Satın alınanlar kampanyanın liman koleksiyonunda kalır; **aynı anda denizde bir aktif tekne** bulunur. Değişim yalnız bütün oyuncular güvenle dönmüşken yapılır; sandıktaki av/eşya tekil olarak devredilir veya liman deposuna bırakılır. Satın alma tekneyi iki kez oluşturmaz; eski sandal silinip borç kilidi yaratılmaz.

İlk tam sürümde rota üzerinde görünür yolculuk korunur. Serbest dümen, fiziksel dalga simülasyonu, tekne batırma/satın alma kaybı, yakıt borcu ve çok araçlı filo yönetimi ayrı kapsamdır. Canlı harita aktif boatId ve konumunu gösterir; limanda bekleyen tekneler aktif ikon olarak kopyalanmaz.

## Efsanevi canlı ve gerçek boss — P4.4

İlk boss çalışma adı **“Batığın Bekçisi”**: derin kesimde, batık/oyuk çevresini sahiplenen büyük bir deniz canlısı. Mevcut avcı balığının yalnız canını büyütmek yeterli değildir. Bir boss, bir karşılaşma alanı, üç açıkça işaret edilen saldırı düzeni ve saldırı sonrası kısa zayıf nokta penceresi vardır. Tür/görünüm üretim öncesi Mert/Utku tarafından tutarlı seçilir; ikinci boss üretilmez.

İlerleme: söylenti → üç fiziksel iz (hasarlı parça, iz/ses, uzak siluet) → haritada yaklaşık alan → yeterli tekne/ekipman → yaklaşma/çekim → araştırmayı tamamla veya savaşa gir → güvenle dön.

- Saldırı hazırlığı görsel/sesle anlaşılır; kayalar/batık parçası oyuncunun saklanma/geri çekilme kararı için kullanılır. Host hedef/hasar/evreyi doğrular.
- Solo oyuncu önce çekim yapıp sonra zıpkına geçebilir; aynı anda hem kamera hem silah tutma veya iki kişinin düğmeye basması şartı yoktur. 1–4 oyuncu ölçeği hedef seçimi, saldırı aralığı ve sağlıkla birlikte test edilir.
- Ölümde ekip döngüsü D07'ye uyar. Boss başarısı ve ödül uygun güvenli dönüş kontrol noktasına bağlanır; herkes başarısızsa tamamlanmamış karşılaşma son sağlam kayda döner. Aynı öldürme/araştırma sonucu ikinci ödül vermez.
- **Araştırma yolu:** Yeterli geçerli görüntüyü getir; kanal/sponsor ilgisi ve araştırma trofesi. **Av yolu:** Bossu yen; tekil ganimet/trofe ve görev sonucu. Birbirini dışlayan ana görev ödülü tek seçilir; öldürme öncesi çekilmiş klibi izlemek mümkün kalır, ticari tekrar kuralları korunur.
- Güvenli kıyı/kasabaya dönmeden boss ganimeti güvenceye alınmış sayılmaz; dış demirlemedeki tekne güvenli teslim noktası değildir. Kalıcı sonuç ev trofe duvarında ve ansiklopedide görülür. Araştırma yolu tamamlanınca boss kaybolmak zorunda değildir; araştırma ödülü tekrar alınamaz.

## Dünya gelişimi, risk ve roller — P4.5

**Kasaba gelişimi:** Balıkçı tezgâhı, ekipman dükkânı ve iskele için birer kalıcı görünür iyileştirme. Toplam üç kasaba iyileştirmesi, iki ev seviyesi. Hazır geliştirme noktaları kullanılır; serbest bina kurma yoktur. Değişiklikler yeni katalog/sergi/depo imkânlarıyla eşleşir, zorunlu sürekli masraf yaratmaz.

**Günlük çeşitlilik:** Sakin/rüzgârlı hava; gece ışığı ve mevcut türlerden birinin gece daha görünür davranışı; bir işaretli yerel akıntı. Hava günlük kayıtta sabittir. Kötü hava uzak rotayı kapatacaksa önceden duyurulur; denizdeki ekip için güvenli dönüş rotası kapatılmaz. İlk sürüm şiddetli fırtına fiziği gerektirmez.

**Sipariş/sponsor:** Üç balık siparişi + üç video sponsor şablonu; aynı anda birer aktif. Bugünün hedefi para, yeni tür, kayıt kalitesi veya riskli habitat arasında seçim sunar. Şartlar hostun doğruladığı olaylardan gelir; basit UI tıklaması görev tamamlamaz.

**Hafif roller:** Kameracı (daha rahat kayıt stabilitesi), Avcı (zıpkın kullanımında küçük avantaj), Kaşif (işaret/oksijen veriminde küçük avantaj), Taşıyıcı (kapasite avantajı). Kasabada ücretsiz değişir; bir oyuncuda tek rol, bonuslar temel değerden hesaplanır, kesin oranlar testte belirlenir. Hiçbir temel eylem role kilitlenmez; solo tüm sistemi oynar. Kalıcı sınıf/XP ağacı yoktur.

## Döngüyü güçlendiren ekler

Bu revizyonda küçük, mevcut sistemlere bağlı ekler: ekip ping'leri ve dönüş yönü (P3.3); ortak depo ve fotoğraf/trofe duvarı (P4.1, içerik P4.4/5); ertesi gün hedef panosu (P4.5); eski gün/yayın geçmişi (P4.2); boss ipuçları ve araştırma/av tercihi (P4.4). Hepsi yukarıdaki içerik bütçesine dâhildir.

**İlk tam sürüm sonrası fikir havuzu; zorunlu teslim değil:** İkinci efsanevi canlı zinciri, ikinci ada, serbest tekne sürüşü, daha çok hava/akıntı, geniş ev dekorasyonu, uzun klip montajı, oyun dışına video dışa aktarma, gelişmiş NPC takvimleri. Bunlar için ilk tam döngünün gerçek oynama verisiyle ayrı kapsam kararı gerekir; ana işlerin önüne alınmaz.

## Kabul için zor senaryolar

Bir oyuncu uyurken diğerinin denizde kalması; 23:59'da yayın/alışveriş; 00:00'da yarım klip aktarımı; tekne satın alırken iki kişinin aynı isteği göndermesi; boss ödülünden önce host kopması; dolu disk; guest'in arşiv aktarımında ayrılması; kayıt açınca aynı gün/izlenme/para/harita ödülünün tekrarı. PHASES P4/P5 kabulünde bunlar gerçek build ve aynı kayıt üzerinden denenir; plan dosyası yazmak PASS değildir.
