# Ortak sistem sözleşmeleri

Plan 3.0 — 15 Eylül 2026. P3 açık, P4–P6 kilitli. İnsan/ekipman, NPC/çanta/sandal sözleşmelerine harita, ayrı kamera satın alma, ev/gün/uyku, medya/kanal, tekne sahipliği ve boss bağlantıları eklendi. Yeni bölümler **tasarımdır; uygulanmış API veya ekip uzlaşısı değildir**. İlgili ara teslimden önce etkilenen alan sahipleri netleştirir; davranış kaynağı [WORLD_SYSTEMS](WORLD_SYSTEMS.md)'tir.

Durum: ihtiyaç duyulan fazdan önce birlikte kesinleştirilecek tasarım. Buradaki tip isimleri uygulanmış sınıflar veya mevcut dosyalar değildir. Görev kodları: A=Mehmet, B=Utku, C=Mert.

Amaç: Mehmet, Utku ve Mert'in birbirine bağlanabilen sistemler üretmesi. P0'da yalnızca P1 için gereken kimlik/oturum bağlantıları kesinleşir. Av/çanta ayrıntıları P2 öncesinde, çekim/ekonomi/kayıt ayrıntıları P3 öncesinde netleşir. İleriki fazın taslağı o özelliği erken uygulama izni vermez.

Tarihli uygulama notları kendi gününün kanıtıdır. 14 Eylül ayrı çalışma dalının durumu [P3-WIP-HANDOFF](../reports/P3-WIP-HANDOFF.md)'ta; yeni kapsamın kabul koşulları [PHASES](PHASES.md) ve [GAMEPLAY_LOOP](GAMEPLAY_LOOP.md)'tadır. Özellikle mevcut dalış sonu ödeme hattı, P3.2'de NPC'ye teslim akışına dönüştürülecektir.

P1 uygulamaları ortak akışa bağlandı: [entegrasyon kaydı](../reports/P1-INTEGRATION-REPORT.md),
[çalıştırma](../P1_PLAY.md). `Core.Contracts.PlayerId.Value` oturumluk ulong bağlantı
kimliğidir; host için 0 geçerlidir. Mert'in geçici string kimliği kullanılmaz.
`SessionState` ve hazır bilgisi Mert'in `DeepDive.Session` modülündedir.
`ISessionNetworkBridge.RequestSceneLoad` kabul/red döndürür; reddedilen yükleme
aşama/revision değiştirmez. `Composition.SessionNetworkAdapter` gerçek ağ listesini,
hazır isteklerini ve sahne yüklemesini bağlar. İstemci kimliği ağ bağlantısından
alınır; aşama değişimini sadece host yapar. Yeni katılım yalnız Lobby'de ve sahne
yüklenmiyorken kabul edilir. UI `ISessionControls` kullanır; test oyuncusu üretmez.
Bu uygulama kaydı, diğer kişilerin incelemesi veya faz kabulü yerine geçmez.

## Sistem sahipliği

Bu tablo uygulama sahipliğidir. Kullanıcı atamasıyla görsel/ses üretimi, kaynak seçimi ve tutarlılık Mert'te; ortak tasarımda son karar ve birincil insan netcode incelemesi Mehmet'tedir. [Ortak sorumluluk kaydı](P0_MEETING.md) sınırları açıklar. Mert görsel/ses kaynaklarını sağlar; alan sahibi kendi koduna/sahnesine bağlar. Ağ inceleyicisi atanması, alan sahiplerinin kendi co-op uygulama ve test sorumluluğunu devretmez; API değişiklikleri etkilenen kişilerle koordine edilir.

| Alan | Sahip | Sınır |
|---|---|---|
| Oyuncu kontrolü ve ekipman kullanımı | Mehmet | Giriş, insan animasyonu/yerel eller/uzak gövde, yürüme/yüzme, zıpkın/kamera kullanımı, oksijen/sağlık, kuşanılan ekipmanın etki ve görünümü |
| Ağ bağlantısı | Mehmet | Bağlan/ayrıl, oyuncu oluşumu, iletişim/transport, ağ sahne yükleme mekanizması |
| Oturum aşamaları | Mert | Hazırlık/dalış/dönüş/sonuç durumu, hazır oyuncular ve geçiş koşulları; gerçek yüklemeyi Mehmet'in ağ servisi yürütür |
| Canlılar ve dünya | Utku | Tür tanımları, oluşma, AI, vurulma/ölüm, av nesnesi, kumsal/su geçişi, derinlik kesimleri ve sandal rotası/demirleme yerleşimi |
| Çekim değerlendirme | Utku | Hedef görünürlüğü, mesafe, kayıt süresi, kalite ve dalış içi tekrar kuralı |
| Çanta/depo ve ekipman sahipliği | Mert | Tekil eşya örnekleri, kapasite doğrulaması, depoya aktarım, ekipman satın alma ve tahsis |
| Ekonomi ve ilerleme | Mert | Fiziksel NPC hizmetleri, satış fiyatı, çekim teslimi/ödemesi, para, geliştirmeler, onarım ve aynı bölgedeki rota açılması |
| Sandal hareketi ve yolcu bağlantısı | Mehmet | Hostun doğruladığı koltuk/binme/inme ve rota üzerinde ağ hareketi; rota içeriği Utku'dan gelir |
| Sandal ilerlemesi ve sefer koşulları | Mert | Parça/onarım/kayıt, rota açılma ve yolcu hazır kuralları; hareketi Mehmet'in servisi yürütür |
| Gün/ev/kanal ve görevler | Mert | Ortak saat/uyku/kapanış, PC arşiv/yayın/gelir, ev/tekne sahipliği ve günlük görev kaydı |
| Klip medyası | Mehmet | Kayıt cihazı bakışından görüntü/oyun sesi, medya aktarımı/bütünlüğü/oynatma; ekonomik sonuç Mert'te |
| Keşif/ansiklopedi/boss | Utku | Keşif ve tür gözlemi, boss AI/evre/sonucu ve çekim kanıtı; UI/kalıcı ilerleme Mert, oyuncu hasarı Mehmet |
| Kalıcı kayıt | Mert | Şema sürümü, tamamlanmış kontrol noktaları, bütünlük ve yedek |
| Alan arayüzleri | İlgili alan sahibi | Mehmet dalgıç HUD'ı, Utku hedef/kalite geri bildirimi verisi, Mert envanter/dükkân/oturum ekranı |
| Ortak veri ve bileşim | Mehmet/Utku/Mert incelemesi; entegratör koordinasyonu | Kimlikler, arayüzler, başlangıç bağlantıları ve ortak yapılandırma |

