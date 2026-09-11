# P2-B dilim kaydı — balık DiveTestArea'ya + sualtı sis/ışığı

Bu bir faz kapanış kaydı değildir; [P2-B](../plan/PHASES.md) görevinin sahne dilimine ait teslim ve test kaydıdır. Faz kapanışı [şablona](../templates/PHASE_REPORT.md) göre ayrıca yapılır.

- Dilim / tarih: P2-B sahne işi — 11 Eylül 2026
- Sahip: Utku (B). İnceleyecek: Mert (WORKFLOW: Utku'nun işini Mert inceler)
- Dal: `p2/utku-fish`
- Ortam: Unity 6000.3.23f1, URP 17.3.0, Windows 11
- Kapsam: yalnızca `Assets/DeepDive/World/` ve `DiveTestArea.unity`

## Teslim

| İş | Sonuç |
|---|---|
| Balık sahnede: `Fish_SeaBass` (NetworkObject, NetworkTransform, CapsuleCollider, CatchObject, FishActor) | TAMAM |
| `SpeciesDefinition` asset'i: `sea_bass` / "Levrek" | TAMAM |
| Temel sualtı sisi/ışığı (fog, ambient, ışık, skybox, su yüzeyi) | TAMAM |
| Sahne bütünlük testleri | TAMAM (7 test) |

Yeni dosyalar:

```
Assets/DeepDive/World/Editor/DiveTestAreaSetup.cs        kurulum script'i (menü + batchmode)
Assets/DeepDive/World/Editor/DeepDive.World.Editor.asmdef
Assets/DeepDive/World/Fish/Species/SeaBass.asset         SpeciesDefinition
Assets/DeepDive/World/Art/FishMaterial.mat               URP/Lit
Assets/DeepDive/World/Art/UnderwaterSky.mat              Skybox/Procedural
Assets/DeepDive/World/Art/WaterSurfaceMaterial.mat       URP/Unlit
Assets/DeepDive/World/Tests/Editor/DiveTestAreaSceneTests.cs
```

Değişen dosyalar: `Assets/DeepDive/World/Scenes/DiveTestArea.unity`, `Assets/DeepDive/World/Tests/Editor/DeepDive.World.Tests.asmdef`. Mehmet'in, Mert'in ve `ProjectSettings/` dosyalarında değişiklik yok.

## Yöntem kararı: sahne elle değil, editör script'iyle yazıldı

`DiveTestAreaSetup.Apply` sahneyi Unity'nin kendi API'siyle kuruyor ve kaydediyor. Gerekçe: `NetworkObject.GlobalObjectIdHash` Unity tarafından `GlobalObjectId`'den üretilir ve sahneye gömülü nesnenin host/istemci eşleşmesi bu değere bağlıdır. `OnValidate` editöre özeldir ve sahneyi dirty işaretlemez; elle yazılmış veya sıfır bir değer build'e sessizce taşınır, hata yalnızca iki makineli denemede ortaya çıkar.

Script idempotenttir: aynı isimli nesneyi günceller, ikinci balık üretmez. Kötü bir sahne merge'inin tamir yolu menüyü yeniden çalıştırmaktır, elle YAML düzeltmek değil.

`NetworkPrefabs.asset` kaydına gerek olmadığı doğrulandı: `NetworkSession` `NetworkConfig.EnableSceneManagement`'ı açıyor ve dalışı `NetworkSceneManager` ile yüklüyor, bu yüzden NGO sahne nesnelerini hash ile eşliyor. Prefab listesi (Mehmet'in dosyası) bu dilimde hiç açılmadı.

## Test sonuçları

| Kontrol | Sonuç ve kanıt |
|---|---|
| EditMode, tüm assembly'ler | PASS 81/81 — `Logs/P2-all-tests.xml` |
| EditMode, `DeepDive.World.Tests` | PASS 54/54 — `Logs/P2-world-tests.xml` |
| Yeni sahne testleri (`DiveTestAreaSceneTests`) | PASS 7/7: tek balık + geçerli tür, `GlobalObjectIdHash != 0`, sunucu yetkili `NetworkTransform`, trigger olmayan collider, `CatchObject` aynı nesnede, gezinme kutusu `SwimVolume` içinde, sis açık |
| Sahne kurulumu | `P2_DIVEAREA_READY fish=Fish_SeaBass species=sea_bass home=(0,4,8) fogDensity=0.055` — `Logs/P2-divearea-apply.log` |
| Üretilen hash | `GlobalObjectIdHash: 3779525944` (sıfır değil, Unity üretti) |
| Görsel kontrol | `Logs/P2-divearea-preview.png` render edildi ve gözle bakıldı; üç tur düzeltme yapıldı (aşağıda) |

