# P1-A — Mehmet'in bağlantı teslimi

Durum: `codex/p1-mehmet-network` dalında uygulama, Windows build ve yerel testler tamamlandı. Hedef
`codex/p1-integration`; bu kayıt faz kapanışı veya diğer iki kişinin onayı değildir.

[Çalıştırma ve API rehberi](../P1_NETWORK.md). Ayrı bilgisayarlarda internet,
Utku incelemesi ve Mert/Utku'nun gerçek sistemleriyle birleşme bekliyor.

## D03/D08 uygulama sınırı

- NGO `2.7.0`, Unity Transport `2.6.0`; mevcut Unity `6000.3.23f1` korunur.
- Bir host ve en fazla üç katılımcı. Solo aynı host yolunu kullanır.
- İlk bağlantı doğrudan UDP/IP ile. Relay veya ücretli servis açılmaz. İnternet
  testi için host'a ulaşılabilir UDP adresi gerekir; yerel test internet testi sayılmaz.
- Oyuncu yalnız kendi girdisini gönderir; hareketi host simüle eder. İstemci
  konum, hız veya geçen süre belirleyemez. Görüş kamerası oyuncuya aittir.
- `PlayerId.Value`, o oturumdaki NGO client ID'dir; host için `0` geçerlidir.
  Kalıcı kimlik değildir; bağlantı kapanınca eski kimlikler kullanılmaz.
- Yeni katılım yalnız hazırlıkta mümkündür. Yükleme sırasında katılım kapatılır;
  dalış boyunca kapalı kalmasını Mert'in oturum akışı belirler.
- Ayrılan katılımcı temizlenir. Host giderse bağlantı kapanır; host devri ve
  otomatik yeniden bağlanma yoktur. Kalıcı kayıt P1 kapsamında değildir.

## Mert ve Utku için bağlantı noktaları

Mehmet'in `Core.Contracts` arayüzü önerisi bu özellik PR'ında incelenir; ortak
sözleşme ancak entegrasyon incelemesinden sonra kesinleşmiş sayılır.

- Mert, `INetworkSession` üzerinden host/katıl/ayrıl, bağlantı durumu, oyuncu
  listesi ve sahne yükleme sonucunu kullanır. `SessionState`, hazır olma ve ne
  zaman geçileceği Mert'in alanında kalır.
- Mert host'ta `SetJoinAllowed(false)` ile dalışa katılımı kapatır;
  `TryLoadScene(sceneName, requestId)` ile gerçek NGO yüklemesini başlatır.
  Hazırlığa başarılı dönüşten sonra `SetJoinAllowed(true)` çağırır.
- Utku'nun sahnelerine `PlayerSpawnPoint` ve suyun kapladığı hacme
  `SwimVolume` eklenir. Zemin normal collider'dır; global katman değişikliği gerekmez.
- Sahne adı build listesinde bulunmalıdır. Herkes aynı sahne/prefab listesiyle
  build alır. Mehmet'in test sahneleri ve test sürücüsü ayrı test paketindedir;
  Mert'in gerçek ekranının veya Utku'nun gerçek alanının yerine teslim edilmez.

Ortak değişiklik: paket manifesti/kilidi ve yeni P1 bağlantı arayüzleri.
Mevcut P0 sahnesi, render varlıkları ve başkasının alan dosyaları değiştirilmez.
İlgili görevler: [Mehmet #12](https://github.com/mehmetalisahingm/DeepDiveGame/issues/12),
[Utku #13](https://github.com/mehmetalisahingm/DeepDiveGame/issues/13),
[Mert #14](https://github.com/mehmetalisahingm/DeepDiveGame/issues/14).
