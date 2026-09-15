# Görsel, animasyon ve ses teslim planı

**Plan 3.0 — 15 Eylül 2026.** İnsan/kıyı/ekipman planına ev/yatak/PC, keşif haritası, üç tekne, boss, gün-gece/hava ve kasaba gelişimi eklendi. Durum: **planlandı, varlıklar bu revizyonla üretilmedi/eklenmedi**. Mert sanat/ses üretimi ve tutarlılık sahibidir; Mehmet oyuncu/ekipman/tekne/medyaya, Utku çevre/canlı/bossa, Mert ev/kasaba/UI'a bağlar. [Fazlar](PHASES.md), [oyun akışı](GAMEPLAY_LOOP.md) ve [dünya sistemleri](WORLD_SYSTEMS.md) ile kullanılır.

## Sanat yönü

**İnsan oranlı stilize kıyı kasabası ve atmosferik sualtı.** Sıcak kum, güneşte solmuş ahşap, açık taş ve küçük renkli dükkânlar; suya girince turkuazdan koyu maviye geçen, derinliği ışıkla anlatan bir dünya. Karakter, balık, bina ve ekipmanda benzer ayrıntı düzeyi/material dili kullanılır. Farklı gerçekçilikte hazır paketlerin yan yana bırakılması kabul değildir.

- **İnsan:** Anatomisi okunur tam vücut, eklemli kol/bacak, el ve yüz silueti; wetsuit, maske, tüp, palet ve çanta. Bir ana insan iskeleti ve dört ayırt edilir giysi/renk varyantı yeterlidir. NPC'ler aynı uyumlu insan setini kıyafet/aksesuarla paylaşabilir.
- **Hareket:** Yerde ayak basışı, su üstünde başın duruşu, sualtında gövdenin yatay yönelmesi ve palet vuruşu görünür. Kamera kullanırken iki el tutuşu korunur; bütün vücut cansız bir nesne gibi kaymaz.
- **Ekipman:** Temel kamera daha P3'te modellenmiş elde ekipmandır. P4'te kamera gövde/tutacak/muhafaza/ışık ekleriyle büyür; ekranın hedefi kaplayacak kadar ölçeklenmez. Tüp/çanta/palet siluetleri seviyeyi anlatır.
- **Kıyı:** İnsan ölçüsünde patika, üç hizmetin tabelası, dükkân cephesi/tezgâhı, kum-su sınırı, sığ taban, iskele ve onarılan sandal tek kompozisyon oluşturur. Süs nesneleri geçişleri kapatmaz.
- **Sualtı:** Kum/kaya/bitki tabakası, rota/derinlik işaretleri, kontrollü sis, yüzeyden gelen ışık, balık hareketi ve mevcut özel olay birlikte kullanılır. Sadece mavi ekran filtresi veya aşırı sis görsel teslim değildir.
- **UI/ses:** Aynı ikon/renk/tipografi ailesi; ekipman, oksijen, çanta ve etkileşim ilk bakışta okunur. Kasaba, kum, suya giriş, yüzey ve sualtı sesleri geçişi hissettirir; ses kamera/av/onarım/satış sonucuyla eşleşir.
- **Ev/PC:** Dışarıdan kasabaya bağlı sıcak bir ortak ev; dört yatak, ortak masa, gerçek oyun klibini gösteren monitör, ekipman rafı ve trofe duvarı. Kullanılmış ahşap/denizci ayrıntıları vardır; evin gelişimi görünür.
- **Tekneler:** Sandal, motorlu tekne ve küçük araştırma teknesi farklı siluet, güverte/koltuk, sandık ve sesle ayırt edilir. Her biri aynı dört insanı doğru ölçekte taşır; sadece model büyütme yeterli değildir.
- **Boss/yaşayan deniz:** Normal canlılara ek tek büyük canlı, okunur saldırı hazırlığı/zayıf nokta ve batık/oyuk kompozisyonu. Sabah/akşam/gece ve rüzgârlı durum renk/ışık/ses/bitki hareketiyle değişir; atmosfer hedef okunurluğunu yok etmez.

Bu yön ürün tasarımıdır; belirli bir ücretli paket veya fotogerçekçilik sözü değildir. Kaynak yöntemi Mert'in sorumluluğunda, bütçe takibi Mehmet'tedir.

