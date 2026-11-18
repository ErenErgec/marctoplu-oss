#!/usr/bin/env bash
set -e

# Backend sırlarını dotnet user-secrets'a yazar ve frontend/.env.local dosyasını oluşturur.
# Firebase değerleri: Firebase Console → Proje Ayarları → Genel → Web uygulaması yapılandırması.
# Backend ve web aynı Firebase projesini kullanmalıdır (JWT audience = proje ID'si).

PROJECT_PATH="backend/MarcToken.Api/MarcToken.Api.csproj"
FRONTEND_ENV_PATH="frontend/.env.local"

ask() { # ask <değişken> <soru> [varsayılan]
    local answer
    if [[ -n "${3:-}" ]]; then
        read -r -p "$2 [$3]: " answer
        answer=${answer:-$3}
    else
        read -r -p "$2: " answer
    fi
    printf -v "$1" '%s' "$answer"
}

echo -e "\033[0;36m=== MARCTOPLU Yerel Kurulum (Mac/Linux) ===\033[0m"

ask dbConn "ConnectionStrings__Default (Postgres)" "Host=localhost;Port=5433;Database=marctoken;Username=postgres;Password=postgres"
ask adminEmails "App__AdminEmails (virgülle ayrılmış, boş bırakılabilir)" ""
ask frontendUrl "App__FrontendUrl" "http://localhost:3000"
ask pythonExec "App__PythonExecutable" "python3"

echo ""
echo "Firebase web uygulaması yapılandırması:"
ask firebaseProjectId "Proje ID (projectId)"
while [[ -z "$firebaseProjectId" ]]; do ask firebaseProjectId "Proje ID (projectId) zorunlu"; done
ask firebaseApiKey "API anahtarı (apiKey)"
ask firebaseAuthDomain "authDomain" "${firebaseProjectId}.firebaseapp.com"
ask firebaseBucket "storageBucket" "${firebaseProjectId}.firebasestorage.app"
ask firebaseSenderId "messagingSenderId"
ask firebaseAppId "appId"

dotnet user-secrets --project "$PROJECT_PATH" set "ConnectionStrings:Default" "$dbConn"
dotnet user-secrets --project "$PROJECT_PATH" set "Firebase:ProjectId" "$firebaseProjectId"
dotnet user-secrets --project "$PROJECT_PATH" set "App:AdminEmails" "$adminEmails"
dotnet user-secrets --project "$PROJECT_PATH" set "App:FrontendUrl" "$frontendUrl"
dotnet user-secrets --project "$PROJECT_PATH" set "App:PythonExecutable" "$pythonExec"

cat > "$FRONTEND_ENV_PATH" <<EOF
NEXT_PUBLIC_API_BASE_URL=http://localhost:8080
NEXT_PUBLIC_FIREBASE_API_KEY="${firebaseApiKey}"
NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN="${firebaseAuthDomain}"
NEXT_PUBLIC_FIREBASE_PROJECT_ID="${firebaseProjectId}"
NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET="${firebaseBucket}"
NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID="${firebaseSenderId}"
NEXT_PUBLIC_FIREBASE_APP_ID="${firebaseAppId}"
EOF

echo ""
echo -e "\033[0;32mKurulum tamamlandı.\033[0m"
echo -e "\033[0;32mBackend user-secrets kaydedildi.\033[0m"
echo -e "\033[0;32mFrontend dosyası oluşturuldu: $FRONTEND_ENV_PATH\033[0m"
echo ""
echo -e "\033[0;33mÇalıştırma komutları:\033[0m"
echo "0) docker compose up -d postgres        # yerel PostgreSQL (localhost:5433)"
echo "1) pip install -r engine/requirements.txt"
echo "2) dotnet run --project $PROJECT_PATH"
echo "3) cd frontend && npm install && npm run dev"
echo "Not: Push bildirimleri ve misafir temizliği için Firebase servis hesabı anahtarını"
echo "     secrets/serviceAccountKey.json olarak koyup GOOGLE_APPLICATION_CREDENTIALS ile gösterin."
