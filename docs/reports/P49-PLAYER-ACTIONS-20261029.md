# P4.9 / #161 — av, teslim ve tekne eylemleri

29 Ekim 2026. Kullanıcı Mehmet'in işlerini ve ek görsel ayrıntıları uygulamayı istedi.
Üretim kodu `bea5733`; `759ca55` aynı ağacı güncel entegrasyonun merge geçmişine bağlar.
Bağımlılıklar: #210 `f12cf31`, #214 `e21db44`, #215 `c72309f`; üçü de `codex/p4-integration` içine birleşti.

## Görünen teslim

- İlk gerçek av kabulü: aynı türün görseli misinayla ele çekilir, sabit boyutla açık torbaya indirilir. Sadece gerçekten çantaya alınan av için oynar. Bilinmeyen türe başka balık modeli uydurulmaz.
- Balıkçıda kaydedilmiş satış: ip saplı, çivili, kapalı ahşap kasa elde kaldırılıp gerçek tezgâhın üstüne bırakılır. Kapalı kasa türe dair uydurma içerik göstermez; adet gerçek servis sonucudur. Ret, boş satış ve eski request tekrarı ikinci kasa oluşturmaz.
- Tekne: mevcut koltuk aynasından oturmuş dizler/ayaklar, bankoya göre -0.52 m beden ve göz yüksekliği, kısa binme/inme el desteği. Yatakta oturma bu pozu tetiklemez. Root veya hareket otoritesi eklenmez.
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
- Son kod (`bea5733`, #214 gece eki dahil): **1404/1404 EditMode PASS**, Windows build. Host/observer görünürlük kapılı son video ve dört süreç tamir kontrollerinin sonucu aşağıdaki kanıta eklenecek.

Test/kanıt tamamlandıkça bu bölüm gerçek sonuçlarla güncellenir. #161/#187 bütün işi, #207 eski sıçrama kök nedeni ve #164 insan/ayrı PC/üç gün kabulü bu ara raporla kapanmaz. #155 hâlâ üç türdür; habitat dağılımı yeni balık türü eklemek değildir.

## Varlık kaynağı

`P49ActionPropsSetup.cs` içinde torba canvas/halat geometrisi özgün üretilir; kasa projedeki özgün RepairTools/CarrySlat meshini tekrar kullanır. URP Lit materyalleri özgündür. Yakalanmış balık kopyaları mevcut SuppliedKit CoastalFish/Tuna/RedSnapper kaynaklarını kullanır; yeni indirilen veya satın alınan kaynak yoktur. Kullanıcının daha önce verdiği kaynak/public GitHub izni korunur.