## Fazlara göre varlık ve entegrasyon

| Paket | Zorunlu içerik | İlk teslim | Kaynak / oyuna bağlayan |
|---|---|---|---|
| V01 — İnsan dalgıç | Bir uyumlu insan modeli/iskelet, dört görünüm, birinci şahıs el/kol seti; tüp/çanta/palet bağlantı noktaları | P3.1 | Mert / Mehmet |
| V02 — Hareket ve kullanım | Bekleme, yönlü yürüme/dönüş, su üstü yüzme/bekleme, sualtı yüzme, dalma/yükselme ve kıyıya çıkma; ekipman tutma/kuşanma/kullanma/saklama | P3.1 | Mert / Mehmet; su çizgisi Utku |
| V03 — Başlangıç ekipmanı | Kamera ve zıpkın gövdesi, doğru el tutuşları, kayıt ışığı; mevcut tüp, çanta ve palet | P3.1 | Mert / Mehmet |
| V04 — Kıyı ve kasaba | Yürünebilir kumsal/patika, üç küçük hizmet cephesi/tezgâhı/tabelası, iskele, sığ taban | P3.2 | Mert kit sağlar / kasaba Mert, kıyı-sualtı Utku |
| V05 — NPC ve işlemler | Üç insan görevli; bekleme, oyuncuya dönme ve kısa işlem jesti; fiyat/çanta/ürün UI'ı | P3.2 | Mert / Mert; etkileşim Mehmet |
| V06 — Sandal | Bir dört kişilik model, bozuk/kısmi/onarılmış görünümler, üç parça/onarım noktası, oturma ve kısa onarım hareketi | P3.3 | Mert / hareket-koltuk Mehmet, rota Utku, onarım durumu Mert |
| V07 — Temel sualtı ve ses | Kum/kaya/az bitki, su yüzeyi/kıyı köpüğü, basit ışık kırılması izlenimi/sis/kabarcık; adım, dalış, yüzme, nefes, av, kayıt, satış, onarım, kürek/su sesleri | İlgili P3.1–P3.3 işiyle; P3.4'te birlikte kabul | Mert / her alan sahibi |
| V08 — Gelişmiş ekipman | Kamera için iki üst model/tutuş/ışık; palet/çanta/zıpkına birer yükseltme; tüp katalog görünümü | P4.3 | Mert / Mehmet; kamera hedef ışığı Utku |
| V09 — Uzak/derin ve canlılar | Üç kesim, beş normal türün modelleri/hareketi, tek olay, resif/derin demirleme ve bir boss keşif odağı | P4.3–P4.4 | Mert kaynak/tutarlılık / Utku |
| V10 — Harita ve keşif | P3 canlı tekne/oyuncu/iskele ikonları; P4 keşif örtüsü, derinlik/işaret ve ansiklopedi sayfaları | P3.3; P4.1/4 | Mert UI / Mehmet konum, Utku keşif |
| V11 — Ortak ev | Bir iç/dış ev, dört yatak/uyku pozu, depo, gün/iş panosu, trofe duvarı başlangıcı | P4.1 | Mert / ev/UI Mert, etkileşim/oyuncu Mehmet |
| V12 — PC ve klip arşivi | Ortak monitör/masa/oturma pozu, gerçek klip oynatıcı yüzeyi, arşiv/başlık/kapak/kanal/sonuç UI ve sesleri | P4.2 | Mert / medya/oynatma Mehmet, arşiv/kanal Mert |
| V13 — Büyük tekneler ve liman | Ayrı motorlu ve araştırma gövdeleri, koltuk/ambar/raf, motor/su sesleri; tekne satıcısı/tezgâh/önizleme | P4.3 | Mert / hareket Mehmet, rota Utku, sahiplik/UI Mert |
| V14 — Boss | Bir boss modeli/iskeleti; üç saldırı hazırlığı/tepki/zayıf nokta, iz/siluet/çevre/ses, ganimet ve araştırma trofesi | P4.4 | Mert kaynak / AI/çevre Utku, oyuncu hasarı Mehmet, ödül Mert |
| V15 — Günlük dünya | Sabah/akşam/gece ışığı, sakin/rüzgârlı su/çevre/ses, bir akıntı görseli; gece hedef okunurluğu | Temel gün P4.1; çeşitlilik P4.5 | Mert tutarlılık / Utku çevre, Mehmet uyarı |
| V16 — Görünür ortak gelişim | İki ev seviyesi, balıkçı/dükkân/iskele için birer iyileştirme; trofe/fotoğraf içerikleri ve rol/sponsor ikonları | P4.5 | Mert / ilgili alan sahibi |
| V17 — Son bütünlük | Ev/PC/harita/üç tekne/boss/gece dâhil materyal/animasyon/LOD/ses dengesi | P4.6 | Mert tutarlılık / her alan sahibi |