## Bağımlılık yönü

Önerilen mantıksal modüller: `Core.Contracts`, `Player`, `World`, `Progression`, `Session` ve `Composition`.

- Alan modülleri ortak sözleşmelere bağımlıdır; birbirlerinin iç sınıflarını veya sahne nesnelerini arayarak iş yapmaz.
- `Composition`, gerçek uygulamaları başlangıçta birbirine bağlar. Küçük ve açık bir bileşim noktası yeterlidir; özel bir framework yazılmaz.
- Static tür/eşya tanımları Unity varlıklarıyla tutulabilir. Ağda ve kayıtta sabit tanım kimliği taşınır; ScriptableObject nesnesi kalıcı oyuncu verisi gibi kullanılmaz.
- Çalışan durum, test sağlayıcısı ve kalıcı kayıt birbirinden ayrılır. Test sağlayıcıları teslim build'inin gerçek oyun yolunda aktif bırakılamaz.
- P1 için gereken modül sınırları P0'da belirlenir; sonraki bağlantılar ilgili fazdan önce netleşir. Bütün modüllerin içi P0'da yazılmaz; ortak isimler tek taraflı değiştirilmez.

## Ortak veri sözlüğü

| Veri | Asgari alanlar ve kural | Üretici → tüketici | İlk gerçek kullanım |
|---|---|---|---|
| PlayerId | Oturum oyuncu kimliği; ağ bağlantı kimliğiyle eşleştirme. Kalıcı kampanya sahibiyle karıştırılmaz | Mehmet → tümü | P1 |
| SessionState | Aşama, sessionId, isteğe bağlı diveId, regionId ve revision | Mert → Mehmet/Utku/UI | P1 |
| DiveId | Her dalış için tekil kimlik; sonraki dalışta değişir | Mert → tümü | P2 |
| SpeciesDefinition | Sabit speciesId, sınıf, davranış parametreleri, ağırlık aralığı | Utku → Mehmet/Mert | P2 |
| ItemDefinition | Sabit itemId, kategori, kapasite/fiyatla ilgili temel tanımlar | Mert; Utku tür eşlemesini inceler → tümü | P2 |
| CaptureResult | captureId, diveId, speciesId, ağırlık, varsa kalite, av nesnesi kimliği | Utku → Mert | P2 |
| InventoryState | playerId, itemInstanceId listesi, mevcut ağırlık, kapasite, revision | Mert → Mehmet/UI | P2 |
| RecordingCandidate | requestId, diveId, playerId, hostun çözdüğü IRecordingTarget bileşeni; Start/Stop ayrı metot, istemci süresi yok | Mehmet → Utku | P3 |
| RecordingResult | recordingId, diveId, playerId, tür/olay kimliği, kalite kademesi (0 ödülsüz, 1–4 Bronze/Silver/Gold/Platinum), hostun ölçtüğü geçerli süre | Utku → Mert/Mehmet | P3 |
| EquipmentDefinition | equipmentId, yuva, seviye, etkiler; para/alış fiyatının sahibi Mert | Mert → Mehmet | P3 |
| LoadoutState | playerId, takılı equipmentInstanceId değerleri ve revision | Mert → Mehmet | P3 |
| DiveSummary | Güvenli dönenler, korunan av/çekim kimlikleri, kayıplar ve kontrol noktası kimliği | Mert → Mehmet/Utku/UI | P2; parasal alanlar P3 |
| TransactionResult | requestId, kabul/red, reasonCode, etkilenen revision | İşlemin sahibi → isteği yapan/UI | P2 |
| SaveSnapshot | schemaVersion, campaignId, checkpointId, ortak para/depo/ekipman/ilerleme ve tamamlanmış ödeme kimlikleri | Mert → disk/yedek | P3 |

### Plan 3.0 ek veri taslakları

Bu adlar kavramsal sözleşmelerdir; yeni sınıfların imzaları değildir. Mevcut tiplere alan eklemek yeterliyse ikinci bir veri sistemi yazılmaz.

