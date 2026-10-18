# Mehmet — #186 tamir sözleşmesi ve #173 güncel kit ölçümü

18 Ekim 2026. Kaynak temel: #185'in birleşmiş `6816fba6849dc54fe200a0778b50a78e4a2ce0e2` commit'i. Bu teslim read-only Editor denetimi ve somut sözleşme hazırlığıdır; fiziksel tamir oyuna bağlanmadı, şema v8 değişmedi, faz/ekip kabulü yazılmadı.

## #186 için somut teslim

[Sözleşme önerisi](../plan/P48_REPAIR_CONTRACT_PROPOSAL.md), mevcut iki otomatik katkı yolunu koddan doğrular:

- `NetworkPlayer.HandlePickup → BoatPartClaim → TownServiceHandler.HandleFoundPart → EconomyManager.TryContributeBoatPart`: ücretsiz parça hemen completed olur.
- `TownServiceHandler.HandlePurchase → EconomyManager.TryContributeBoatPart(Purchased)`: parça ücreti ve completed aynı transaction'dır.

Öneri bunları edinilmiş malzeme / geçici taşıma-çalışma / son doğrulanmış katkı olarak ayırır. R01 üç görünür ahşap/bağlantı grubunu, R02 grup başına tek kalıcı parçayı, R11 yalnız edinmede tek ücreti önerir. R01/R02/R11 kesinleşmiş kullanıcı/ekip kararı sayılmaz.

Sekiz eski maskenin tamamı için görünür eşleme ve kalan katkı tablosu, v8'den önerilen yeni kullanılmamış malzeme alanına geçiş, save rollback, kopma/00:00/replay sonuçları ve #187/#188/#189 dosya/entegrasyon sınırları hazırdır. **Bu, uygulanmış veya test edilmiş yeni save migration değildir.**

## #173 için gerçekten çalıştırılan ölçüm

`DeepDive.Editor.P4PlayerScaleAudit.Run`, disposable additive sahnede mevcut `PlayerPresentationView.Apply` binding'ini çalıştırır. Kaynak sahne/prefab/catalog kaydedilmez. Mevcut kameranın 16:9 projeksiyonunda eldeki nesne ölçülür; üçüncü şahıs için oyuncunun 3 m önündeki geçici gözlemci kullanılır. Böylece uzaktaki nesne yanlışlıkla kendi oyuncusunun kamera düzlemine göre değerlendirilmez.

Renderer AABB köşelerinden ekran sınırı, en yakın derinlik ve nişangâh sınırı çıkarılır. Bu konservatif statik ölçü piksel görünürlüğü, animasyon sırasında el teması veya oyuncu konforu kabulü değildir.

| Ölçü | Sonuç |
|---|---|
| Oyuncu / göz / FOV / near plane | 1,80 m / 1,55 m / 60° / 0,05 m |
| Elde kamera en yakın derinlik | 0,34 m; ekran merkezini kapsayan bounds yok |
| Elde zıpkın | Arka bounds near plane'e uzanıyor; kesit ekran dışında. Bu ölçüm görünür clipping hatası diye raporlanmaz |
| Üçüncü kişi nesneleri | 3 m gözlemcide en yakın derinlik zıpkın 2,77 m, kamera 2,73 m |
| Kamera görsel kademeleri | Üç kademe aynı `P3_FirstPersonCamera` / `P3_ThirdPersonCamera` modeline fallback ediyor. Ayrı üst kademe görseli eksik; gameplay tier değiştiği iddiası yok |
| Ahşap sandal | 2,40 × 1,03 × 4,63 m |
| Dükkân / yan bina | 4,30 × 4,50 × 4,30 m / 7,00 × 5,20 × 8,00 m |
| Yatak / depo | 1,25 × 0,60 × 2,05 m / 1,35 × 1,60 × 0,75 m |
| Kit collider'ları | Ölçülen beş görsel prefabın tümünde 0; gameplay collider/anchor ayrı kalıyor |

Ham sonuç: [current-kit.json](evidence/p173-scale-20261018/current-kit.json). Denetim derlenip gerçekten çalıştı. Mevcut oyuncu/kamera ve dört ev başlangıcı/iskele baseline EditMode testleri **2/2 PASS**: [scale-tests.xml](evidence/p173-scale-20261018/scale-tests.xml). Bu değişiklik için 1291 testin yeniden yerelde çalıştığı iddia edilmez.

## Birleşmiş temel için gerçek süreç kanıtı

`6816fba` üzerinde [birleşim sonrası Linux fiziksel dünya CI](https://github.com/mehmetalisahingm/DeepDiveGame/actions/runs/39116279062) PASS. İndirilen artifact'taki **10 oyuncu/reload raporunun tamamı** tek tek okundu ve PASS doğrulandı: [manifest](evidence/p173-scale-20261018/world-validation.json).

| Süreç | Gerçek rota | Sonuç |
|---|---|---|
| Solo | 179,91 m, tek world yüklemesi, gün 2; sonra yeni host | PASS |
| Host + guest | 181,19 / 181,50 m, tek world yüklemesi, gün 2; sonra yeni host | PASS |
| Dört süreç | 183,41 / 183,67 / 173,25 / 170,61 m, tek world yüklemesi, gün 2; sonra yeni host | PASS |

Ham JSON'lar aynı evidence klasöründe `merged-world-*` adlarıyla tutulur. Bu kaynakta [Unity test/Windows build job'u](https://github.com/mehmetalisahingm/DeepDiveGame/actions/runs/39116279068) ve [günlük ürün döngüsü](https://github.com/mehmetalisahingm/DeepDiveGame/actions/runs/39116279065) de PASS olarak doğrulandı. Aynı workflow'un uzun fleet/boss alt işleri bu dar kayıtla geçmiş sayılmaz.

## Açık kalan gerçek işler

#173: animasyon sırasında gerçek el/nesne teması, birinci/arkadaş kamera videosu, gündüz/gece okunurluk, kare süresi/donanım ve kullanıcının görsel/ölçek kabulü. Mevcut prefabda gerçek collider'lı ev kapısı yok; açıklık ölçüsü kapı modeli kabulü değildir.

#186: R01/R02/R11 gerçek kararı ve alanlar arası sözleşmenin kesinleşmesi. #187: taşıma/yerleştirme/keser/darbe gameplay uygulaması ve gerçek input kanıtı. #188/#189 world/art/save teslimleri yapılmış sayılmaz.

#138/#140/#141/#157: yukarıdaki teknik satırlar yeni temel ile kanıtlandı; ayrı bilgisayarlar/dört insan/üç gün, ölüm/00:00/aktif host kaybı ve gerçek ekip değerlendirmesi açık kalır. #141/#159 başlama sınırı bu hazırlıkla kaldırılmaz. Hiçbir issue veya faz bu raporla kapatılmadı.
