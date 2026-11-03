# #218 / #155 — Beş türün gerçek Windows build'inde 1/2/4 süreç görünürlüğü ve yakın/uzak görüntü (3 Kasım 2026)

Durum: **Gözlem kanıtı. Kabul veya PASS değildir.** Prob hiçbir şeyi yargılamaz; bu belgedeki okunurluk yorumu bir kişinin resimlere bakışıdır ve Utku/Mehmet'in görsel incelemesinin yerine geçmez.

## Ne ölçüldü

- Taban: `codex/p4-integration` `0cfbe91` (#238 sonrası, beş tür sahnesi). Build: `tools/Build-P1.ps1 -Integrated` (Windows, entegre oyuncu); `tools/Test-P411-SpeciesVisibility.ps1` host (grafik cihazlı) ve istemcileri (`-nographics`) gerçek süreç olarak açar.
- `Assets/Tests/P1/Runtime/P411SpeciesVisibilityProbe.cs` her süreçte dalış sahnesi yüklenip 15 balık spawn olduktan 2 sn sonra: türe göre kaç balık spawn oldu ve kaçında etkin bir `Renderer` var; grafik cihazı olan süreçte her türden bir gerçek balığın yakın (2,5 m) ve uzak (9 m) resmi, yandan, sahnenin kendi ışık/sis ayarlarıyla geçici bir kameradan. Durum değiştirmez.

## Sonuç (JSON'lar `evidence/p411-species-runtime/`)

| Süreç sayısı | Rapor veren süreç | Her süreçte |
|---|---|---|
| 1 (yalnız host) | 1 / 1 | 15 balık spawn; sea_bass, tuna, red_snapper, grouper, lanternfish 3'er adet, üçünde de etkin renderer |
| 2 (host + 1 istemci) | 2 / 2 | aynı |
| 4 (host + 3 istemci) | 4 / 4 | aynı |

Yani bu koşularda beş türün hepsi, her süreçte, 3'er balıkla ve görünür renderer'la spawn oldu. "Görünür" burada yalnız etkin renderer demektir; kameradan kadraja girme veya okunurluk ayrıca aşağıda.

## Resimlerden okunan (yorum, ölçüm değil)

[species-near-far-contact-sheet.png](evidence/p411-species-runtime/species-near-far-contact-sheet.png) (üst sıra 2,5 m, alt sıra 9 m; tek koşu, balıkların o anki yeri ve açısı rastgele):

- **Yakın (2,5 m):** beş tür renk ve silüetle ayrışıyor: levrek (Humphead) koyu yeşil/kahve, çıkık alınlı; ton beyaz-mavi gövde, sarı kuyruk; kırmızı mercan kırmızı; orfoz turuncu-kahve; fener balığı siyah, başında mavi ışıklı uzantı.
- **Uzak (9 m):** sis ve mesafede levrek, ton, kırmızı mercan ve orfoz küçük, silik noktalar. **Fener balığı bu mesafede görünmüyor**: koyu gövdesi derin suyun koyu mavi-yeşili içinde kayboluyor (ışıklı uzantısı da dahil, 2 koşuda da). Önceki koşuda kırmızı mercan ve orfoz da 9 m'de görünmezdi; yani uzak okunurluk bu sahne ışığında zayıf ve koşudan koşuya balığın konumuna göre değişiyor.
- Bu resimler oyuncu kamerasından değil geçici bir kameradan; oyuncunun gerçek sualtı kamerası, yüzme el feneri veya post ayarları farklı olabilir.

## Denenmedi / açık

- Oyuncu kamerasından ve gerçek ekipmanla (el feneri vb.) uzak okunurluk; gece/gündüz ışığı; "normal hızda hareketle ayrışma" (resimler tek kare).
- Av → kıyıya dönüş → satış → çekim → ansiklopedi zinciri yeni iki tür için; ölüm pozu ve animasyon son kareleri; kayıt/ekonomi geriye uyumu gerçek kayıtla.
- Tuna'nın sığ kıyıdaki yeni yolunda dalgıç yolu ve NPC/kasa görüşü.
- Ayrı PC üzerinde 1/2/4 oyuncu (bu koşular tek makinede yerel süreçlerdir); performans/kare hızı ölçülmedi.
- #164 ve #207 açık kalır. Fener balığının uzakta okunmaması Utku/Mehmet ile görsel karar gerektirir (malzeme/ışık, uzantının parlaklığı, derin suda ışık).
