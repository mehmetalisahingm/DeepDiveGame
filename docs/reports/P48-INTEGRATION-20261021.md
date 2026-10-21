# P4.8 — kuru kızak, kıyı erişimi ve tamir entegrasyonu

21 Ekim 2026. Kod kaynağı `bf31302493284741b7bd89a31a3e1b8bb63427e5`, Unity `6000.3.23f1`. Bu rapor teknik adayın gerçekten koşulmuş Windows kanıtıdır; ayrı bilgisayar/insan/nihai sanat kabulü veya P4 kapanışı değildir. [Ham sonuçlar ve DLL SHA-256 manifesti](evidence/p48-integration-20261021/manifest.json).

## Birleştirilen davranış

#190 fiziksel taşı/yerleştir/üç darbe, #194 kuru oturak malzemesi, #195 kıyı plakası altı sıkışma ve balık yerleşimi, #196 S1 plaj kızağı aynı kaynakta toplandı. S1 `(3.6, 8.4, -12.6)` kırık sandalın sunumudur; onarılan aynı `boat-1` mevcut wp0, dört koltuk ve rota otoritesini kullanır. Ortak kullanılmamış malzeme askıları gerçek `repair-drop-0` çevresine bağlandı; kuru engine başlangıcıyla karışan eski askı ofseti kullanılmaz. Görev ipucu plaj kızağını ve ortak askıyı belirtir. R01/R02/R11 alan kararları mevcut #186 yorumlarına dayalıdır; yeni ekip onayı üretilmedi.

İki açık review bulgusu düzeltildi: edinme/final retleri aynı istek ve hedef/kaynakla terminal kalır; bakiye veya malzeme değişince eski ret yeni harcamaya dönüşmez. SaveFailed aynı payload ile tekrar denenebilir. Tamir yerleştirme/alma, balık toplama onayı yerine Repair geri bildirimi kullanır. Bu davranışlara dört yeni EditMode testi eklendi.

## Gerçek doğrulama

| Kontrol | Sonuç ve kapsam |
|---|---|
| Tam EditMode | **1339/1339 PASS**. İlk birleşimde eski `Motor` metni ve taşınmış parçayı eski audit konumuyla karşılaştıran iki test başarısızdı. Metin seçilmiş oturak adıyla güncellendi; bütün alan ölçümü `P48RepairAreaAudit.Run` ile yeniden üretildi. Son XML sıfır failed. Test atlanmadı. |
| Windows build | `tools/Build-P1.ps1 -Integrated` başarıyla tamamlandı. Runtime koşuları bu DLL'lerle yapıldı; manifest modül hash'lerini içerir. |
| Dört gerçek süreç, fiziksel tamir | `Test-P46-ProductDay.ps1 -Players 4 -Port 25127 -RepairOnly`: **4/4 PASS**. Üç ayrı taşıyıcı al/bırak/askıdan geri al/yerleştir/darbe yaptı; dördüncü süreç gözlemci. Kalıcı kayıt v9, üç completed, sıfır unused. Aynı-parça yarışı bu senaryo değildir. |
| İki süreç, tam ürün günü | `Test-P46-ProductDay.ps1 -Players 2 -Port 25107`: **host + guest PASS**. Üç dalış, iki gerçek avın satışı, kamera alımı, gerçek clip/oynatma, fiziksel tamir, tekne/harita, ev PC yayını, uyku ve gün 2. Yeni host sürecinde kayıt/clip playback ve duplicate publish reddi PASS. Gelir 60. |
| İki süreç, tamir sonrası gerçek sefer | `Test-P1-Integrated.ps1 -Players 2 -Port 25147 -Trip -Capture`: **2/2 PASS**. Katkı 0→1→2→3, tamamlanmış yuvaya tekrar ret, hint kapanması, iskelede binme ve Docked→Outbound→Anchored→Inbound→Docked. Guest inme/yeniden binme ve gerçek save/load doğrulaması dahil. |
| Dört süreç, integrated Boat | `Test-P1-Integrated.ps1 -Players 4 -Port 25167 -Boat -Capture`: **4/4 PASS**. Dört süreç tamir sonucunu gördü; ek bağlantı `RoomFull`, yanlış zamanda bağlantı `WrongPhase` ile reddedildi; client1 yeniden bağlandı. Tamir ortasında kopma kabulü değildir. |

Önceki ayrı #194 koşusunda solo ürün günü PASS, co-op ilk parça yaklaşımı timeout idi. Ayrı #195 koşusunda solo PASS, co-op kayıt başlatmadan hareketli hedefi takip ederken timeout idi. Birleşik sürücü kuru parçayı su rotasına sokmaz, kızağın katı gövdesini dolaşır ve temel kameranın geçerli bandında 10 m içinden gerçek kaydı başlatmayı dener. Oyun balığı, kayıt kalite/ödül eşiği, satış veya hak koşulları gevşetilmedi. Yukarıdaki birleşik iki süreçli tam gün bu akışı geçti.

## Görsel kanıt ve kalan sınır

![Gerçek Windows build'de kuru plaj kızağında ilk darbe; geçici mekanik model](evidence/p48-integration-20261021/dry-cradle-prototype.png)

Görüntü gerçek build'den alınmıştır. Gövde/tahta/keser/çivi ve kısa vuruş sesi mekanik prototiptir; #189 son model/rig/ses veya kullanıcı görsel kabulü değildir. Yeni su/mercan/şehir kalite işleri #193 planıdır; bu tamir entegrasyonu onları uygulamaz.

Özel dört süreçli aynı-parça yarışı, tamir sırasında guest kopması/00:00, yerel ve arkadaş kamerasından video, iki bilgisayar/dört insan/üç gün kabulü açık kalır. Oturuma giriş mevcut Lobby kapısını korur; Dive'a geç katılma kabulü üretilmez. #141/#159/#187/#188/#189'un bütün kabulü bu rapordan türetilmez. GitHub CI, PR #190'ın aynı kod kaynağına bağlı ayrı koşularından izlenir; bu rapor yazılırken henüz bitmemiş CI PASS sayılmadı.
