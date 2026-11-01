# P4.9 / #220 — Tek NetworkManager üzerinde oda yeniden açılışında kozmetik olay fişleri

1 Kasım 2026. Önceki `PlayerActionPresentationBinding`, anti-replay `PlayerActionReceipt` ve `ServicePresentationReceipt` fişlerini yalnız NetworkManager veya onun mesaj dağıtıcısının referansı değişince sıfırlıyordu. Ancak Unity/NGO üzerinde oda kapatılıp aynı bileşenle yeni oda açıldığında bu referanslar aynı kalabilir; yeni odada obje/istek kimlikleri yeniden başlayabilir. Önceki oturumun eski fişleri, ilk meşru **yakalanan av** veya **balık satışının kasa görselini** sessizce reddedebilirdi.

## Düzeltme
`PlayerActionPresentationBinding.Update` artık önceki ve güncel `IsListening` durumunu takip eder. Aynı manager ile **açık → kapalı → açık** geçişinde fişler ve kozmetik olay sayacı sıfırlanır; manager / dispatcher değişiminde önceki davranış korunur. Normal aktif odada her kare sıfırlama yapılmaz: yinelenmiş eski görsel ve ödeme kozmetiği hâlâ reddedilir. Mesaj dinleyicisi gereksiz yere yeniden kaydedilmez; para, kaydetme, av ve yetki otoritelerine dokunulmaz.

## Test
`PlayerActionSessionBoundaryTests`: 6 durum tablosu; eski odada aynı av/satışın yinelenmesinin reddi ve yeni odada **aynı ID'li ilk gerçek isteğin** tekrar kabulü. Bu testler saf kural ve fiş sınıflarına karşı çalışır; gerçek NGO kapat/aç ve Windows GPU sonuçları için GitHub CI ve nihai #164 kabulü ayrıca gereklidir.

## Sınır
Bu değişiklik yalnızca görsel olayı etkiler. Oyuncunun envanter/satış işlemlerini ya da hareketini değiştirmez. Eşzamanlı istemci kopmaları, görsel kayıtlar ve iki fiziksel bilgisayarın uzun oyun günü denemeleri bu PR'ın tek başına PASS iddiası değildir.

## Oyuncunun yerel HUD'u da sıfırlanır

`NetworkPlayer.OnNetworkDespawn` ve `OnNetworkSpawn` artık yalnız görsel-geri-bildirim alanlarını temizleyen `ResetTransientActionFeedback` fonksiyonunu çağırır: eski pickup/hit metni ve retikül zamanlayıcısı, son gözlenen istek ID'si, son yerel ses/mesaj türü. İstek sayacı `actionSequence` özellikle **sıfırlanmaz**; böylece aktif uygulamadaki RPC istek otoritesi ve tekrar korumasına dokunulmaz. `PlayerFeedbackSessionResetTests` temizliğin idempotent olduğunu ve istek sayacının aynı kaldığını sınar. Bu test yeni bir gerçek host kopma/sahne koşusu değildir; CI ve ayrı bilgisayar son kabulü ayrıca izlenir.
