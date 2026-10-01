# P4.2-A — gerçek clip capture ve playback seam (#100)

## Uygulama

- P3 `RecordingDiveBinding` otoritesi korunur; ikinci recording authority yoktur.
- Host/server, gerçek `NetworkPlayer.RecordingPresentation` süresince oyuncunun server-authoritative göz/ileri/FOV bilgisinden 320x180 JPEG kareleri toplar.
- World `TakeRegistered` demeden capture aday olmaz; güvenli dönüşten sonra `RecordingQueued` gerçek persistent `recordingId` üretmeden clip archive'a girmez.
- `clipId = clip-<recordingId>` deterministiktir; tekrar/retry ikinci clip oluşturmaz.
- Yerel `.ddclip` container timestamp'li gerçek kareleri taşır; SHA-256 ve byte boyutu `ClipManifest` içine yazılır.
- Dosya `Application.persistentDataPath/DeepDiveClips` altında tutulur ve yazım temp -> final şeklinde tamamlanır.
- `ClipArchive` Mert'in #102 kanal/save authority'sine gerçek manifesti verir.
- `ClipPlayback` dosyayı tekrar açar, container + kayıtlı size/hash doğrulamasından sonra timestamp'li kareleri oynatır.
- Null/headless grafik cihazında sahte clip üretilmez.

## Testler

`RecordingClipFileTests` şunları kapsar:
- deterministik ve path-safe clip kimliği,
- encode/decode round-trip,
- bozuk/truncated container reddi,
- SHA-256 stabilitesi,
- gerçek `RecordingResult` -> manifest ownership/quality/duration,
- eksik recording id / geçersiz kalite / sıfır süre reddi.

## Acceptance kanıtı

Actions run `36624657007`:
- EditMode + açık XML doğrulaması: PASS.
- Windows integrated build: PASS.
- Fixture'sız 2-process product run'da host ve client raporları `passed=true`.
- Host gerçek kayıt için `P4_MEDIA_CLIP archived` yazdı: 41 kare, 98,336 byte.
- Aynı run `P4_MEDIA_PRODUCT_OK` ile gerçek capture -> safe recording -> archive -> playable local media zincirini doğruladı.
- İlk workflow step'i ürün hatası nedeniyle değil, acceptance PowerShell ifadesindeki parantez eksikliği nedeniyle kırmızıydı (`Test-Path ... -and`). Script düzeltildi; final recheck ürün smoke + mevcut #102 `-Media` regresyonunu birlikte çalıştırır.

## Bilinen sınır

P4.2-A bu fazda clip payload'ını capture yapan makinenin yerel persistent storage'ında tutar. Manifest/channel state ağda aynalanır; farklı fiziksel bilgisayara clip byte transferi bu issue'nun acceptance kapsamına eklenmedi. Ayrı-PC shared playback gerekiyorsa ayrıca transport/storage işi açılmalıdır.
