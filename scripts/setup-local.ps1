param(
    [string]$ProjectPath = "backend/MarcToken.Api/MarcToken.Api.csproj",
    [string]$FrontendEnvPath = "frontend/.env.local"
)

# Backend sırlarını dotnet user-secrets'a yazar ve frontend/.env.local dosyasını oluşturur.
# Firebase değerleri: Firebase Console -> Proje Ayarları -> Genel -> Web uygulaması yapılandırması.
# Backend ve web aynı Firebase projesini kullanmalıdır (JWT audience = proje ID'si).

$ErrorActionPreference = "Stop"

function Ask([string]$Prompt, [string]$Default = "") {
    if ($Default) {
        $answer = Read-Host "$Prompt [$Default]"
        if ([string]::IsNullOrWhiteSpace($answer)) { return $Default }
        return $answer
    }
    return Read-Host $Prompt
}

Write-Host "=== MARCTOPLU Yerel Kurulum (Windows) ===" -ForegroundColor Cyan

$dbConn = Ask "ConnectionStrings__Default (Postgres)" "Host=localhost;Port=5433;Database=marctoken;Username=postgres;Password=postgres"
$adminEmails = Ask "App__AdminEmails (virgülle ayrılmış, boş bırakılabilir)"
$frontendUrl = Ask "App__FrontendUrl" "http://localhost:3000"
$pythonExec = Ask "App__PythonExecutable" "python"

Write-Host ""
Write-Host "Firebase web uygulaması yapılandırması:"
$firebaseProjectId = Ask "Proje ID (projectId)"
while ([string]::IsNullOrWhiteSpace($firebaseProjectId)) { $firebaseProjectId = Ask "Proje ID (projectId) zorunlu" }
$firebaseApiKey = Ask "API anahtarı (apiKey)"
$firebaseAuthDomain = Ask "authDomain" "$firebaseProjectId.firebaseapp.com"
$firebaseBucket = Ask "storageBucket" "$firebaseProjectId.firebasestorage.app"
$firebaseSenderId = Ask "messagingSenderId"
$firebaseAppId = Ask "appId"

dotnet user-secrets --project "$ProjectPath" set "ConnectionStrings:Default" "$dbConn"
dotnet user-secrets --project "$ProjectPath" set "Firebase:ProjectId" "$firebaseProjectId"
dotnet user-secrets --project "$ProjectPath" set "App:AdminEmails" "$adminEmails"
dotnet user-secrets --project "$ProjectPath" set "App:FrontendUrl" "$frontendUrl"
dotnet user-secrets --project "$ProjectPath" set "App:PythonExecutable" "$pythonExec"

$envLines = @(
    "NEXT_PUBLIC_API_BASE_URL=http://localhost:8080",
    "NEXT_PUBLIC_FIREBASE_API_KEY=$firebaseApiKey",
    "NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=$firebaseAuthDomain",
    "NEXT_PUBLIC_FIREBASE_PROJECT_ID=$firebaseProjectId",
    "NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET=$firebaseBucket",
    "NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=$firebaseSenderId",
    "NEXT_PUBLIC_FIREBASE_APP_ID=$firebaseAppId"
)
Set-Content -Path "$FrontendEnvPath" -Value $envLines -Encoding utf8

Write-Host ""
Write-Host "Kurulum tamamlandı." -ForegroundColor Green
Write-Host "Backend user-secrets kaydedildi." -ForegroundColor Green
Write-Host "Frontend dosyası oluşturuldu: $FrontendEnvPath" -ForegroundColor Green
Write-Host ""
Write-Host "Çalıştırma komutları:" -ForegroundColor Yellow
Write-Host "0) docker compose up -d postgres        # yerel PostgreSQL (localhost:5433)"
Write-Host "1) pip install -r engine/requirements.txt"
Write-Host "2) dotnet run --project $ProjectPath"
Write-Host "3) cd frontend; npm install; npm run dev"
Write-Host "Not: Push bildirimleri ve misafir temizliği için Firebase servis hesabı anahtarını"
Write-Host "     secrets/serviceAccountKey.json olarak koyup GOOGLE_APPLICATION_CREDENTIALS ile gösterin."
