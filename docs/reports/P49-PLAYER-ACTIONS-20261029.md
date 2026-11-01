# P4.9 / #161 — av, teslim ve tekne eylemleri

29 Ekim 2026. Kullanıcı Mehmet'in işlerini ve ek görsel ayrıntıları uygulamayı istedi.
Son oyun kodu ve varlıklar `47e616e`; `513d2f7` development tamir yürüyüşü/kanıtı ve test başlatıcısını, `2e4c890` normal ürün günü av yaklaşımını değiştirir. Üç baş arasında Assets/DeepDive, Assets/Editor, ProjectSettings ve Packages ağacı aynıdır; `game-tree-equivalence.json` kaynak ağaçlarını kaydeder.
Bağımlılıklar: #210 `f12cf31`, #214 `e21db44`, #215 `c72309f`; üçü de `codex/p4-integration` içine birleşti.

## Görünen teslim

- İlk gerçek av kabulü: aynı türün görseli misinayla ele çekilir, sabit boyutla açık torbaya indirilir. Sadece gerçekten çantaya alınan av için oynar. Bilinmeyen türe başka balık modeli uydurulmaz.
- Balıkçıda kaydedilmiş satış: ip saplı, çivili, kapalı ahşap kasa elde kaldırılıp gerçek tezgâhın üstüne bırakılır. Kapalı kasa türe dair uydurma içerik göstermez; adet gerçek servis sonucudur. Ret, boş satış ve eski request tekrarı ikinci kasa oluşturmaz.
- Tekne: mevcut dört koltuğun altına iki özgün ahşap banko, oturmuş dizler/ayaklar, bankoya göre -0.52 m beden ve göz yüksekliği, kısa binme/inme el desteği. Yatakta oturma bu pozu tetiklemez. Root veya hareket otoritesi eklenmez.
- Torba: katlı canvas, dikiş, deri altlık, bağlı ağız ve iki ip tutamak. Kasa: yuvarlatılmış ahşap latalar, köşe bağları, çivi başları ve ip saplar. İkisi özgün proje varlığıdır; Editor üreticisi GUID koruyarak yeniden üretir. Meshler malzeme başına birleştirilir (torba 3, kasa 5 renderer), collider/rigidbody/NetworkObject yoktur.
- İlk GPU koşusunda görülen görüntüsüz guest başlatma hatası düzeltildi. Ayrıca gerçek Null grafik cihazında deniz düzlemsel yansıması denenmez; göz/oyun verisi bağlantıları çalışmaya devam eder.

## 9-1 eylem ve yetki envanteri

| Eylem | İstemci niyeti / kabul edilen kaynak | Sunum | Hareket/işlem kilidi |
|---|---|---|---|
| Zıpkın | SubmitHarpoonLocal → host action gate/hedef | Var olan atış/vuruş ve ekipman | Mevcut gameplay kuralları |
| Kuşanma | Mevcut host HeldEquipment aynası | #209 indir/değiştir/kaldır | Animasyon yeni kilit eklemez |
| Av alma | SubmitPickupLocal → CatchState/çanta ilk consume | Yeni host kozmetik olay → torba/gerçek tür | Kabul geri alınmaz; yeni atış/ekipman kesebilir |
| NPC satış | ServiceInteraction → kayıtlı TownServiceOutcome | Yeni tekil host kozmetik olay → kasa | Ekonomi animasyonu beklemez |
| Bin/in | Mevcut trip isteği → MySeatId | Diz/beden/göz ve kısa el desteği | Yalnız mevcut koltuk otoritesi |
| Sandal tamiri | Mevcut reserve/placed/stroke mirrors | #187 el/tahta/keser/çivi, yeni #214 gövde | Mevcut repair hands busy önceliği |
| Kayıt/uyku/pasiflik | Kendi mevcut host aynaları | Yeni eylemi keser / yatak tekneye dönüşmez | Kendi gameplay yetkileri |