Görsel düzeltmeler: su yüzeyi düzlemi aşağı baktığı için `Lit` shader ile neredeyse siyah kalıyordu, `Unlit`'e alındı; duvar üstü (y=7) ile yüzey (y=8.1) arasındaki boşlukta sis uygulanmayan skybox sert siyah bir şerit olarak görünüyordu, düzlem 30×30'dan 60×60'a büyütülerek kapatıldı.

## Bilinen kusur — bilerek kabul edildi

Av toplandığında `CatchObject.Consume` → `NetworkObject.Despawn(true)` sahneye gömülü nesneyi yok ediyor. NGO şu uyarıyı basıyor: *"Destroying in-scene network objects can lead to unexpected behavior."*

Davranış doğrudur: balık host'ta ve istemcilerde kaybolur, sonraki dalışta sahne yeniden yüklendiği için geri gelir. Maliyeti yalnızca log kirliliğidir. Alternatifleri daha kötü olduğu için kabul edildi:

- `Despawn(false)`: ceset istemcilerde görünür kalır.
- Prefab'tan çalışma zamanında spawn: `NetworkPrefabs.asset` kaydı gerekir, o Mehmet'in dosyasıdır ve bu dilimin kapsamı dışındadır.

Karar kullanıcı onayıyla alındı (11 Eylül 2026).

## Açık sorun: av zinciri Composition'da bağlı değil

`DiveContext.Bind` ve `CatchClaim.Bind` bütün depoda yalnızca testlerden çağrılıyor (`FishRulesTests.cs:204`, `CatchRulesTests.cs:219`). `SessionNetworkAdapter` içinde bu çağrılar **yok**; `DiveInventoryBinding` adında bir tip ne çalışma ağacında ne de uzak dalların hiçbirinde var. Yapısal kanıt: `DeepDive.Composition.asmdef` yalnızca `DeepDive.Core.Contracts`, `DeepDive.Network`, `DeepDive.Session`, `Unity.Netcode.Runtime` ve `Unity.Collections` referanslarını taşıyor — `DeepDive.World` ve `DeepDive.Inventory` görünür değil, dolayısıyla bu bind asmdef değişmeden derlenemez. `InventoryManager.TryAddCatch`'in de üretim kodunda çağıranı yok.

Sonucu: bugün balık yüzer, kaçar, vurulur ve ölür; ama `P2_FISH_DEAD_NO_DIVE` logu düşer, `CatchObject` kurulmaz ve toplama hiç denenemez. Bağlama noktası Composition'da, tüketicisi Mert'in `InventoryManager`'ı. World tarafındaki seam'ler (`IDiveContext`, `ICatchClaimSink`) hazır; gerçek av testinden önce bu bağlamanın sahibi kararlaştırılmalıdır.

## Ekip için üç bağımlılık

1. **`maxHealth = 3` ile `NetworkPlayer.harpoonDamage = 1` birbirine bağlı.** 0.65 sn bekleme ile üç vuruş ≈ 2 saniye. Mehmet hasar değerini değiştirirse `SeaBass.asset` yeniden ayarlanmalıdır.
2. **Smart Merge kurulu değil.** `.gitattributes` `*.unity` için `merge=unityyamlmerge` tanımlıyor, ama bu makinede `git config merge.unityyamlmerge.cmd` boş; Git sessizce normal merge'e düşüyor. P0'da kurulacağı yazılmıştı, kurulmadı.
3. **`ProjectSettings/GraphicsSettings.asset`'i URP kendisi yazıyor.** Grafik modunda her editör açılışında `m_LightsUseLinearIntensity` ve `m_LightsUseColorTemperature` 1 oluyor (`UniversalRenderPipeline.cs:465-466`). Ortak dosya ve bu dilimin kapsamı dışında olduğu için commit'e alınmadı; herkeste çıkacağı için kimin kaydedeceği ekipçe kararlaştırılmalı.

## Doğrulanmayanlar

Aşağıdakiler yapılmadı; PASS sayılmaz:

- Gerçek host + istemci dalışı (iki makine gerekir; P2 kapanışının 1/2/4 oyuncu şartı).
- Av zincirinin uçtan uca çalışması — yukarıdaki bağlama eksikliği yüzünden mümkün değil.
- PlayMode testi yok; balığın gerçekten yüzdüğü yalnızca saf C# `FishMotion` testleriyle doğrulandı.
- Build alınmadı; bu dilim kod/sahne değişikliğidir, CI bağlama işi P2-B'nin sonraki dilimidir.
