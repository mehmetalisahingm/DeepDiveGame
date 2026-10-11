# P4.6-B: fiziksel esnaf dükkânları (#139)

Tarih: 2026-10-12. Taban: `codex/p4-integration` @ `a1dc5c6`. Dal: `p4/mert-town-shops`.

## Ne var
- Dört hizmet noktasının her biri (ekipman, balık alıcı, kayıt alıcı, tekne satıcı) kendi **dükkânına** kavuştu (`Composition/TownStorefront`): müşterinin önünde durduğu **tezgâh** (tek katı collider), arka duvar, hizmete özel renkli **çatı**, üzerinde hizmetin adı yazan **tabela** (`EKIPMAN`, `BALIK ALICI`, `KAYIT ALICI`, `TEKNE SATICI`), yere işaretli **müşteri noktası** (`CustomerPoint`, tezgâhtardan 1,7 m önde) ve gece yanan **fener**. Her hizmete özel tezgâh üstü: tüp/kamera, balık sandıkları/terazi, monitör/kayıt cihazı, tekne maketi/şamandıra.
- Dükkân, `ServicePointAnchor`'ın **çocuğu** olarak çalışma anında kurulur (sahne yüklenince; P4.5 ev/kasaba gelişimleriyle aynı yöntem): **sahne dosyasında değişiklik yok**. Utku/Mehmet #138'de anchor'ları taşırsa dükkânlar onlarla gider. Anchor'ın altında zaten `Storefront` adlı bir çocuk varsa (yazılmış gerçek bina) yer tutucu **kendini çekmez**. Tezgâh collider'ı anchor'ın altında olduğu için nişan alınca aynı hizmete çözülür.
- Fener ışığı yeni bir saat tutmaz: `CoastalAtmosphere`'in okuduğu aynı ortak gün kaynağından (`DaylightModel.NightFactor`) beslenir; gündüz kapalı, gece yanar. Eski kapsül NPC'nin renk plakası (2,9 m) çatıyla çakıştığı için çalışma anında gizlenir.
- Kurallar değişmedi: mesafe doğrulaması hâlâ host'un `TownServiceHandler` + `ServiceInteractionRules`'ında (gerçek oyuncu konumu, dalış dışı). Bu iş yalnız fiziksel yüzü ekler.
- Malzemeler gerçek asset (`Town/Resources/DevVisuals/Dev_*.mat`, 6 yeni: Green, Purple, Cream, Charcoal, Red, Sand); CreatePrimitive varsayılanı build'de magenta oluyor.

## Kanıt
- EditMode tam suite **1149/1149** (önceki 1143 + 6 yeni `TownStorefrontTests`): her hizmet türü için tezgâh/tabela/müşteri noktası/fener; tabela müşteri tarafından okunur (aynalanmaz); tezgâh anchor'a çözülür ve tek collider; dört hizmetin adı ve çatı rengi ayrı; ikinci kurulum çoğaltmaz, yazılmış bina varsa çekilir; fener gündüz kapalı/gece açık eğrisi; **gerçek `DiveTestArea` kumsalında** dükkânlar şeride sığar, birbirine değmez, yüzme şeridini daraltmaz, müşteri noktasının altı şerit zemini (prop değil).
- **Gerçek 2-süreç Windows smoke `Test-P1-Integrated.ps1 -Players 2 -Shops`** (iki süreç de PASS; kayıt: [host](evidence/p46/shops-host.json), [guest](evidence/p46/shops-client1.json)). Her süreç kendi sahnesinde dört dükkânı doğrular (tabela metni, fener, gerçek malzeme, nişan alınabilir tezgâh), sonra gerçek yürüyüşle:
  - hiçbir dükkân oturumu yokken satın alma ve araç seçimi **`NotAtShop`** ile reddedilir;
  - ekipman dükkânına yürür, etkileşimle açar; tezgâhta istek kurala ulaşır (`InsufficientFunds`, mesafe nedeniyle değil);
  - satırın öbür ucundaki tekne satıcısına yürür (7,2 m): ekipman satın alması artık **`NotAtShop`**; tezgâhta araç seçimi kurala ulaşır (`NotOwned`);
  - iki oyuncu aynı tezgâhta: her biri diğerinin gövdesini müşteri noktasında görür (**co-op görünürlüğü**);
  - host satıcıdan ekipman ucuna geri yürür: tekne satıcısı oturumu menzil dışı → **`NotAtShop`**;
  - gün saati 12:00 → 22:00 → 12:00 (**etiketli saat girdisi**; ışık/yayılım/fener ürünün): iki makinede de tüm fenerler gündüz kapalı, gece yanık, gündüz tekrar kapalı.
- Ekran görüntüleri gerçek build'den: [gündüz](media/p46-shops-day.png), [gece](media/p46-shops-night.png) (tabelalar okunur, magenta yok).
- Aynı build'de dükkâna yürüyen eski modlar yeniden koşuldu (hepsi host + guest PASS, reload'lu olanlarda reload dahil): `-Town`, `-Record`, `-Fleet`, `-Living`, `-Acceptance`, `-Deep`, `-Event`, `-Trip`.
- Satış ve kayıt teslimi için mesafe/menzil kanıtı: balık satışı (`-Town`, `-Living`) ve kayıt teslimi (`-Record`) bu dükkânların önüne gerçek yürüyüşle gidilerek yapıldı; uzaktan etkileşimin reddi EditMode'da (`TownSceneTests`, `ServiceInteractionRules`).

## Doğrulanmadı / açık
- **#138 yok:** gerçek şehir/cadde zemini henüz yok; dükkânlar bugünkü kumsal şeridindeki anchor sırasında. Anchor'lar taşınınca dükkânlar onlarla gelir ama yeni konumda sığma/yürünebilirlik #138/#141 kabulünde yeniden bakılmalı.
- Smoke 2 süreçle koşuldu; **4 süreç ve ayrı bilgisayar yapılmadı** (`GoToService` yalnız host/guest için ayrı duruş noktası verir, 3 guest aynı noktaya yığılır).
- Geri yürüyüş (satıcı oturumunun menzil dışı kapanması) yalnız host'ta; guest satıcıda kaldı (iki bedenin aynı noktaya yürümesi yolu tıkamasın diye). Ekipman dükkânı için menzil dışı kapanma ikisinde de sınandı.
- Gece/gündüz saati etiketli girdi (smoke'ta). Fener ışığı gerçek bir ışık ve gerçek ortak günden okunur, ama saat tek tek zaman diliminde (gün içi ilerleyerek) izlenmedi.
- "Kayıt/medya" hizmeti olarak kayıt alıcı NPC dükkânı yapıldı. Evdeki PC/kanal (P4.2) bir dükkân değil; ev sahnesi bu işe girmedi.
- Dükkânlar yine ilkel yer tutucu sanat; gerçek bina/tabela sanatı ayrı bir geçiş.
- Tabela yazısı yerleşik `LegacyRuntime` yazı tipi (ASCII, aksansız); Türkçe karakter yok (projedeki diğer metinlerle aynı).
- Dükkânlar 4 ayrı yapı değil, her biri tek açık tezgâh; "iç mekân" yok.
