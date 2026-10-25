# #187 — el tutuşları, kazanılmış malzeme ve gerçek kayıt reddi

24 Ekim 2026; durum 25 Ekim'de yenilendi. Mehmet'in devam işi. Aşağıdaki
başarılı yerel kanıtların Unity uygulama/test kaynağı `2857678`;
`a3fe9fa` P4 entegrasyonu dala alınmıştır. Önceki kamera #204 ve Utku'nun
HuntFirst #205 değişiklikleri korunur. #187 ve faz kabulü bu raporla kapanmaz.

## Uygulama

`RepairCarryView` host'un yayımladığı taşıma/yerleştirme/darbe durumunu tüketir.
Görünür tahta/keserin kavrama noktaları mevcut CoastalDiver/CoastalArms el
kemiklerine kozmetik IK ile bağlanır. Oyuncu kökü, hareket girdisi, katkı, para
ve kayıt otoritesi animasyona taşınmaz. İki gerçek prefabın erişilebilir hedeflere
uzanması ve kök transformunun korunması EditMode'da da doğrulanır.

`DiverPresentationCatalog` isteğe bağlı `repairPlankPrefab`, `repairHatchetPrefab`
ve `repairImpactClip` alanları sağlar. Tahta `Grip_Left`/`Grip_Right`, keser
`GripPoint` işaretlerini tüketir; iç içe import/pivot/ölçek korunur. Alanlar henüz
atanmadı; kullanılan tahta/keser/ses mevcut mekanik prototiptir. Mert'in #189
nihai sanat, ses, parmak/alet teması ve görsel kabulü yerine geçmez.

## Doğrulama

| Kontrol | Sonuç / ham kanıt |
| --- | --- |
| Windows development build | PASS, `Logs/P1-build.log`, `P1_BUILD_SUCCEEDED` |
| EditMode | 1350/1350 PASS, `Logs/repair-20261024-final-editmode.xml` |
| İki gerçek Windows süreci, kazanılan parça | PASS, `Logs/P46-product-20261024-171505-2` |
| Yeni host sürecinde aynı tamir kaydı | PASS, aynı klasör `reload.json` |
| Tam ürün günü / onarılmış tekne / yakın sefer | `2857678` PASS, `Logs/P46-product-20261024-171550-2`, host/guest + yeni host |
| Son `f0ae302` dört süreç tekrarı | PASS değil: `Logs/P46-product-20261024-172717-4/player2.json`, `Unexpected travel discontinuity: 1,535102`; diğer raporlar yok |
| Son `f0ae302` iki süreç tekrarları | `172728-2` ve `172739-2` klasörlerinde sonuç raporu yok; PASS verilmez |

`f0ae302` test sürücüsünde Q sonrası askıdan çalışma noktasına düz gidiş,
mevcut `Around` rotasıyla düzeltilmiştir. GitHub'da bu kod başının unity,
product-day ve real-world kontrolleri PASS; özel dört süreçlik son yerel
tekrarın yerine yazılmaz. #206 taslak kalır. Önceki `2857678` yerel kanıtları
son kaynağın bütünleşik kabulü diye sunulmaz.

Satın alma senaryosu sıfır para ve sıfır parça ile başlar. Gerçek kıyı avı,
güvenli dönüş ve NPC satışı 120 kredi kazandırır. Gerçek dükkân oturumundaki
satın alma 120 krediyi bir kez harcar, yalnız kullanılmamış malzeme üretir;
tamir sayısı sıfır kalır. E/Q ile askıdan alma/taşıma/geri alma, yuvaya
yerleştirme ve üç doğrulanmış darbe aynı normal oyun yolundan geçer.

Son darbeden önce yalnız bu development koşusunun kampanya dosyası
`FileStream(..., FileShare.None)` ile kilitlenir. Gerçek `File.Replace` paylaşım
hatası verir. Para, revision, son iyi dosya içeriği, kullanılmamış malzeme ve
iki darbeli çalışma aynen korunur. Kilit `using` ile bırakılır; sonraki gerçek
owner darbesi bir tamamlanmış katkı ve bir malzeme tüketimi yazar. Yeni host
üç tamamlanmış/0 kullanılmamış parçayı, aynı bakiyeyi, satış sayısını ve günü
yeniden yükler. Sahte persist delegesi, para/parça fixture'ı veya teleport yoktur.

Tekrar:

```powershell
./tools/Test-P46-ProductDay.ps1 -Players 2 -Port 24947 -RepairOnly -RepairPurchase -RepairVideo -Mute
./tools/Test-P46-ProductDay.ps1 -Players 4 -Port 24963 -RepairOnly -RepairInterruptions -Mute
./tools/Test-P46-ProductDay.ps1 -Players 2 -Port 24955 -Mute
```

## Gerçek kamera kanıtı

- [Oyuncu kamerası videosu](evidence/repair-grips-20261024/repair-owner.mp4): 94 gerçek kare, yaklaşık 12,6 saniye.
- [Arkadaş kamerası videosu](evidence/repair-grips-20261024/repair-peer.mp4): 91 gerçek kare, yaklaşık 12,1 saniye.
- [Oyuncu karesi](evidence/repair-grips-20261024/repair-owner.png) ve [arkadaş karesi](evidence/repair-grips-20261024/repair-peer.png).
- Kaynak eşlemesi, JSON raporları, dosya SHA256'ları ve kare zamanları aynı [kanıt klasöründe](evidence/repair-grips-20261024/manifest.json) tutulur. Eski `2857678` build'i sonrasında yeniden derlenmiştir; ona ait teslim binary'si/hashi varmış gibi kayıt oluşturulmaz.

Videolar normal owner kameralarının `RenderPipeline.SubmitRenderRequest`
çıktısıdır; el IK'sinden sonra yaklaşık 8 Hz örneklenir. Masaüstü ekran kaydı
değildir, IMGUI arayüzü içermez. Gerçek örnekleme süreleri korunur; üretilmiş
görüntü, ara kare, hızlandırma veya temsili animasyon eklenmez. Koşu sesleri
kapalıdır; videolar sessizdir. Final sanat/aletin çiviye teması onayı değildir.

## Açık sınırlar

Tüm süreçler aynı PC'dedir. İnternet/ayrı PC ve gerçek üç kişilik görsel kabul
üretilmedi. #189 nihai model/ses/tutuş-temas teslimi, karma kabul satırlarının
tamamı, #141/#159 ve faz kapanışı açık kalır. Gece testi opt-in hızlandırılmış
gerçek host saatini kullanır; doğal 20 dakikalık gün denemesi değildir.
