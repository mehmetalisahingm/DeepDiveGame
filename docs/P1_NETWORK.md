# P1-A bağlantı ve oyuncu

Bu belge ayrı P1-A laboratuvarının teknik referansıdır. Üç kişinin birleşmiş
oyununu çalıştırmak için [P1 ortak build rehberini](P1_PLAY.md) kullanın.

Kaynak: `codex/p1-mehmet-network`, [PR #16](https://github.com/mehmetalisahingm/DeepDiveGame/pull/16).
Unity `6000.3.23f1`, NGO `2.7.0`, Unity Transport `2.6.0`.

## Hemen çalıştır

1. `Builds/P1-NetworkLab/DeepDiveGame-P1.exe` dosyasını çalıştır.
2. **Oda oluştur / Solo** ile host başlat. İkinci oyunda host IP'sini yazıp **Katıl**.
3. **F1** ile kontrolü al: WASD yürür, fare bakışı çevirir. Suda Space yukarı,
   sol Ctrl aşağı yüzdürür. **Esc** imleci açar; **Ayrıl** bağlantıyı kapatır.
4. Host, test ekranındaki düğmelerle sualtına geçer ve hazırlığa döner.

Bu ekran ve iki boş test sahnesi yalnız P1-A ağ doğrulaması içindir. Mert'in
oyuncu/hazır ekranı ve Utku'nun gerçek test bölgesi ayrı görevlerdir.

Unity'de `Assets/Tests/P1/Fixtures/P1NetworkLab.unity` açılıp Play ile hareket
denenebilir. Laboratuvar sahne geçişini kendi Windows test build'inde deneyin;
Editor'ün ortak build listesi artık gerçek `PrepArea` / `DiveTestArea` sahnelerini
kullanır. İki laboratuvar sahnesi Windows test komutunda açıkça verilir.

Windows build ve test komutları, Unity Editor bu projeyi açmıyorken depo kökünde:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Build-P1.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Build-P1.ps1 -Tests
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Test-P1-Network.ps1 -Solo -Port 17778
powershell -NoProfile -ExecutionPolicy Bypass -File tools/Test-P1-Network.ps1
```

Çıktı `Builds/P1-NetworkLab/DeepDiveGame-P1.exe`; taşırken klasörün tamamını taşı.
Test sahnelerini yeniden üretmek gerekirse `Build-P1.ps1 -GenerateFixtures`.
Yerel raporlar `Logs/P1-*` altındadır; kaynak olarak commit edilmez.

## Birleştirme

- `Assets/DeepDive/Core` içindeki `INetworkSession` ve `PlayerId` ortak bağlantı
  önerisidir; [koordinasyon kaydı](plan/P1_A_INTEGRATION.md) PR incelemesindedir.
- Kalıcı bootstrap nesnesine `NetworkManager`, `UnityTransport`, `NetworkSession`
  eklenir. Player Prefab, testteki `NetworkDiver` düzenini izler: `NetworkObject`,
  sunucu yetkili `NetworkTransform`, `CharacterController`, `NetworkPlayer`,
  sahibine özel Camera/AudioListener. Prefab aynı listeyle her iki build'de kayıtlıdır.
- Mert kendi bileşim noktasından `INetworkSession` referansını alır. `Changed`
  bağlantı/oyuncu listesini yeniler. `SceneLoaded` sonucu **host'ta** bildirilir;
  oturum revision ve hazır kuralları Mert'in sorumluluğundadır.
- Host `TryLoadScene(sceneName, requestId)` çağırır. `requestId` sıfırdan farklı,
  oturum içinde tekil olmalı; aynı id/aynı sahne tekrarı ikinci yükleme başlatmaz.
  Aynı id/farklı sahne reddedilir. Geçişte katılım kapanır; hazırlığa başarılı
  dönüşte Mert `SetJoinAllowed(true)` çağırır. Dalış boyunca kapalı kalır.
- Utku her sahneye `Slot` değerleri **0, 1, 2, 3** olan dört `PlayerSpawnPoint`
  koyar. Aynı slotun iki kez bulunması hatadır. Pozisyon ayak hizasıdır.
  Su hacmine `SwimVolume` ve BoxCollider ekler; zemin normal collider kullanır.
- Gerçek menü sahnesi `NetworkSession.offlineScene` alanına atanır. Host kaybında
  veya ayrılmada bağlantı/oyuncular temizlenir ve o menü yüklenir.
- Test sürücüleri `UNITY_EDITOR || DEVELOPMENT_BUILD` koşullu ayrı assembly'dedir;
  gerçek oyun başlangıcına bağlanmaz. P0 build listesi değiştirilmez.

## Ağ ve doğrulama sınırı

Hareket paketi yalnız tuş yönü, bakış açısı ve artan sıra taşır. RPC yalnız
oyuncunun sahibinden kabul edilir. Host yönü sınırlar, eski/bozuk paketi reddeder,
0,25 saniye girdi gelmezse hareketi durdurur ve fiziği kendi saatinde hesaplar.
Kamera yereldir; konumlar NGO NetworkTransform ile çoğaltılır. P1'de istemci
tahmini yoktur; yüksek gecikmede kontrol hissi ayrıca değerlendirilecek.

Doğrudan UDP varsayılan portu `7777`dir. Aynı bilgisayarda `127.0.0.1`, LAN'da
host'un yerel IP'si kullanılır. İnternette host'a ulaşılabilen adres ve UDP port
yönlendirmesi gerekir; CGNAT bunu engelleyebilir. Router/firewall ayarı otomatik
değiştirilmedi, Relay hesabı veya ücretli servis açılmadı. IP adreslerini public
issue'lara yazmayın. Bu arkadaş co-op bağlantısı hesap doğrulaması veya şifreli
oyuncu kimliği sağlamaz.

Mehmet'in netcode incelemesinde göstereceği yollar: `NetworkSession.Approve`,
`NetworkPlayer.MoveRpc/FixedUpdate`, `NetworkSession.Disconnected/Stopped`.
Loglarda `P1_APPROVAL`, `P1_CONNECTED`, `P1_SCENE`, `P1_STOPPED` takip edilir.
İki farklı bilgisayarda internet ve netcode yedeğinin tekrarı P1 faz kapısında kalır.

API kaynakları: [Unity connection approval](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/basics/connection-approval.html),
[Unity network scene loading](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/basics/scenemanagement/using-networkscenemanager.html).
