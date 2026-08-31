# P0 projesini açma ve build alma

- Editör: **Unity 6.3 LTS — 6000.3.23f1**. Başka yama ile açıp projeyi yükseltmeyin.
- URP: **17.3.0**. Visual Studio Editor paketi: **2.0.26** (VS Code Unity eklentisiyle de kullanılır).
- Kesin kaynaklar: `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json` ve `Packages/packages-lock.json`. Sonraki fazların ağ/oyun paketleri henüz eklenmedi.
- Hedef: Windows x64, P0 için Mono, Direct3D 11. Örnek build için IL2CPP derlemesi şart değil.

## Aç

1. Unity Hub'dan **6000.3.23f1** kurun ve kendi Unity hesabınızla geçerli lisansınızı etkinleştirin. Lisans/hesap dosyalarını Git'e koymayın.
2. Depoyu klonlayın. P0-A incelemesi sürerken aşağıdaki dalı alın; PR birleştikten sonra ortak faz dalını kullanın.

   ```sh
   git fetch origin
   git switch --track origin/codex/p0-mehmet-foundation
   ```

3. Hub → Add project from disk ile **depo kökünü** seçin: `Assets`, `Packages` ve `ProjectSettings` klasörlerinin bulunduğu `DeepDiveGame`.
4. İlk paket yükleme/derlemeyi bekleyin. `Assets/P0/Scenes/P0Example.unity` sahnesini açıp Play'e basın.

Beklenen görüntü: koyu mavi arka plan, düz zemin ve aydınlatılmış turkuaz küp. Hareket, balık, co-op veya menü yok; bu yalnızca kurulum kontrolüdür.

## Windows build

Unity menüsünden **DeepDive → Build P0 Windows** seçilebilir. Komut satırından, bu projeyi açan Unity penceresini kapatıp depo kökünde çalıştırın:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\Build-P0.ps1
```

Unity farklı klasördeyse `-UnityEditor 'D:\...\Editor\Unity.exe'` ekleyin. Komut mevcut sahneyi derler; sahneyi yeniden üretmez. Yerel log: `Logs/P0-build.log`.

Çıktı: `Builds/P0-Windows/DeepDiveGame-P0.exe`. Başka bilgisayara **P0-Windows klasörünün tamamını** gönderin; yalnızca EXE yeterli değildir. Açıldığında aynı küp/zemin görünmeli, pembe materyal veya hata olmamalı. Pencereyi X ya da Alt+F4 ile kapatın.

**Ortak test paketi:** [P0-A-9596244 Windows ZIP ve SHA-256](https://github.com/mehmetalisahingm/DeepDiveGame/releases/tag/untagged-7da6b5907df23077c77e). Bu yayınlanmamış taslağı yazma davetini kabul etmiş ekip üyeleri kendi GitHub hesaplarıyla görür; yayınlamayın. ZIP'i ayrı bir klasöre tamamen çıkarın. Kaynak commit `9596244`, inceleme [PR #4](https://github.com/mehmetalisahingm/DeepDiveGame/pull/4). Build iki kez alındı; görsel kontrol ve üç bilgisayar testi henüz tamamlanmadı.

Her kişi kendi GitHub görevine kullanılan build kimliğini ve kendi bilgisayarındaki sonucu yazar. Mehmet'in bilgisayarındaki kontrol diğer iki kişinin testi yerine geçmez.

## Dosya sınırı

- `Assets`, varlıklarla birlikte `.meta`, `Packages`, `ProjectSettings` ve `tools` kaynakları paylaşılır.
- `Library`, `Temp`, `Logs`, `UserSettings` ve `Builds` üretilir, Git'e girmez.
- `Assets/P0` Mehmet'in kurulum örneğidir; gelecekteki oyuncu/dünya/kasaba klasör ve sahne düzenini Utku P0-B'de netleştirir.
- İlk `.gitignore`, metin/.meta ve ortak render ayarları Utku incelemesine sunulan başlangıç önerisidir. P0-B tamamlandı sayılmaz.
- P0-A PR'ı `codex/p0-integration` dalına gider. Utku incelemesi ve gerçek ekip kontrolleri olmadan P0 kapanmaz; P1 kapalı kalır.

Komutların dayanağı: [Unity Editor komut satırı](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html) ve [BuildPipeline.BuildPlayer](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/BuildPipeline.BuildPlayer.html).
