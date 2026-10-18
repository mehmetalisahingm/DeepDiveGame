# #186 — Fiziksel sandal tamiri için somut sözleşme önerisi

18 Ekim 2026. Mehmet'in #186 hazırlığı; #187/#188/#189 için birlikte incelenecek dar tasarım. **Öneridir; R01/R02/R11 kararı, diğer kişilerin onayı veya P4.6/P4.7 kabulü değildir.** Sonraki açık kullanıcı talimatı (“Evet, benim tamir işimi şimdi uygula”) yalnız #187 için uygulama istisnası verir; #141/#159 kabulü açık kalır. Kaynak temel #185 birleşimi `6816fba`. Bu istisnadaki uygulama R01/R02/R11 seçimlerini dar teknik temel olarak kullanır: v9 `UnusedBoatPartIds`, mevcut tamamlanmış ID listesi ve `DeepDive-P4-5` protokolü. Diğer kişilerin onayı veya #188/#189 nihai teslimi değildir.

## Görünen sonuç ve önerilen kararlar

Oyuncu kıyıda bir parçayı bulur veya dükkândan alır, elde taşır, doğru yuvaya yerleştirir ve üç kısa kontrollü darbeyi tamamlar. Her tamamlanan tahta/bağlantı sandal üzerinde kalıcı görünür. Birinci şahıs ve arkadaş kamerası aynı host sonucunu gösterir. İlk ücretsiz av ve yüzerek kıyı erişimi onarım istemez.

| Karar | Önerilen dar seçim | Uygulamaya etkisi |
|---|---|---|
| R01 | Başlangıç kürekli sandalında gövde yaması, oturak/destek ve kürek bağlantısı; eski üç teknik ID korunur | Motor ve yakıt tankı görünümü satın alınan Tripo motorbotta kalır. Katalog/quest adı değişimi Mert ve world eşlemesi Utku ile birlikte yapılır. |
| R02 | Üç ana grubun her birinde tek kalıcı görünür parça; her biri ayrı tamamlanır | Mevcut üç bit sekiz tamamlanma kombinasyonunu taşır. İlk teslimde daha fazla kalıcı alt-slot üretilmez. Çivi/darbe sayısı ekonomi veya kalıcı ilerleme sayısı değildir. |
| R11 | Edinilmiş malzeme tamamlanmış onarımdan ayrıdır; ücret yalnız satın almada | Son doğrulanmış darbe malzemeyi tüketir ve tamamlanmayı tek transaction ile kaydeder. Bulma/alışveriş doğrudan onarım yapmaz. |
| R03 | E ile al/yerleştir, açık bırak/iptal girdisi, elde keserle kontrollü darbe | Kesin tuş bağları mevcut input akışında doğrulanır; tahta eldeyken zıpkın/kamera aynı anda kullanılmaz. Serbest çivi fiziği yoktur. |

Bu seçimler düşük kapsamlı uygulanabilir öneridir. Çok sayıda bağımsız tahta istenirse R02'nin stable alt-slot/veri modeli ayrıca değiştirilir; üç bit altında sahte ilerleme üretilmez.

## Tekil malzeme ve geçici çalışma

Üç teknik kimlik değişmez: `boat-part-hull`, `boat-part-engine`, `boat-part-fuel-tank`. Her biri kampanya içinde en çok bir kullanılmamış malzeme veya bir tamamlanmış katkıdır. `boat-1` aynı sandal kimliğidir. Malzeme kişisel av çantasına girmez; kampanyanın ortak onarım malzemesidir ve satılamaz.

Kalıcı durum: `Available` (henüz edinilmemiş), `Acquired` (edinilmiş, kullanılmamış), `Completed` (tüketilmiş, onarımda). Aynı kimlik hem Acquired hem Completed olamaz.

Oturum içi geçici durum: ortak rafta/dünyada → tek oyuncuda taşınan → tek yuva için rezerve/yerleştirilmiş → darbe 0..2 → son transaction. Taşıyıcı client ID, rezervasyon ve yarım darbe ilerlemesi kalıcı kayıt değildir. D06 korunur; başka kampanya sahibi veya kalıcı guest taşıyıcısı yaratılmaz.

