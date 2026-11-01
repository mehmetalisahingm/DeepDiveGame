# P4.11 / #219 — İlk resif/derin su görsel yerleşimi (1 Kasım 2026)

Bu PR, Mert'in birleşmiş #217 `ReefKit` prefablarını, mevcut `DiveTestArea` içindeki önceden author edilmiş resif rafı ve derin havzaya *görsel olarak* yerleştirir. Utku'nun #219 işi için ilk izole katkıdır; tam kabul değildir.

## Ne değişti
- `ReefHabitatVisuals` ayrı bir runtime sahne-yükleme dinleyicisi olarak eklendi; ortak sahne YAML, zemin, mağaza, ev ve tekne rotalarına dokunmaz.
- Altı dekor öğesi resif rafı üzerinde (y=-8; z=19..40), dört öğe derin havza üzerinde (y=-26; z=46..65) sabit konumlarla üretilir.
- İlk yerleşim resifin batısında (kırmızı mercan balıklarının doğu spawn sırasından uzak), derin kesimin doğusunda (tonların batı sırasından uzak), duvar ve araştırma teknesi merkezinden uzaktadır.
- Yeniden sahne yüklenince tekrar üretilir; aynı sahnede ikinci `Build` çağrısı duplicate oluşturmaz.
- Başlamadan önce tüm kaynak prefablar incelenir: eksik model veya collider/NetworkObject varsa kısmi yerleşim yapılmaz.
- Balık türü, spawn, hareket, çarpışma, rota, görüş, ödül, kayıt ve ağ otoritesi değiştirilmez.

## Kanıt ve sınırlar
- `ReefHabitatVisualsTests` 2 yeni EditMode kontrolü: prefab kimliği, collider/NetworkObject yokluğu, adet, bölge ve taban yükseklikleri.
- CI henüz yeniden çalışmadan PASS iddia edilmez.
- Gerçek Windows oyununda gün/gece kadraj, dalgıç güzergâhı ve av erişimi; 1/2/4 süreçte aynı görüntü; kare süresi etkisi **henüz ölçülmedi**.
- Resifin mevcut basamaklı tabanına yeni doğal eğimli mesh eklenmedi. Bu ilk görsel yerleşim #219'un tüm kabul maddelerini karşılamaz.
- Utku ve Mert'in yapacağı stil/konum ve engel ölçümlerinden sonra gerçek sahne yerleşimi revize edilecektir. #164 nihai kabul açık tutulur.