LOD, uzaktaki nesnenin daha basit gösterimidir. İlk insan/ekipman seti tamamlanmadan çok sayıda kıyafet, NPC veya balık varyantı üretilmez. Mert her modeli sıfırdan yapmak zorunda değildir; uyumlu, kullanımı doğrulanmış bir seti düzenlemek mümkündür. Alan sahipleri kaynak beklerken ilgili sözleşme/bağlantıyı hazırlayabilir; nihai faz kabulünde geçici kapsül/küp, insan veya ekipman yerine geçmez.

## Animasyon ve ekipman bağlantı şartları

- İnsan modeli, hareket/collider otoritesini değiştirmeyen görsel alt nesnedir. Animasyon ağdaki onaylı hız/yön/su durumu ile sürülür; karakteri ikinci kez ileri taşımaz.
- Yerel el/kol gösterimi ile uzaktaki tam vücut ayrı görünürlük ayarı kullanır. Oyuncu kendi yüzünün içini veya iki set üst üste eli görmez; arkadaşının gövdesi kaybolmaz.
- El tutuşu için model başına bağlantı noktası/poz bulunur. Zıpkın, temel kamera ve iki gelişmiş kamera aynı hatalı tutuşa zorlanmaz. Basit el yerleştirme yeterliyse kapsamlı animasyon sistemi kurulmaz.
- Su üstünde baş/gövde ile sualtındaki yatay yüzme ayrılır; derinlik değişiminde poz keskin biçimde atlamaz. Sığdan karaya çıkınca paletler yere göre konumlanır.
- Kamera kayıt ışığı, tutuşu ve kuşanılan model aynı ekipman durumundan gelir. Dört oyuncuda yürürken/yüzerken ekipman gövdeden kopmaz veya yanlış oyuncuda görünmez.
- Sandalda koltuklar insan boyutuna uygundur; oturmuş oyuncu ile sandal farklı hızlarda gitmez. Tamir ve NPC işlem hareketleri sonuç doğrulamasının yerini almaz.
- Model/animasyon tesliminde ölçek, yön, pivot, rig, material, çarpışma ve `.meta` eşleşmesi alan sahibiyle kontrol edilir. Ana oyuncu prefabı ve ortak sahne tek entegratör sırasıyla değiştirilir.

## P3 görsel kabulü

Her kontrol **gerçek oyun build'inde** yapılır. Aynı commit/build ve görüntü kaydı kısa faz raporuna eklenir; bu belgeye önceden PASS yazılmaz.

- [ ] **Kasaba:** İki oyuncu yürür; insan anatomisi/hareketi ve üç hizmetin NPC/tabelası seçilir. Ayakta kayma, T-pose veya insan yerine kapsül yoktur.
- [ ] **Su sınırı:** Bir oyuncu kıyıdan girip yüzeye/sualtına/karaya geçerken diğeri izler. Su çizgisi, gövde pozu, nefes/ambiyans ve ekipman geçişleri tutarlıdır.
- [ ] **Elde kamera:** Yerelde eller ve kamera kadrajı; diğer oyuncuda tutuş, kayıt ışığı ve saklama görünür. Kamera/eller ana hedef alanını kapatmaz; silah/kamera aynı anda yanlışlıkla görünmez.
- [ ] **Av ve mağaza:** Balık/catch/çanta ilişkisi, doluluk, satış öncesi teklif ve satış sonucu anlaşılır. UI olmadan da satış yapılan yer bulunabilir.
- [ ] **Sandal:** Bozuk, kısmi ve onarılmış durumlar ayırt edilir. Dört insan koltukta gidiş/dönüşte kaymaz; iskele/demirleme ve suyla temas okunur.
- [ ] **Harita:** Açılan haritada iskele ve hareket eden sandal doğru yerde görünür; oyuncu yönü/ikon ve dönüş işareti dünya koordinatlarıyla tutarlıdır.
- [ ] **Sualtı:** Kum/kaya/canlı siluetleri ve dönüş yönü seçilir; sis/ışık nişanı ve kayıt hedefini gizlemez. Olayın başlangıç ışık/sesi gerçek senaryoda görülür/duyulur.
- [ ] **Birlikte kalite:** Rastgele farklı sanat stilleri, eksik/pembe materyal, belirgin doku ölçeği farkı, aşırı parlama veya ses seviyesinde rahatsız edici sıçrama yoktur.