| Veri | Asgari alanlar / kural | Üretici → tüketici | Teslim |
|---|---|---|---|
| PlayerPresentationState | playerId, görünüm, locomotionMode, onaylı hız/yön, activeEquipmentInstanceId, kullanım/kayıt durumu, revision | Mehmet; loadout Mert → bütün istemciler | P3.1 |
| EquipmentVisualDefinition | equipmentId/seviye, yerel el/uzak gövde modeli, tutuş/bağlantı pozu, efekt kimlikleri | Mert kaynak/katalog → Mehmet; çekim yeteneği Utku | P3.1; üst kameralar P4.3 |
| ServicePointDefinition | Sabit serviceId, hizmet tipi, worldAnchor, etkileşim mesafesi, katalog | Mert → Mehmet etkileşim / ekonomi / UI | P3.2 |
| PendingTurnInState | Tekil av/kayıt kimliği, kaynak DiveId, güvenli dönüş durumu, carryingPlayerId veya ortak emanet, paid/sold durumu, revision | Mert; aday kayıt Utku/Mehmet → UI/kayıt | P3.2 |
| DiveParticipantState | DiveId, playerId, Active/Returned/Passive durumu; Returned oyuncu için ikinci dalış hakkı yaratılmaz | Mert; canlılık/konum Mehmet → tümü | P3.2 |
| BoatRepairState | boatId, gerekli/tamamlanan parça kimlikleri, Broken/InProgress/Repaired, revision | Mert → Mehmet/Utku/UI/kayıt | P3.3 |
| BoatTripState | boatId, tripId, routeId, Docked/Outbound/Anchored/Inbound, seat→player eşlemesi, sefer grubu, sefer sorumlusu, revision | Mert sefer koşulları; Mehmet hareket → tümü | P3.3 |
| DiveRouteDefinition | routeId, departure/anchor kimlikleri, rota noktaları, gerekli tekne sınıfı, kesim/derinlik/dönüş süresi | Utku; açılma Mert → Mehmet/UI | Yakın P3.3; ileri P4.3 |
| RecordingCapability | equipmentId/seviye, doğrulanmış menzil, düşük ışık yeteneği/ışık konisi | Mert katalog → Mehmet/Utku | P4.3 |
| WorldMapState | regionId, onaylı oyuncu/aktif boatId konumları, iskele; keşfedilmiş hücre/yer/işaret ve revision | Mehmet konum, Utku keşif → Mert UI/kayıt | P3.3; keşif P4.1 |
| CampaignDayState | campaignId/dayId, saat, Running/Closing/Summary/Morning, uyuyan/etkin oyuncular, hava tohumu, revision | Mert → herkes; oyuncu/su durumu Mehmet | P4.1 |
| DaySummary | dayId, satılan av, gelir/gider, keşif/kayıp, yayın kuyruğu, ilerleme, kapanış kimliği | Mert; olay üreticileri A/B → UI/kayıt | P4.1; kanal P4.2 |
| RecordingClipManifest | clipId/recordingId/dayId, kameracı, doğrulanmış konu/kalite, süre, içerik hash/boyut, medya hazır durumu, güvenli dönüş | Mehmet medya, Utku sonuç → Mert arşiv/yayın | P4.2 |
| PublicationState | publicationId/clipId, NPC veya Kanal ticari hakkı, başlık/kapak, queuedDay/resultDay, izlenme/takipçi/gelir, settledId | Mert → PC/UI/kayıt | P4.2 |
| VesselOwnershipState | ownedVesselInstanceId listesi, definitionId, activeBoatId, sandık eşya kimlikleri, revision | Mert → Mehmet hareket/Utku rota/UI | P4.3 |
| SpeciesDiscoveryState | speciesId, görülme/çekim/av/araştırma kanıtları, açılmış bilgi, ödül kimlikleri | Utku → Mert ansiklopedi/kayıt | P4.1; içerik P4.4 |
| BossEncounterState | encounterId, evre, host hedef/sağlık, araştırma/yenilgi, güvenli sonuç ve rewardId | Utku; hasar Mehmet → Mert ilerleme/kayıt | P4.4 |

## Birimler ve kimlik kuralları

- Mesafe metre, süre saniye, ağırlık gram olarak tanımlanır. UI isterse kilogram gösterir.
- Para tam sayı oyun kredisi olarak tutulur; ekonomi hesabında kayan noktalı para kullanılmaz.
- Ağ zamanı için kritik süreler ev sahibinin zamanından doğrulanır; istemcinin gönderdiği kayıt süresi doğrudan kabul edilmez.
- Statik tanım kimlikleri yeniden adlandırılınca eski kaydın davranışı planlanır; görüntülenen isim kimlik yerine kullanılmaz.
- Av/eşya/çekim örnekleri tekil kimlik taşır. Oturum içi ağ nesnesi kimliği tek başına kalıcı eşya kimliği değildir.
- Her değiştirici istek requestId taşır; ev sahibi aynı isteği ikinci kez yeni işlem olarak uygulamaz.
- Ortak para ve envanter değişimleri artan revision ile yayınlanır; eski cevap yeni durumu geri alamaz.

## Yetki ve iş akışları

Oyuncu kendi girdisini, kamerasını ve yerel görsel/ses geri bildirimini yönetir. Sonuç doğuran canlı, oksijen, eşya, para ve ilerleme değişikliklerini ev sahibi doğrular. Bu model ev sahibine güvenilen arkadaş co-op modelidir; ev sahibine karşı tam hile koruması iddiası yoktur.

### Av alma

P2 #26 uygulaması: Composition içindeki `DiveInventoryBinding`, World'ün `IDiveContext` ve `ICatchClaimSink` arayüzlerini uygular. Host/Dive durumunda bağlanır; dalış/oturum kapanınca ve bileşen devre dışı kalınca kendi bağlarını kaldırır. `SessionNetworkAdapter` aynı nesnede tek `InventoryManager` kullanır. Sonuç eşlemesi: Ok → Accepted; InventoryFull → InventoryFull; WrongPhase/PlayerInactive → InvalidState; InvalidTarget/AlreadyClaimed → InvalidTarget; bilinmeyen → Rejected. [Doğrulama ve kalanlar](../reports/P2-COMPOSITION.md).

1. Mehmet, playerId/requestId/av kimliği ile toplama isteği gönderir.
2. Ev sahibi oyuncunun canlılığını, dalışını, mesafesini ve avın alınabilirliğini denetler.
3. Utku'nun av kaydı ile Mert'in çanta kapasitesi birlikte doğrulanır.
4. Avı alınmış işaretleme ve çantaya ekleme tek mantıksal işlem olarak tamamlanır; kapasite yetersizse av yerde kalır.
5. Sonuç ve güncel envanter ilgili oyunculara gönderilir; ikinci eşzamanlı istek reddedilir.

Utku'nun av tüketme işlemi, Mert eklemeyi kabul etmeden çalıştırılamaz. Bu koordinasyonun kod konumu P2 işlerine başlamadan belirlenir; iki kişi ayrı toplama otoritesi yazmaz.

