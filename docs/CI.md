# P2 otomatik build

`.github/workflows/unity-p2.yml`, güvenilen depo dallarındaki push ile EditMode testlerini ve mevcut `DeepDive.Editor.P1IntegratedBuild.BuildWindows` metodunu çalıştırmak için hazırlanmıştır. Sürüm `6000.3.23f1`; hedef Windows Mono. Fork PR'ları tetiklemez, kişisel bilgisayara runner kurulmaz, oyun yayımlamaz. Action sürümleri commit SHA ile sabittir.

## Gerçek engel — 11 Eylül 2026

GitHub deposunda Unity CI secret'ları bulunmuyor. Bu yüzden ilk lisans kontrolü açık hata verir; Unity çalıştırılmadan yeşil test/build sonucu üretmez. Sorumlu Mehmet; Utku'nun CI işi kullanıcı talimatıyla bu çalışmada devralındı. CI dosyasının varlığı başarılı bulut build'i değildir.

Gerçek GitHub denemesi: [run 34611444144](https://github.com/mehmetalisahingm/DeepDiveGame/actions/runs/34611444144), kaynak `66d541f`. Lisans ön kontrolünde `Unity CI licensing is not configured` hatasıyla durdu; Unity test/build adımları çalışmadı.

Mehmet, uygun Unity CI lisansını seçip GitHub Actions repository secrets bölümüne `UNITY_EMAIL`, `UNITY_PASSWORD` ve lisans türüne göre `UNITY_LICENSE` veya `UNITY_SERIAL` eklemeli. Şifre/lisans sohbet mesajına, kaynak dosyasına veya rapora yazılmaz. Yerel Hub oturumunun GitHub runner'da geçerli olduğu varsayılmaz; ücretli lisans satın alınmadı.

Resmî uygulama belgeleri: [GameCI activation](https://game.ci/docs/github/activation/) ve [custom build method](https://game.ci/docs/github/builder/#buildmethod). Bulut sürüm/imaj ve lisans uyumluluğu, bilgiler sağlandıktan sonra gerçek çalıştırmada doğrulanır.

## Doğrulanmış yerel yöntem

Unity Editor kapalıyken:

```powershell
.\tools\Build-P1.ps1 -Integrated
.\tools\Test-P1-Integrated.ps1 -Players 2 -Hunt
```

İkinci komut iki gerçek oyun süreci açar; istemci girdisi ve RPC ile sahnedeki balığın vurulmasını, ava dönüşmesini, çantaya girmesini ve iki tarafta despawn olmasını kontrol eder. Test sağlayıcısı normal oyunda etkin değildir; yalnız `-p2-hunt 1` test seçeneğinde çalışır. Bu test, Mert'in henüz bağlanmamış güvenli dönüş/istemci çanta UI kabulü veya farklı bilgisayarlarda internet testi değildir.
