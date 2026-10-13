# #154 görsel kalite: mevcut placeholder envanteri (taslak)

Tarih: 2026-10-13. Taban: `codex/p4-integration` @ `7af7a3f`. Dal: `p4/mert-visual-inventory`. Kapsam: yalnız **envanter**; sahne/ortak dosya değişikliği yok (o iş #151/#152 birleşmesine sıralandı). Ücretli satın alma yetkisi yoktur; hiçbir dış varlık bu belgeyle eklenmedi.

## Kısa tablo: gerçek sanat ne, placeholder ne?
| Alan | Şimdiki durum | Placeholder mı? | Not |
|---|---|---|---|
| Oyuncu insan modeli | Quaternius Ultimate Modular Characters "Beach" + Humanoid animasyonu (`Player/Character`), kaynak ve lisans `docs/assets/CREDITS.md`'de | **Gerçek (kaynaklı)** | Yüzme klibi yok, kodla poz; tüp/kamera/zıpkın prop'ları ilkel geometri |
| Ekipman prop'ları | `CameraProp`, `HarpoonProp`: küp/silindir/kapsül | Placeholder | CREDITS'te "yerel üretim" olarak kayıtlı |
| NPC'ler (4 hizmet) | Kapsül gövde + küre baş (`Town/Editor/TownServiceSetup`) | Placeholder | V05 "insan görevli" teslim edilmedi |
| Dükkân cepheleri | Primitive tezgâh/çatı/tabela/fener (`TownStorefront`, #142) | Placeholder | Çalışma anında, `Dev_*` düz renk malzeme |
| Ev (PrepArea + DiveTestArea) | Küp bloklar: yatak/depo/PC (`HomeSceneSetup`, `HomePcView`), ev rafı/trofe (`DevelopmentVisuals`) | Placeholder | PC kutusu bir yerde hâlâ magenta (CreatePrimitive malzemesi) |
| Kasaba zemini/binaları | `ContinuousTownLayout` slab/dolgu/kenar blokları; "teknik küp blokajı" | Placeholder (blockout) | #138/#151 ile değişiyor |
| Kumsal/sığlık/iskele | Beach_Platform/Wade, Dock; `CoastalHarborLink` | Placeholder | Sahne dosyasında ~38 yerleşik ilkel mesh (`DiveTestArea`) |
| Su, gökyüzü, ışık | `WaterSurfaceMaterial`, `UnderwaterSky`, `CoastalAtmosphere` (kodla gün/gece sis+güneş) | Kısmen gerçek davranış, sanatı yok | Doku yok, tek düz renk malzeme |
| Balık | Tek tür (`SeaBass.asset`), `FishMaterial` | Placeholder | Beş tür bütçesi #155 (Utku) |
| Sandal/tekneler | `BoatHullPresentation`: küp parçalar | Placeholder | Üç gövde de ilkel |
| Boss/derin | `DeepEncounterWorld`: küre işaret + kapsül boss | Placeholder | |
| Bitki/kaya/çevre seti | Yok | Eksik | V07/V09 |
| UI | IMGUI kutuları + uGUI panolar, yerleşik font | Teknik arayüz | Okunabilirlik #154 kabulünde ölçülecek |
| Ses | 4 SFX (Mixkit, kaynaklı) | Çok az | Ambiyans/adım/su yok |

Sayılar: `.fbx` 7 (hepsi oyuncu karakteri/animasyonu), prefab 7 (hepsi oyuncu/ekipman), doku **0** (`.png/.jpg/.tga` yok), malzeme ~50 (düz renk: 22 `DevVisuals`, 11 karakter, 9 `Town/Art`, 4 `World/Art`), ses 4. Kodla üretilen ilkel geometri: 10 çalışma-anı dosyası (`TownStorefront`, `DevelopmentVisuals`, `ContinuousTownLayout`, `CoastalHarborLink`, `HomeSceneSetup`, `HomePcView`, `BoatHullPresentation`, `PlayerPresentationView`, `DeepEncounterWorld`, `P45WorldConditions`).

## ASSET_PLAN karşılaştırması (V paketleri)
Karşılanan: V01/V02/V03'ün insan kısmı. **Placeholder kalan:** V04 (kıyı/kasaba), V05 (NPC), V06/V13 (tekneler), V07 (çevre/ses), V09/V14 (canlı/boss), V11/V12 (ev/PC), V15/V16 (gün ışığı sanatı, gelişim). V17 "son bütünlük" ancak bunlardan sonra.

## Sonraki adım (henüz yapılmadı)
1. **Görsel referans:** stilize kıyı yönü için kullanıcıdan bir yön onayı gerekir (renk paleti, bina silueti, ölçek). Aday setlerin lisansı doğrulanmadan hiçbir şey eklenmeyecek; her ekleme `docs/assets/CREDITS.md`'ye kaynak/lisans/tarihle işlenir.
2. **Önce/sonra ölçümü:** UI okunabilirliği, ışık ve kare süresi için ölçüm yöntemi (aynı build, aynı vantage). Henüz ölçülmedi.
3. **Ortak sahne değişiklikleri** (`DiveTestArea`, `PrepArea`, oyuncu prefabı) #151/#152 birleşmesinden sonra, Utku/Mehmet'le sırayla.