### Çekim değerlendirme

1. Mehmet değerlendirme isteğinde kamera niyetini ve hedefi bildirir; görüntü dosyası bu isteğin yerine geçmez. P3 değerlendirme hattıdır; P4.2 klip verisini ayrı medya/aktarım hattında taşır.
2. Utku ev sahibinde kayıt aralığı, görüş hattı, hedefin etkinliği, mesafe ve kadraj koşullarını denetler.
3. Kalite, doğrulanmış örneklerden hesaplanır. İstemcinin "kalite=100" veya "süre=60" beyanı ödül kaynağı değildir.
4. İlk sürüm önerisi: aynı dalışta aynı tür/olay için ekip çapında yalnızca en iyi geçerli kayıt ödüle aday kalır; farklı oyuncuların aynı hedefi kaydetmesi çoğaltma yaratmaz.
5. Oyuncu/tür-olay başına en iyi kayıt saklanır. En iyi kaydın sahibi güvenli dönemezse sonraki en iyi güvenli kayıt seçilir. Dalış/tür-olay başına yalnız tek kayıt ödüllendirilir.
6. Bir kayıt ödendiğinde yeniden değerlendirme isteği ikinci ödeme oluşturmaz.

Kalite eşikleri ve fiyat katsayıları Utku/Mert'in ortak veri tablosunda tutulur. Utku kaliteyi, Mert krediyi belirler. P3.2'de güvenli kayıt NPC'ye teslimle ödenir; P4.2'de bunun alternatifi ev PC'sindeki kanal yayınıdır. Aynı dalış/konu için en iyi güvenli kayıt tek ticari hak kazanır; NPC ve kanal aynı hakkı iki kez ödeyemez. Diğer klipler arşivde izlenebilir. P4 kamera yetenekleri görüş/kadraj/süre ve tek ödeme denetimini atlayamaz.

13 Eylül 2026 Composition bağlantısı (tarihsel uygulama; NPC teslimine dönüşüm aşağıda):
- `RecordingWorldBinding`, hostta `RecordingEvaluation.Bind(RecordingDirector)` ve oyuncu bazında `RecorderViews` adaptörlerini kurar. Misafirin bakışı hostun doğruladığı input yaw/pitch değerlerinden gelir; hostta kapalı olan misafir kamerasının dönüşü kullanılmaz.
- `DiveContext` sahibi mevcut `DiveInventoryBinding` olarak kalır. Dalış bitince kamera/evaluation bağlantıları bırakılır; yeni dalış yeni Director kullanır. Stop, Start'ta kilitlenen hedefi kullanır.
- `IsPayable` ve tekrar kayıt koruması Director içindedir; Composition Stop sırasında para ödemez. `InventoryManager.OnDiveSummaryReady` geldiğinde `Director.SettleDive(summary)` ödeme hattına iletilir.
- **Ödeme backend'i henüz eksik:** mevcut `EconomyManager.PriceFor` yalnız av fiyatıdır; çekim fiyatı/ödeme API'si değildir. Mert'in gerçek, tekrar ödemeyen kayıt API'si hazır olunca `RecordingWorldBinding.SetPaymentHandler(Func<RecordingResult, PlayerActionResult>)` ile bağlanır. Yalnız gerçek kredi işlemi `Accepted` döndürür. Sayısal fiyatlar Mert'in sorumluluğundadır.
- Ödeme handler'ı yokken `RecordingClaim` bilinçli olarak unbound kalır; sonuçlar sıfır krediyle ödenmiş sayılmaz. Dalış özetiyle bekletilen kayıtlar aynı oturumda handler sonradan bağlanınca işlenebilir, aktif sonraki dalışa karışmaz. Oturum kapanınca temizlenir; kalıcı kayıt henüz bu bağlantının kapsamında değildir.

### Satın alma ve ekipman etkisi

1. Mert ev sahibinde fiyatı, bakiyeyi, satın alma iznini, mevcut ekipmanı ve requestId'yi doğrular.
2. Para düşme ve ekipman oluşturma tek işlemdir; biri olup diğeri kaybolamaz.
3. Kampanya kontrol noktası güncellenir; ardından sonuç istemciye başarı olarak bildirilir.
4. Ekipman örneği aynı anda birden fazla dalgıca tahsis edilemez; tahsisi Mert doğrular.
5. Mehmet yeni LoadoutState üzerinden özellikleri temel değerlerden yeniden hesaplar. Aynı bildirimin tekrarı bonusu tekrar eklemez.
6. İlk sürümde satın alma/tahsis kasabada yapılır; dalış sırasında ekipman yükseltme yoktur.

Plan 3.0: yeni kampanya ücretsiz kıyı av setiyle başlar; kamera ayrı satın alınır. Kamera fiziksel bir ekipman örneğidir, dört oyuncuya tek örnek eşzamanlı tahsis edilemez. Eski kayıttaki meşru kamera/ekipman geri alınmaz; yalnız yeni kampanya başlangıcı bu kurala uyar.

### Dalış sonu ve kalıcı kayıt

