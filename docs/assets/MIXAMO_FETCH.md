# Mixamo kaynak dosyalarını yerelde yeniden üretme (P3.1-C)

> 23 Eylül 2026: Bu set artık isteğe bağlı tarihsel kaynaktır. Ortak build, kaynakları depoda bulunan CC0 `CoastalDiver` / `CoastalArms` setini kullanır. Oyunu açmak veya build almak için Mixamo indirmeniz gerekmez. Katalog özel Mixamo importuyla kendiliğinden değişmez. Kaynaklar: [CREDITS.md](CREDITS.md).

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
| `Assets/DeepDive/Player/Character/DiverBase.fbx` | "Y Bot" (Mixamo'nun iki nötr mankeninden biri; "X Bot" da rig/bone adlandırması aynı olduğu için işlevsel olarak eşdeğerdir), "Swimming" (Swimming Underwater) klibiyle birlikte indir | `query=y%20bot&type=Character`, sonra Animations sekmesinden "Swimming" (Description: Swimming Underwater) seçip **With Skin** olarak indir | Format: FBX Binary, "Without Skin" DEĞİL — mesh+rig ilk indirmede gelir |
| `Assets/DeepDive/Player/Character/Animations/Walk.fbx` | "Walking" (Description: **Male Standard Walk** — "walking" araması çok sonuç döndürür, açıklaması "Male Standard Walk" olanı seç) | `query=walking&type=Motion` (2. sayfada) | In Place: kapalı, Skin: Without Skin (avatar zaten DiverBase'de var) |
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
