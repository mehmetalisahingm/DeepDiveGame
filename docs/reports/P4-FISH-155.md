# #155: Tek balık görülmesi ve tür/habitat dağılımı (Utku, 27 Ekim 2026)

Bu rapor teşhis ve ilk yerleşim paketini anlatır. Beş tür teslimi, nihai görsel kabul ya da ayrı PC/insan kabulü değildir.

## Neden tek balık görüldü

13 Ekim'deki üç kişilik oyun testinde `DiveTestArea` sahnesinde tek bir avlanabilir balık vardı: `Fish_SeaBass`. Avlanınca `CatchObject.Consume` onu kalıcı olarak despawn ediyordu (Mehmet, #155, 16 Ekim). Bu tarihten sonra iki değişiklik geldi:

- 18 Ekim (`3202dbf`): sahneye 15 avlanabilir balık eklendi. `SuppliedKitImport` bunları isim sırasına göre 5 levrek, 5 ton ve 5 kırmızı mercana ayırdı.
- `FishDiveLifecycleBinding`, her yeni dalışta yerleştirilmiş balıkları başlangıç konumlarında yeniden spawn ediyor.

Yani "tek balık" teknik olarak geçti. Ama kullanıcının asıl itirazı devam ediyordu: derine inince başka bir şey görülmüyordu.

## 27 Ekim öncesi sayım (gerçek sahne, EditMode)

| Tür | Adet | Konum | Model |
|---|---|---|---|
| `sea_bass` (levrek) | 5 | Kıyı bandı, z 2.5–8, y 3.5–4.5 | `CoralGrouper` |
| `tuna` (ton) | 5 | Aynı band | `Tuna` |
| `red_snapper` (kırmızı mercan) | 5 | Aynı band | `RedSnapper` |
| Resif (raf z 19–40, üst y −8) | 0 | — | — |
| Derin (havza z 46+, üst y −26) | 0 | — | — |

Derinlik bantları: sığ y 0…8, resif y −12…0, derin y −12'nin altı (`DiveDepthBands`, yüzey y 8). Üç türün taban fiyatı aynı: 120.

## Bu teslimde yapılan

`CoastalFishPopulationSetup.Home(index)`, türe göre bir habitat atıyor. Sahne aynı betikle yeniden yazıldı. Sahnede yalnızca 10 balığın konumu değişti. İsimler, türler ve NGO kimlikleri aynı kaldı.

| Tür | Yeni yer | Bant |
|---|---|---|
| Levrek ×5 | Kıyıda, yer değişmedi | Sığ |
| Kırmızı mercan ×5 | Resif rafının üstü, y −4, x 3.5–8, z 30.5–38 | Resif |
| Ton ×5 | Resif kenarının ötesinde açık su, y −8.5…−9, x −7…−1, z 48–56 | Resif bandının alt kısmı, derin havzanın üstü |

Konumların sağladığı koşullar:

- Her balığın dolaşma zarfı (yatayda 6 m, düşeyde 3 m, gövde payı 0.45 m) suyun içinde kalıyor ve sahne nesnelerine değmiyor. Bu, mevcut `P4WadeUndersideTests` ile doğrulandı.
- Resif kayalarına (üstleri y −3) yatayda en az 6.45 m mesafe var.
- Resif düşüşü (z 15–19) zarfın altında kalıyor.
- Ton, boss arenasından (7.5, −18, 57.5) en az 12 m uzakta.
- Hiçbir balık demir yerlerinin su sütununda değil. Sütun, araştırma teknesinin 4.4×10 m ayak izi; ilgili test `DiveTestAreaReefDeepSceneTests`.
- Gece aktif tür levrek kıyıda kaldı. Avlanan sürücüler en yakın görünür balığı hedefliyor.

## Doğrulama

- Yeni `P155FishHabitatTests.EachSpeciesKeepsItsOwnHabitat`: eski yerleşimde ton balıkları kıyıda olduğu için **kalıyor**, yeni yerleşimde geçiyor.
- EditMode tamamı: **1369/1369**.
- İlk denemede iki balık resif ve derin demir sütunlarına düştü, `DiveTestAreaReefDeepSceneTests` bunu yakaladı. Üç ev konumu sütunun dışına alındı.
- Aynı PC'de ürün günü (29 Ekim, `-Mute`): 1p, 2p ve 4p ×2 **PASS**, yeni süreçte açma da PASS (`Logs/P46-product-20261029-180506-889-4`, `-180855-418-4`, `-181240-983-2`, `-181559-396-1`).
- Sürücü düzeltmesi: varsayılan 4p gün, sürücüdeki kalabalık yüzünden ilk 4 denemenin 3'ünde takıldı. İki kez yatakta (oyuncu 2 yatak sırasını kesen komşu yüzünden host'tan `ETKILESIM HEDEFI YOK` aldı), bir kez dalış girişinde (iki misafir aynı giriş noktasına sıkıştı). Balık yerleşimi bu noktalara dokunmuyor; değişikliksiz integration'da tek 4p geçti. `ProductDaySmokeDriver`'da #188'de yalnız `-HuntFirst` için açtığım yatak şeridi ve ayrık su girişi artık her koşuda kullanılıyor.

## Açık kalanlar

- **Beş normal tür.** Bugün 3 tür var. 19 Ekim'deki iş paylaşımına göre kalan ikisi resif için orfoz, derin için fener balığı. Model, rig, ansiklopedi ve ekonomi Mert'te. Spawn, habitat ve erişim kilidi Utku'da.
- **Levrek modeli.** Levrek bugün `CoralGrouper` (orfoz benzeri) modelini kullanıyor. Orfoz resife eklenince ikisi karışır. Mert'e not.
- **Derinlikle artan değer.** Üç tür 120'de eşit. Bu Mert'in ekonomi işi; bu teslim fiyata dokunmadı.
- **Gerçek görünürlük.** Resif ve derin balıklarının 1/2/4 oyuncuda gerçekten görülmesi, ürün sürücüleri o habitatlara dalmadığı için bu koşularda ölçülmedi. Ayrı bir görünürlük koşusu ve kullanıcı/ekip değerlendirmesi gerekiyor.
- Ton gerçek "derin" bantta (y < −12) değil, resif kenarındaki açık suda. Derin bant fener balığına ayrıldı.