Kanıt: kasaba, su geçişi, birinci/üçüncü şahıs ekipman, satış ve sandal yolculuğunu gösteren kısa klipler veya kare dizileri. Tek pazarlama görüntüsü hareket/co-op kanıtı değildir. Mert tutarlılığı, Mehmet insan/ekipmanı, Utku dünya/canlıyı mevcut ortak test içinde değerlendirir; yeni imza matrisi oluşturulmaz.

## P4 görsel kabulü

- [ ] Üç kamera kademesi aynı açı/mesafede ayırt edilir; yerel el, uzak insan, dükkân önizlemesi ve gerçek ekipman etkisi eşleşir.
- [ ] Sığ/resif/derin kesimler renk kadar çevre silueti, canlı, ışık ve rota işaretiyle de farklıdır. Derinliğin karanlığı oynanabilir görüşü tamamen kapatmaz.
- [ ] Beş türün yüzme/kaçma/vurulma davranışı modellerinde okunur; avcı tehlikesi ve nadir tür ayırt edilir. Tek özel olayın VFX/sesi çevreyle uyumludur.
- [ ] Üç tekne farklı siluet/güverte/ses/ambarla görünür; dört yolcu, canlı harita ikonu ve resif/derin rota gidiş-dönüşü tutarlıdır.
- [ ] Evde dört insanın yatağa girme/kalkma ve PC'de klip izleme hâli görülür. Monitörde oynayan görüntü o oturumda oyuncunun kaydettiği gerçek sahnedir; hazır tanıtım videosu değildir.
- [ ] Aynı klibi iki oyuncu birlikte izler; kapak/başlık/günlük kanal sonucu okunur. Keşif haritası, ansiklopedi ve gün özeti aynı görsel dili kullanır.
- [ ] Bossun üç saldırı hazırlığı, zayıf noktası, geri çekilmesi/yenilgisi ve araştırma/av trofesi ayırt edilir. Avcı balığın büyük kopyası kabul değildir.
- [ ] Sabah/akşam/gece ve rüzgâr/akıntı atmosferi oynanışı korur; ev/kasaba gelişimlerinin önce/sonra farkı gerçek sahnede görülür.
- [ ] UI, NPC, ekipman ve çevrenin aynı sanat/ses diliyle tamamlandığı gerçek tam döngüde görülür; kullanılacak varlıkların kaynak kaydı tamamdır.

## Performans ve hızlı üretim sırası

1. P3.1'de tek insan + temel elde kamera + küçük kıyı parçasını iki oyuncuyla göster; bu görünür örnek üzerinden model/renk/ölçek hatalarını düzelt.
2. Aynı kit ile P3.2 NPC/tezgâh/kumsalı, P3.3 sandal/iskele parçasını tamamla. Kullanılmayan geniş çevre paketi sahneye doldurulmaz.
3. P3.4'te dört oyuncu, su yüzeyi, saydam parçacıklar ve ışıklar birlikte açıkken kare süresini ölç. Başlangıç hedefi 1080p/60 FPS; ölçüm yoksa ulaşıldığı yazılmaz. Test ortamı teknik kanıtta kaydedilir, kişisel ekip profili toplanmaz.
4. P4.1 ev/gün/harita; P4.2 gerçek klip/PC; P4.3 ekipman/tekne; P4.4 boss/derin içerik; P4.5 dünya gelişimi sırasıyla küçük sanat paketleri teslim et. P4.6'da hepsini aynı gerçek build'de kabul et.
5. Kayıt açıkken dört oyuncu, PC oynatımı ve medya aktarımının kare süresi/bellek/disk etkisini ayrıca ölç. Görsel kalite için oynanabilirlik hedefini sessizce düşürme; klip yöntemi/bütçesini P4.2 ön denemesinde çöz. Uzak nesne/ışık/doku sadeleştirmesini ölçüme göre yap.
6. P5 ilk insan/ev/boss/tekne veya ilk video üretme fazı değildir; tamamlanmış sistem/sanattaki hata ve performansı düzeltir.

