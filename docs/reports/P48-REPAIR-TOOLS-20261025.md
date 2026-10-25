# #187 — özgün tamir araçları, avuç tutuşu ve çivi teması

25 Ekim 2026. Kullanıcının Mehmet'in işlerini uygulama ve gerektiğinde asset
üretme talimatıyla teslim edildi. Unity uygulama ve test kaynağı
`40eb0659b40f7e975b48cf5d9f74eb269d44556b`; Windows / Unity `6000.3.23f1`.
PR #206, hedef `codex/p4-integration`. Bu rapor ana dalda yayın veya faz
kapanışı değildir.

## Oyunda değişenler

- Yaklaşık 78 cm meşe tahta, fiziksel damar/budak detayları, kırık/pahlı kenarlar;
  biçimlendirilmiş ahşap saplı, deri sargılı ve dövme çelik başlı keser;
  şekilli çivi ve üç çıtalı onarım paneli üretildi. Özgün kısa ahşap/metal
  vuruş sesi eklendi. Kaynak ve yeniden üretme menüsü asset README'sindedir.
- Katalog gerçek prefabları ellere, malzeme askısına ve mevcut tamir yuvalarına
  bağlar. Çiviler host'un kabul ettiği her darbede panele oturur; keserin
  temas noktası vurulan çiviye yönelir. Görseller malzeme/para/tamamlanma vermez.
- Eldeki parça/alet artık bileğe değil avuç yüzeyine bağlanır. Parmaklar
  kavramaya bükülür, iptal/bitirme sonrası eski poz geri gelir. Tamirde yerel
  omuzlar dünya gövdesiyle hizalanır; aşağı bakmak eli erişim dışına itmez.
  Uzak beden de aynı host eylemini gösterir. Oyuncu kökü/root motion değiştirilmez.
- Kayıt şeması, mevcut yuva kimlikleri ve tedarik edilmiş Rowboat modeli
  değiştirilmedi. Bu küçük panel/alet paketi #189'un tam hasarlı/onarılmış
  sandal gövde seti değildir.

## Aynı kaynak/build ile doğrulama

| Kontrol | Sonuç |
| --- | --- |
| EditMode | **1357/1357 PASS**; gerçek prefab ölçeği/mesh, katalog, avuç hedefi, temas ve parmak pozunun geri gelmesi dahil |
| Windows integrated build | **PASS**, `P1_BUILD_SUCCEEDED integrated=true target=windows` |
| İki gerçek Windows süreci | **Host + guest PASS**, `Logs/P46-product-20261025-135948-2` |
| Özgün art / kabul edilmiş darbe teması / avuç tutuşu | İki raporda da **true**; ölçülen en düşük avuç hedef hatası 0 m |
| Gerçek av → NPC satışı → satın alma | **PASS**; 120 kazanıldı/120 harcandı, edinme anında onarım olmadı |
| E/Q taşıma, bırakma, askıdan geri alma, yerleştirme | **PASS**; tamamlanma maskesi 0 → 7, tam üç katkı |
| Gerçek Windows dosya kilidi / retry | **PASS**; iki darbeli iş ve malzeme korundu, retry tek tüketim/katkı yazdı |
| Yeni host sürecinde aynı kayıt | **PASS**, üç tamamlanmış ve sıfır kullanılmamış parça, bakiye/satış/gün korundu |

Binary DLL SHA256'ları ve kaynak eşlemesi [manifestte](evidence/repair-tools-20261025/manifest.json),
ham test XML'i ve üç süreç raporu aynı klasördedir. Reload raporunda art/video
bayraklarının false olması normaldir: yeniden açma kontrolü etkileşimi tekrar
oynatmaz; kayıt durumunu doğrular. Tutuş ölçümü tüm animasyonun kusursuzluğu
iddiası değildir; gerçek kareler ayrıca incelendi.

Tekrar:

```powershell
./tools/Build-P1.ps1 -Integrated
./tools/Test-P46-ProductDay.ps1 -Players 2 -Port 25119 -RepairOnly -RepairPurchase -RepairVideo -Mute
```

## Gerçek oyun görüntüsü

- [Oyuncu kamerası](evidence/repair-tools-20261025/repair-owner.mp4): 237 gerçek kare, yaklaşık 14,18 saniye.
- [Arkadaş kamerası](evidence/repair-tools-20261025/repair-peer.mp4): 230 gerçek kare, yaklaşık 13,84 saniye.
- [Yakın el/alet karesi](evidence/repair-tools-20261025/repair-owner.png), [uzak beden karesi](evidence/repair-tools-20261025/repair-peer.png).

Kareler gerçek owner kamerasının URP render isteğinden, kozmetik IK sonrasında
alınır. CSV zamanları korunarak VFR video kodlandı; üretilmiş görüntü, ara kare,
hızlandırma yoktur. Masaüstü kaydı değildir; genel IMGUI katmanı görünmez.
Koşu sessizdir, videolar ses kabulünü kanıtlamaz. Mesh/material/WAV özgündür;
üçüncü taraf indirme veya mikrofon kaydı kullanılmadı.

## Kalanlar

Bu değişiklik #187'nin el/alet bağlantısı ve satın alma/kayıt reddi kanıtını
ilerletir. #189'un bütün sandal hasar/görsel/ses teslimi, dünya yerleşimi #188,
nihai sanat/ayrı PC/insan kabulü bu paketle tamamlandı sayılmaz. Eski özel
dört süreç hareket sıçraması **#207 açık kalır**; bu iki süreç PASS'i onun
kök nedenini çözmüş değildir. Ertelenen dört kişi/yarış/kopma/00:00, üç gün
ve son görsel kabuller [#164 defterinde](../plan/DEFERRED_ACCEPTANCE.md) korunur.
P4 üretimi yürür; P5/P6 bu teslimle açılmaz.
