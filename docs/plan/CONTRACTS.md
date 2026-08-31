# Ortak sistem sözleşmeleri

Durum: ihtiyaç duyulan fazdan önce birlikte kesinleştirilecek tasarım. Buradaki tip isimleri uygulanmış sınıflar veya mevcut dosyalar değildir. Görev kodları: A=Mehmet, B=Utku, C=Mert.

Amaç: Mehmet, Utku ve Mert'in birbirine bağlanabilen sistemler üretmesi. P0'da yalnızca P1 için gereken kimlik/oturum bağlantıları kesinleşir. Av/çanta ayrıntıları P2 öncesinde, çekim/ekonomi/kayıt ayrıntıları P3 öncesinde netleşir. İleriki fazın taslağı o özelliği erken uygulama izni vermez.

## Sistem sahipliği

Bu tablo uygulama sahipliğidir; görsel/ses üretim yöntemi, tasarımda son söz veya insan netcode inceleyicisini kendiliğinden belirlemez. Bunlar [P0 toplantısında](P0_MEETING.md) seçilir. Ağ inceleyicisi atanması, alan sahiplerinin kendi co-op uygulama ve test sorumluluğunu devretmez.

| Alan | Sahip | Sınır |
|---|---|---|
| Oyuncu kontrolü ve ekipman kullanımı | Mehmet | Giriş, yürüme/yüzme, zıpkın/kamera kullanım isteği, oksijen/sağlık, kuşanılan ekipmanın etkisi |
| Ağ bağlantısı | Mehmet | Bağlan/ayrıl, oyuncu oluşumu, iletişim/transport, ağ sahne yükleme mekanizması |
| Oturum aşamaları | Mert | Hazırlık/dalış/dönüş/sonuç durumu, hazır oyuncular ve geçiş koşulları; gerçek yüklemeyi Mehmet'in ağ servisi yürütür |
| Canlılar ve dünya | Utku | Tür tanımları, oluşma, AI, vurulma/ölüm, av nesnesi ve çevre içerikleri |
| Çekim değerlendirme | Utku | Hedef görünürlüğü, mesafe, kayıt süresi, kalite ve dalış içi tekrar kuralı |
| Çanta/depo ve ekipman sahipliği | Mert | Tekil eşya örnekleri, kapasite doğrulaması, depoya aktarım, ekipman satın alma ve tahsis |
| Ekonomi ve ilerleme | Mert | Satış fiyatı, çekim ödemesi, para, geliştirmeler, görevler, bölge açılması |
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
| RecordingCandidate | requestId, diveId, playerId, hedef kimliği, başlat/bitir bilgisi | Mehmet → Utku | P3 |
| RecordingResult | recordingId, diveId, playerId, tür/olay kimliği, kalite, geçerli süre | Utku → Mert/Mehmet | P3 |
| EquipmentDefinition | equipmentId, yuva, seviye, etkiler; para/alış fiyatının sahibi Mert | Mert → Mehmet | P3 |
| LoadoutState | playerId, takılı equipmentInstanceId değerleri ve revision | Mert → Mehmet | P3 |
| DiveSummary | Güvenli dönenler, korunan av/çekim kimlikleri, kayıplar ve kontrol noktası kimliği | Mert → Mehmet/Utku/UI | P2; parasal alanlar P3 |
| TransactionResult | requestId, kabul/red, reasonCode, etkilenen revision | İşlemin sahibi → isteği yapan/UI | P2 |
| SaveSnapshot | schemaVersion, campaignId, checkpointId, ortak para/depo/ekipman/ilerleme ve tamamlanmış ödeme kimlikleri | Mert → disk/yedek | P3 |

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

1. Mehmet, playerId/requestId/av kimliği ile toplama isteği gönderir.
2. Ev sahibi oyuncunun canlılığını, dalışını, mesafesini ve avın alınabilirliğini denetler.
3. Utku'nun av kaydı ile Mert'in çanta kapasitesi birlikte doğrulanır.
4. Avı alınmış işaretleme ve çantaya ekleme tek mantıksal işlem olarak tamamlanır; kapasite yetersizse av yerde kalır.
5. Sonuç ve güncel envanter ilgili oyunculara gönderilir; ikinci eşzamanlı istek reddedilir.

