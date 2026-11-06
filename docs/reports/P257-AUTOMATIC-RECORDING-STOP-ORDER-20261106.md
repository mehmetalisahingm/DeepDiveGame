# P4 #257 — Çok oyunculu çekimde eski otomatik durdurma bildiriminin yeni çekimi kapatmaması

Sorun ve sınır: ağ mesajları farklı anlarda ulaşabilir. Balık A pickup olduğunda ya da kamera süresi bitince host kayıt A'yı durdurur ve istemciye otomatik `Stop` bildirimi verir. Mevcut kod, hostun bu bildirimi gönderirken hedef kimliğini sıfır gönderiyordu; istemci bildirim geldiği anda yeni balık B'yi kaydetmeye başlamışsa, bildirim koşulsuz yerel HUD'u `localRecording=false` yapabilirdi.

Düzeltme: `StopOnServer`, mevcut mesajdaki `targetId` alanıyla **bitirdiği gerçek NetworkObject** kimliğini gönderir. `ApplyLocalResult`, yalnız otomatik stop bildirimlerinde eşleşme denetimi yapar. A bitişi B çekimini durdurmaz; A bitişi A'nın çekimini normal durdurur. Normal erken manuel durdurma ve kayıt başlatma yanıtlarının sıra/istek koruması değişmedi. Ağ paketi boyu, yeni RPC, root-motion, video ödeme/oyuncu yetkisi değişmedi.

`P257AutomaticStopOrderingTests` altı hedef-sıra durumu ve av pickup / süre bitimi örneklerini kapsar. Bu bir deterministik seam testidir; gerçek 2 GPU + misafir kameracı/avcı + 1/4 oyuncu ve ayrı PC testleri henüz yürütülmedi. #257 ve #164 bütün kabul kutuları kapanmış sayılmaz.