- D06: kampanya kayıt sahibi host'un bilgisayarındadır. O host yokken diğerleri aynı kampanyayı sürdüremez; başka host'un açtığı ayrı kampanya öncekinin ilerlemesini otomatik almaz. Manuel kayıt aktarımı, bulut senkronu ve host devri v1 kapsamında değildir.
- Güvenli dönüşte uygun geçici av/çekimler kampanyanın bekleyen satış/değerlendirme verisine aday olur. Erken dönen oyuncunun verisi kilitlenir; bütün katılımcılar Returned/Passive olunca tek tamamlanmış dalış kontrol noktası yazılır. Kıyıya veya sandala çıkmak otomatik ödeme değildir.
- Satış/satın alma/görev ödülü aynı kalıcı işlem güncellemesinde işlenir; yeniden açılışta yinelenen para üretmez.
- Kayıt geçici dosyaya yazılır, doğrulanır ve güvenli şekilde önceki kaydın yerini alır; son sağlam yedek korunur.
- Disk hatası başarı gibi gösterilmez. İşlem geri alınır veya yeniden denenebilir hatada bırakılır; kural P3 işlerine başlamadan seçilir.
- Ev sahibi dalış sırasında giderse tamamlanmamış dalış geri yüklenmez; son tamamlanmış kontrol noktası kullanılır.
- Bir sonraki dalışta eski diveId'ye ait sayaç, av, olay veya işlem isteği yeniden kullanılamaz.

### İnsan ve elde ekipman sunumu — P3 taslak

Üretim Mert, hareket/ekipman entegrasyonu Mehmet, su geçişi Utku ile koordine edilir.

1. Hareket ve oyun sonucu mevcut host otoritesinde kalır. Görsel insan modeli onaylı hız/yön ile animasyon yapar; animasyonun kök hareketi ağ oyuncusunu ikinci kez hareket ettirmez.
2. Yerel kamera yalnız sahibince kontrol edilir; yerel el/kol ve uzak tam vücut görünürlüğü ayrıdır. Diğer oyuncu kendi kamerasını veya birinci şahıs kollarını kontrol edemez.
3. `LocomotionMode` en az kara, su üstü, sualtı, oturmuş ve pasif hâllerini ayırır. Etkin kamera/zıpkın modu tutuşla birlikte yayınlanır; her kemiği her kare ağdan göndermek şart değildir.
4. Mert'in onaylı loadout'u equipmentId/seviyeyi belirler; Mehmet aynı veriden etki ve görünümü seçer. Satın almadan bir üst kamera/tüp görünümü veya etkisi elde edilemez. Gecikmiş revision eski modeli geri getiremez.
5. Kuşanma/kullanma/saklama isteği oyuncu sahipliği, aşama ve durumla doğrulanır; görsel kayıt lambası doğrulanmış kayıt durumunu izler. Model/prefab referansı istemciden yetki olarak kabul edilmez.
6. Yakınlık/koltuk/su durumunu oyun collider'ı belirler; görsel el veya yüzme klibi av alma/hasar/ışın denetimini değiştirmez.

Kabul: iki oyuncu birbirini karada, su üstünde, sualtında ve kamera kullanımında izler; dört oyuncuda aynı ekipman/tutuş durumu görülür. Test sahnesi görüntüsü tek başına gerçek prefab entegrasyonu kanıtı değildir.

### Fiziksel NPC, av satışı ve kayıt teslimi — P3 taslak

Mert servis/ekonomi/envanter sahibi; Mehmet yaklaşma/etkileşim üreticisi; Utku/Mehmet kayıt adayı üreticileridir. P3.2 öncesi birlikte kesinleşir.

1. İstek, `requestId`, `serviceId`, işlem türü ve seçilen tekil av/kayıt/ekipman kimliklerini taşır. Oyuncu kimliği hostta bağlantıdan alınır.
2. Host doğru NPC'yi/hizmeti, gerçek oyuncu mesafesini, etkileşim erişimini, kasaba/satış aşamasını ve veri revision'ını doğrular. İstemci UI'ının açık olması izin değildir; sualtından veya yanlış dükkândan işlem yapılamaz.
3. Güvenli dönüş avı **satılmamış** tutar. Çanta dünyadaki görsel temsil, envanter tek gerçek kaynaktır. Oyuncunun taşıdığı av ve ortak emanetteki av aynı kimliği iki yerde sahiplenemez; emanet teslimi kapasiteyi doğrular.
4. Av satışı seçilen sahip olunan avları tüketme, doğru fiyatı bir kez ödeme, sold kimliğini yazma ve kalıcı kontrol noktasını güncelleme işlemidir. Kapasite/para UI'ı sadece onaylı sonuçtan sonra güncellenir.
5. Kayıtta mevcut dalış/tür-olay başına en iyi güvenli sonuç kuralı korunur. NPC yalnız bekleyen ve ticari hakkı kullanılmamış adayı kabul eder; iki oyuncu aynı adayı teslim ederse bir kez ödeme olur. P4.2'de kanal kuyruğuna verilmiş aday NPC'ye tekrar satılamaz.
6. **Mevcut ödeme hattındaki değişiklik:** `Director.SettleDive(summary)` / `RecordingWorldBinding` dalış sonunu adayı sabitlemek için kullanır. P3.2'de burada doğrudan kredi verilemez; NPC işlemi gerçek ödeme servisini çağırır. Otomatik av satışı yolu varsa aynı şekilde aday oluşturma ile değiştirilir. Eski doğrudan ödeme ile yeni NPC yolu aynı anda açık bırakılmaz.
7. Disk hatasında ürün tüketilmiş/para kazanılmış başarı mesajı verilmez; tek işlem geri alınır veya aynı kimlikle yeniden denenebilir hata olarak tutulur. Tekrar deneme yeni av/kayıt kimliği yaratmaz.
8. Satın alma ve tahsis aynı NPC doğrulamasından geçer. Ortak eşya aynı anda birden fazla dalgıca takılmaz. P3.1 kamera görünümü ile P3.2 satın alma sonucu aynı loadout üzerinden birleşir.

Kabul: uzaktan istek, yanlış NPC, iki oyuncunun aynı av/kaydı satması, eski UI teklifi, disk hatası ve yeniden açma denenir. P3'te NPC'ye gitmeden doğrudan kredi hata sayılır; P4.2 kanal ödemesi yalnız doğrulanmış PC yayınının günlük sonucudur.

### Sandal tamiri ve yolculuk — P3 taslak

