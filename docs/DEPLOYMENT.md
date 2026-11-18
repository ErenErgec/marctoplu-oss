# 🌐 MARCTOPLU — Sunucu (VPS) ve Domain Canlıya Alma Rehberi

Bu rehber, kiralayacağınız bir **Linux Sunucu (VPS/VDS)** ve **Domain (Alan Adı)** üzerine MARCTOPLU projesini kurmanız, otomatik SSL (HTTPS) almanız ve mobil uygulamayı bu sunucuya bağlamanız için adım adım hazırlanmıştır.

---

## 1. Gereksinimler

### 🖥️ Önerilen Sunucu Özellikleri (VPS)
- **Sağlayıcı:** Hetzner (CX22/CX32 önerilir), DigitalOcean, Contabo, Linode veya herhangi bir yerli/yabancı bulut sağlayıcı.
- **İşletim Sistemi:** Ubuntu 24.04 LTS veya Ubuntu 22.04 LTS
- **RAM:** Minimum 2 GB (4 GB önerilir)
- **CPU:** 2 vCPU
- **Disk:** 30+ GB SSD / NVMe

---

## 2. Alan Adı (DNS) Yapılandırması

Domain sağlayıcınızın (GoDaddy, Namecheap, Metunic, Cloudflare vb.) DNS yönetim paneline girerek aşağıdaki **2 adet A Kaydı**'nı ekleyin:

| Tür | Ad (Host) | Değer (Değer / IP) | TTL | Açıklama |
|---|---|---|---|---|
| **A** | `@` *(veya alan adınız)* | `SUNUCU_IP_ADRESINIZ` | Otomatik / 300 | Ana site (`marctoplu.com`) |
| **A** | `api` | `SUNUCU_IP_ADRESINIZ` | Otomatik / 300 | Mobil API (`api.marctoplu.com`) |

> [!NOTE]
> DNS kayıtlarının tüm dünyada yayılması (propagation) genellikle 5-15 dakika sürer.

---

## 3. Sunucuya Bağlanma ve İlk Hazırlık

Bilgisayarınızın terminalinden SSH ile sunucunuza bağlanın (ilk girişte root şifresi sorulur):
```bash
ssh root@SUNUCU_IP_ADRESINIZ
```

### 3.0. SSH Anahtarı ile Giriş (Önerilir)
Şifre yerine anahtar çifti kullanılır: özel anahtar (`~/.ssh/id_ed25519`) bilgisayarınızda kalır, açık anahtar (`.pub`) sunucuya konur. Aşağıdaki komutlar **kendi bilgisayarınızda** (macOS/Linux) çalıştırılır.

1. **Anahtar çifti oluşturun** (varsayılan konum için Enter; bir passphrase belirlemeniz önerilir):
   ```bash
   ssh-keygen -t ed25519 -C "cihaz-adi"
   ```
2. **Açık anahtarı sunucuya kopyalayın** (son kez root şifresi sorulur):
   ```bash
   ssh-copy-id -i ~/.ssh/id_ed25519.pub root@SUNUCU_IP_ADRESINIZ
   ```
3. **Deneyin:** `ssh root@SUNUCU_IP_ADRESINIZ` artık sunucu şifresi sormamalı.
4. **(macOS) Passphrase'in her seferinde sorulmaması için** anahtarı Anahtar Zinciri'ne ekleyin ve `~/.ssh/config` oluşturun:
   ```bash
   ssh-add --apple-use-keychain ~/.ssh/id_ed25519
   printf 'Host *\n    AddKeysToAgent yes\n    UseKeychain yes\n    IdentityFile ~/.ssh/id_ed25519\n' >> ~/.ssh/config && chmod 600 ~/.ssh/config
   ```
   İsterseniz aynı dosyaya bir kısayol da ekleyebilirsiniz (sonra `ssh marctoplu` yeterli olur):
   ```
   Host marctoplu
       HostName SUNUCU_IP_ADRESINIZ
       User root
   ```

> [!IMPORTANT]
> Özel anahtarı (`id_ed25519`) kimseyle paylaşmayın ve güvenli bir yere yedekleyin. Her cihaz için ayrı anahtar oluşturup 2. adımı o cihaz için tekrarlayın; aynı özel anahtarı cihazlar arasında kopyalamayın.

#### (İsteğe bağlı) Şifreyle girişi kapatma
Anahtarla girişin çalıştığından emin olduktan sonra, **sunucuda** şifreyle girişi tamamen kapatabilirsiniz:
```bash
cat > /etc/ssh/sshd_config.d/00-anahtar-zorunlu.conf <<'EOF'
PasswordAuthentication no
KbdInteractiveAuthentication no
PermitRootLogin prohibit-password
PubkeyAuthentication yes
EOF
sshd -t && systemctl restart ssh
```
Dosya adındaki `00-` önemlidir: sshd bir ayar için ilk okuduğu değeri kullanır ve Ubuntu'daki `50-cloud-init.conf` genellikle `PasswordAuthentication yes` içerir.

