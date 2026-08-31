## Görev ve faz

- Aktif faz:
- Görev kimliği (ör. P2-A / P2-FIX-01):
- PR türü: özellik → faz dalı / faz kapanışı → main
- Hedef dal:
- Sistem sahibi:
- İnceleyen:

## Değişiklik

Hangi sorunu çözüyor, ne değişiyor ve oyuncuya/diğer sisteme etkisi nedir?

## Sınırlar ve bağlantılar

- Değişen veri/API/sahne/prefab:
- Etkilenen diğer kişi:
- Kapsam dışı veya gelecek faza ait değişiklik: yok / açıklama ve onay bağlantısı
- Varsa plan değişikliği kaydı:

## Doğrulama

- Build/commit:
- Çalıştırılan testler ve gerçek sonuçları:
- Oyuncu sayısı ve ayrı bilgisayar/süreç düzeni:
- Çalıştırılmayan testler ve nedeni:
- Kanıt bağlantıları:

## Teslim kontrolü

- [ ] Görev açık faza ait; sonraki fazın özelliği eklenmedi.
- [ ] Etkilenen ortak sözleşme ve test sağlayıcıları güncellendi.
- [ ] Test sağlayıcısı teslimin gerçek oyun yolunda bırakılmadı.
- [ ] Sahne/prefab referansları ve gerekli `.meta` dosyaları doğrulandı.
- [ ] Sır, önbellek veya gereksiz büyük dosya eklenmedi.
- [ ] Başka kişinin incelemesi ve ilgili build/test sonucu mevcut.
- [ ] STATUS'ta görev ve kanıt güncellendi; faz erken tamamlandı yapılmadı.

## Yalnızca faz kapanış PR'ı

- Faz raporu bağlantısı:
- A/B/C aday commit onayları:
- Kalan kritik/yüksek önem hatası:
- Ana dala birleşme sonrası doğrulamayı yapacak kişi:

Bu PR'ın birleşmesi tek başına sonraki fazı açmaz; birleşmiş ana dal commit'inin doğrulaması ve STATUS kaydı gerekir.