Utku'nun av tüketme işlemi, Mert eklemeyi kabul etmeden çalıştırılamaz. Bu koordinasyonun kod konumu P2 işlerine başlamadan belirlenir; iki kişi ayrı toplama otoritesi yazmaz.

### Çekim değerlendirme

1. Mehmet kamera niyetini ve hedefi bildirir; gerçek video akışı göndermez.
2. Utku ev sahibinde kayıt aralığı, görüş hattı, hedefin etkinliği, mesafe ve kadraj koşullarını denetler.
3. Kalite, doğrulanmış örneklerden hesaplanır. İstemcinin "kalite=100" veya "süre=60" beyanı ödül kaynağı değildir.
4. İlk sürüm önerisi: aynı dalışta aynı tür/olay için ekip çapında yalnızca en iyi geçerli kayıt ödüle aday kalır; farklı oyuncuların aynı hedefi kaydetmesi çoğaltma yaratmaz.
5. Mert yalnızca güvenli dönmüş uygun kayıtları ödeme adayına dönüştürür. Kayıt sahibinin başarısızlığı durumunda sonraki en iyi güvenli kayıt seçimi veya ödülsüz kalma kuralı P3 işlerine başlamadan kesinleştirilir.
6. Bir kayıt ödendiğinde yeniden değerlendirme isteği ikinci ödeme oluşturmaz.

Kalite eşikleri ve fiyat katsayıları Utku/Mert'in ortak veri tablosunda tutulur. Utku kaliteyi, Mert krediyi belirler.

### Satın alma ve ekipman etkisi

1. Mert ev sahibinde fiyatı, bakiyeyi, satın alma iznini, mevcut ekipmanı ve requestId'yi doğrular.
2. Para düşme ve ekipman oluşturma tek işlemdir; biri olup diğeri kaybolamaz.
3. Kampanya kontrol noktası güncellenir; ardından sonuç istemciye başarı olarak bildirilir.
4. Ekipman örneği aynı anda birden fazla dalgıca tahsis edilemez; tahsisi Mert doğrular.
5. Mehmet yeni LoadoutState üzerinden özellikleri temel değerlerden yeniden hesaplar. Aynı bildirimin tekrarı bonusu tekrar eklemez.
6. İlk sürümde satın alma/tahsis kasabada yapılır; dalış sırasında ekipman yükseltme yoktur.

### Dalış sonu ve kalıcı kayıt

- D06: kampanya kayıt sahibi host'un bilgisayarındadır. O host yokken diğerleri aynı kampanyayı sürdüremez; başka host'un açtığı ayrı kampanya öncekinin ilerlemesini otomatik almaz. Manuel kayıt aktarımı, bulut senkronu ve host devri v1 kapsamında değildir.
- Güvenli dönüşte uygun geçici av/çekimler kampanyanın bekleyen satış/değerlendirme verisine aktarılır ve kontrol noktası yazılır.
- Satış/satın alma/görev ödülü aynı kalıcı işlem güncellemesinde işlenir; yeniden açılışta yinelenen para üretmez.
- Kayıt geçici dosyaya yazılır, doğrulanır ve güvenli şekilde önceki kaydın yerini alır; son sağlam yedek korunur.
- Disk hatası başarı gibi gösterilmez. İşlem geri alınır veya yeniden denenebilir hatada bırakılır; kural P3 işlerine başlamadan seçilir.
- Ev sahibi dalış sırasında giderse tamamlanmamış dalış geri yüklenmez; son tamamlanmış kontrol noktası kullanılır.
- Bir sonraki dalışta eski diveId'ye ait sayaç, av, olay veya işlem isteği yeniden kullanılamaz.

## Ortak red sonuçları

En az şu nedenler ayrıştırılmalıdır: `WrongPhase`, `InvalidTarget`, `OutOfRange`, `NotVisible`, `InventoryFull`, `AlreadyClaimed`, `AlreadyProcessed`, `InsufficientFunds`, `PlayerInactive`, `SaveFailed`, `SessionClosed`.

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
| P4 başlamadan | Aynı bölgedeki görev/ekipman/keşif ilerlemesi ve içerik verileri |

Bağlantının iki tarafını yazacak kişiler kısa bir görüşmede alanları ve örnek sonucu netleştirir. Ayrı imza matrisi gerekmez; değişiklik bu belgeye ve ilgili göreve yazılır. Hiç kimse diğer tarafın beklediği veri tipini sessizce değiştirmez.
