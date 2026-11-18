# 📚 MARCTOPLU — Toplu MARC & ISBN Kataloglama Platformu

MARCTOPLU; kütüphaneciler, sahafçılar, yayınevleri, kütüphane yöneticileri ve araştırmacılar için geliştirilmiş; barkod/kamera tarama, Excel tabanlı toplu ISBN sorgulama ve zenginleştirilmiş **MARC 21 (`.mrc`)** ile **Excel (`.xlsx`)** katalog çıktıları üreten uçtan uca çok kanallı bir platformdur.

## 🚦 Hazır Kullan ya da Kendin Kur

| | Nasıl? | Kimin için? |
|---|---|---|
| **Hazır kullan** | [marctoplu.com](https://marctoplu.com) üzerinden hesap açın, ISBN listenizi yükleyin. Sunucu, güncelleme ve kaynak bakımıyla uğraşmazsınız. Mobil uygulama ile barkod tarama da dahildir. | Kurulumla uğraşmak istemeyen kütüphaneler, sahaflar, yayınevleri |
| **Kendin kur** | Bu depoyu klonlayıp kendi sunucunuzda çalıştırın (aşağıdaki **Yerel Geliştirme** bölümü ve [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)). Kendi Firebase projeniz ve PostgreSQL veritabanınız gerekir; mobil uygulamayı yayınlayacaksanız kendi mağaza hesaplarınız da ([marctoplu_mobile/README.md](marctoplu_mobile/README.md)). | Kendi altyapısını yönetmek isteyen kurumlar ve geliştiriciler |

Kod [AGPL-3.0](LICENSE) lisanslıdır: ücretsiz kurabilir, değiştirebilir ve dağıtabilirsiniz. Değiştirilmiş bir sürümü ağ üzerinden hizmet olarak sunarsanız kaynak kodunu da kullanıcılarınıza açmanız gerekir.

---

## 🏗️ Mimari ve Teknoloji Yığını

Platform modern, ölçeklenebilir ve yüksek performanslı mikro servis/modüler monolit mimarisiyle tasarlanmıştır:

- **Web Frontend:** [Next.js 16](https://nextjs.org/) (App Router, React 19, TypeScript, modern responsive UI, SEO & Store uyumluluk sayfaları)
- **Mobil Uygulama:** [Flutter](https://flutter.dev/) (iOS & Android) — Kamera ile anlık ISBN tarama, toplu Excel yükleme, anlık MARC önizleme/paylaşma, Store IAP
- **Backend API:** [ASP.NET Core 8 Web API](https://dotnet.microsoft.com/) (C#) — EF Core 8, Hosted Background Worker servisleri, katı JWT doğrulaması
- **Katalog Arama Motoru:** Python MARC Motoru (`engine/marc_toplu_sorgula.py` & `backend/MarcToken.Api/python/job_runner.py`)
- **Veritabanı:** PostgreSQL (EF Core Migrations ile otomatik şema oluşturma ve güncelleme)
- **Kimlik Doğrulama:** [Firebase Authentication](https://firebase.google.com/) (E-posta/Şifre, Google Sign-In, Misafir/Anonim Giriş)
- **Bildirimler:** Firebase Cloud Messaging (FCM) ile toplu iş tamamlanma push bildirimleri
- **Ödeme & Token Altyapısı:**
  - **Mobil (tek aktif kanal):** Google Play Billing & Apple StoreKit (In-App Purchase — IAP), mağaza iade bildirimleri (webhook) ile otomatik token düşümü
  - **Web:** Satın alma için kullanıcı mobil uygulamaya yönlendirilir. [iyzico](https://www.iyzico.com/) checkout kodu depoda duruyor ancak **askıdadır** (yapılandırılmadan da API sorunsuz çalışır).
  - **Ekonomi Modeli:** 1 bulunan kayıt = 1 MARC Token. İş başında ISBN sayısı kadar token ayrılır; bitince bulunamayan/sorgulanamayan ISBN'lerin, çıktısız biten (iptal/hata) işlerin ise tamamı iade edilir.
- **Ters Vekil & SSL:** [Caddy 2](https://caddyserver.com/) ile otomatik Let's Encrypt SSL ve HTTP/3 (QUIC) desteği

---

## 📁 Proje Dizin Yapısı

```text
MARCTOPLU/
├── frontend/                     # Next.js web kullanıcı paneli, admin arayüzü ve yasal sayfalar
├── backend/
│   └── MarcToken.Api/            # ASP.NET Core 8 Web API, EF Core, background worker'lar
│       └── python/               # C# worker ile Python motoru arasındaki köprü (job_runner.py)
├── marctoplu_mobile/             # Flutter tabanlı iOS ve Android mobil uygulaması
├── engine/                       # Çekirdek Python MARC sorgulama ve dış kaynak entegrasyon motoru
├── scripts/                      # Yerel kurulum ve sunucu dağıtım betikleri
│   ├── setup-local.sh            # Mac / Linux yerel ortam kurulum betiği
│   ├── setup-local.ps1           # Windows PowerShell yerel kurulum betiği
│   └── deploy-server.sh          # VPS / VDS sunucu tek komutla canlıya alma betiği
├── secrets/                      # Firebase serviceAccountKey.json ve hassas anahtarlar
├── Caddyfile                     # Caddy ters vekil (reverse proxy) ve SSL konfigürasyonu
├── docker-compose.yml            # Yerel geliştirme ortamı Docker Compose dosyası
├── docker-compose.prod.yml       # Canlı sunucu (production) Docker Compose dosyası
└── docs/                         # Mimari (ARCHITECTURE.md) ve canlıya alma (DEPLOYMENT.md) dokümanları
```

---

## ⚡ Temel Özellikler

### 1. Web Platformu (`frontend/`)
- **Toplu Sorgulama:** Excel (`.xlsx`) dosyası yükleyerek arka plan kuyruğuna gönderme. "ISBN" başlıklı sütun yoksa ilk sütun kullanılır; örnek dosya web panelinden indirilebilir.
- **Canlı İlerleme Takibi:** Yüzdelik ilerleme çubuğu, anlık taranan adet ve kalan süre göstergesi.
- **Zengin Çıktı Formatları:** Sorgu tamamlandığında Excel (`.xlsx`), kütüphane otomasyonlarına (Koha, YORDAM vb.) aktarılabilir MARC 21 (`.mrc`, ISO 2709) ve MARCXML (`.xml`), ayrıca referans yöneticileri için RIS (`.ris`) indirme. Library of Congress'te bulunan kitaplar LoC'un tam kaydıyla, diğerleri kaynağı belirtilen minimal MARC kaydıyla gelir.
- **Cüzdan & Token Yönetimi:** Bakiye ve hesap hareketleri (ledger) dökümü. Token paketleri mobil uygulamadan satın alınır.
- **Destek Formu:** "Destek & Sorun Bildir" ile talep gönderme; talepler admin panelinde listelenir.
- **Yönetici (Admin) Paneli (`/admin`):**
  - Sistem geneli istatistikler (toplam kullanıcı, işlem hacmi, kuyruk durumu)
  - Canlı ve geçmiş işleri filtreleme/izleme
  - Kullanıcılara manuel token tanımlama / bakiye düzenleme
  - Token paketleri oluşturma, düzenleme ve yayından kaldırma
  - Mağaza satın almalarını (tutar, ortam, iade durumu) listeleme
  - Destek taleplerini görüntüleme, çözüldü olarak işaretleme ve telafi tokeni tanımlama
- **Store & Yasal Uyumluluk:** Gizlilik Politikası (`/gizlilik`), Kullanım Koşulları (`/kullanim-kosullari`) ve Hesap Silme (`/hesap-silme`) sayfaları.

### 2. Mobil Uygulama (`marctoplu_mobile/`)
- **Kamera ile ISBN Barkod Tarama:** Kitap arkasındaki barkodu kamerayla saniyeler içinde okuma.
- **Anlık MARC Önizleme:** Bulunan eserin künye bilgilerini, alan kodlarını görüntüleme, panoya kopyalama ve paylaşma.
- **Taranan ISBN'lerle Toplu İş:** Kamerayla taranan ISBN listesini doğrudan toplu işe çevirme.
- **Mobilden Excel Yükleme:** Cihaz hafızasından `.xlsx` seçerek toplu iş başlatma.
- **Cihaz Başına Ücretsiz Deneme (Device Trial):** Cihaz parmak izi kontrolü ile suistimali engelleyen tek seferlik ücretsiz deneme token kredisi.
- **Uygulama İçi Satın Alma (IAP):** Google Play ve App Store üzerinden doğrudan token paketi satın alma (Replay-attack korumalı backend doğrulaması).
- **Push Bildirimleri:** Uzun süren toplu işler arka planda tamamlandığında kullanıcıya anında bildirim gönderme.

### 3. C# Arka Plan Servisleri (`Hosted Services`)
- **`JobWorkerService`:** Paralel işlem kapasitesi (`MaxParallelJobs`), zaman aşımı kontrolü (`RunningJobTimeoutSeconds`), Python motoru ile IPC iletişimi ve hassas token iade/düşüm mekanizması.
- **`FileCleanupService`:** Belirtilen saklama süresi (`FileRetentionDays`, varsayılan 7 gün) dolan iş dosyalarını diskten temizler.
- **`GuestCleanupService`:** Hesabını kalıcıya dönüştürmeyen anonim misafir kullanıcıları ve eski kayıtlarını otomatik olarak süpürür (`GuestRetentionDays`, varsayılan 3 gün).

---

## 🛠️ Yerel Geliştirme (Local Setup)

### Gereksinimler
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) ve npm
- [Python 3.10+](https://www.python.org/)
- [PostgreSQL 16+](https://www.postgresql.org/) (veya Docker)
- [Flutter SDK](https://flutter.dev/) (Mobil geliştirme için)
- Bir [Firebase](https://console.firebase.google.com/) projesi: Authentication'da **E-posta/Şifre**, **Google** ve **Anonim** girişleri açık, bir **Web uygulaması** eklenmiş olmalı. Web yapılandırma değerleri (apiKey, appId…) Proje Ayarları → Genel sayfasındadır.
- (İsteğe bağlı) Firebase servis hesabı anahtarı: Proje Ayarları → Hizmet Hesapları'ndan indirip `secrets/serviceAccountKey.json` olarak kaydedin. Push bildirimleri ve misafir hesaplarının temizliği için gerekir; yoksa backend uyarı verip çalışmaya devam eder.

> Depodaki örnek dosyalar yer tutucu (`YOUR_FIREBASE_API_KEY`, `firebase-proje-id`) içerir. Backend ve web aynı Firebase projesini kullanmalıdır; aksi halde giriş yapılsa bile API istekleri 401 döner.

### Hızlı Kurulum Betiği (Yöntem B için)

`dotnet run` ile çalışacak backend'in sırlarını ve web'in `frontend/.env.local` dosyasını yapılandırmak için:

**Mac / Linux:**
```bash
./scripts/setup-local.sh
```

**Windows:**
```powershell
powershell -ExecutionPolicy Bypass -File scripts/setup-local.ps1
```

Betik Firebase değerlerinizi sorar, backend sırlarını `dotnet user-secrets` içine yazar ve `frontend/.env.local` dosyasını oluşturur. Elle yapmak isterseniz `frontend/.env.local.example` dosyasını `frontend/.env.local` olarak kopyalayıp doldurun.

---

### Yöntem A: Docker Compose ile Tek Komutta Çalıştırma

Tüm servisleri (PostgreSQL, Backend, Frontend) konteynerler içinde başlatmak için önce kök dizinde `.env` oluşturup Firebase değerlerinizi ve admin e-postanızı girin:

```bash
cp .env.example .env
docker compose up --build
```

- **Frontend:** [http://localhost:3000](http://localhost:3000)
- **Backend API:** [http://localhost:8080](http://localhost:8080)
- **Swagger UI:** [http://localhost:8080/swagger](http://localhost:8080/swagger)
- **PostgreSQL:** `localhost:5433` (kullanıcı/şifre: `postgres`/`postgres`)

Web'deki `NEXT_PUBLIC_*` değerleri build sırasında gömülür; `.env` değiştirince `docker compose up --build` ile yeniden derleyin.

---

### Yöntem B: Ayrı Ayrı Çalıştırma

#### 1. Python Bağımlılıkları
Backend işleyicisi Python motorunu kullandığı için ortamınızda Python ve gerekli kütüphaneler kurulu olmalıdır:
```bash
pip install -r engine/requirements.txt
```

#### 2. Backend (ASP.NET Core 8)
PostgreSQL'iniz yoksa yalnızca veritabanını Docker ile başlatabilirsiniz (`docker compose up -d postgres`, port 5433). Sırları yukarıdaki kurulum betiğiyle verin.
```bash
dotnet restore backend/MarcToken.Api/MarcToken.Api.csproj
dotnet run --project backend/MarcToken.Api/MarcToken.Api.csproj
```
*(Veritabanı migration'ları uygulama açılışında EF Core tarafından otomatik olarak uygulanır.)*

#### 3. Frontend (Next.js)
```bash
cd frontend
npm install
npm run dev
```

#### 4. Mobil (Flutter)
```bash
cd marctoplu_mobile
flutter pub get
flutter run --dart-define=API_BASE_URL=http://localhost:8080
```
`API_BASE_URL` verilmezse uygulama resmi sunucuya (`https://api.marctoplu.com`) bağlanır. Android emülatöründe bilgisayarınıza `http://10.0.2.2:8080` ile ulaşılır. Kendi Firebase projenizle çalıştırma: [marctoplu_mobile/README.md](marctoplu_mobile/README.md).

---

## 🔐 Kimlik Doğrulama & Yetkilendirme

- Sistem, istemcilerden gelen `Authorization: Bearer <token>` başlığındaki Firebase JWT token'larını doğrular.
- Backend; Issuer (`https://securetoken.google.com/<projectId>`), Audience (`<projectId>`), imza ve geçerlilik süresini (lifetime) katı modda kontrol eder.
- `/api/admin/*` endpoint'leri, `App:AdminEmails` konfigürasyonundaki e-posta adresleriyle eşleşen yöneticilere açıktır (Docker'da `.env` içindeki `ADMIN_EMAILS`). Liste boşsa admin paneli kapalıdır.

---

## 🚀 Canlıya Dağıtım (Production)

Proje, Ubuntu VPS/VDS sunucularda Docker ve Caddy ters vekiliyle sıfır kesintiye yakın çalışacak şekilde optimize edilmiştir.

Detaylı adım adım rehber için **[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)** dosyasını inceleyin.

### Hızlı Dağıtım Komutları

```bash
# İlk kurulum (UFW firewall + Swap + Docker konteynerleri)
./scripts/deploy-server.sh --init

# Sonraki kod güncellemeleri
git pull && ./scripts/deploy-server.sh
```

### Sistem Sağlık Kontrolleri (Health Checks)
- **Liveness:** `GET https://api.<DOMAIN>/health` -> `{"status":"ok"}`
- **Readiness:** `GET https://api.<DOMAIN>/health/ready` -> PostgreSQL bağlantısını test eder (`{"status":"ready"}`)

---

## 🤝 Katkı ve Güvenlik

- Katkı süreci için [CONTRIBUTING.md](CONTRIBUTING.md) dosyasına bakın.
- Güvenlik açıklarını herkese açık issue olarak değil, [SECURITY.md](SECURITY.md) dosyasındaki yolla bildirin.

---

## 📄 Lisans

Copyright (C) 2026 Eren Ergeç

Bu program özgür yazılımdır: [GNU Affero Genel Kamu Lisansı sürüm 3](LICENSE) (AGPL-3.0) koşulları altında yeniden dağıtabilir ve/veya değiştirebilirsiniz. Program faydalı olması umuduyla, ancak HİÇBİR GARANTİ OLMADAN dağıtılmaktadır.

**Marka:** "MARCTOPLU" adı ve logosu lisansın kapsamı dışındadır. Çatallanan (fork) veya değiştirilmiş sürümler bu ad ve logoyla, ya da resmi hizmetmiş izlenimi verecek şekilde yayınlanamaz. Ayrıntılar için [TRADEMARK.md](TRADEMARK.md) dosyasına bakın.
