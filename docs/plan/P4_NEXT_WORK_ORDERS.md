# 16 Ekim 2026 — Birleşim sonrası alan görevleri

Kullanıcının doğrudan talimatıyla PR [#169](https://github.com/mehmetalisahingm/DeepDiveGame/pull/169), `72c5bbc79451ddcacdd6ad567a1e75b83e80010b` olarak P4 entegrasyonuna birleşti. Önceki PR head'inin kaynak ağacı ile birleşmiş commit'in kaynak ağacı aynıdır. CI EditMode/Windows build geçti; eski iptal edilmiş koşu PASS sayılmaz, atlanan fleet/boss CI işleri de çalışmış gibi yazılmaz. Bu birleşim, Utku/Mert adına PR incelemesi veya ürün kabulü oluşturmaz.

Birleşmiş commit'ten tekrar Windows build ve iki gerçek süreçli dünya turu, ardından aynı kampanyayla yeni host süreci PASS. [Birleşim sonrası rapor ve kanıt](../reports/P4-CONTINUOUS-SESSION-157.md). Faz/nihai ürün kabulü açık kalır.

## Yeni alt işler

| Görev | Sahip | Ana iş | Somut teslim |
|---|---|---|---|
| [#170](https://github.com/mehmetalisahingm/DeepDiveGame/issues/170) | Mehmet | #157 / #141 | Av, satış, gerçek klip, sandal/sefer, fiziksel PC dönüşü, uyku ve diskten devamı aynı oturum/kampanyada kanıtla |
| [#171](https://github.com/mehmetalisahingm/DeepDiveGame/issues/171) | Mert | #159; #154/#153 bağlantısı | Bir ev, bir dükkân, kıyı ve kırık sandal için uyumlu sınırlı görsel/ses kiti ve kaynak kaydı |
| [#172](https://github.com/mehmetalisahingm/DeepDiveGame/issues/172) | Utku | #159; #153/#138/#140 bağlantısı | Mevcut yolların ayrı mesafe/süre ölçümü, anchor/collider haritası ve dolu kısa örnek yerleşimi |
| [#173](https://github.com/mehmetalisahingm/DeepDiveGame/issues/173) | Mehmet | #159; #157 bağlantısı | İnsan/kapı/tezgâh ölçeği, yakın kamera/el-nesne kadrajı ve dört kişi geçişi |

#170 GitHub'da #157'nin, #171/#172/#173 ise #159'un alt işidir. Mevcut #153–#157 ve #159–#164 tekrar açılmadı; bu görevler [PRODUCTION_BLUEPRINT](PRODUCTION_BLUEPRINT.md) 7-1…7-5'in kişi bazında teslimleridir. Her issue gerçek hesap sahibine atanmıştır.

## Sıra ve sınır

1. Mevcut P4.6 içinde #170 birleşik teknik döngü ve #138/#140/#141'in eksik kanıtları tamamlanır. Ayrı bilgisayarda dört oyuncu/üç gün ve üç kişinin gerçek kabulü #141'de kalır; aynı PC'deki süreçler bunun yerine geçmez.
2. P4.7 için mevcut sahneyi ölçme, placeholder/kaynak envanteri, referans/palet ve rig/kamera inceleme hazırlığı yapılabilir. Yeni sanat/geometri/rig'in oyuna uygulanması P4.6 teknik temel kabulünden sonra başlar. Issue oluşturulması aşamayı açmaz.
3. Mert'in örnek kiti, Utku'nun örnek alanı ve Mehmet'in kamera/ölçek bağlantısı aynı kısa bölümde birleştirilir; gerçek Windows görüntüsü, gündüz/gece okunurluk, kare süresi ve kullanıcının hedef kalite değerlendirmesi #159'a bağlanır.
4. Kısa görsel örnek kabulünden sonra #160 fiziksel taşıma/yerleştirme/keser-çivi tamiri; ardından #161 ortak eylemler, #162 kasaba/ev, #163 canlı deniz ve #164 nihai kabul gelir. Bu görev açılışı bu özellikleri uygulamaya başlamaz.

R01/R02/R11 tamir semantiği/alt-slot/edinme-contribution kararları açık kalır. Plan önerisi ekip kararı gibi yazılmaz. P5/P6 kilidi, ücretli kaynak ve yayın için ayrı yetki sınırları değişmez.
