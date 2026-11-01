# #218 · Beş tür görsel kiti — R05 tür tablosu önerisi (1 Kasım 2026)

Durum: **ÖNERİ.** R05 sahibi Utku'dur (`docs/plan/EXPERIENCE_DESIGN.md` R05); bu tablo kimlik/habitat/ağırlık kararı değildir. Utku ve Mehmet onaylamadan koda, fiyata veya sahneye uygulanmadı. Ağırlık ve fiyat sütunları #218 ve Q11'in dediği gibi "ilk sayı"dır, denge geçişi ayrıdır. PASS veya kabul yazılmadı.

## Depodaki gerçek durum (`codex/p4-integration` 668a47a)

- Oyunda üç normal tür var: `sea_bass`, `tuna`, `red_snapper`; hepsi `EconomyManager` içinde 120/av (`EconomyManager.cs:145-147`). Toplam 15 birey (5 + 5 + 5).
- Görsel yolları `PlayerActionView.cs:61-62` içinde: `sea_bass → SuppliedKit/CoastalFish`, `tuna → SuppliedKit/Tuna`, `red_snapper → SuppliedKit/RedSnapper`.
- `CoastalFish` prefabı `Assets/DeepDive/Art/Supplied/Fish/CoralGrouper.fbx` modelinden üretilir (`SuppliedKitImport.cs:42`). Yani levrek, adı orfoz olan modelin görselini kullanıyor; #218'in söylediği levrek–orfoz karışıklığı budur.
- `Assets/DeepDive/Art/Supplied/Fish/` içinde yalnız `CoralGrouper`, `RedSnapper`, `Tuna` (ve tekne) FBX'i var. Elimdeki iki Quaternius ZIP'inde (`FBX-20261016…`, `Pirate Kit…`) yalnız durağan `Prop_Fish_Mackerel` ve `Prop_Fish_Tuna` var; bunlar rig/yüzme klibi içermez, Q10'u karşılamaz.
- Dolayısıyla eksik iki tür için **depoda kaynak model yok**. "Animated Cute Fish" paketinin ZIP'i başlangıçta bu bilgisayarda yoktu; onayla resmî sayfadan indirildi (aşağıda).

## Önerilen tablo (Q11 dağılımı: sığ 2, resif 2, derin 1)

| SpeciesId | Ad (çalışma) | Habitat / derinlik | Model durumu | Ağırlık (g, ilk sayı) | Taban değer (ilk sayı) |
|---|---|---|---|---|---|
| `sea_bass` | Levrek | sığ kıyı, 0–10 m | **yeni siluet gerekir** (şu an CoralGrouper kopyası) | 600–1800 | 120 (değişmez) |
| `tuna` | Ton balığı | sığ-açık su, 0–15 m, hızlı kaçış | mevcut `Tuna.fbx` | 3000–8000 | 120 (değişmez) |
| `red_snapper` | Kırmızı mercan | resif, 10–25 m | mevcut `RedSnapper.fbx` | 800–2500 | 120 (değişmez) |
| `grouper` | Orfoz | resif/kaya yatağı, 15–30 m, yavaş, yakın kaçış | mevcut `CoralGrouper.fbx` (levrekten ayrılır) | 2000–6000 | 180 (yeni tür) |
| `lanternfish` | Fener balığı | derin, 30 m ve altı, düşük ışık | **yeni model gerekir** | 300–900 | 320 (yeni tür) |

Gerekçe ve sınırlar:

- Tuna "otomatik derin" ilan edilmedi (Q11); hızlı açık su balığı olarak sığ-açık suya konuldu. Bu Utku'nun habitat kararına bağlı.
- Mevcut üç türün 120 fiyatına dokunulmaz: eski kayıt ve daha önce ödenmiş avlar etkilenmez. Fark yalnız yeni iki türün **ilk satışından** gelir; sadece derine inmek çarpan olmaz, değer `SpeciesId` tablosundan gelir.
- Orfoz modeli zaten depoda; levrek ona bağlı olduğu için karışıklığı gidermek levreğe ayrı bir siluet vermeyi gerektirir. Fener balığı için de yeni kaynak şarttır. Yani iki yeni model gerekir: levrek ve fener balığı.
- 2/2/1 dağılımı R05'te Utku ile netleşmeden uygulanmaz; kıyıdaki iki başlangıç türünün (şu an sığ olanlar) kamerasız/sandal tamirsiz erişimi korunmalıdır.
- Fiyat tablosu eklemek `EconomyManager` ve kayıt geriye uyumunu etkiler. Bu Mert'in dosyasıdır; uygulama ayrı PR'dır.

## Kaynak: indirildi, repoya alınmadı