> [!WARNING]
> Mevcut oturumu **kapatmadan** yeni bir terminalden `ssh root@SUNUCU_IP_ADRESINIZ` ile girişi doğrulayın. `ssh -o PubkeyAuthentication=no root@SUNUCU_IP_ADRESINIZ` komutu `Permission denied (publickey)` vermelidir. Sorun olursa açık oturumdan `rm /etc/ssh/sshd_config.d/00-anahtar-zorunlu.conf && systemctl restart ssh` ile geri alın. Anahtar kaybolursa tek giriş yolu VPS sağlayıcısının web konsolu olur.

Şifreyle giriş açık kalacaksa root şifresinin uzun ve rastgele olduğundan emin olun ve kaba kuvvet denemelerine karşı `fail2ban` kurun (3.1).

### 3.1. Sistemi Güncelleyin ve Güvenlik Duvarını Ayarlayın
```bash
# Paket listesini güncelle ve yükselt
apt update && apt upgrade -y

# Güvenlik duvarını (UFW) yapılandır (SSH, HTTP ve HTTPS'e izin ver)
ufw allow 22/tcp
ufw allow 80/tcp
ufw allow 443/tcp
ufw allow 443/udp
ufw --force enable

# SSH'ye kaba kuvvet denemelerini engelle (varsayılan ayarlarla SSH'yi korur)
apt install -y fail2ban && systemctl enable --now fail2ban
```

### 3.2. Docker ve Docker Compose Kurulumu
Tek satırda resmi Docker kurulumunu yapın:
```bash
curl -fsSL https://get.docker.com | sh
```
Kurulumu doğrulayın:
```bash
docker --version
docker compose version
```

---

## 4. Projeyi Sunucuya Çekme ve Yapılandırma

### 4.1. Repoyu Klonlayın
```bash
cd /opt
git clone https://github.com/ErenErgec/MARCTOPLU.git
cd MARCTOPLU
```

### 4.2. Çevre Değişkenlerini (`.env`) Ayarlayın
```bash
cp .env.prod.example .env
nano .env
```
Dosyada şu alanları güncelleyin:
- `DOMAIN=alanadiniz.com`
- `ACME_EMAIL=epostaniz@domain.com`
- `POSTGRES_PASSWORD=<uzun-rastgele-sifre>` (örn. `openssl rand -base64 32` çıktısı)
- `NEXT_PUBLIC_APP_STORE_URL=https://apps.apple.com/app/marctoplu/id<APP_ID>` (App Store Connect'teki Apple ID; uygulama App Store'da yayında değilken boş bırakın. Boşken web'deki App Store butonları gizlenir ve iPhone/iPad'de uygulama karşılaması gösterilmez. Build sırasında gömüldüğü için değiştirince `frontend` yeniden build edilmeli)
*(Kaydedip çıkmak için: `CTRL+O`, `ENTER`, `CTRL+X`)*

### 4.3. Firebase Servis Hesabı Anahtarını Ekleyin
Firebase Console > Proje Ayarları > Hizmet Hesapları (Service Accounts) bölümünden indirdiğiniz JSON dosyasını sunucuya yükleyin:
```bash
mkdir -p secrets
nano secrets/serviceAccountKey.json
# JSON içeriğini buraya yapıştırıp kaydedin.
```

---

## 5. Sistemi Başlatma (Canlıya Alma)

Hazırladığımız dağıtım betiğini çalıştırın:
```bash
./scripts/deploy-server.sh
```

Bu betik otomatik olarak:
1. PostgreSQL, .NET API, Next.js Frontend ve Caddy konteynerlerini inşa eder,
2. Caddy ters proxy'si üzerinden Let's Encrypt'ten **ücretsiz SSL sertifikası** temin eder,
3. Servisleri arka planda çalışır hale getirir.

### Canlı Logları İnceleme
```bash
# Tüm servislerin logları
docker compose -f docker-compose.prod.yml logs -f

# Sadece Caddy (SSL ve gelen istekler) logları
docker compose -f docker-compose.prod.yml logs -f caddy

# Sadece Backend logları
docker compose -f docker-compose.prod.yml logs -f backend
```

---

## 6. Doğrulama ve Test

Tarayıcınızdan şu adresleri kontrol edin:
- `https://api.alanadiniz.com/health` -> `Healthy` yanıtı ve yeşil SSL kilidi görmelisiniz.
- `https://alanadiniz.com` -> Web arayüzünün başarıyla açıldığını görmelisiniz.

