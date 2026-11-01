# P4.11 / #218 — Beş türün veri ve ekonomi zemini

1 Kasım 2026. R05 çalışma tablosunu kaynak olarak kullanarak iki yeni `SpeciesDefinition` varlığı oluşturuldu: `grouper` (Orfoz, 2–6 kg) ve `lanternfish` (Fener balığı, 0,3–0,9 kg). Üç mevcut kimlik (`sea_bass`, `tuna`, `red_snapper`) değiştirilmedi.

## Oyun kuralları

`EconomyManager` varsayılan av satış fiyatı yalnız `SpeciesId` üzerinden hesaplar. Üç mevcut av için 120 kredi korunur; yeni orfoz 180, fener balığı 320 kredi. Su derinliğinden otomatik fiyat çarpanı **yok**. İki yeni tür için çekim ödül kalitesi, mevcut standart 25/50/100/200 kademelerine bağlıdır; bu kademeler `SetRecordingReward` ile değiştirildiğinde iki tür de güncellenir.

Yeni tanımlar: orfoz maxHealth=4, swimSpeed=1.0, fleeSpeed=2.8, fleeRadius=3, wanderRadius=5; fener balığı maxHealth=2, swimSpeed=0.9, fleeSpeed=2.6, fleeRadius=4, wanderRadius=4. Bu değerler oynanabilir denge testi bekleyen başlangıç değerleridir.

## İspat kapsamı

`P411FiveSpeciesDefinitionTests` ile beş ayrı, geçerli, kararlı ID, yeni türlerin gram aralığı ve mevcut/eski tür fiyatlarının ayrılığı sınanır. Unity CI sonucuna göre PASS ayrıca değerlendirilmelidir.

**Henüz tamamlanmadı:** Yeni iki türün FBX/animasyon görseli, prefablar ve CREDITS; yeni 2 türün sahnede gerçekte spawn/hareket/kaçış/av/satış/çekim/ansiklopedi uçtan uca bağlantısı; levreğe orfozdan farklı model verilmesi; 1/2/4 gerçek süreç görünürlük ve nihai #164 kabulü. Kullanıcının inceleme bekleme istememesi veri hazırlığını öne alır; doğrulanmamış oynanışı PASS ilan etmez.

Refs #218 #155 #163.
