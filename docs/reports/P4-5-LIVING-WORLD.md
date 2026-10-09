# P4.5-C: sipariş/sponsor, ev-kasaba gelişimi, rol seçimi ve dar denge turu (#132)

Bu belge #137 tesliminin tarihsel raporudur. Güncel world/UI/save kabulü: [P4-5-ACCEPTANCE.md](P4-5-ACCEPTANCE.md).

Tarih: 2026-10-09. Taban: `codex/p4-integration` @ `8be9c75` (#134 dahil). Dal: `p4/mert-orders-town-roles`.

## Ne var
- **Günlük hedefler** (`Living/LivingWorldAuthority`, saf C#, host): her kampanya gününe **bir balık siparişi + bir video sponsoru**. Seçim `(gün, tohum, mevcut şablonlar)` fonksiyonudur (gün tohumu `CampaignDayState.WeatherSeed`), dün seçilen şablon tekrar seçilmez, kayıttan yüklenince **yeniden zar atılmaz**. Dünyanın sunamadığı şablon üretilmez (`IOrderWorld`: gece çekimi yok / yeni tür kalmadı).
  - Siparişler: `order-fish-quick` (2 av, 80), `order-fish-bass` (4 levrek, 200), `order-fish-heavy` (toplam 3 kg, 120).
  - Sponsorlar: `sponsor-new-species` (türün ilk kaydı, 150), `sponsor-event` (özel olay klibi, 180), `sponsor-quality` (kalite ≥3 klip, 120). Dördüncü, **uyuyan** `sponsor-night` (220) Utku'nun gece verisi (#131) gelene kadar hiç üretilmez.
  - İlerleme **yalnız host'un doğruladığı olaylardan**: ekonominin kaydettiği balık teslimi (`OnCatchesSold`, teslim kimliği tekil) ve kanalın kabul edip yazdığı yayın (`OnPublished`, klibin doğrulanmış manifesti). UI tıklaması / yeniden adlandırma ilerletmez. Her olay kanıt kimliğiyle **bir kez** sayılır; ödül kimliği (`reward:dayN:<şablon>`) ekonomide de saklanır → kaç tekrar/yeniden açılış olursa olsun **tek ödeme**. Yazma hatasında adım ve para geri alınır.
- **Gelişim** (tek seferlik, kalıcı, sürekli masraf yok): `home-2` (ortak depo +20 yuva, sergi rafı) 700; `town-fisher` (balıkçı tezgâhı: av satışı +%10) 450; `town-shop` (ekipman dükkânı: Tup III açılır) 550; `town-dock` (iskele: araç fiyatları −%15) 450. Etkiler ilgili sistemde okunur: `EconomyManager.StorageCapacity`, `CatchPrice`, `TryPurchase("tube-3")` (yoksa `RequirementMissing`), `VehiclePrice`. Yeniden yükleme depoyu 40'a kırpmaz (`MaxStorageCapacityItems`).
- **Rol seçimi**: ücretsiz, oyuncu başına tek, PC'de, dalış dışında. Rol **tipi ve etkisi Mehmet'in** (`CrewRole`, `CrewRoleEffectBinding.TryApplyRoleServer`, #134); burada yalnız seçim + kayıt + UI. Seçilen rol her yoklamada oyuncunun gövdesiyle uzlaştırılır (yeniden doğma/yeniden açma/yeniden bağlanma), bonus tabandan yeniden hesaplandığı için katlanmaz. D06: yalnız host'un rolü dosyaya yazılır; ayrılan misafirin rolü onunla gider.
- **Kayıt v8** (aynı kampanya dosyası, geç bağlanan otorite): `LivingWorldSaveData` + ekonominin `RewardIds` listesi. Eski dosya = boş varsayılan; geri yükleme fail-closed (bilinmeyen/yanlış türde şablon, aralık dışı ilerleme, tekrarlı/bilinmeyen gelişim, geçersiz rol, bilinmeyen sürüm kural dışı bir şey veremez).
- **Arayüz** (`B`): günün siparişi + sponsoru ve ilerleme, gelişim listesi (satın alma PC'de), rol düğmeleri. Konum/hedef bilgisi yok. Gelişimler sahnede **görünür** (`DevelopmentVisuals`, sahne dosyası değişmeden çalışma anında): ev rafı + kupalar, balıkçı tezgâhı (tente/tezgâh), dükkân rafı, iskele fenerleri; alınana kadar yalın bir parsel işareti. Prop malzemeleri `Assets/DeepDive/Town/Resources/DevVisuals` altında gerçek asset (CreatePrimitive'in varsayılan malzemesi build'de magenta oluyor).

## Dar denge turu
`BalanceNarrowPassTests` hesabı sabitler; **oyun testi sonucu değil, varsayımlı aritmetiktir** (varsayımlar: av başına sabit 120, oyuncu-gün başına ~4 av + NPC'de 1 kalite-3 kayıt (100), ortalama av ~1,75 kg).

| Kalem | Karar | Neden |
|---|---|---|
| Sipariş bonusu | Quick 80 (+%33), Bass 200 (+%42), Heavy 160→**120** (+%50 → 2 ortalama av) | Heavy, 3 kg ≈ 2 ortalama avla fazla kârlıydı (+%67) |
| Sponsor bonusu | 120–180, ilk kalite-3 klibin kanal gelirinin (135) ≤2 katı | Sponsor günü yayın değerini kabaca ikiye katlar, katlamaz |
| Balıkçı tezgâhı | 450, +%10 | 2 oyuncu×4 av/gün ile ≈4,7 günde amorti (≤6 gün) |
| İskele | 600/%10 → **450/%15** | Eski değerde iki tekneyi alsan bile 330 tasarruf < 600: asla amorti olmayan vergiydi. Yeni: 495 > 450 |
| Tup III | 500 (Tup II 250'nin üstü), dükkân gelişimiyle açılır | Bir sonraki seviye, daha pahalı |
| Tüm merdiven | 2 oyunculu ekip için ≈11,5 bin kredi ≈ **10 gün** (8–16 aralığı); solo ≈ 20 gün (≤30) | Sandal onarımı + 9 ekipman kademesi/oyuncu + 2 tekne + 4 gelişim |

Oyuncu sayısı arttıkça kişi başı ekipman maliyeti de artar, paylaşılan kalemler (tekne/gelişim) bölünür. Gerçek sayılar P4.6'daki üç oyun günü ve gerçek oynayışta ölçülecek; bu tablo o zaman yeniden tartışılır.

## Kanıt
- EditMode tam suite **1131/1131** (`LivingWorldTests`: tahta/seçim/determinizm/yeniden zar yok, uygunluk, sipariş/sponsor ilerleme + tek ödeme + replay, gerçek `ChannelAuthority` ile sponsor, gerçek ekonomi olayı, gelişim etkileri, yazma hatası geri alma, roller, gerçek `EconomySaveStore` dosyasıyla kaydet/aç, eski dosya, kurcalanmış kayıt; `BalanceNarrowPassTests`).
- **Gerçek 2-süreç Windows smoke `Test-P1-Integrated.ps1 -Players 2 -Living`** (+ gerçek ikinci host başlatması): gerçek yürüyüşle PC'ye; uzaktan istek host'ta `NotAtPc`; guest tezgâhı, host ev/dükkân/iskeleyi alır; tekrar `AlreadyProcessed`; guest rolünü değiştirir (Kameraci → Taşıyıcı), host Avcı; **roller iki oyuncunun gerçek gövdesine (`CurrentCrewRole`) ulaştı**; iki aynada etkiler (depo +20, satış +%10, araç −%15, Tup III açık); ev rafı gerçekten kuruldu; gerçek dalış + iki oyuncunun gerçek balık teslimi (tezgâh bonuslu 2×132) günün siparişini tamamladı ve **tek ödemeyle** ödedi (bakiye 2700 → 3308); kasaba proplarının üçü kuruldu; dosya doğru, `LoadNow` çoğalmaz; **ikinci host başlatması**: tahta/gelişimler/host rolü (gövdeye yeniden uygulandı)/bakiye aynı, tekrar satın alma `AlreadyProcessed`, tekrar zar yok. Ekran görüntüleri: ev rafı ve kasaba sırası (tente + dükkân rafı) gerçek build'den.
- Aynı son build'de regresyon (host + client1 PASS, reload'lu olanlarda reload dahil): `-Living` (3 koşu), `-Town`, `-Record`, `-RoleEffects` (Mehmet), `-Day`, `-Media`, `-Storage`, `-HomeSleep`, `-Explore`, `-Fleet`, `-Deep`, `-Trip`, `-Boat`, `-Event`, `-Acceptance` (4 koşu). İki smoke'un beklentisi bu özelliğe göre güncellendi, gerileme değil: `-Town` artık av teslimleriyle tamamlanan günün siparişinin bonusunu bakiyeye katar (tam bir kez), `-Deep`'in fail-closed denetimi #128'den beri gerçek derin dünya doğrulayıcısı bağlı olduğu için `WorldUnavailable` **veya** `InvalidContext` kabul eder (yanlış bağlam ikisinde de sayılmaz; doğrulayıcı yok durumu EditMode'da).
- `-Acceptance` gerçek klibe karşı sponsoru da denetler: bugünün sponsoru klibe uyuyorsa tamamlanır ve bir kez öder, uymuyorsa açık kalır (çalıştırma sonuçları PR açıklamasında).

## Doğrulanmadı / açık
- **Etiketli fikstür** (smoke): başlangıç parası (5000, `SeedRecorderCamera` gibi) ve dalışta av enjeksiyonu (`-Town` gibi: avlanma yolu `-p2-hunt`'ta). Teslim, satın alma, rol, yayın, kayıt gerçek yollardır.
- Utku'nun hava/gece/akıntı verisi (#131) yok: gece sponsoru dormant, "yeni tür kaldı mı" varsayılanı yalnız tek tür (`sea_bass`) içeriğine göre (ilk kayıt yayınlanınca o sponsor üretilmez). Gerçek veri bağlanınca `OrderWorld.Bind` ile değişir. Orders'ın habitat/gece/rota uygunluğu #131 ile yeniden bağlanacak.
- Smoke tek bir günü ve **birinin** sipariş şablonunu görür (`order-fish-quick`); gün tohumu motorun gerçek tohumu olduğu için şablon koşuya göre değişebilir, ama üç sipariş de EditMode'da ve aynı kural yolundan geçer.
- **Mehmet:** kaydedilen rolün gövdeye uygulanması onun `TryApplyRoleServer`'ı üzerinden; ben yalnız seçim/kayıt/UI + uzlaştırma yaptım. Rolün **kasabada** (PC) seçildiği varsayıldı.
- IMGUI panelinin (`B`) görsel kontrolü ve 4 süreç/ayrı bilgisayar yapılmadı. Ev odasındaki mevcut PC kutusu (Mehmet'in rig'i) hâlâ magenta (CreatePrimitive malzemesi); bu PR'ın kapsamı dışı.
- Fiyatlar/ödüller çalışma değeri; yukarıdaki tablo oyun testi değil, hesap.
