# P4.1-C üçüncü teslim: keşif haritası, ansiklopedi, keşif kaydı ve istemci aynası (#90)

Tarih: 2026-09-27. Taban: `codex/p4-integration` @ `8b73f23` (Utku #94 birleşik). Dal: `p4/mert-exploration-ui`.

## Ne var
- **Harita sisi** (`MapUI/ExplorationMapPresenter`, saf): Utku'nun `ExplorationSnapshot`'ından her hücre için bir karo. Konum `ExplorationMapProjection.TryCellCenter`'dan gelir; UI kendi dünya→harita hesabını yapmaz. Açılmamış hücre kapalı, açılmış hücre derinlik bandıyla (`shallow` / bilinmiyor) boyanır. Geçersiz grid = hiç karo, tahmin yok. `BoatMapView` sisi ikonların altına çizer; alt satırda `kesif n/36`.
- **Ansiklopedi** (`MapUI/EncyclopediaPresenter`, saf; `J`): yalnız host'un saydığı türler listelenir (keşfedilmemiş tür "???" satırı olarak bile görünmez, var olanı sızdırmaz). Kanıt → sayfa: görme/kayıt/av siluet+kategori açar; **kayıt** adı ve habitat bandını açar; **av** kesin av verisini açar; ilerleme `n/3`. Metin UI'nın (`sea_bass` → "Levrek"); metni olmayan tür adlandığında kimliğiyle görünür.
- **İstemci aynası** (`Composition/ExplorationMirror`): otoriteler yalnız host'ta; host, read model'in revizyonu değişince (ve yeni bağlanana) anlık görüntüyü named message ile gönderir, istemci aynı `ExplorationSnapshot`'ı kurar. Yük yalnız grid + hücre (açık/bant) + tür kanıtı/habitat kimlikleri; **hiçbir konum yok** (testli).
- **Kayıt** (`Core/P4ExplorationSaveContracts.cs` + `Composition/ExplorationPersistenceAdapter`): `EconomySaveData` **v4** `HasExploration` + `Exploration` (açık hücreler: id + grid + bant; gözlemler: kendi id/hücre/bant/gün). Gün, para, tekne ile **aynı dosya**. Geri yükleme gözlemleri Utku'nun kendi idempotent yolu `SpeciesObservationAuthority.TryApply` ile yapar (tekrar = `AlreadyCounted`). Başka bölgenin dosyası reddedilir; id/koordinat uyuşmayan, grid dışı, boşluklu bant kayıtlar atılır. Otorite bağlı değilken kayıt, eldeki keşfi dokunmadan ileri taşır. v1–v3 dosyalar keşifsiz yüklenir.
- `ExplorationFeed` (Core): host read model'inin UI'a yayınlandığı tek yer. **Bağlamak Mehmet'in keşif binding'inin işi** (#89 takip PR'ı); bağlı değilse sis/ansiklopedi hiç çizilmez.
- Ayrıca: `EconomySaveStore` artık her başarılı yazmadan sonra "geç bağlanan"a verilecek günü/keşfi günceller (önceden ilk yüklemedeki eski gün geri verilebiliyordu).

## Utku'dan tek istek: hücre geri yükleme
`ExplorationCellAuthority`'de "kayıttan açık hücreyi geri koy" girişi yok; hücre yalnız onaylı bir konum ulaştığında açılıyor. Uydurma bir dünya noktasıyla açmak otoriteye tahmin sokar, yapmadım. Şimdilik adaptör kayıtlı hücreleri kaybetmez (her kayıtta geri yazar) ve `UnrestoredCells` ile bildirir, ama yeniden açılışta sis bu hücreleri **gösteremez**. Önerim (Utku'nun dosyası, ben değiştirmedim): `bool RestoreDiscovered(int gridX, int gridZ, string depthBandId)` — grid içi + geçerli bant ise açar, keşif olayı üretmez, revizyonu artırır.

## Kanıt
- EditMode **761/761** (16 yeni `ExplorationUiAndSaveTests`: Utku'nun gerçek otoriteleri, gerçek `EconomySaveStore` + dosya, geç bağlama, v3 dosyası, kurcalanmış dosya, ayna tel formatı ve konum sızmaması).
- **Gerçek 2-süreç `tools/Test-P1-Integrated.ps1 -Players 2 -Explore`: host PASS, client1 PASS.** İki süreçte de: ayna verisi geldi, sis 36 hücre / en az 2 açık, ansiklopedide `sea_bass` siluet açık ve ad gizli (yalnız görme). Host: görme `CountedNewEvidence`, aynı görmenin tekrarı `AlreadyCounted`; kayıt dosyasında 2 hücre + 1 gözlem; bu dosyadan taze otoritelere adaptörle geri yükleme aynı sayıları verdi.
  - **Dürüst not:** oyunda Utku'nun otoritelerini kuran/besleyen host kabuğu (Mehmet'in binding'i) henüz yok. Smoke'ta bu kabuk **test sürücüsünde, açıkça etiketli bir fikstür** olarak kuruldu (gerçek sahne bölgesi/su, host-otoriter oyuncu konumları, tek görme). Ayna, sunucular, adaptör ve kayıt ürün kodudur; fikstür ürüne girmez.
- Aynı build'de `-Day` (yeniden açılan host dahil), `-HomeSleep`, `-Trip` PASS.

## Doğrulanmadı
- Gerçek oyun içi keşif besleme (Mehmet'in binding'i gelince aynı `-Explore` smoke'u fikstürsüz koşulacak), doğrulanmış görme olayı, günlük özete keşif yazımı (`DayEngine.RecordDiscovery` binding'de bağlanmalı).
- Yeniden açılışta sis (Utku'nun hücre geri yükleme girişine bağlı), IMGUI çizimi (headless çizmez; sunucu çıktısı sayısal doğrulandı), 4 süreç, ayrı bilgisayar.
- Harita sisi ile ikonlar bu arenada birebir örtüşür (testli); bölge 5 m çizgilerine oturmazsa hizalama Utku'nun projeksiyon notuna göre yeniden ele alınmalı.
