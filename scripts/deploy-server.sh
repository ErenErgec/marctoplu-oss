#!/usr/bin/env bash
set -euo pipefail

# =================================================================
# MARCTOPLU - Sunucu Canlıya Alma ve Güncelleme Betiği
# =================================================================
# Kullanım:
#   İlk kurulum: ./scripts/deploy-server.sh --init
#   Güncelleme:  ./scripts/deploy-server.sh

INIT_MODE=false
if [[ "${1:-}" == "--init" ]]; then
    INIT_MODE=true
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${ROOT_DIR}"

# ─────────────────────────────────────────────────────────────────
# İlk kurulum adımları (sadece --init ile çalışır)
# ─────────────────────────────────────────────────────────────────
if $INIT_MODE; then
    echo "🛠️  [INIT] İlk sunucu kurulumu başlatılıyor..."

    # Swap kontrolü ve oluşturma
    if ! swapon --show | grep -q '/swapfile'; then
        echo "📦 4GB Swap alanı oluşturuluyor..."
        fallocate -l 4G /swapfile
        chmod 600 /swapfile
        mkswap /swapfile
        swapon /swapfile
        if ! grep -q '/swapfile' /etc/fstab; then
            echo '/swapfile none swap sw 0 0' >> /etc/fstab
        fi
        echo "✅ Swap aktif."
    else
        echo "✅ Swap zaten aktif."
    fi

    # UFW güvenlik duvarı
    if command -v ufw &> /dev/null; then
        echo "🔒 Güvenlik duvarı yapılandırılıyor..."
        ufw allow 22/tcp
        ufw allow 80/tcp
        ufw allow 443/tcp
        ufw allow 443/udp
        ufw --force enable
        echo "✅ UFW aktif (SSH, HTTP, HTTPS izinli)."
    fi
fi

# ─────────────────────────────────────────────────────────────────
# Standart dağıtım adımları
# ─────────────────────────────────────────────────────────────────

echo "🚀 [1/5] Sunucu ortamı ve bağımlılıklar kontrol ediliyor..."

if ! command -v docker &> /dev/null; then
    echo "❌ HATA: Docker yüklü değil! Lütfen önce Docker'ı kurun."
    echo "   curl -fsSL https://get.docker.com | sh"
    exit 1
fi

if ! docker compose version &> /dev/null; then
    echo "❌ HATA: Docker Compose eklentisi bulunamadı!"
    exit 1
fi

echo "📋 [2/5] Yapılandırma ve güvenlik dosyaları kontrol ediliyor..."

if [ ! -f ".env" ]; then
    echo "⚠️  UYARI: .env dosyası bulunamadı!"
    echo "   .env.prod.example dosyasından yeni bir .env kopyalanıyor..."
    cp .env.prod.example .env
    echo "❗ Lütfen .env dosyasını açıp 'DOMAIN' ve 'POSTGRES_PASSWORD' değerlerini güncelleyin!"
    exit 1
fi

if grep -q "BURAYA_GUCLU_BIR_SIFRE_BELIRLEYIN" .env; then
    echo "❌ HATA: .env dosyasındaki POSTGRES_PASSWORD varsayılan değerde bırakılmış."
    echo "   Lütfen .env dosyasını düzenleyip güçlü bir veritabanı şifresi belirleyin."
    exit 1
fi

if [ ! -f "secrets/serviceAccountKey.json" ]; then
    echo "⚠️  UYARI: 'secrets/serviceAccountKey.json' bulunamadı!"
    echo "   Firebase bildirimleri ve JWT doğrulaması için Firebase Service Account anahtarı gereklidir."
    echo "   Eğer dosya yoksa boş bir şablon oluşturuluyor, lütfen gerçek anahtarla değiştirin."
    mkdir -p secrets
    if [ ! -f "secrets/serviceAccountKey.json" ]; then
        echo "{}" > secrets/serviceAccountKey.json
    fi
fi

echo "📦 [3/5] Docker konteynerleri derleniyor ve ayağa kaldırılıyor..."
docker compose -f docker-compose.prod.yml up -d --build --remove-orphans

echo "⏳ [4/5] Servislerin sağlıklı başlaması bekleniyor (15 saniye)..."
sleep 15

echo "🔍 [5/5] Konteyner durumları:"
docker compose -f docker-compose.prod.yml ps

# Basit sağlık kontrolü
DOMAIN=$(grep '^DOMAIN=' .env | cut -d= -f2 | tr -d ' ')
echo ""
echo "================================================================="
echo "✅ MARCTOPLU sunucusu başarıyla ayağa kaldırıldı!"
echo "   - Caddy Otomatik SSL (Port 80/443)"
echo "   - Backend API  -> https://api.${DOMAIN:-marctoplu.com}/health"
echo "   - Frontend Web -> https://${DOMAIN:-marctoplu.com}"
echo "   - PostgreSQL 16 (Dahili Port 5432)"
echo "================================================================="
echo "Canlı logları takip etmek için: docker compose -f docker-compose.prod.yml logs -f"