---

## 7. Mobil Uygulamayı Canlı Sunucuyla Derleme

Sunucunuz ayağa kalktıktan sonra, mobil uygulamanızı yeni API adresinizle derleyin:

### Android Release APK / AppBundle:
```bash
cd marctoplu_mobile
flutter build appbundle --release --dart-define=API_BASE_URL=https://api.alanadiniz.com
```

### iOS Release Build (Xcode):
```bash
cd marctoplu_mobile
flutter build ipa --release --dart-define=API_BASE_URL=https://api.alanadiniz.com
```

---

## 8. Otomatik Veritabanı Yedekleme (Cronjob)

Veri kaybını önlemek için sunucuda her gece otomatik veritabanı yedeği alın:

```bash
crontab -e
```
En alta şu satırı ekleyin (Her gece saat 03:00'da yedeği `/opt/backups` klasörüne alır):
```cron
0 3 * * * mkdir -p /opt/backups && docker exec marctoplu_postgres pg_dump -U marcuser marctoken | gzip > /opt/backups/db_backup_$(date +\%F).sql.gz
```

---

## 9. Uygulama İçi Satın Alma (IAP) Doğrulaması

Backend, mobil satın almaları kredi yüklemeden önce Google Play ve App Store'da doğrular. **Bir platformun anahtarı sunucuda yoksa o platformdaki satın almalar reddedilir** (kullanıcı "daha sonra tekrar denenecek" mesajı görür, ödemesi tüketilmez). Bu yüzden anahtarları IAP doğrulamalı sürümü deploy etmeden **önce** sunucuya koyun.

### 9.1. Android (Google Play)
1. Google Cloud Console'da (Firebase projesi `marctoplu` ya da ayrı bir proje) **Google Play Android Developer API**'yi etkinleştirin.
2. Bir service account oluşturup JSON anahtarını indirin (mevcut Firebase service account da kullanılabilir).
3. Play Console → **Kullanıcılar ve izinler** → service account e-postasını davet edin. Uygulama izni olarak **"Finansal verileri, siparişleri ve iptal anketi yanıtlarını görüntüleme"** yetkisini verin. Yetkinin etkinleşmesi birkaç saat sürebilir.
4. JSON dosyasını sunucuya koyun:
   ```bash
   nano /opt/MARCTOPLU/secrets/play-service-account.json
   ```

### 9.2. iOS (App Store)
1. App Store Connect → **Kullanıcılar ve Erişim → Entegrasyonlar → Uygulama İçi Satın Alma** → yeni anahtar oluşturun ve `.p8` dosyasını indirin (yalnızca bir kez indirilebilir).
2. Aynı sayfadaki **Issuer ID** ve anahtarın **Key ID** değerlerini not edin.
3. Anahtarı sunucuya koyun:
   ```bash
   nano /opt/MARCTOPLU/secrets/apple-iap-key.p8
   ```

### 9.3. `.env` değişkenleri
```bash
IAP_GOOGLE_SERVICE_ACCOUNT_PATH=/app/secrets/play-service-account.json
IAP_APPLE_ISSUER_ID=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
IAP_APPLE_KEY_ID=XXXXXXXXXX
IAP_APPLE_PRIVATE_KEY_PATH=/app/secrets/apple-iap-key.p8
IAP_APPLE_ALLOW_SANDBOX=true   # App Review ve TestFlight için true kalmalı
```
Yollar konteyner içindeki yollardır (`./secrets` → `/app/secrets`).

### 9.4. Kontrol
Deploy sonrası backend açılış logunda iki platform için de "aktif" yazmalı:
```bash
docker compose -f docker-compose.prod.yml logs backend | grep '\[IAP\]'
```
`YAPILANDIRILMAMIŞ` görüyorsanız dosya yolunu ve `.env` değerlerini kontrol edin. Doğrulama sırasında Google'dan 401/403 geliyorsa service account'un Play Console yetkisi henüz etkinleşmemiştir.

### 9.5. İade bildirimleri (webhook)
İade edilen satın almanın tokenleri kullanıcıdan otomatik düşülür (bakiye eksiye inebilir; iade geri alınırsa tokenler geri yüklenir). Bunun için iki mağazanın da sunucu bildirimlerini backend'e göndermesi gerekir. Uç noktalar anonimdir. Bildirim yalnızca tetikleyicidir; iade, mağaza API'sinden 9.1/9.2'deki kimlik bilgileriyle ayrıca doğrulanır. Ek sır veya `.env` değişkeni gerekmez.

**Android (Real-time Developer Notifications):**
1. Google Cloud Console → **Pub/Sub** → bir konu (topic) oluşturun, örn. `play-rtdn`.
2. Konunun izinlerine `google-play-developer-notifications@system.gserviceaccount.com` hesabını **Pub/Sub Publisher** rolüyle ekleyin.
3. Konuya bir **push** aboneliği ekleyin. Uç nokta: `https://api.marctoplu.com/api/payments/iap/webhooks/google`.
4. Play Console → **Para kazanma kurulumu → Google Play Faturalandırma** → **Gerçek zamanlı geliştirici bildirimleri**: konu adını girin (`projects/<proje-id>/topics/play-rtdn`). Bildirim içeriği olarak **iptal edilen satın almaları da** kapsayan seçeneği seçin, örn. "Abonelikler, iptal edilen satın almalar ve tüm tek seferlik ürünler".
5. Aynı sayfadaki **Test bildirimi gönder** ile bağlantıyı deneyin. Backend logunda "Google Play test bildirimi alındı" görünmeli.

**iOS (App Store Server Notifications V2):**
1. App Store Connect → Uygulama → **Uygulama Bilgileri → App Store Sunucu Bildirimleri**.
2. Hem **Üretim** hem **Sandbox** sunucu URL'sine `https://api.marctoplu.com/api/payments/iap/webhooks/apple` yazın, sürüm **Version 2**.

Kontrol:
```bash
docker compose -f docker-compose.prod.yml logs backend | grep -i 'iade\|bildirim'
```
Mağaza API'sine ulaşılamazsa uç nokta 503 döner ve mağaza bildirimi tekrar gönderir (Pub/Sub üstel geri çekilmeyle, Apple 5 kez).

---

## 10. Sunucu Kapalıyken Vercel'de Tanıtım Sitesi

VPS kapalıyken `marctoplu.com` yalnızca bir vitrin olarak Vercel'den yayınlanabilir. Bunun için frontend **tanıtım modunda** (`NEXT_PUBLIC_PROMO_MODE=1`) derlenir:

- `/` herkese açık tanıtım sayfasını gösterir (`components/promo/PromoLanding.tsx`). Firebase ya da API çağrısı yapılmaz.
- `/login`, `/register`, `/profile`, `/admin`, `/checkout/*` geçici olarak (307) `/`'e yönlenir (`next.config.mjs`).
- Yasal sayfalar (`/gizlilik`, `/kullanim-kosullari`, `/hesap-silme` ve İngilizce takma adları) mağaza incelemesi için açık kalır.

### 10.1. Vercel projesi
1. Vercel → **Add New → Project** → GitHub reposunu içe aktarın.
2. **Root Directory:** `frontend`. Framework otomatik olarak Next.js seçilir; build komutlarını değiştirmeyin. (`output: "standalone"` Vercel build'inde `VERCEL=1` olduğu için kendiliğinden kapanır.)
3. **Environment Variables** (Production):
   | Anahtar | Değer |
   |---|---|
   | `NEXT_PUBLIC_PROMO_MODE` | `1` |
   | `NEXT_PUBLIC_FIREBASE_*` (6 adet) | `frontend/.env.local.example` ile aynı |
   | `NEXT_PUBLIC_APP_STORE_URL` | (İsteğe bağlı) App Store linki; boşsa buton gizlenir |
4. **Production Branch:** `main`. `main`'e her push Vercel'de yeni bir deploy başlatır.

### 10.2. Alan adı
1. Vercel projesi → **Settings → Domains** → `marctoplu.com` ve `www.marctoplu.com` ekleyin.
2. DNS panelinde, Vercel'in o ekranda gösterdiği kayıtları girin. Genelde şunlardır:
   | Tür | Ad | Değer |
   |---|---|---|
   | A | `@` | `76.76.21.21` (Vercel farklı bir IP gösterirse onu kullanın) |
   | CNAME | `www` | `cname.vercel-dns.com` |
3. `api` kaydına dokunmayın. Sunucuya bakmaya devam etsin.
4. SSL sertifikasını Vercel kendisi alır. DNS yayıldıktan sonra Domains ekranında yeşil tik görünür.

### 10.3. Sunucuya geri dönüş
1. DNS'te `@` ve `api` A kayıtlarını VPS IP'sine çevirin, `www` kaydını `marctoplu.com`'a CNAME yapın. Caddy `www`'yu ana alan adına yönlendirir ve onun için de sertifika alır.
2. Vercel'de domaini kaldırın (veya projeyi silin). Böylece iki yer aynı domain için sertifika almaya çalışmaz.
3. Sunucuda `./scripts/deploy-server.sh` çalıştırın. Caddy, DNS VPS'e döndükten sonra sertifikayı yeniden alır.
4. Docker build'i `NEXT_PUBLIC_PROMO_MODE` vermediği için sunucudaki site normal panel olarak açılır.
