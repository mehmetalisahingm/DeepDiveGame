# P1 ortak build

Mehmet'in ağ/oyuncu kodu, Mert'in oda ve oturum ekranı, Utku'nun `PrepArea` ve
`DiveTestArea` sahneleri aynı oyun akışına bağlandı. P1 kapandı; aktif faz P2. [Kapanış ve kalan kontroller](reports/P1-REPORT.md).

## Güncel projeyi al

GitHub'da `codex/p2-integration` dalını kullanın. `main` birleşmiş P1 temelidir.
Yerel değişikliklerinizi koruyarak:

```powershell
git fetch origin
git switch codex/p2-integration
git pull --ff-only
```

Unity `6000.3.23f1` ile `Assets/DeepDive/World/Scenes/PrepArea.unity` sahnesini açıp
Play'e basın. Windows build almak için Unity Editor kapalıyken:

```powershell
.\tools\Build-P1.ps1 -Integrated
```

Çıktı: `Builds/P1-Integrated/DeepDiveGame-P1.exe`. Arkadaşa oynanabilir paket verirken
yalnız exe'yi değil, **P1-Integrated klasörünün tamamını** ZIP yapın. Kaynak geliştirme
GitHub'dan devam eder; `Builds`, `Library` ve `Logs` commit edilmez.

## Birlikte oyna

1. Host portu seçip **Oda oluştur / Solo** düğmesine basar. Solo için aynı akış kullanılır.
2. Diğerleri host IP adresini ve aynı portu yazıp **IP ile katıl** düğmesine basar.
   Aynı bilgisayarda `127.0.0.1`; aynı ağda host'un yerel IP adresi kullanılır.
3. Herkes **Hazır** olur. Host **Hazırlığa başla**, ardından **Dalışa başla** der.
4. Host **Dönüşü başla** ve **Odaya dön** ile turu bitirir. Hazır bilgileri sıfırlanır.
5. **Odadan ayrıl** bağlantıyı temizler. Host ayrılırsa diğerleri de oda ekranına döner.

`F1`: fareyi oyuna al ve ekranı gizle. `Esc`: ekranı aç. `WASD`: hareket.
Fare: bakış. Suda `Space` / `Sol Ctrl`: yukarı / aşağı. Host, hazırlık alanındaki
giriş veya sualtındaki çıkış hacminin içindeyken `E` ile ilgili geçişi de başlatabilir.
Aşama düğmeleri yalnız host'ta, hazır düğmesi her oyuncunun kendi hesabında çalışır.

En fazla dört oyuncu alınır. Dalışta yeni katılım kapalıdır. Eski P1-A laboratuvar
build'i bu ortak build ile uyumlu değildir (`DeepDive-P1-2` protokolü); herkes aynı build'i kullanmalıdır.

## Test sınırı

```powershell
.\tools\Build-P1.ps1 -Tests
.\tools\Test-P1-Integrated.ps1 -Players 1 -Port 18771
.\tools\Test-P1-Integrated.ps1 -Players 2 -Port 18772
.\tools\Test-P1-Integrated.ps1 -Players 4 -Port 18774
```

Bu komutlar **tek bilgisayarda** ayrı oyun süreçleri açar. Gerçek ekran düğmeleri,
hazır durumu, host yetkisi, yürüme/yüzme, sahne geçişi, dönüşte hazır sıfırlama,
katılımcının ayrılıp yeniden katılması ve host'un ayrılması denenir. Dört oyuncu
testinde beşinci oyuncu ve dalış sırasında gelen bağlantı da reddedilmelidir.

İnternet testi ayrı yapılmalıdır. Bu sürüm doğrudan IP/UDP bağlantısı kullanır;
Relay/oda kodu yoktur. Host'un seçtiği UDP portuna internetten erişilebilmesi gerekir.
Bu teslim modem/firewall ayarı değiştirmez, ücretli servis açmaz.
İki gerçek bilgisayarda aynı build ile 2/4 oyuncu testi ve ekibin gerçek tamam kaydı
olmadan P1 kapanmaz. Test sonucu [entegrasyon raporuna](reports/P1-INTEGRATION-REPORT.md) yazılır.
