# P4.6 — farklı Windows bilgisayarlarda doğrulama kılavuzu

**Durum:** Araç teslimi. Bu belgenin eklenmesi ayrı-PC testi yapıldığı, dört insanın oynadığı veya P4.6'nın kapandığı anlamına **gelmez**. #138/#140/#141/#157 ve #173 kabul açık.

## Tek build, dört gerçek bilgisayar — otomatik fiziksel rota

Bu araç **her bilgisayarda bir gerçek Unity Windows oyuncu süreci** çalıştırır. Geliştirme komutuyla gerçek hareket RPC'lerini gönderir; hileyle teleport/başlangıç parası eklemez. Oyuncu klavyesini yapay sürücü kontrol ettiği için **manuel insan oynanışı yerine geçmez**. Aynı LAN/VPN üstünde erişilebilir host IPv4 adresi kullanılır, host'un Unity Transport UDP portu erişilebilir olmalıdır. Tüm makineler aynı `DeepDive-P4-2` build/protokolünü ve commit'i kullanır; exe + kritik DLL SHA-256 manifestleri karşılaştırılır.

Oyunu önce bütün bilgisayarlara **aynı eksiksiz `Builds/P1-Integrated/` klasörü** ile kur. Her makinede PowerShell'i proje kökünden aç. **Host'u başlat, hemen ardından üç farklı bilgisayardan client'ları başlat**. Aynı `RunId`, `Players`, `Port` kullan; her makinede `Slot` farklıdır.

Host bilgisayarda:

```powershell
./tools/Test-P46-DistributedWorld.ps1 -Players 4 -Slot 0 -Port 27677 -RunId okt16-p46
```

Diğer üç bilgisayarda (host'un gerçek erişilebilir IP adresini yerleştir):

```powershell
./tools/Test-P46-DistributedWorld.ps1 -Players 4 -Slot 1 -Port 27677 -HostIp <HOST_LAN_IP> -RunId okt16-p46
./tools/Test-P46-DistributedWorld.ps1 -Players 4 -Slot 2 -Port 27677 -HostIp <HOST_LAN_IP> -RunId okt16-p46
./tools/Test-P46-DistributedWorld.ps1 -Players 4 -Slot 3 -Port 27677 -HostIp <HOST_LAN_IP> -RunId okt16-p46
```

Bu komutlar **bir bilgisayarda topluca değil**, her biri ayrı bilgisayarda çalıştırılır. Windows varsayılan build yolu `Builds/P1-Integrated/DeepDiveGame-P1.exe`; özel konumda `-BuildPath` kullan. İkinci bilgisayara erişmeden bu kabul `PASS` sayılamaz.

Her makinede `Logs/P46-distributed-<run-id>-slotN/` klasörü, ham oyun `playerN.json`, Unity `.log` ve `machine-evidence.json` dosyalarını üretir. Bunları bir değerlendirme bilgisayarına ayrı klasörler olarak kopyala, ardından:

```powershell
./tools/Verify-P46-DistributedWorld.ps1 -EvidenceDirs @(
  './evidence/slot0','./evidence/slot1','./evidence/slot2','./evidence/slot3'
)
```

Doğrulayıcı dört **farklı makine adı**, 0–3 tekil slot, aynı `RunId`, UDP portu, eksiksiz aynı EXE/DLL SHA-256, her oyuncuda gerçek 120m+ yürüyüş, `worldLoads=1`, dört oyuncu ağ aynası, evden PC/yatak, gün 2 ve sıfır hata bekler. MachineName yalnız rapor ayırt etmek içindir; bağımsız üçüncü taraf kimlik ispatı değildir.

## Tam insan ürün/ekip kabulü — otomatik testten bağımsız

Utku koordinatörlüğünde Mehmet (hareket/ağ), Mert (görsel/kayıt) ve dördüncü oyuncu **gerçek klavye/fareyle** aynı Windows sürümünde birlikte oynamalı. Aşağıdaki her satır ayrı kanıt ister:

1. Dört karakter başlangıç evi → yatak geçişi → şehir/dört hizmet noktası → kıyı/rampa → kuru 2m iskele → tekneye tek tek binme ve geri çıkma; gerçek input ve 1./3. kişi görüntüsü
2. Av/iki gerçek satış → kazanılmış kamera/clip → PC'de oynatma/yayın → tek gelir, kayıt ve network oyun gününün 1→2→3→4 geçişi; üç **ardışık oyun günü**
3. Ölüm/kayıp dalgıç; ayrıca 00:00 otomatik kapanış, aktif sefer/dalış sırasında host kapanması, istemci kopma/yeniden bağlanma (başarısızlıklar da açık raporlanır)
4. Her bilgisayardan Windows build manifesti, bağlantı/host portu, Unity ve ilgili video/frame-time logları; oyun yeniden açıldığında para/tekne/gün/klip/yayın çoğalmamış olmalı
5. Mert'in #171 grafik/rig kiti + Utku'nun #172 gerçek örnek alanı tesliminden **sonra**, Mehmet #173: near clipping, tutuş, karakter yüksekliği, gündüz/gece ve FPS; kullanıcı gerçek kalite değerlendirmesi

Sonuçlar #141 GitHub issue'sunda commit, build, dört makine, dört ayrı gerçek oyuncu kanıtı, üç gün/ölüm/00:00/host kaybı satırlarıyla ve üç ekip üyesinin kendi gerçek incelemesiyle kaydedilir. Şimdiye kadarki 1/2/4 **aynı bilgisayar** otomasyonu bunun yerine kullanılamaz.

## CI otomasyonu ile fark

#179 Linux runner içinde gerçek 1/2/4 ayrı oyun **süreçleri**, fiziksel ev-şehir-kıyı dönüşü, disk reload ve üç günlük *aday* koşularını içerir. Bu bir CI makinesidir. Bu dokümandaki dağıtık Windows aracı farklı makinelerdeki gerçek ağ gözlemini toplayabilir, ama insan oynanışı, gerçek WAN gecikmesi, üç gün ve host kaybı testini kendi başına yapmaz.

Kaynak durum: [#170 ürün akışı](P4-PRODUCT-DAY-170.md), [#157 dünya turu](P4-CONTINUOUS-SESSION-157.md), [#173 kamera/ölçek](P4-PLAYER-SCALE-173.md).
