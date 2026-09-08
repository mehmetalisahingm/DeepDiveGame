# P0 kapanış kaydı

- Faz / tarih: P0 / 8 Eylül 2026
- Koordinatör: Mehmet
- Ortak temel: `main` / `14e34f1`
- Faz PR'ları: [PR #8](https://github.com/mehmetalisahingm/DeepDiveGame/pull/8), [PR #10](https://github.com/mehmetalisahingm/DeepDiveGame/pull/10)
- Örnek build: `P0-A-9596244`, Windows x64/Mono
- Ortam: Mehmet, Utku ve Mert'in ayrı Windows kurulumları; Unity `6000.3.23f1`

## Teslim ve test

| Görev / kabul maddesi | Sonuç ve kanıt |
|---|---|
| Mehmet (P0-A) | PASS — ortak Unity/URP temeli, sürümler ve Windows build'i main'e birleşti; [görsel kontrol](../evidence/P0-A-9596244-visual-check.md) geçti |
| Utku (P0-B) | PASS — Git düzeni [PR #7](https://github.com/mehmetalisahingm/DeepDiveGame/pull/7) ile birleşti; main `14e34f1`, Unity açılışı, sahne ve Windows build sonucu [görev #2'de](https://github.com/mehmetalisahingm/DeepDiveGame/issues/2#issuecomment-5584821314) kaydedildi |
| Mert (P0-C) | PASS — main `d46faff`, Unity açılışı, sahne ve Windows build sonucu [görev #3'te](https://github.com/mehmetalisahingm/DeepDiveGame/issues/3#issuecomment-5560508048) kaydedildi; kaynak planı PR #10 ile birleşti |
| Ortak kabul | PASS — üç kişi güncel ortak temeli aynı Unity sürümünde açtı ve Windows build'ini çalıştırdı |
| Git/meta düzeni | PASS — `.gitignore`, LFS/Smart Merge, Visible Meta Files ve Force Text depoda doğrulandı |

Mert'in test ettiği `d46faff` ile Utku'nun test ettiği `14e34f1` arasında yalnızca README ve plan belgeleri değişti; Unity proje/build içeriği değişmedi.

- Açık sorun: Utku'nun Unity kurulumunda isteğe bağlı Visual Studio 2026 modülü hata verdi; Unity Editor, Windows Build Support ve ayrı kurulu Visual Studio ile proje/build başarılı olduğu için P0'ı engellemedi.
- P0_MEETING açık kararları: kullanıcı kararıyla P0 kapısını bekletmez; ilgili P1+ özelliği başlamadan görev içinde netleştirilir. D06 host kayıt sınırı korunur.
- Sonuç: **P0 KAPALI, P1 AÇIK.**
