# P4.9 / #220 — Kozmetik tekrar etkinleşince sandal kamera pozunu geri yükleme

Sorun: `PlayerActionView.OnDisable`, görsel iptal temizliği ve kamera telafisi için `NetworkPlayer.SetVisualBoatSeated(false)` çağırıyordu. Aynı `BoatTripPlayerSync` nesnesinde MySeatId hâlâ doluysa, nesne yeniden etkinleştiğinde yeni bir `OnValueChanged` olayı gelmeyebilir; böylece oyuncu sandalda otururken kamera ayakta kalabilir.

Düzeltme: `PlayerActionView.OnEnable` ilk `Awake` öncesinde null guard kullanır; sonraki etkinleşmelerde `trip.IsSeated` ile gerçek sandal koltuğunu sorgular, eski giriş/çıkış geçiş kozmetiğini temizler ve `RestoreBoatSeatPresentation` üzerinden kamera görselini yeniden uygular. Bed gibi genel `NetworkPlayer.Seated` kullanılmaz; sadece gerçek `BoatTripPlayerSync.IsSeated` oturma ofsetini açar. Bir seat restore ikinci board veya exit sayılmaz; değişen para/oyuncu hareketi/root-motion/network state yoktur.

Testler: `P49BoatSeatReactivationTests`, var olan sandal pozunun görsel olarak idempotent yüklenmesini, bed/non-boat sahnede kaldırılmasını ve ilk `OnEnable` guard'ını sınar. Gerçek component enable/disable/replication ve 1/2/4 runtime testleri CI veya nihai kabulde ayrıca doğrulanacaktır. Bu teknik düzeltme #220 / #164 nihai PASS değildir.
