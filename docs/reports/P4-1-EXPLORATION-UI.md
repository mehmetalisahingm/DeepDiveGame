# P4.1-C üçüncü teslim: keşif haritası, ansiklopedi, keşif kaydı ve istemci aynası (#90)

Tarih: 2026-09-27. İlk taban: `codex/p4-integration` @ `15de479`; #96 ve Utku'nun hücre restore girişi #97 sonrasında restore follow-up'ı uygulanmıştır.

## Ne var
- **Harita sisi** (`MapUI/ExplorationMapPresenter`, saf): Utku'nun `ExplorationSnapshot`'ından her hücre için bir karo. Konum `ExplorationMapProjection.TryCellCenter`'dan gelir; UI kendi dünya→harita hesabını yapmaz. Açılmamış hücre kapalı, açılmış hücre derinlik bandıyla (`shallow` / bilinmiyor) boyanır. Geçersiz grid = hiç karo, tahmin yok. `BoatMapView` sisi ikonların altına çizer; alt satırda `kesif n/36`.
- **Ansiklopedi** (`MapUI/EncyclopediaPresenter`, saf; `J`): yalnız host'un saydığı türler listelenir. Kanıt → sayfa: görme/kayıt/av siluet+kategori açar; **kayıt** adı ve habitat bandını açar; **av** kesin av verisini açar; ilerleme `n/3`.
- **İstemci aynası** (`Composition/ExplorationMirror`): otoriteler yalnız host'ta; host, read model'in revizyonu değişince (ve yeni bağlanana) anlık görüntüyü named message ile gönderir, istemci aynı `ExplorationSnapshot`'ı kurar. Yük yalnız grid + hücre (açık/bant) + tür kanıtı/habitat kimlikleri; **hiçbir canlı dünya konumu yok**.
- **Kayıt** (`Core/P4ExplorationSaveContracts.cs` + `Composition/ExplorationPersistenceAdapter`): `EconomySaveData` v4 içinde gün, para ve tekneyle aynı dosyada tutulur. Açık hücreler artık doğrudan `ExplorationCellAuthority.TryRestore(cellId, gridX, gridZ, depthBandId)` ile gerçek World authority'sine geri yüklenir; adaptör ayrı bir `savedCells` / `UnrestoredCells` shadow state tutmaz. Gözlemler `SpeciesObservationAuthority.TryApply` ile geri yüklenir. Her iki yol da replay/idempotency kurallarını kendi authority'sinde uygular.
- **Geç bağlama:** `EconomySaveStore` dosyayı exploration authority kurulmadan önce okursa kayıt elde tutulur; `ExplorationPersistenceAdapter` daha sonra bağlandığında hücreler ve gözlemler gerçek authority'lere restore edilir.
- `ExplorationFeed` (Core): host read model'inin UI'a yayınlandığı tek yer. Gerçek oyun binding'i Mehmet'in Composition katmanında yapılır.

## #97 sonrası hücre geri yükleme
Utku'nun #97 değişikliği `ExplorationCellAuthority.TryRestore(...)` girişini ekledi. Restore yolu artık uydurma world position üretmez ve save formatından bağımsız primitive verilerle authority'yi besler. Geçersiz cell id/koordinat, grid dışı hücre veya geçersiz depth band authority tarafından reddedilir. Aynı save ikinci kez uygulanırsa hücre revizyonu ikinci kez ilerlemez ve ikinci keşif olayı üretilmez.

`ExplorationPersistenceAdapter` bu API'ye geçirildi. Böylece yeniden açılışta sis, restore edilmiş gerçek `ExplorationSnapshot` üzerinden doğrudan açılabilir; kaydedilmiş hücrelerin yalnız adaptör içinde taşındığı eski geçici yol kaldırıldı.

## Kanıt geçmişi
- #96 tabanında EditMode **774/774 PASS** ve gerçek 2-süreç `-Explore`, `-Storage`, `-Day`, `-HomeSleep`, `-Trip` PASS kaydı vardır.
- #97 kendi PR'ında restore API'si için **777/777 PASS** kaydı taşır.
- Restore follow-up testleri `ExplorationUiAndSaveTests` içinde authority restore, fog açılması, replay revision idempotency, tamper filtreleme ve late-bind restore davranışını pinler.

**Bu follow-up commitleri hazırlanırken burada Unity test runner veya Windows runtime smoke yeniden çalıştırılmadı.** Birleşik head için son doğrulama gerçek geliştirme makinesinde yeniden koşulmalıdır; geçmiş PASS kayıtları yeni follow-up head'inde otomatik PASS kabul edilmez.

## Kalan entegrasyon doğrulaması
- Mehmet'in gerçek host exploration binding'i: oyuncu konumlarının `IExplorerPositionSource` üzerinden beslenmesi, gerçek sighting/recording/catch kanıtlarının `SpeciesObservationAuthority`'ye aktarılması, `ExplorationFeed` + save adapter bağlanması ve `DayEngine.RecordDiscovery` bildirimi.
- Bu binding sonrası fixture'sız gerçek `-Explore` 2-process smoke.
- Follow-up head üzerinde `-Storage`, `-Day`, `-HomeSleep`, `-Trip` regresyonlarının tekrar koşulması.
- IMGUI görsel kontrolü, 4 süreç ve ayrı bilgisayar testi sonraki kabul katmanında kalır.