- Kullanıcı onayıyla (1 Kasım 2026) Quaternius **Animated Cute Fish Pack** (Feb 2020) FBX klasörü indirildi: [resmî sayfa](https://quaternius.com/packs/cutefish.html), Drive klasörü, 52 FBX, ZIP 3,5 MB. Sayfada lisans **CC0** yazıyor (1 Kasım 2026'da okundu). Daha önce depoya alınan `CoralGrouper`, `RedSnapper`, `Tuna` aynı paketten.
- ZIP depo dışında tutuluyor (kullanıcının `Downloads` klasöründe `drive-download-20261101T142039Z-1-001.zip`). **Depoya hiçbir model dosyası eklenmedi**: önizleme için beş aday FBX geçici olarak içe alındı ve silindi; hangi modellerin kalıcı olacağı R05 onayına bağlı.
- Bayt taramasında paketteki balıkların (Anglerfish, Tang, Humphead, Turbot, Swordfish, CoralGrouper, Tuna, Sunfish) aynı `Fish_Armature` rigini ve aynı klip adlarını taşıdığı görüldü: `Swimming_Impulse`, `Swimming_Fast`, `Attack`, `Death`, `Out_Of_Water`. Bu, yüzme, hızlanma/kaçış, ölüm ve sudan çıkma klibi tablosunun (#218 3. madde) tek rig üzerinden kurulabileceğini gösterir; Unity'de klip uzunlukları ve pivotlar henüz ölçülmedi.
- **Fener balığı:** `Anglerfish.fbx` (318 KB) kahverengi, dişli, başında ışıklı olta uzantılı: derin türün açık siluet adayı.
- **Levrek:** Pakette adı levrek olan model yok ve hepsi iri gözlü, kısa gövdeli çizgi film balıkları; uzun ince bir levrek gövdesi yok. Beş aday Unity'de depodaki üç balıkla aynı kadrajda çizildi (aşağıdaki kanıt). Bulgular, **yalnız görsel ayrışmaya göre ve türün gerçek biyolojisine değil**:
  - `Anglerfish`: silüeti en ayırt edilir (başta olta uzantısı, dişli geniş ağız). Fener balığı için uygun.
  - `Tang`: yuvarlak disk gövde, belirgin çatal kuyruk; ayrışır ama levrek gibi okunmaz.
  - `Turbot`: yassı elmas gövde (yassı balık); ayrışır, levrek değil.
  - `Swordfish`: gagalı küçük gri gövde; ayrışır, levrek değil.
  - `Humphead`: yeşil, çıkık alınlı, orta uzun gövde; mevcut `CoralGrouper`'dan renk ve alın hatları ile ayrılır. Levrek için en yakın ve çalışma adayı olarak önerilir, ama sadece bu görüntüye dayanarak; renk ve ölçek Unity'de oyun ışığında yine denenmelidir.
  - Mevcut `CoralGrouper`, `Tuna` ve `RedSnapper` birbirine yakın tıknaz silüetlere sahip; bu üçü uzaktan yalnız renkle ayrışıyor. Yeni türlerin gerçekten ayrı silüet getirmesi bu yüzden önemli.
  - Aynı modelin renk kopyası tür sayılmaz (#218).
- Orijinal Drive sayfasında `License.txt` (364 bayt) ve `Preview.jpg` da var; lisans dosyası indirmeye dahil edilmedi, CC0 kaydı yalnız resmî sayfaya dayanır.

## Kanıt (editör önizlemesi, Windows oyuncusu görüntüsü değil)

- [Işıklı sıra](evidence/p411-species/species-lineup-lit.png) ve [siluet sırası](evidence/p411-species/species-lineup-silhouette.png): soldan sağa `CoralGrouper`, `Tuna`, `RedSnapper` (depoda), `Anglerfish`, `Tang`, `Humphead`, `Turbot`, `Swordfish` (aday). Hepsi depodaki fish hattıyla (`SuppliedKitImport.Make`, 1,05 m uzunluk) ölçeklendi, `Swimming_Impulse` klibinin %30'unda örneklendi, yandan, ortografik kamerayla çizildi. Kadrajı zenginleştirilmiş mercan/su ışığı yok; düz mavi zemin ve tek yönlü ışık.
- [`species-candidates.csv`](evidence/p411-species/species-candidates.csv): her modelde altı klip; `Swimming_Impulse` 1,17 s, `Swimming_Fast` 1,04 s, `Death` 3,00 s. Klipler sekiz modelde de aynı süredeler.
- Betik: `Assets/Editor/P411SpeciesPreview.cs` (`-executeMethod DeepDive.EditorTools.P411SpeciesPreview.PreviewBatch -previewOut <klasör>`). Çalışması için beş aday FBX'in (`Anglerfish`, `Tang`, `Humphead`, `Turbot`, `Swordfish`) geçici olarak `Assets/DeepDive/Art/Supplied/Fish/` içine kopyalanması gerekir; bunlar depoya **eklenmedi**, önizleme sonrası silindi. Geçici `Cand_*` prefab ve materyalleri betik kendisi siler.
- Ölçülmedi: oyun içi ışıkta, sualtı sisinde, gece ve uzak/yakın kadrajda okunurluk; ölüm/av görselinde canlı yüzme döngüsünün durması; çok oyunculu görünürlük. Bunlar türler gerçekten bağlandığında ölçülür.

## Sonraki adım (sırayla)

1. Utku/Mehmet bu tabloyu onaylar veya düzeltir (R05).
2. Onaylanan modeller (şimdilik Anglerfish ve levrek adayı) `.meta` dosyalarıyla içe alınır, `CREDITS.md` kaydı eklenir.
3. Önce bir tür uçtan uca (levrek yeni siluet), sonra fener balığı ve orfoz ayrımı; her biri için av → kıyıya dönüş → tek satış → çekim → ansiklopedi zinciri ve 1/2/4 süreç görünürlüğü ölçülür.
