# Mixamo kaynak dosyalarını yerelde yeniden üretme (P3.1-C)

`Assets/DeepDive/Player/Character/DiverBase.fbx` ve `Animations/{Walk,SwimSurface,SwimIdle,Idle}.fbx`
repoya **commit edilmez** (bkz. `.gitignore`). Mixamo lisansı, karakter/animasyonların oyun
içinde kullanımını serbest bırakıyor ama ham dosyaların tek başına (standalone) dağıtımını
yasaklıyor — public bir repoda bu dosyaları tutmak o yasağı ihlal eder. Bu yüzden her
geliştirici kendi Adobe hesabıyla aynı beş dosyayı indirip **aynı dosya adı ve klasörle**
yerel projeye koymalı; `.meta` dosyaları repoda kalır, GUID'ler sabit olduğu için prefab/
controller referansları otomatik yeniden bağlanır.

## Mixamo'da arama (site içi arama kutusu bazen JS event'lerine tepki vermiyor — URL ile arama çalışıyor)

`https://www.mixamo.com/#/?page=1&query=<terim>&type=Character` (karakter için) veya
`type=Motion` (animasyon için).

## İndirilecek 5 dosya

| Hedef dosya | Mixamo öğesi | Arama terimi / tip | Not |
|---|---|---|---|
| `Assets/DeepDive/Player/Character/DiverBase.fbx` | "Default Character" (nötr manken), "Swimming" (Swimming Underwater) klibiyle birlikte indir | `type=Character`, sonra karakter sayfasında "Swimming" animasyonunu seçip **With Skin** olarak indir | Format: FBX Binary, "Without Skin" DEĞİL — mesh+rig ilk indirmede gelir |
| `Assets/DeepDive/Player/Character/Animations/Walk.fbx` | "Walking" | `query=walking&type=Motion` | In Place: kapalı, Skin: Without Skin (avatar zaten DiverBase'de var) |
| `Assets/DeepDive/Player/Character/Animations/SwimSurface.fbx` | "Swimming To Edge" (Breastroke) | `query=swimming&type=Motion` | Sonuçlarda "Swimming To Edge" |
| `Assets/DeepDive/Player/Character/Animations/SwimIdle.fbx` | "Treading Water" | `query=treading&type=Motion` | |
| `Assets/DeepDive/Player/Character/Animations/Idle.fbx` | "Idle" (Standing Idle) | `query=idle&type=Motion` | |

## İndirdikten sonra

1. Beş dosyayı yukarıdaki tam yola aynı adla kopyala (üzerine yaz, `.meta` dosyaları repoda
   zaten var — silme).
2. Unity'yi aç, `DeepDive/P3.1-C/Setup Diver Character` menüsünü çalıştır
   (`Assets/DeepDive/Player/Character/Editor/DiverCharacterSetup.cs`) — import ayarlarını,
   klip isimlerini/loop'ları, Animator Controller'ı ve `DiverCharacter.prefab`'ı bu script
   yeniden kurar; idempotent, tekrar çalıştırmak güvenli.
3. `docs/assets/CREDITS.md`'deki kayıt zaten güncel; yeni bir satır eklemene gerek yok, sadece
   indirme tarihini değiştirmiyorsan mevcut kayıt geçerli kalır.