## Kaynak notları — 6 Eylül arşivi

Aşağıdaki kaynak adayları eski P0-C araştırma notlarıdır; bu revizyonda siteler/lisanslar yeniden doğrulanmadı ve paket seçimi yapılmadı. Güncel kaynak satın alma/indirme kararı olarak kullanılamazlar. Her gerçek varlığın kaynağı ve geçerli kullanım/ekip/public repo şartları eklenmeden önce dosya bazında doğrulanır ve mevcut [CREDITS](../assets/CREDITS.md)'e kaydedilir.

### Eski görsel kaynak adayları

| Kategori | Örnek içerik | Önerilen kaynak yöntemi | Lisans notu |
|---|---|---|---|
| Karakter / dalgıç ekipmanı | Dalgıç modeli, tüp, palet, maske, çanta | [Unity Asset Store](https://assetstore.unity.com/) (3D karakter/ekipman paketleri); alternatif olarak [Sketchfab](https://sketchfab.com/) üzerinde CC0/CC-BY işaretli modeller | Asset Store: paket başına EULA farklı, [Unity Asset Store şartları](https://unity.com/legal/as-terms) her paket öncesi tekrar okunur. Sketchfab: model başına yükleyenin seçtiği lisans (CC0'dan CC-BY-NC-ND'ye kadar değişir) — her model tek tek kontrol edilir |
| Deniz canlıları (balık, avcı, nadir tür) | Yüzen balık modelleri, animasyonlu canlılar | Unity Asset Store (sualtı/deniz canlısı paketleri); Sketchfab (CC0/CC-BY modeller) | Aynı — paket/model başına lisans kontrolü şart; NC (ticari olmayan) etiketli modeller ticari hedef netleşmeden kullanılmaz |
| Kasaba / dükkân sahneleri | Bina, iskele, dükkân içi mobilya, çevre kiti | Unity Asset Store (çevre/yapı kitleri); [Kenney.nl](https://kenney.nl/assets) (CC0 yapı/mobilya kitleri); [Poly Haven](https://polyhaven.com/) (CC0 texture/HDRI, model) | Kenney ve Poly Haven: **CC0**, atıf gerekmez. Asset Store: paket EULA'sı geçerli |
| VFX (su kabarcığı, ışık huzmesi, çekim efekti) | Parçacık efektleri, sualtı ışık/sis | Kenney.nl (CC0 parçacık paketleri); Unity Asset Store (VFX Graph paketleri) | Kenney: CC0. Asset Store: paket EULA'sı |
| UI ikonları | Envanter, dükkân, hazır olma ekranı ikonları | Kenney.nl (CC0 UI/ikon paketleri); [game-icons.net](https://game-icons.net/) | Kenney: CC0, atıf gerekmez. game-icons.net: **CC-BY 3.0**, her ikon için atıf gerekir (siteden atıf metni alınır) |

### Eski ses kaynak adayları

| Kategori | Örnek içerik | Önerilen kaynak yöntemi | Lisans notu |
|---|---|---|---|
| Sualtı ambiyansı | Dalış sırasında sürekli ortam sesi | [Freesound.org](https://freesound.org/); [Zapsplat](https://www.zapsplat.com/) | Freesound: ses başına lisans değişir (**CC0, CC-BY, CC-BY-NC** — her dosyanın sayfasında ayrı yazar). Zapsplat: ücretsiz hesapla **atıf gerekir**, ücretli abonelikte atıfsız kullanım |
| Oksijen / nefes sesi | Nefes alma, düşük oksijen uyarısı | Freesound.org; Zapsplat | Aynı — dosya başına kontrol |
| Zıpkın / av sesi | Atış, vuruş, av yakalama geri bildirimi | Freesound.org; [Mixkit Sound Effects](https://mixkit.co/free-sound-effects/) | Freesound: dosya başına kontrol. Mixkit: kendi ücretsiz lisansı (ticari kullanıma izin verir, atıf zorunlu değil) — CC değil, [Mixkit lisans sayfası](https://mixkit.co/license/) okunur |
| Kamera kayıt sesi | Deklanşör, kayıt başladı/bitti sesi | Freesound.org; Mixkit | Aynı |
| UI sesleri | Buton tıklama, envanter, satış geri bildirimi | Kenney.nl (CC0 arayüz ses paketleri); Mixkit | Kenney: CC0. Mixkit: kendi lisansı |

### Eski lisans özetleri — kullanım öncesi yeniden kontrol edilir

- **CC0 / Public Domain** (Kenney.nl, Poly Haven, bazı Freesound dosyaları): atıf gerekmez, ticari kullanım serbest. En düşük risk.
- **CC-BY**: atıf zorunlu; atıf metni mevcut [CREDITS](../assets/CREDITS.md) dosyasında tutulur.
- **CC-BY-NC / CC-BY-NC-SA**: ticari olmayan kullanımla sınırlı. Proje ticari hedefi henüz netleşmedi ([P0_MEETING T03](P0_MEETING.md)); T03 kararına kadar bu tür varlıklar kullanılmaz veya yalnızca geçici/yerel test amaçlı tutulup repoya eklenmez.
- **CC-BY-SA**: paylaşım aynı lisansla yapılmalıdır; türetilmiş/değiştirilmiş varlığın da aynı şartla paylaşılması gerekebilir — kullanılacaksa bu yükümlülük ayrıca değerlendirilir.
- **Marketplace'e özgü EULA** (Unity Asset Store, Zapsplat, Mixkit): CC lisansı değildir, kendi şartları geçerlidir; "ücretsiz indirme" tek başına ham dosyayı public repoda paylaşma izni anlamına gelmez.

## Ham varlık public repoya eklenmeden önce kontrol listesi

- [ ] Kaynağın güncel lisans sayfası (paket/dosya bazında) okundu; yalnızca "ücretsiz" etiketine güvenilmedi.
- [ ] Ticari kullanım izni var mı kontrol edildi; proje ticari hedefi netleşmeden (T03 açık) NC lisanslı varlık kullanılmadı.
- [ ] Atıf gerekiyorsa atıf metni not edildi ve CREDITS dosyasına eklenecek şekilde kaydedildi.
- [ ] Lisans, dosyanın **olduğu gibi public repoda paylaşılmasına** izin veriyor mu (bazı Asset Store EULA'ları ve CC-BY-NC-ND gibi lisanslar yalnızca projede kullanım izni verir, yeniden dağıtımı/paylaşımı yasaklayabilir).
- [ ] Ekip kullanım hakkı: satın alınan/lisanslanan varlık tek kişi hesabına mı bağlı, yoksa ekip/proje için kullanılabilir mi.
- [ ] Kaynak bağlantısı, indirilen sürüm/tarih ve lisans özeti kayıt altına alındı (lisans ileride değişebilir; indirme anındaki hâli referans alınır).
- [ ] Büyük ikili dosya ise [WORKFLOW.md](WORKFLOW.md) uyarınca Git LFS kurulumu önceden yapıldı.
- [ ] Kişisel ödeme/hesap bilgisi repoya yazılmadı.
- [ ] İzin belirsizse varlık eklenmedi; yerine geçici kendi üretimimiz kullanıldı ([P0_MEETING](P0_MEETING.md) sınırı).

## Sıradaki teslim ve açık seçimler

- İlk sanat paketi **P3.1 V01/V02/V03**: insan rig'i, minimum hareketler ve temel elde kamera/zıpkın. Bu plan satın alma veya varlıkların hazır olduğu anlamına gelmez.
- Mert kaynak/üretim yöntemi, paket uyumu ve lisans kaydını; Mehmet/Utku kendi entegrasyonunu aynı ara teslimde yapar. Ücretli kaynak gerekiyorsa mevcut bütçe kuralı uygulanır; harcama varsayılmaz.
- Kaynak kontrolü her gerçek eklemede yapılır; sonuç mevcut `docs/assets/CREDITS.md` dosyasına eklenir. Liste P4.6'da tamamlanır, ilk insan ve temel sanat P4'e ertelenmez.