Mehmet hareket/koltuk, Utku rota/çevre, Mert onarım/envanter/sefer/kayıt sahibidir. Aynı sandal prefabı ve sahneye sırayla bağlanırlar; üç ayrı sandal otoritesi kurulmaz.

1. Onarım ve seyahat ayrı durumlardır. Yeni kampanyada `BoatRepairState=Broken`; üç sabit parçanın her biri en çok bir kez katkı olur. Parça sahipliği/mesafesi/onarım noktası hostta doğrulanır; tüketim + katkı + kayıt atomiktir.
2. Parçalar başlangıç kasabası/kumsalından ücretsiz bulunabilir veya balık geliriyle satın alınabilir. Eksik kalem yeniden edinilebilir; tüketilen kalem veya ücretsiz parça döngüsü satılıp para çoğaltamaz. Son parça `Repaired` yapar; P3 yakın rotasına yeterlidir.
3. Onarım malzemesi ve kilitli sefer, normal av/ekipman sistemine ikinci otorite yaratmaz. Dünya parçaları tekil kimlikle envantere/onarım verisine bağlanır; genel crafting gerekmiyor.
4. `BoatTripState` küresel `SessionState`'ten ayrıdır. Kıyıdan dalan oyuncular ile sandal seferine katılanlar aynı DiveId içinde olabilir. Kıyıdan her suya giriş veya sandal varışı yeni dalış başlatmaz.
5. Host koltuğa yakınlık, tek oyuncu/koltuk, azami dört yolcu, hazır durumu, tamir ve rota kilidini doğrular. Sabit rota hareketini host yürütür; yolcular koltuk bağlantı noktalarına göre eşlenir. Hareket sırasında inme reddedilir.
6. İlk binen oyuncu sefer sorumlusu olabilir; yalnız doğrulanmış yolcu rota seçer/başlatır. Yetki kopmada yaşayan yolcuya devredilir. Bu sadece sandal kontrolüdür, ağ host devri değildir.
7. Demirleme noktasında sefer grubu korunur. Dönüş için etkin sefer dalgıçları tekrar binmiş olmalıdır; kıyıdan bağımsız dalan oyuncu yanlışlıkla bu gruba eklenmez. Pasif/kopan kişinin engeli temizlenir; diğer etkin dalgıç geride bırakılmaz.
8. Boş sandalı geri çağırma/otomatik iskeleye döndürme ancak etkin sefer dalgıcı kalmadığında mümkündür. Tekrarlı çağrı ikinci sandal üretmez. İskelede bekleyen oyuncular boş araca yeniden erişebilir.
9. Sandala çıkış nefes almayı sağlar fakat tüpü tamamen doldurmaz; kalıcı güvenli dönüş ve satış hakkı kıyı/iskele sınırından gelir. Herkes pasifse D07, host koparsa D06/D08 uygulanır.
10. Son tamamlanmış kampanya kaydı tamir/menzil/açılmış rota durumunu tutar. Hareket hâlindeki sefer yeniden yüklenmez; kampanya açılınca sandal iskelede ve koltuklar boştur. Tamamlanmamış dalışın avı kayda taşınmaz.

Kabul: solo üç parçayla onarım, iki kişinin aynı parçayı eklemesi, dört koltuk, gidiş/dönüş, seyahatte kopma, dalgıç suda iken geri dönüş reddi, boş sandal çağrısı ve tekrar açma. Yerel testten sonra en az iki bilgisayardaki dört süreçte denenir.

### Bekleyen eşya, onarım ve kayıt uyumu — P3 taslak

- Mevcut kayıt yalnız host loadout'unu koruyan uygulama sınırına sahiptir ([14 Eylül kaydı](../reports/P3-WIP-HANDOFF.md)). Plan 3.0, bütün bu ek verinin zaten kaydedildiği iddiası değildir.
- Yeni şema; bekleyen av/kayıt, itemInstanceId başına tek taşıyıcı/emanet, ortak ekipman sahipliği, tamamlanmış onarım kalemleri, sandal seviyesi/rota açılımı ve consumed/paid/sold kimliklerini taşır. P4 alanları gerektiği fazda eklenir.
- Guest bağlantı ID'si kalıcı oyuncu kimliği sayılmaz. Kopmada/yeniden açmada ona bağlı korunan av ve ortak ekipman kasaba emanetine serbest bırakılır; yeni bağlantı ID'sine kendiliğinden tahsis edilmez. Tekrar tahsis host onayından geçer. Host'un mevcut yükleme davranışı korunur.
- Satılmayan taşınan av kapasite tüketir. Sonraki dalışa götürülürse aynı eşya aktif dalış riskine geçirilir; kasabada ikinci güvenli kopyası bırakılmaz. D07 kaybı tamamlanmış başarısız dalış kaydında işlenir. Host çökmesinde son tamamlanmış kontrol noktasına dönüş sınırı korunur.
- Eski schemaVersion için kontrollü göç yapılır: para/ödenmiş kimlikler/ekipman korunur; bulunmayan onarım başlangıçta bozuk, yeni bekleyen listeler boş olur. Ödenmiş eski kayıtlar NPC'de tekrar aday olamaz. Bozuk dosya yeni kampanya gibi sessizce sıfırlanmaz.
- Her şema değişikliğinde eski kayıt + yeni kayıt, başarısız yazma, host yeniden açma ve guest tahsis regresyonu gerekir. Gerçek API ve göç değişikliği ayrıca uygulanıp test edilmeden sözleşme tamamlandı yazılmaz.

### Kamera, derinlik ve rota gelişimi — P4.3 taslak

