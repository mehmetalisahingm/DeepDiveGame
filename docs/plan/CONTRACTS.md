# Ortak sistem sözleşmeleri

Durum: P0'da ekip tarafından kesinleştirilecek tasarım. Buradaki isimler uygulanmış sınıflar veya mevcut dosyalar değildir.

Amaç: A, B ve C'nin farklı veri tipleri, farklı yetki kuralları veya birbiriyle konuşamayan sistemler üretmesini önlemek. Gerçek API imzaları P0'da bu belgeye göre sabitlenir. İleriki fazın sözleşmesinin bulunması o özelliği erken uygulama izni vermez.

## Sistem sahipliği

| Alan | Sahip | Sınır |
|---|---|---|
| Oyuncu kontrolü ve ekipman kullanımı | A | Giriş, yürüme/yüzme, zıpkın/kamera kullanım isteği, oksijen/sağlık, kuşanılan ekipmanın etkisi |
| Ağ bağlantısı | A | Bağlan/ayrıl, oyuncu oluşumu, iletişim/transport, ağ sahne yükleme mekanizması |
| Oturum aşamaları | C | Hazırlık/dalış/dönüş/sonuç durumu, hazır oyuncular ve geçiş koşulları; gerçek yüklemeyi A'nın ağ servisi yürütür |
| Canlılar ve dünya | B | Tür tanımları, oluşma, AI, vurulma/ölüm, av nesnesi ve çevre içerikleri |
| Çekim değerlendirme | B | Hedef görünürlüğü, mesafe, kayıt süresi, kalite ve dalış içi tekrar kuralı |
| Çanta/depo ve ekipman sahipliği | C | Tekil eşya örnekleri, kapasite doğrulaması, depoya aktarım, ekipman satın alma ve tahsis |
| Ekonomi ve ilerleme | C | Satış fiyatı, çekim ödemesi, para, geliştirmeler, görevler, bölge açılması |
| Kalıcı kayıt | C | Şema sürümü, tamamlanmış kontrol noktaları, bütünlük ve yedek |
| Alan arayüzleri | İlgili alan sahibi | A dalgıç HUD'ı, B hedef/kalite geri bildirimi verisi, C envanter/dükkân/oturum ekranı |
| Ortak veri ve bileşim | A/B/C incelemesi; entegratör koordinasyonu | Kimlikler, arayüzler, başlangıç bağlantıları ve ortak yapılandırma |

## Bağımlılık yönü

Önerilen mantıksal modüller: `Core.Contracts`, `Player`, `World`, `Progression`, `Session` ve `Composition`.

- Alan modülleri ortak sözleşmelere bağımlıdır; birbirlerinin iç sınıflarını veya sahne nesnelerini arayarak iş yapmaz.
- `Composition`, gerçek uygulamaları başlangıçta birbirine bağlar. Küçük ve açık bir bileşim noktası yeterlidir; özel bir framework yazılmaz.
- Static tür/eşya tanımları Unity varlıklarıyla tutulabilir. Ağda ve kayıtta sabit tanım kimliği taşınır; ScriptableObject nesnesi kalıcı oyuncu verisi gibi kullanılmaz.
- Çalışan durum, test sağlayıcısı ve kalıcı kayıt birbirinden ayrılır. Test sağlayıcıları teslim build'inin gerçek oyun yolunda aktif bırakılamaz.
- İsimler ve assembly sınırları P0'da kesinleştirilir; sonradan tek taraflı yeniden adlandırma yapılmaz.

## Ortak veri sözlüğü

| Veri | Asgari alanlar ve kural | Üretici → tüketici | İlk gerçek kullanım |
|---|---|---|---|
| PlayerId | Oturum oyuncu kimliği; ağ bağlantı kimliğiyle eşleştirme. Kalıcı kampanya sahibiyle karıştırılmaz | A → tümü | P1 |
| SessionState | Aşama, sessionId, isteğe bağlı diveId, regionId ve revision | C → A/B/UI | P1 |
| DiveId | Her dalış için tekil kimlik; sonraki dalışta değişir | C → tümü | P2 |
| SpeciesDefinition | Sabit speciesId, sınıf, davranış parametreleri, ağırlık aralığı | B → A/C | P2 |
| ItemDefinition | Sabit itemId, kategori, kapasite/fiyatla ilgili temel tanımlar | C; B tür eşlemesini inceler → tümü | P2 |
| CaptureResult | captureId, diveId, speciesId, ağırlık, varsa kalite, av nesnesi kimliği | B → C | P2 |
| InventoryState | playerId, itemInstanceId listesi, mevcut ağırlık, kapasite, revision | C → A/UI | P2 |
| RecordingCandidate | requestId, diveId, playerId, hedef kimliği, başlat/bitir bilgisi | A → B | P3 |
| RecordingResult | recordingId, diveId, playerId, tür/olay kimliği, kalite, geçerli süre | B → C/A | P3 |
| EquipmentDefinition | equipmentId, yuva, seviye, etkiler; para/alış fiyatının sahibi C | C → A | P3 |
| LoadoutState | playerId, takılı equipmentInstanceId değerleri ve revision | C → A | P3 |
| DiveSummary | Güvenli dönenler, korunan av/çekim kimlikleri, kayıplar ve kontrol noktası kimliği | C → A/B/UI | P2; parasal alanlar P3 |
| TransactionResult | requestId, kabul/red, reasonCode, etkilenen revision | İşlemin sahibi → isteği yapan/UI | P2 |
| SaveSnapshot | schemaVersion, campaignId, checkpointId, ortak para/depo/ekipman/ilerleme ve tamamlanmış ödeme kimlikleri | C → disk/yedek | P3 |

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

