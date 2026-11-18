# Güvenlik Politikası

## Açık bildirimi

Bir güvenlik açığı bulduysanız lütfen **herkese açık issue açmayın**. Bunun yerine şu yollardan biriyle bildirin:

- GitHub'da bu deponun **Security → Report a vulnerability** sekmesi (özel bildirim)
- E-posta: [info@marctoplu.com](mailto:info@marctoplu.com), konu satırına `[GÜVENLİK]` yazın

Bildiriminizde şunlar olursa daha hızlı ilerleriz:

- Etkilenen bileşen (backend, web, mobil, motor) ve sürüm/commit
- Açığı yeniden üretme adımları
- Olası etkisi (veri sızıntısı, yetki yükseltme, token/ödeme kötüye kullanımı vb.)

Bildiriminizi en geç 7 gün içinde yanıtlamaya çalışırız. Düzeltme yayınlanana kadar açığı paylaşmamanızı rica ederiz. Düzeltmeden sonra, isterseniz katkınızı adınızla anarız.

## Kapsam

- Bu depodaki kod
- [marctoplu.com](https://marctoplu.com) ve `api.marctoplu.com` üzerindeki resmi hizmet

Resmi hizmete karşı test yaparken başka kullanıcıların verisine erişmeyin, hizmeti aksatacak yük (DoS) oluşturmayın ve otomatik tarayıcıları düşük hızda çalıştırın. Mümkünse testleri kendi kurduğunuz bir kopya üzerinde yapın.

## Desteklenen sürümler

Yalnızca `main` dalındaki en güncel sürüm güvenlik güncellemesi alır.