Guest ayrılırsa veya çalışma iptal edilirse kullanılmamış malzeme ortak erişilebilir noktaya döner. Host kaybında son sağlam disk kaydı yüklenir; kalıcı Acquired malzemeler rafta yeniden görünür. Düşürülen nesne zeminin altında veya erişilemez suda bırakılmaz; güvenli noktayı Utku sağlar.

## Transaction ve otorite tablosu

| İstek/sonuç | Host doğrulaması | Kalıcı değişim | Ret/iptal |
|---|---|---|---|
| Ücretsiz edinme | Gerçek E/raycast, aktif oyuncu, görünür yakın world parçası, edinilmemiş/tamamlanmamış kimlik | Acquired ekle, tek save; kaynak world görünümü bu kayıttan türetilir | Save reddinde world parçası alınmış sayılmaz |
| Satın alma | Mevcut gerçek ekipman dükkânı/mesafe/aktiflik, yeterli bakiye, aynı tekil kimlik | Acquired ekle ve mevcut fiyatı bir kez kes, tek save | Save reddinde bakiye ve sahiplik birlikte geri alınır; tekrar ücret yok |
| Taşımayı al | Acquired, tek taşıyıcı, uygun mesafe/aktiflik/aşama | Yok | Başka taşıyıcıda veya Closing ise ret |
| Yerleştir | Taşıyıcı ve parça/yuva eşleşmesi, mesafe/görüş, boş yuva, gün açık | Yok; geçici rezervasyon | Parça silinmez; başka yuvadaki oyuncu engellenmez |
| İlk iki darbe | Aynı çalışma, doğru konum/görüş/alet, hostça hız sınırı ve sıra | Yok | Sunum başarı/ödül üretmez |
| Son darbe | Aynı kontroller, tüketilmemiş malzeme ve tamamlanmamış katkı | Acquired çıkar + Completed ekle + revision artışı, tek save | Save reddinde malzeme/tamamlanma/revision birlikte geri alınır; çalışma yeniden denenebilir |
| Bırak/kopma/Closing | Host geçici sahipliği çözer | Tamamlanan katkı korunur; kullanılmamış malzeme ortak erişimde kalır | Yarım animasyon onarım sayılmaz; yeni çalışma kapanışta açılmaz |

İstemci yalnız niyet gönderir. Mesafe, görüş, part/slot kimliği, aşama, ekipman, aktiflik ve rezervasyon hostta doğrulanır. Edinme/satın alma/çalışma/darbe için request alanları ayrıdır. Aynı istek ve aynı payload aynı sonucu verir; aynı kimlikle farklı payload ret olur. Aynı parça veya yuvadaki iki oyuncudan en çok biri tamamlar. Replay ve yeniden yükleme fiyat, malzeme veya ödül çoğaltmaz.

Katkı mevcut onarım/fleet otoritesine gider; ayrı bir `3/3` sayacı kurulmaz. Son katkı sonrası sandal sahipliği, dört koltuk, yakın rota ve aktif araç mevcut otoritelerden türemeye devam eder. Tekne satın alma, serbest sürüş veya yeni crafting bu sözleşmenin kapsamı değildir.

## v1–v8 eski kayıtların kayıpsız eşlenmesi

Ürün bugün `EconomySaveData.BoatPartIds` içinde yalnız tamamlanan parçaları saklar. Eski kayıt bulunmuş ama tamamlanmamış malzeme tutmaz. Önerilen v9 alanı yalnız **kullanılmamış edinilmiş** teknik kimliklerin listesidir; isim ve serializer Mert ile kesinleştirilir. Eski kayıtta bu liste boş başlar. Mevcut tamamlanan ID ve bakiye aynen korunur; kimseye tekrar malzeme/ücret/ödül yazılmaz.

Bit sırası `BoatRepairParts.All`: Hull=1, Engine=2, FuelTank=4. Görünür yeni adlar yalnız R01 kabulünden sonra kullanılır.

| Eski maske | Tamamlanmış teknik ID | Önerilen görünür tamam | Yeni kullanılmamış malzeme | Kalan katkı |
|---|---|---|---|---|
| 000 | — | Kırık başlangıç | Boş | 3 |
| 001 | Hull | Gövde yaması | Boş | 2 |
| 010 | Engine | Oturak/destek | Boş | 2 |
| 011 | Hull, Engine | Gövde + destek | Boş | 1: FuelTank |
| 100 | FuelTank | Kürek bağlantısı | Boş | 2 |
| 101 | Hull, FuelTank | Gövde + kürek bağlantısı | Boş | 1: Engine |
| 110 | Engine, FuelTank | Destek + kürek bağlantısı | Boş | 1: Hull |
| 111 | Üçü | Onarılmış aynı sandal | Boş | 0; tekrar tamire zorlama yok |

