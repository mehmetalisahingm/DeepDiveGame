# P4.2-C: ev PC'si, klip arşivi, oyun içi kanal, yayın ve medya kaydı (#102)

Tarih: 2026-09-28. Taban: `codex/p4-integration` @ `2b422dc` (P4.1 kapanışı). Dal: `p4/mert-home-pc-channel`.

## Ne var
- **Sözleşme** (`Core/P4MediaContracts.cs`): `ClipManifest` (CONTRACTS "RecordingClipManifest" satırından: clipId/recordingId/diveId/gün, sahip, konu, kalite, süre, hash/boyut, medya hazır, güvenli dönüş), `ClipArchive` seam'i (Mehmet'in yakalaması bitmiş klibi buraya verir), `ClipPlayback` seam'i (Mehmet'in oynatıcısı), `ChannelIds` (yayın kimliği = klibin saf fonksiyonu), kayıt şekilleri (`PublicationSave` = CONTRACTS "PublicationState").
  - **Sahiplik notu:** `ClipManifest` benim tüketici tarafı şeklim; klibin ne olduğunu ve ne zaman geçerli olduğunu Mehmet belirler (#100). Onun PR'ı farklı alan isterse bu yapı ona uyar.
- **Arşiv + kanal** (`Media/ChannelAuthority`, saf, host): klip clipId başına bir kez (yakalama tekrarı `AlreadyArchived`), 128 klip tavanı. Yayın yalnız **sahibi**, yalnız **ticari aday** (hazır, güvenli dönmüş, gerçek konu, kalite 1–4), gün kapanmıyorken ve ticari hak boştayken. Ret kodları: `NotOwner`, `NotPublishable`, `MediaNotReady`, `PublicationAlreadyQueued`, `RightsConsumed`, `DayClosing`, `SaveFailed`, (binding) `NotAtPc`.
- **Tek ticari hak** (CONTRACTS P4.2 #5, `EconomyManager`): NPC'de ödenmiş kayıt yayınlanamaz; yayınlanan kayıt NPC kuyruğundan aynı adımda çekilir ve bir daha kuyruğa giremez. Yayının kayıt yazımı başarısızsa yayın ve hak birlikte geri alınır, aynı istek yeniden denenebilir.
- **Sonuç ve ödeme** (CONTRACTS P4.2 #6): N. gün kuyruğa giren yayın, N. günün kapanışında sabit host kurallarıyla (`ChannelResultRules`: kalite², konu yeniliği, takipçi) değerlendirilir ve **kapanışın kendi tek yazımıyla** (N+1 sabahı) ödenir; `settleId` ile bir kez. Gün motoruna yeni kapanış adımı: `IDayCloseHooks.SettleNextDayResults` (özetten sonra, yazımdan önce; yazma tekrarında yeniden koşmaz). Günlük özet `PublicationsQueued` sayar; kanal geliri özet geliri gibi gösterilmez.
- **Kayıt:** `EconomySaveData` **v5**: `HasMedia` + `Media` (arşiv, yayınlar, takipçi) + `ChannelRightIds` + `ChannelSettleIds`. Gün/para/tekne/keşifle aynı dosya; geç bağlama ve "bağlı değilken ileri taşı" kuralları keşifle aynı. Kurcalanmış dosya yayın uyduramaz/çoğaltamaz.
- **Ağ** (`Composition/MediaNetworkBinding`): host `ClipArchive`'i arşive bağlar; her süreçten yayın isteği (named message) alır, **göndericinin gerçek konumunun PC'ye yakınlığını host'ta** doğrular; arşivi ve akışı her sürece aynalar.
- **PC** (`Composition/HomePcView`): evde fiziksel PC hedefi (`HomePcAnchor`, depo'nun karşısında); yaklaşınca ekran: arşiv (kendi klipleri işaretli, ticari olmayan/yayında etiketli), seçili klip ayrıntısı, oynatıcı bağlıysa OYNAT (değilse bunu söyler, sahte video yok), başlık + YAYINLA, kanal akışı (sonuç bekleniyor / izlenme-takipçi-kredi). Konum bilgisi yok.

## Kanıt
- EditMode **807/807** (15 yeni `ChannelTests`: gerçek `EconomyManager`, gerçek `EconomySaveStore` + dosya, gerçek `DayEngine` kapanışı — yazma hatasında tekrar dahil).
- **Gerçek 2-süreç `tools/Test-P1-Integrated.ps1 -Players 2 -Media`**: host ve client1 PASS; her iki süreçte ret/kabul sırası aynı: `NotAtPc` (uzaktan) → `NotOwner` (diğerinin klibi) → `NotPublishable` (ticari olmayan) → kabul → `PublicationAlreadyQueued` (tekrar). İki süreç de iki oyuncunun yayınını kendi akışında gördü; kapanıştan sonra kendi yayınının sonucunu (izlenme/gelir) gördü. Host: host'un kaydı NPC kuyruğundan çekildi; kapanışta ödeme bir kez ve beklenen toplam kadar; tekrar ödeme yok; dosyada yayınlar settleId'li ve bakiye aynı; `LoadNow` sonrası çoğalma yok.
  - **Dürüst not:** klipleri üreten yakalama (#100) henüz yok. Smoke'ta klipler **test sürücüsünde açıkça etiketli fikstür** tarafından, #100'ün kullanacağı **aynı `ClipArchive` seam'inden** verildi; gün kapanışı da fikstürce tetiklendi (gece yarısı yerine). Yayın istekleri her sürecin kendi oyuncusundan, gerçek PC'de.
- **Gerçek ikinci host başlatması** (`-Media` sonrası aynı kayıt dosyası, `host-reload`): PASS — 4 klip, 2 yayın (ikisi de settleId'li ve izlenmeli), takipçi > 0, bakiye 228 (aynı), host kaydının kanal hakkı hâlâ alınmış; yeniden settle 0 ödeme, aynı klibin tekrar yayını `PublicationAlreadyQueued`.
- Aynı build'de gerileme: `-Explore`, `-Storage`, `-Day` (+ yeniden açılan host), `-HomeSleep`, `-Trip` host + client1 PASS.

## Doğrulanmadı
- Gerçek klip yakalama/oynatma (#100) ve Utku'nun medya dünya verisi (#101); bunlar gelince aynı `-Media` smoke'u fikstürsüz koşulacak.
- Başlık/kapak karesi seçimi (kapak, gerçek kare gelince), PC kontrol kilidi ve ortak izleme ("diğerleri aynı klibi izler", oynatıcıya bağlı), IMGUI görsel kontrolü, 4 süreç, ayrı bilgisayar.
- `ChannelResultRules` sayıları çalışma değerleridir; denge P4.5/oyun testinde.
