# #173 — Mevcut oyuncu/kamera ölçek hazırlığı

16 Ekim 2026. Bu rapor yeni sanat/rig uygulaması veya P4.7 kabulü değildir. #173'ün izin verilen mevcut sahne/prefab ölçüm hazırlığıdır. Teknik temel #138/#140/#141 kabulü ve Mert/Utku örnek kiti henüz kapanmadığı için yeni kit kadrajı ve kullanıcı görsel kabulü bekler.

## Tekrar üretim

Unity 6000.3.23f1 batch mode, `DeepDive.Editor.P4PlayerScaleAudit.Run`. Araç `NetworkDiver.prefab` ile gerçek `DiveTestArea` sahnesini okur; mevcut deterministik ev/dükkân bloklarını ek sahnede kurup ölçer, sahneyi kaydetmeden kapatır. Oyuncu sahnesi/prefabı/rig'i değiştirilmez. [Ham ölçüm](evidence/p173-scale/current-prefab.json), yerel çıktı `Logs/P173-player-scale.json`.

## Ölçülen başlangıç

| Nesne/ölçü | Sonuç | Kullanımı |
|---|---|---|
| CharacterController boyu / yarıçapı | 1,80 / 0,35 m | İnsan ve varlık ölçeği için mevcut teknik beden |
| Skin / etkili tek kişi genişliği | 0,08 / 0,86 m | Dar geçitte collider güvenlik payı dahil |
| İki / dört kişi yan yana teknik genişlik | 1,72 / 3,44 m | Trafik tasarımı için ölçü; dört kişinin her kapıdan yan yana geçmesi şartı değildir |
| Step offset / slope limit | 0,30 m / 45° | Merdiven/rampa ve kit collider sınırları |
| Kara yürüme hızı | 4 m/s | Doğrudan yol, alışveriş turu ve gezinti süreleri ayrı ölçülmeli |
| Göz yüksekliği / FOV / near clip | 1,55 m / 60° / 0,05 m | Yakın el/nesne ve tezgâh kadrajı başlangıcı |
| Ev açık cephe genişliği | 11,65 m | Şu an collider'lı gerçek bir kapı modeli yok; bunu kapı kabulü sayma |
| En yakın spawn–yatak çıkışı / spawn–spawn | 0,807 / 1,25 m | Mevcut kapsül ve yatak yerleşimi başlangıcı; yeni kitte tekrar kontrol edilir |
| İskele genişlik / boy | 2 / 3,5 m | İki sıra/ardışık trafik; dört kişiyi yan yana iskele dışına taşırma |
| Dükkân tezgâh gövdesi | 1,80 × 1,00 × 0,50 m | Kamera göz seviyesine göre gerçek hizmet hedefini kapatmamalı |
| Tezgâh üstü | 1,95 × 0,06 × 0,64 m; üst 1,06 m | Mert'in tezgâh/nesne/elin temas yüksekliği başlangıcı |

0,807 m spawn–yatak çıkışı mesafesi fiziksel 0,70 m kapsül çapından büyüktür; iki skin dahil 0,86 m'den küçüktür. Bu nedenle yalnız sayısal mesafeye dayanarak yeni kitte dört kişi trafik kabulü verilmez. Mevcut gerçek süreç turları ve yeni örnekte tekrarlanacak yerel/uzak kamera görüntüsü birlikte değerlendirilir.

## Bu teslimde yapılabilen ve bekleyen

Mevcut prefab/kamera/kapsül/tezgâh/iskele ve ev açıklığı ölçüldü; tekrarlanabilir read-only audit aracı hazırlandı. Günlük döngü testi gerçek input ile dünya/tezgâh/iskele/yatak/PC yollarını kullanır; teknik hareket kanıtı görsel kit kabulü değildir.

Mert'in #171 örnek rig/nesne/malzeme/ses kiti ve Utku'nun #172 kısa örnek alanı gelince: aynı kit üzerinde tutuş/near clipping, yerel ve arkadaş bakışı videosu, gündüz/gece hedef okunurluğu, kamera rahatsızlığı ve kare süresi ölçümü yapılır; kullanıcının hedef kalite değerlendirmesi #159'a bağlanır. Tahta/alet için taşıma veya contribution sistemi bu ölçüm aracıyla eklenmez; #160 sınırı korunur. #173 açık kalır, başkaları adına kabul yazılmaz.

## Otomatik tekrar kontrolü (Mehmet)

Yeni `Assets/Tests/P4/Editor/P4PlayerScaleBaselineTests.cs` dosyasında iki EditMode regresyonu eklendi:

1. Var olan `NetworkDiver` karakter kapsülü, perspektif kamera göz yüksekliği, FOV ve near-clip temel sınırları oyun prefab'ından okunur. Kamera/rig değişirse yeni görüntü kabulü gerekmesi görünür hale gelir.
2. Gerçek sahne/kasaba yerleşiminde dört ev spawn'ı için fiziksel kapsül çapı çakışması ve 2 m iskelede sırayla geçiş geometrisi kontrol edilir. İskele **dört kişinin yan yana durması için yeterli diye onaylanmaz**.

Bunlar CI tamamlanmadan PASS diye işaretlenmez. Yakın nesne/ellerin görüntüde kırpılması, yeni Mert rig'i ve Utku yerleşimi, 1/2/4 gerçek oyuncu kadrajı ile kullanıcı sanat kabulü için yine gerçek Windows görüntüsü gerekir. P4.7 uygulaması bu testle açılmaz.