1. A, playerId/requestId/av kimliği ile toplama isteği gönderir.
2. Ev sahibi oyuncunun canlılığını, dalışını, mesafesini ve avın alınabilirliğini denetler.
3. B'nin av kaydı ile C'nin çanta kapasitesi birlikte doğrulanır.
4. Avı alınmış işaretleme ve çantaya ekleme tek mantıksal işlem olarak tamamlanır; kapasite yetersizse av yerde kalır.
5. Sonuç ve güncel envanter ilgili oyunculara gönderilir; ikinci eşzamanlı istek reddedilir.

B'nin av tüketme işlemi, C eklemeyi kabul etmeden çalıştırılamaz. Bu koordinasyonun kod konumu P0'da belirlenir; iki kişi ayrı toplama otoritesi yazmaz.

### Çekim değerlendirme

1. A kamera niyetini ve hedefi bildirir; gerçek video akışı göndermez.
2. B ev sahibinde kayıt aralığı, görüş hattı, hedefin etkinliği, mesafe ve kadraj koşullarını denetler.
3. Kalite, doğrulanmış örneklerden hesaplanır. İstemcinin "kalite=100" veya "süre=60" beyanı ödül kaynağı değildir.
4. İlk sürüm önerisi: aynı dalışta aynı tür/olay için ekip çapında yalnızca en iyi geçerli kayıt ödüle aday kalır; farklı oyuncuların aynı hedefi kaydetmesi çoğaltma yaratmaz.
5. C yalnızca güvenli dönmüş uygun kayıtları ödeme adayına dönüştürür. Kayıt sahibinin başarısızlığı durumunda sonraki en iyi güvenli kayıt seçimi veya ödülsüz kalma kuralı P0'da kesinleştirilir.
6. Bir kayıt ödendiğinde yeniden değerlendirme isteği ikinci ödeme oluşturmaz.

Kalite eşikleri ve fiyat katsayıları B/C'nin ortak veri tablosunda tutulur. B kaliteyi, C krediyi belirler.

### Satın alma ve ekipman etkisi

1. C ev sahibinde fiyatı, bakiyeyi, satın alma iznini, mevcut ekipmanı ve requestId'yi doğrular.
2. Para düşme ve ekipman oluşturma tek işlemdir; biri olup diğeri kaybolamaz.
3. Kampanya kontrol noktası güncellenir; ardından sonuç istemciye başarı olarak bildirilir.
4. Ekipman örneği aynı anda birden fazla dalgıca tahsis edilemez; tahsisi C doğrular.
5. A yeni LoadoutState üzerinden özellikleri temel değerlerden yeniden hesaplar. Aynı bildirimin tekrarı bonusu tekrar eklemez.
6. İlk sürümde satın alma/tahsis kasabada yapılır; dalış sırasında ekipman yükseltme yoktur.

### Dalış sonu ve kalıcı kayıt

- Güvenli dönüşte uygun geçici av/çekimler kampanyanın bekleyen satış/değerlendirme verisine aktarılır ve kontrol noktası yazılır.
- Satış/satın alma/görev ödülü aynı kalıcı işlem güncellemesinde işlenir; yeniden açılışta yinelenen para üretmez.
- Kayıt geçici dosyaya yazılır, doğrulanır ve güvenli şekilde önceki kaydın yerini alır; son sağlam yedek korunur.
- Disk hatası başarı gibi gösterilmez. İşlem geri alınır veya yeniden denenebilir hatada bırakılır; kural P0'da seçilir.
- Ev sahibi dalış sırasında giderse tamamlanmamış dalış geri yüklenmez; son tamamlanmış kontrol noktası kullanılır.
- Bir sonraki dalışta eski diveId'ye ait sayaç, av, olay veya işlem isteği yeniden kullanılamaz.

## Ortak red sonuçları

En az şu nedenler ayrıştırılmalıdır: `WrongPhase`, `InvalidTarget`, `OutOfRange`, `NotVisible`, `InventoryFull`, `AlreadyClaimed`, `AlreadyProcessed`, `InsufficientFunds`, `PlayerInactive`, `SaveFailed`, `SessionClosed`.

Bu isimler P0 API tasarımında kesinleştirilir. Red, durum değişikliği yapmadan anlaşılır UI geri bildirimi üretmelidir. Ağ tekrarı gibi AlreadyProcessed sonucu varsa daha önceki sonuç döndürülebilir.

## Sözleşme değişikliği

1. Değişiklik gerekçesi ve etkilenen üretici/tüketiciler yazılır.
2. Etkilenen kişiler değişikliği inceler; kapsam büyüyorsa plan değişikliği süreci uygulanır.
3. Test sağlayıcıları, gerçek uygulamalar, kayıt/ağ uyumluluğu ve bu belge birlikte güncellenir.
4. Birleştirme dalında derleme ve ilgili regresyon geçmeden sözleşme tamamlanmış sayılmaz.

## P0 kapatılmadan kesinleştirilecek ayrıntılar

- Ortak para harcama yetkisi, ekipman tahsisi ve kampanya sahibinin kalıcı kimliği.
- Çekim için ekip çapında en iyi kayıt seçimi ve kayıt sahibinin başarısızlık durumu.
- Başarısız/kopan oyuncunun eşyalarının yaşam döngüsü ve pasif oyuncunun ekranı.
- Güvenli dönüşü başlatma/bitirme koşulu; kasabaya geçiş yaşayan ve pasif oyuncular için tek bir oturum durumu üretmeli.
- Kayıt hata davranışı ve eski kayıt sürümünün okunma/göç kuralları.
- Canlı/av/çekim için kesin API imzaları; bunları kimlerin uygulayacağı ve test sağlayıcılarının konumu.