1. Mert equipmentId/seviye/fiyat/görünüm/yeteneği tek katalogda tanımlar. Mehmet kuşanma/elde model ve tüp/palet/çanta etkisini, Utku kamera menzil/düşük ışık doğrulamasını uygular.
2. Kamera 1 menzil artışı, kamera 2 düşük ışık yeteneği verir. İstemci kendi ekipman seviyesini, ışık gücünü veya kaliteyi seçemez; host tahsis edilmiş ekipmandan çözer. Işık açısı/mesafesi/görüş denetimi diğer kayıt koşullarıyla birlikte çalışır.
3. Utku aynı regionId altında kıyı/resif/derin kesim ve toplam üç deniz demirlemesi sağlar. Mert sandal/motorlu/araştırma teknesi sahipliğiyle rotayı açar; Mehmet aktif gövdeyle yolculuğu yürütür. İkinci bölge açılma sistemi kurulmaz.
4. Rota ilerlemesi ve dalgıç kapasitesi ayrıdır. UI yaklaşık derinliği, rota kilidini ve önerilen ekipmanı gösterir; büyük kamera tek başına derinlik erişimi değildir. Denge metre/fiyatları [oyun akışındaki](GAMEPLAY_LOOP.md) taslaktan testle ayarlanır.

### Harita ve kalıcı keşif — P3.3 / P4.1 taslak

Mehmet onaylı oyuncu/aktif tekne konumunu, Utku dünya-harita dönüşümünü/keşif olayını, Mert UI ve kayıt katmanını sağlar. Host yalnız gerçekten erişilmiş hücre/yerleri açar; istemci tüm haritayı açılmış gönderemez. Tekne değişince ikon yeni activeBoatId'ye bağlanır. Oyuncu/tekne ikonları keşif örtüsünün arkasında navigasyon sağlar; gizli balık/boss konumunu yayınlamaz. Keşif/ansiklopedi ödülü sabit kimlikle bir kez verilir; harita tekrar açmak ödül değildir.

### Gün sonu ve uyku işlemi — P4.1 taslak

1. Mert tek CampaignDayState otoritesidir; istemciler host saatini gösterir. Uyku isteğini Mehmet gerçek oyuncu/yatak/yakınlık/etkin durumdan doğrulatır. Aynı yatağı iki oyuncu kullanamaz.
2. En az bir etkin bağlı oyuncu ve hepsinin uyuması erken kapanış koşuludur. Kopan/pasif oyuncu kümeden çıkar; etkin oyuncu yokken gün üretilemez. 00:00 uykuya bakmadan Closing başlatır.
3. Closing yeni ticaret/yayın/seyahat isteğini kilitler; hostça önceden kabul edilenler tamamlanır. Açık dalış sonuçları D07 ve güvenli alan kontrolüyle tek kez kapatılır. Güvenli av korunur; deniz/tekne/dış demirlemede kalan yük güvenli sayılmaz.
4. DaySummary ve yeni sabah kanal sonuçları, nextDayId ve ödeme kimlikleri tek kalıcı işlemde yazılır. Yazma başarısızsa ertesi sabah başarı gibi açılmaz; aynı closeId ile tekrar denenir. Yeniden açma para/gün/sponsor ödülünü çoğaltmaz.
5. Hava/olay tohumu aynı gün için sabittir. Host kopması son sağlam kontrol noktasına döner; istemcinin bilgisayar saati ödül veya gün ilerlemesi kaynağı değildir.

### İzlenebilir medya ve kanal — P4.2 taslak

Mehmet medya/oynatma, Utku hedef/kalite, Mert PC/arşiv/yayın/para/kayıt sahibidir. Encoding/kapsayıcı/paket seçimi teknik ön denemede netleşir; herhangi bir yöntem henüz doğrulanmış değildir.

1. Yakalama sadece oyuncunun kuşandığı oyun kamerası ve oyun ortam sesidir. Başlangıç bütçesi WORLD_SYSTEMS'te verilidir; kare/süre/boyut/limitler deneme sonucu belirlenir. Oyun dışı dosya, masaüstü veya mikrofon girdisi yoktur.
2. clipId ile recordingId ayrı ve bağlıdır. İstemcinin medya üretmesi doğrulanmış kalite/ödül hakkı vermez; hosttan gelen hedef/süre/kadraj sonucu ve güvenli dönüş gerekir. Boş kadraj veya ticari aday olmayan klip arşivde oynatılabilir, ödeme üretmez.
3. Medya manifest'i beklenen byte boyutu/hash/süre ile tamamlanır; yarım aktarım hazır değildir. Bütünlük denetimi görsel hileye karşı tam güvence iddiası değildir; oyun arkadaş co-op host güven sınırını korur. Aktarım kayıt/oyun hareketini boğmayacak şekilde sınırlandırılır ve gerçek ağda ölçülür.
4. PC kontrolü yakın oyuncuya verilir; diğerleri aynı aktif klip ve zaman konumunu izler. Kontrol sahibi ayrılınca kilit bırakılır. İstemcinin dosya yolu ödül veya arşiv yetkisi değildir; medya kampanya/clipId ile çözülür.
5. Yayın, gerçek PC yakınlığı ve güvenli/tamamlanmış medya ile uygundur. Ticari hak anahtarı campaignId/diveId/subjectId için tek en iyi adaydır; NPC veya Kanal seçimi hak kullanımıyla aynı işlemde kaydolur. Metadata yeniden adlandırma yeni hak yaratmaz.
6. Gün N'de onaylanmış publicationId sonucu N+1 sabahında bir kez ödenir. İzlenme/takipçi/gelir hostun sabitlenmiş kalite/yenilik/risk kurallarından gelir. İstemci veya video dosyası takipçi/para bildiremez.
7. P3 eski puan kayıtları izlenebilir medya gibi gösterilmez; uygun eski kayıt NPC yolunda bir kez ödenebilir. Silinen klibin yayın/ödeme geçmişi kalır; dosyayı silip yeniden ekleme tekrar kazandıramaz. Favoriler sessizce silinmez, dolu disk/yazma hatası görünürdür.
8. D06 korunur: kampanya ve ortak arşiv hostta kalır; başka host otomatik devralmaz. Yerel yeniden açma ve guest'e klip oynatma gerçek testtir; harici sosyal medyaya paylaşım bu API'nin işi değildir.

