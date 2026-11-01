# #218 · Beş tür görsel kiti — R05 tür tablosu önerisi (1 Kasım 2026)

Durum: **ÖNERİ.** R05 sahibi Utku'dur (`docs/plan/EXPERIENCE_DESIGN.md` R05); bu tablo kimlik/habitat/ağırlık kararı değildir. Utku ve Mehmet onaylamadan koda, fiyata veya sahneye uygulanmadı. Ağırlık ve fiyat sütunları #218 ve Q11'in dediği gibi "ilk sayı"dır, denge geçişi ayrıdır. PASS veya kabul yazılmadı.

## Depodaki gerçek durum (`codex/p4-integration` 668a47a)

- Oyunda üç normal tür var: `sea_bass`, `tuna`, `red_snapper`; hepsi `EconomyManager` içinde 120/av (`EconomyManager.cs:145-147`). Toplam 15 birey (5 + 5 + 5).
- Görsel yolları `PlayerActionView.cs:61-62` içinde: `sea_bass → SuppliedKit/CoastalFish`, `tuna → SuppliedKit/Tuna`, `red_snapper → SuppliedKit/RedSnapper`.
- `CoastalFish` prefabı `Assets/DeepDive/Art/Supplied/Fish/CoralGrouper.fbx` modelinden üretilir (`SuppliedKitImport.cs:42`). Yani levrek, adı orfoz olan modelin görselini kullanıyor; #218'in söylediği levrek–orfoz karışıklığı budur.
- `Assets/DeepDive/Art/Supplied/Fish/` içinde yalnız `CoralGrouper`, `RedSnapper`, `Tuna` (ve tekne) FBX'i var. Elimdeki iki Quaternius ZIP'inde (`FBX-20261016…`, `Pirate Kit…`) yalnız durağan `Prop_Fish_Mackerel` ve `Prop_Fish_Tuna` var; bunlar rig/yüzme klibi içermez, Q10'u karşılamaz.
- Dolayısıyla eksik iki tür için **depoda kaynak model yok**. "Animated Cute Fish" paketinin ZIP'i bu bilgisayarda bulunamadı.

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
- ZIP depo dışında tutuluyor (`%TEMP%cutefish-20261101`, Downloads'taki `drive-download-20261101T142039Z-1-001.zip`). **Depoya hiçbir dosya eklenmedi**: Unity lisansı geçersiz olduğu için FBX'ler importlanıp `.meta` dosyaları üretilemiyor.
- Bayt taramasında paketteki balıkların (Anglerfish, Tang, Humphead, Turbot, Swordfish, CoralGrouper, Tuna, Sunfish) aynı `Fish_Armature` rigini ve aynı klip adlarını taşıdığı görüldü: `Swimming_Impulse`, `Swimming_Fast`, `Attack`, `Death`, `Out_Of_Water`. Bu, yüzme, hızlanma/kaçış, ölüm ve sudan çıkma klibi tablosunun (#218 3. madde) tek rig üzerinden kurulabileceğini gösterir; Unity'de klip uzunlukları ve pivotlar henüz ölçülmedi.
- **Fener balığı:** `Anglerfish.fbx` (318 KB) kahverengi, dişli, başında ışıklı olta uzantılı: derin türün açık siluet adayı.
- **Levrek:** Pakette adı levrek olan model yok. Tanıtım görselindeki balıkların hepsi iri gözlü, kısa gövdeli çizgi film biçiminde; `Tang`, `Humphead`, `Turbot`, `Swordfish` aday olarak not edildi ama hangisinin levrek gibi okunduğu Unity'de aynı kadrajda, yan yana ve tür rengiyle denenmeden seçilmedi. Aynı modelin renk kopyası tür sayılmaz (#218).
- Orijinal Drive sayfasında `License.txt` (364 bayt) ve `Preview.jpg` da var; lisans dosyası indirmeye dahil edilmedi, CC0 kaydı yalnız resmî sayfaya dayanır.

## Sonraki adım (sırayla)

1. Utku/Mehmet bu tabloyu onaylar veya düzeltir (R05).
2. Unity lisansı yeniden etkinleştirilir; Anglerfish ve levrek adayı FBX'leri içe alınır (`.meta` ile), `CREDITS.md` kaydı eklenir.
3. Önce bir tür uçtan uca (levrek yeni siluet), sonra fener balığı ve orfoz ayrımı; her biri için av → kıyıya dönüş → tek satış → çekim → ansiklopedi zinciri ve 1/2/4 süreç görünürlüğü ölçülür.
