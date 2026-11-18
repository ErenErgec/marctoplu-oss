# Katkı Rehberi

MARCTOPLU'ya katkı vermek istediğiniz için teşekkürler. Hata bildirimi, öneri ve pull request'ler memnuniyetle karşılanır.

## Nasıl çalışıyoruz?

Bu depo, resmi MARCTOPLU hizmetinin kaynak kodunun herkese açık kopyasıdır. Geliştirme ayrı bir depoda sürer ve hazır olan sürümler buraya toplu olarak aktarılır. Bu yüzden:

- Kabul edilen pull request'ler doğrudan bu depoda birleştirilmez. Değişiklik ana geliştirme deposuna alınır, test edilir ve bir sonraki sürümle buraya gelir. PR'ınız bu durumda "aktarıldı" notuyla kapatılır. Sürüm commit'ine, `Signed-off-by` satırınızdaki ad ve e-postayla bir `Co-authored-by:` satırı eklenir; GitHub katkıyı profilinizde gösterir (e-posta GitHub hesabınıza bağlı olmalı).
- Büyük bir değişiklik planlıyorsanız önce bir issue açıp yaklaşımı konuşalım. Böylece emeğiniz boşa gitmez.

## Geliştirme ortamı

Kurulum adımları [README.md](README.md) içindeki **Yerel Geliştirme** bölümündedir (kendi Firebase projeniz gerekir). PR açmadan önce değiştirdiğiniz bölümün kontrollerini depo kök dizininden çalıştırın:

```bash
cp .env.example .env && docker compose up --build              # tüm stack
(cd frontend && npm run lint && npm run build)                   # web
(cd marctoplu_mobile && flutter analyze && flutter test)         # mobil
python3 -m unittest discover -s engine/tests -v                  # motor (ağ çağrısı yok)
docker run --rm -v "$PWD/backend/MarcToken.Api":/src -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet build   # backend (.NET SDK yoksa)
```

## Kurallar

- Arayüz metinleri, kod yorumları ve commit mesajları **Türkçe** yazılır.
- Commit mesajı önekleri: `feat:`, `fix:`, `refactor:`, `docs:`, `style:`.
- Veritabanı şeması yalnızca EF Core migration ile değişir; elle SQL yazılmaz.
- Token hareketleri yalnızca `TokenService` üzerinden yapılır.
- Ücret kuralı: yalnızca **bulunan** kayıtlar ücretlendirilir. Bu kuralı değiştiren bir katkı, web giriş/kayıt sayfalarındaki ve `/kullanim-kosullari` sayfasındaki metni de güncellemelidir.
- İş durumu değerleri sabittir: `queued`, `running`, `completed`, `failed`, `cancelled`.
- Gizli anahtar, `.env` dosyası veya Firebase servis hesabı dosyası commit etmeyin.

## Geliştirici Menşe Sertifikası (DCO)

Her commit'in sonuna aşağıdaki satırı ekleyerek [Developer Certificate of Origin 1.1](https://developercertificate.org/) metnini kabul ettiğinizi belirtirsiniz:

```
Signed-off-by: Adınız Soyadınız <eposta@ornek.com>
```

`git commit -s` bu satırı otomatik ekler. Bu satır, katkının sizin tarafınızdan yazıldığını ve proje lisansı ([AGPL-3.0](LICENSE)) altında paylaşma hakkınız olduğunu beyan eder. İmzasız commit içeren PR'lar kabul edilmez.

## Davranış

Saygılı ve yapıcı olun. Kişisel saldırı, taciz veya ayrımcılık içeren katkı ve yorumlar kaldırılır.