Yeni kayıt yüklenirken bilinmeyen/tekrarlı veya Completed ile çakışan Acquired kimliği sessizce para/malzeme üretmez; geçersiz veri görünür hata verir. Yeni şema etkinleşmeden eski okuyucuyla uyum ve protokol değişimi ayrıca ele alınır. Kayıt formatı yalnız bu taslakla yükseltilmez.

## Alan ve dosya teslimi

| İş | Sahip | Dosya/seam sınırı |
|---|---|---|
| #186 | Mehmet + Utku + Mert | Dar Core sözleşmesi, R01/R02/R11 karar kaydı ve geçiş tablosu |
| #187 | Mehmet | Network oyuncu input/taşıma/çalışma otoritesi, yerel/uzak eylem binding'i, gerçek input smoke |
| #188 | Utku | World part/slot/güvenli bırakma anchor'ları, erişim/collider/çalışma alanı |
| #189 | Mert | Economy edinme/tüketim/save rollback, görev/mesh/ses seti ve eski kayıt restore |

Ortak sahne sırası Utku anchor → Mehmet etkileşim → Mert sunum → ortak build. Mevcut `NetworkPlayer.HandlePickup`, `BoatPartClaim`, `TownServiceHandler` ve `EconomyManager.TryContributeBoatPart` yolları koordineli değiştirilir. Mert/Utku teslimi veya onayı bu belgeyle yapılmış sayılmaz.

## Aynı build ile doğrulama

- Solo ve iki gerçek süreç: bul/satın al → taşı → bırak/yeniden al → yerleştir → son darbeye kadar 0 katkı → tek tamamlanma → gerçek binme/yakın rota.
- Dört süreç: iki kişi aynı parça/yuva, farklı yuvalarda çalışma, başka oyuncunun avlanabilmesi; yerel ve arkadaş kamerası, geç katılmada doğru parça/model.
- Yukarıdaki sekiz eski maske ve her biri için yeniden açma; üç ayrı 2/3 kampanyada yalnız eksik grup tamamlanır. 3/3 geriye gitmez.
- Bulma, alışveriş ve son darbe save hataları; aynı request replay, aynı request farklı payload, yetersiz bakiye, dolu/meşgul yuva, uzaktan/duvar arkasından etkileşim.
- Taşırken/yerleştirirken/darbe ortasında guest kopması, host kaybı, pasiflik, uzaklaşma, suya bırakma ve 00:00. Kalıcı malzeme kaybı, çift harcama/ödül veya sahte tamamlanma yok.
- Gündüz/gece hedef okunurluğu ve birinci/üçüncü kişi gerçek video. Tek PC süreç kanıtı ayrı PC/dört insan/üç gün kabulü yerine yazılmaz.

Bu belge sözleşme incelemesini somutlaştırır. #186'nın karar/onay satırları ve #187'nin uygulama kabulü henüz tamamlanmış sayılmaz.

## #187 uygulama istisnasının somut sınırı

E: gerçek parçayı al / eşleşen yuvaya yerleştir. Sol tık: üç kontrollü darbe (en az 0,45 sn aralık). Q: eldeki/yerleştirilmiş işi iptal et ve kullanılmamış malzemeyi iskeledeki ortak askıya döndür. Serbest fırlatma fiziği yok. Alım ve son darbe ayrı atomik save işlemidir. Satın alınmış malzeme askıdan alınır; yeniden alırken ücret kesilmez. Tamamlanmış eski bitler korunur; yarım darbeler ve bağlantıya özgü taşıyıcı kimliği kayda girmez.

Mehmet'in etkileşim kanıtı için mevcut tekne pozunda geçici kırık gövde, üç yuva/tahta/çivi, elde keser ve kısa vuruş sesi üretilir. Bunlar #189 nihai sanat/ses seti veya #188 dört kişilik nihai yerleşim kabulü değildir. Ortak sahne dosyası değişmez. Economy/TownService ince seam değişiklikleri PR #190'da Mert'e; runtime iskele slot/askı koordinatları Utku'ya görünür kılınır. Ekip onayı varsayılmaz.