İsteğe bağlı host→client named message ve doğrulama sınırı [CONTRACTS](../plan/CONTRACTS.md#29-ekim-161--kabul-edilmiş-eylemin-kozmetik-sunumu) içindedir. Yeni NetworkVariable, RPC veya save alanı eklenmedi.

## 9-5 kamera, input ve rahatsızlık sınırı

Kamera yönü, FOV ve sallanma eklenmez; sadece koltuktaki gerçek görünür baş yüksekliği değişir. Hareket ve seçim tuşları animasyon beklemez. Kayıt/tamir/pasiflik önceliklidir. Yeni modal, basılı tutma süresi veya süreli başarı penceresi yoktur. Mevcut ret UI'sı korunur. Yeni ses paketi ve kullanıcıyla rahatsızlık/okunurluk değerlendirmesi bu kod testiyle kabul edilmiş sayılmaz.

## Doğrulama kaydı

- Birleşik bağımlılık tabanı: **1388/1388 EditMode PASS**.
- İlk eylem uygulaması: **1401/1401 EditMode PASS**, Windows build. `fbf621e` iki gerçek GPU sürecinde üç dalış, iki gerçek av/satış, kazanılmış kamera, gerçek klip, sefer, uyku ve fresh-host reload PASS. İlk kadrajlar teslim sırasında QA Stop'un kamerayı kuzeye sıfırladığını gösterdi; bu kayıt final görsel kabulü sayılmadı.
- Son oyun/varlık kodu (`47e616e`, #214 gece eki dahil): **1406/1406 EditMode PASS**, Windows build. Yeni test, eski kasa meshinde eksik yan yüzeyi yakalayarak FAIL oldu; tüm submesh'lerin birleştirilmesiyle geçti. Dört koltuğun her birinin altında gerçek banko yüzeyi de mesh ışınıyla doğrulanır.
- `47e616e` iki gerçek GPU sürecinde üç dalış, iki av/satış, kazanılmış kamera, gerçek klip/oynatma/tek yayın, yakın sefer/iki bin-in, uyku ve fresh-host reload **PASS**. Her iki süreç 2 Collect + 2 Deliver + 2 Board + 2 Disembark gördü; gözlemcide dört türün hepsi kadraj + engel kontrolünü geçti. Koltukta göz yüksekliği ve inince dönüşü iki tarafta doğrulandı.
- `513d2f7`: **1407/1407 EditMode PASS**, Windows build. Gerçek CharacterController ile kuzey kıç çalışma noktasından ortak askıya kuru güney yolu eklendi; normal oyunun collider, root, hız veya etkileşim kuralları değiştirilmedi.
- 30 Ekim `513d2f7`: dört gerçek **Direct3D 11 / GTX 1650** süreci, aynı parça yarışı, R03 iptal/geri alma, kazananın gerçek kopma/rejoin'i, kısmi onarımda gerçek gün motoruyla 00:00, üç kalıcı tamir ve fresh-host disk reload **PASS**. Yarış/kopma/rejoin/00:00 doğrulayıcısı host raporundadır; dört nihai oyuncu raporu ve reload eksiksizdir. Ölçülen en büyük hareket adımları sırasıyla 0.160/0.136/0.128/0.113 m; eski 1.5 m sınırı korunur. Bu, dört ayrı PC veya performans kabulü değildir.

## Gerçek görüntü ve kaynak eşlemesi

30 Ekim son kaynak `2e4c890`: **normal iki süreçli ürün günü ve fresh-host reload PASS** (`-Players 2 -Mute`; ActionVideo/Render seçeneği yok). Üç dalış, iki av/satış, kazanılmış kamera, gerçek klip/oynatma/tek yayın, sefer ve yeni gün tamamlandı. Böylece CI'da takılan normal av yaklaşımı, video sürücüsüne bağlı olmadan gerçek inputla tekrarlandı. Windows host GPU, guest Null cihaz kullanır; bu koşu yukarıdaki iki/dört gerçek GPU kanıtından ayrıdır.

Kanıt kökü: [p49-actions-20261029](evidence/p49-actions-20261029/).

- [Yerel eylem videosu](evidence/p49-actions-20261029/actions-player0.mp4): 139 gerçek kare, 8 eylem kesiti, 9.13 s.
- [Arkadaş eylem videosu](evidence/p49-actions-20261029/actions-player1.mp4): 131 gerçek kare, 8 eylem kesiti, 8.82 s.
- Videolar opt-in development sürücüsünün gerçek owner kamerasını kaydeder. Yalnız boş bekleme araları kesilir; eylem içi örnek süreleri korunur, hızlandırma/yeni kare/interpolasyon yoktur. Kaynak saat/kare/tür CSV ve segment JSON'ları yanındadır; iki MP4 bütünüyle çözümlenerek kontrol edildi.
- `player0.json`, `player1.json`, `reload.json` ve `final-product-day.zip`: 29 Ekim `Logs/P46-product-20261029-194030-443-2`, kaynak `47e616e`.
- `repair-player0..3.json`, `repair-reload.json`, `four-gpu-repair-20261030.zip`: 30 Ekim `Logs/P46-product-20261030-204055-799-4`, kaynak `513d2f7`.
- `normal-player0.json`, `normal-player1.json`, `normal-reload.json`, `final-normal-product-20261030.zip`: 30 Ekim `Logs/P46-product-20261030-204825-191-2`, kaynak `2e4c890`. Son oynanabilir ZIP `DeepDive-Actions-2e4c890-Windows.zip`; `final-windows-zip.json` tüm dosyaları `final-build-manifest.json` ile doğrular. `provenance.json` koşu/kaynak eşlemesini, `evidence-sha256.json` kanıt dosyalarının hashlerini tutar.
- `build-manifest.json`/`windows-build.log` ve `repair-build-manifest.json`/`repair-windows-build.log` iki gerçek binary setini ayrı tutar. `windows-zip.json` içindeki 84,569,365 byte oynanabilir `DeepDive-Actions-47e616e-Windows.zip`, 302 dosyanın tamamında manifest SHA256 ile eşleşti. Arşivlenmiş iki oyuncu görselleri bu sete aittir.

![Av alma, yerel kamera](evidence/p49-actions-20261029/collect-owner.png)
![Balıkçıya kasa teslimi, arkadaş kamerası](evidence/p49-actions-20261029/deliver-observer.png)
![Bankolar ve oturuş, arkadaş kamerası](evidence/p49-actions-20261029/boat-observer.png)

## Korunan FAIL'ler ve CI devamı

`failed-null-guest-launch.zip` ilk video seçeneğinin guest'i yanlışlıkla Null grafikle açtığı FAIL'dir. Başlatıcı düzeltildi; Null cihazda deniz yansıması da çağrılmaz. `failed-shore-approach.zip` videolu av sürücüsünün sığlık dolgusunun arkasında kaldığı FAIL'dir. `failed-four-gpu-rack-path.zip` yeni kıç çalışma noktasından Q sonrası askıya yürüyüşte takılan dört sürecin eksiksiz FAIL raporlarıdır; yukarıdaki kuru yol/fresh süreç tekrarıyla giderildi. Başarısız kayıtlar yeni PASS ile değiştirilmedi.

GitHub #216 `47e616e` başında [Unity/Windows](https://github.com/mehmetalisahingm/DeepDiveGame/actions/runs/40913667428) ve [1/2/4 dünya](https://github.com/mehmetalisahingm/DeepDiveGame/actions/runs/40913667444) PASS; [ürün günü](https://github.com/mehmetalisahingm/DeepDiveGame/actions/runs/40913667549) solo PASS, iki süreç step 2 FAIL'dir. `ci-original-product-failure.zip` orijinal artefaktı korur: host (-6.40, 5.89, -4.93) konumunda av yaklaşımında takılıp oksijenini tüketir. Video yolunda gerçek inputla doğrulanan önce sığlık burnunu geç–sonra alçal yaklaşımı normal test sürücüsüne de uygulandı. Oyun fiziği ve süre/hareket eşiği değişmez. Son normal iki süreç yerel tekrarı `2e4c890` ile PASS; yeni CI sonucu #216'da izlenir. Eski kırmızı koşu PASS değildir.

`repair-worksite-update-sample.png` sürücünün Update aşamasındaki erken hata ayıklama karesidir; LateUpdate el IK'sı öncesinde çekildiği için el teması kabulü değildir.

#161/#187 bütün işi, #207 eski sıçrama kök nedeni ve #164 insan/ayrı PC/üç gün kabulü bu paketle kapanmaz. Yeni el teması/torba beden bağlantısı işi #220'de, doğal habitat #219'da, Mert'in resif kiti #217 ve eksik balık seti #218'de takip edilir. #155 hâlâ üç türdür; habitat dağılımı yeni balık türü eklemek değildir.

## Varlık kaynağı

`P49ActionPropsSetup.cs` içinde torba canvas/halat geometrisi özgün üretilir; kasa projedeki özgün RepairTools/CarrySlat meshini tekrar kullanır. URP Lit materyalleri özgündür. Yakalanmış balık kopyaları mevcut SuppliedKit CoastalFish/Tuna/RedSnapper kaynaklarını kullanır; yeni indirilen veya satın alınan kaynak yoktur. Kullanıcının daha önce verdiği kaynak/public GitHub izni korunur.
