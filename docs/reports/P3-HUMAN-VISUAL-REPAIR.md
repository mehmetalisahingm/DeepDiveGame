# P3 insan ve ekipman görünümü — 23 Eylül 2026

## Değişiklik

Ortak build artık geliştiricinin özel Mixamo indirmesine bağlı değil. Quaternius CC0 insan modeli, Humanoid idle/walk klipleri ve gerçek rig'den ayrılmış birinci şahıs kolları kaynaklarıyla depoda. Kamera iki tutacak, lens ve arka ekranla yeniden oluşturuldu; eller ve uzak oyuncunun kamerayı tuttuğu poz bağlandı. FBX kemiklerindeki ölçek ekipmana taşınmıyor. Hareket otoritesi ve kök hareket kuralları değişmedi.

Sualtında gövde yatay dönüyor; kol/bacak hareketi kodla üretiliyor. Bu, özel yüzme klibi veya son animasyon kalitesi değildir. Kıyı ve sualtı ışık/sis renkleri gerçek yerel kameranın yüksekliğine göre ayrılıyor. Boş elde birinci şahıs kolları ekranı kapatmıyor.

Sandal parçasının tamamlanması host'tan bit maskesiyle yayınlanır. Alınan parçanın görseli ve pickup collider'ı bütün oyuncularda kapanır. Güvenli dönüş ayrıca oyuncunun o dalışta suya girmiş ve kuru karaya ulaşmış olmasını denetler. Protokol `DeepDive-P3-6`; eski build ile karışık oturum desteklenmez.

## Birleşim ve etkilenen alanlar

- Temel: `codex/p3-integration` / `9ca6b07`. #70/#71/#74'ün kıyı, pickup ve gerçek yüzerek dönüş düzeltmeleri korunur; #77 tekne yolculuğu altyapısı dahil.
- Mehmet: oyuncu sunumu, eller, ekipman ve görsel doğrulama.
- Mert alanına bağlantı: ortak karakter/kamera kataloğu ve `EconomyPlayerSync.BoatPartsMask`.
- Utku alanına bağlantı: DiveTestArea'ya yalnız sunum bileşenleri; rota, su hacmi ve ekibin düzelttiği rampa geometrisi değiştirilmedi.
- Bu kayıt diğer kişilerin sanat/kabul onayı değildir. Kaynak/lisans: [CREDITS](../assets/CREDITS.md).

## Doğrulama

Unity `6000.3.23f1`, Windows çıktısı: `Builds/P1-Integrated/DeepDiveGame-P1.exe`. Son çalışma ağacı üzerinde:

| Kontrol | Sonuç | Yerel kanıt |
|---|---|---|
| Bütün EditMode testleri; kaynak rig, gerçek kol mesh'i, URP materyali ve ekipman ölçeği dahil | **579/579 PASS** | `Logs/P3-human-final-tests.xml` |
| Entegre Windows build | **PASS** | `Logs/P1-build.log`, `P1_BUILD_SUCCEEDED` |
| İki süreç: kayıt → gerçek yüzerek kuru dönüş → NPC ödemesi; reconnect | **2/2 PASS** | `Logs/P1-integrated-20260923-233147-696-2/` |
| İki süreç: üç ücretsiz parçayı E ile toplama, ikinci alma reddi, Broken→Repaired; görsel/collider iki tarafta kapanır | **2/2 PASS** | `Logs/P1-integrated-20260923-233147-232-2/` |
| Dört süreç: yürüyüş/yüzüş, kamera sahipliği, reconnect, hazır sıfırlama; beşinci ve geç katılma reddi | **6/6 süreç PASS** | `Logs/P1-integrated-20260923-233148-094-4/` |

Parça testinin ilk turu, alınmış parçanın collider'ı artık kapandığı için `Rejected` yerine doğru `InvalidTarget` sonucu geldiğinde durdu. Test, tamamlanmış parça biti + kapalı collider + ikinci ilerleme olmaması koşullarıyla güncellendi; son turda her iki süreç üç parçanın gizlendiğini ayrıca doğruladı. Önceki başarısız tur son PASS'e dahil edilmez.

## Görsel kanıt

`DeepDive.Editor.P3VisualPreview.Capture` editör içinde gerçek katalog/rig/ekipmanla insan, eller ve yüzme pozu görüntüsü üretir. Bunlar sanat önizlemesidir. `Test-P1-Integrated.ps1 -Capture` ayrıca çalışan Windows oyuncusunun Lobby/Dive/Return ekranlarını kaydeder; gerçek çok oyunculu akış kanıtı bu smoke raporlarıdır.

![Katalogdan insan ve elde kamera önizlemesi](media/p3-human.png)

![Gerçek Windows iki oyunculu dalış görüntüsü](media/p3-runtime.png)

## Açık sınırlar

Bu teslim P3 veya görsel kabul kapanışı değildir. Kasaba/NPC/deniz çevresi hâlâ temel geometri içeriyor; çanta/palet, kayıt lambası, zıpkın sanatı ve doğal yüzme animasyonu ayrıca geliştirilmelidir. Ortak ev, gün ve izlenebilir video P4'te kilitli kalır. Guest ekipmanının yeniden açmada emanete korunması bu değişiklikte uygulanmadı. Tekne koltuk/hareket ve canlı haritanın gerçek sahne bağlantısı P3.3 işidir. İki ayrı bilgisayar/internet ve ekibin görsel oynama kabulü burada yapılmış sayılmaz.
