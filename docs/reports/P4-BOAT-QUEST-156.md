# #156: sandal görevi 2/3 — teşhis ve yönlendirme

Tarih: 2026-10-13. Taban: `codex/p4-integration` @ `7af7a3f`. Dal: `p4/mert-boat-quest`.

Kullanıcı üç kişi oynarken 2/3'e ulaştı, üçüncü parçayı bulamadı. **Hangi parçanın eksik kaldığı bilinmiyor; sorun yeniden üretilmedi.** Aşağıdaki, kodda ve gerçek build'de bulunan olası nedenler ve yapılan düzeltmedir, kesinleşmiş bir "kök neden" değildir.

## Teşhis
1. **Fiziksel erişim (doğrulandı, sorun değil).** Yeni EditMode testi (`BoatPartReachabilityTests`) gerçek `DiveTestArea` sahnesine `ContinuousTownLayout`'u kurar ve sığlık rampasından (yüzücünün karaya çıktığı tek yer) 0,5 m ızgarada kapsül yürütür: üç parçaya da 1,5 m (alma menzili) içinden ulaşılır ve hiçbir parça katı geometrinin içinde değildir. Aynı test **#151/#152 dalında (be8a082) da** geçti; bu PR'lar birleşince de regresyon korumasıdır. Sınır: ızgara yaklaşıklığı, kapsül yükseltmesi 0,5 m (ayak bileği seviyesi basamaklar); gerçek karakter denetleyicisi değildir.
2. **Gerçek akış çalışıyor.** `-Players 2 -Boat`: gerçek E ile 0/3→3/3 (host gövde+yakıt, guest motor), aynı parça ikinci kez toplanamaz, otomatik tamamlama yok.
3. **Sessiz başarısızlık adayları (kodda):**
   - Ücretsiz parça yalnız **dalış aşamasında** toplanır (`NetworkPlayer.HandlePickup`: `session.DiveActive` değilse `InvalidState`). Yeni kasabada Prep/Return'de sahilde dolaşan oyuncu parçayı görür, E basar ve **geri bildirim almaz**. Bu kapı Mehmet'indir; değiştirmedim (aşağıda karar).
   - Görev arayüzü yalnız `SANDAL ONARIM: n/3` gösteriyordu: eksik parçanın adı ve yeri yoktu.
   - Motor, kısmen suyun altındaki sığlık rampasında ve 0,6 m'lik küçük bir yer tutucu; üç kişi iki kıyı ucunu bulup rampadakini kaçırmış olabilir.

## Yapılan
- `Core/P4BoatGuide.cs` (`BoatRepairGuide`): eksik parçaları host'un aynaladığı bit maskesinden çıkarır, her parçanın adını (Govde / Motor / Yakit tanki) ve oyuncudan pusula + metre yönünü verir. Saf metin/geometri, kural yok.
- `Composition/BoatRepairHintView`: sandal onarılana kadar `SANDAL: EKSIK PARCALAR` panelini gösterir (gerçek Canvas; "Govde: bati, 3 m", "Motor: kuzeydogu, 5 m", …) ve aşamaya göre not: dalışta "E: parcayi topla", dışında "Parcalar dalista E ile toplanir; dukkandan da alinir". Mevcut `SANDAL ONARIM n/3` kutusu ve kurallar aynı. Parça konumları Utku'nun sahne anchor'larından okunur; taşınırsa ipucu onunla gider.
- Kayıp yok: 2/3'te kaydedilmiş kampanya (gerçek `EconomySaveStore` dosyası, her üç kombinasyon) yeniden açılınca 2/3'te kalır, eksik parça **bulunabilir ya da dükkândan (120) alınabilir**, iki oyuncu aynı anda alırsa tek katkı (`AlreadyProcessed`), ücretsiz parça para düşmez, onarım bir sonraki yeniden açılışta korunur. Kampanya sıfırlaması gerekmez.

## Kanıt
- EditMode tam suite **1165/1165**; bu işin 10 yeni testi: `BoatPartReachabilityTests` (2: parçalar katı geometride gömülü değil, sığlıktan yürünür) ve `BoatQuestGuideTests` (8: eksik parça listesi, maske bitleri, pusula, satır metni, 2/3 kayıttan devam x3 parça, dükkân yolu).
- Gerçek 2-süreç Windows `-Players 2 -Boat -Capture` (host + guest PASS): rehber panelinin satırları 0/3'te üç parçayı, kısmi sayıda yalnız eksikleri sayar, 3/3'te kaybolur; üç durum iki süreçte de gözlendi. Ekran görüntüsü gerçek build'den ([`p4-boathint.png`](media/p4-boathint.png)): "Govde: bati, 3 m / Motor: kuzeydogu, 5 m / Yakit tanki: dogu, 22 m / E: parcayi topla".
- Aynı build'de regresyon: `-Trip`, `-Town`, `-Boat` host + guest PASS.

## Karar / açık
- **Tasarım kararı (kullanıcı, 13 Ekim):** ücretsiz parçalar Prep/Return aşamasında da fiziksel yakınlık ve E etkileşimiyle toplanabilsin; sahilde gördüğü parçayı almak için dalış başlatmak gerekmesin. Host doğrulaması, tek katkı ve kayıt kuralları korunur. Bu etkileşim kapısını (`NetworkPlayer.HandlePickup` / `DiveActive`) **Mehmet #157 kapsamında** üstlenecek; bu PR'da değiştirilmedi. O değişiklik girince `BoatRepairGuide.OutsideDiveNote` ("dalista E ile toplanir") yanlış olur ve kaldırılmalı/güncellenmeli.
- **Utku:** parçaların fiziksel ipucu (görünür işaret/ışık, daha büyük model) ve dağılımı onun; motorun suyun altındaki konumu gözden geçirilebilir.
- Hangi parçanın eksik kaldığı bilinmiyor (kullanıcı da bilmiyor): **kesin neden bulundu denmiyor**. Panel ve 2/3 kayıt testleri sorunu çözmeye yönelik kanıttır, yeniden üretim değildir. 3 kişilik/ayrı bilgisayar oturumu yapılmadı (2 süreç).
- Yeni panel IMGUI kutu yığınının altında (y=306) ve dev oturum panelini örter; asıl HUD yerleşimi #154'e bırakıldı.