### Tekne satın alma ve aktif araç — P4.3 taslak

Mert limandaki fiyat/sahiplik/envanter/kayıt işlemini, Mehmet gövde/koltuk/hareketi, Utku rota uygunluğunu sağlar. Üç tanım (sandal/motorlu/araştırma) farklı gövde/kapasite/rota verisi taşır. Satın alma tekil vesselInstanceId üretir; eski araç silinmez. Aktif değişim sadece bütün oyuncular güvenli dönmüş, sefer yokken yapılır; sandık eşya aktarımı/koltuk temizleme/activeBoatId/kayıt atomiktir. Tekne sandığı denizde güvenli depo sayılmaz; batma veya araç satıp çoğaltma sistemi eklenmez.

### Boss, görev ve araştırma — P4.4 / P4.5 taslak

Utku hostta boss hedef/evre/sağlık ve tek encounterId; Mehmet hasar/oyuncu/ekipman; Mert görev/ödül/kayıt sahibidir. Araştırma ve av sonucu aynı ana görev ödülünü ikinci kez oluşturamaz. Boss sonucu güvenli dönüş checkpoint'ine bağlanır; tamamlanmamış dalışın sonucu host ayrılmasında geriye dönebilir. Normal avcı türü gerçek boss davranışının yerine geçmez; üç okunur saldırı/zayıf nokta ve solo–dört oyuncu kontrolü gerekir.

Sponsor/sipariş, keşif/ödeme/rol/geliştirme olaylarını kimlikleriyle tüketir; erişilmeyen hedefi görev olarak üretmez. Rol bonusları loadout gibi temel değerden yeniden hesaplanır; ücretsiz rol değişimi bonus katlamaz. Gün/kanal/tekne/ansiklopedi/boss/ev için şema göçü, eski kaydı geri yükleme ve tekrarlı sonuç testi ilgili ara teslimin işidir.

## Ortak red sonuçları

En az şu nedenler ayrıştırılmalıdır: `WrongPhase`, `InvalidTarget`, `OutOfRange`, `NotVisible`, `InventoryFull`, `AlreadyClaimed`, `AlreadyProcessed`, `InsufficientFunds`, `PlayerInactive`, `SaveFailed`, `SessionClosed`.

Plan 3.0'da gerekirse `WrongService`, `BoatNotRepaired`, `SeatOccupied`, `TripInProgress`, `DiversStillOutside`, `RouteLocked`, `DayClosing`, `MediaNotReady`, `ArchiveFull`, `PublicationAlreadyQueued`, `RightsConsumed`, `VesselInUse` ayrıştırılır. İsimler uygulamadan önce netleşir; UI bekleme/red nedenini açıklar.

Bu isimler ilgili fazın API tasarımında kesinleştirilir. Red, durum değişikliği yapmadan anlaşılır UI geri bildirimi üretmelidir. Ağ tekrarı gibi AlreadyProcessed sonucu varsa daha önceki sonuç döndürülebilir.

## Sözleşme değişikliği

1. Değişiklik gerekçesi ve etkilenen üretici/tüketiciler yazılır.
2. Etkilenen kişiler değişikliği inceler; kapsam büyüyorsa plan değişikliği süreci uygulanır.
3. Test sağlayıcıları, gerçek uygulamalar, kayıt/ağ uyumluluğu ve bu belge birlikte güncellenir.
4. Birleştirme dalında derleme ve ilgili regresyon geçmeden sözleşme tamamlanmış sayılmaz.

## Hangi ayrıntı ne zaman kesinleşir?

| Zaman | Birlikte netleştirilecek konu |
|---|---|
| P0 | Oyuncu kimliği, oturum aşaması, sahne geçişi ve P1'de gereken temel sınırlar |
| P2 başlamadan | Av/toplama/çanta API'si, kapasite reddi, güvenli dönüş ve pasif/kopan oyuncunun avı |
| P3 başlamadan | Çekim değerlendirmesi, tekrar ödül, ortak para, ekipman tahsisi, kayıt/yedek/disk hatası |
| P3.1 öncesi (revizyon) | İnsan sunum durumları, el/uzak gövde, rig ve ekipman model/poz eşlemesi — Mehmet/Mert, su durumunda Utku |
| P3.2 öncesi (revizyon) | NPC mesafe/işlem, otomatik ödemeden teslim adayına dönüşüm, çanta/emanet ve şema göçü — üç alan sahibi |
| P3.3 öncesi (revizyon) | Onarım parça otoritesi, sefer/koltuk/rota, erken dönüş/kopma — Mehmet/Utku/Mert |
| P4.1 öncesi | DayState/DiveState ayrımı, uyku/00:00, güvenli yük, harita/keşif ve günlük kayıt işlemi |
| P4.2 öncesi | Gerçek klip teknik ön denemesi, medya manifest/aktarım/oynatma, NPC veya kanal tek hak ve günlük ödeme |
| P4.3 öncesi | Üç tekne sahipliği/aktif değişim/sandık, üst kamera yetenekleri ve rota/harita verisi |
| P4.4/5 öncesi | Boss/araştırma/ödül, ansiklopedi kanıtları, uygun sipariş/sponsor ve rol/geliştirme verileri |

Bağlantının iki tarafını yazacak kişiler kısa bir görüşmede alanları ve örnek sonucu netleştirir. Ayrı imza matrisi gerekmez; değişiklik bu belgeye ve ilgili göreve yazılır. Hiç kimse diğer tarafın beklediği veri tipini sessizce değiştirmez.

P2 başlangıcı: Mehmet, Utku ve Mert av/vuruş/çanta istek ve sonuçlarını bağımlı koddan önce netleştirir. Bu açılış kaydı sözleşme uzlaşısı değildir.
