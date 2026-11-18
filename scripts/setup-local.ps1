param(
    [string]$ProjectPath = "backend/MarcToken.Api/MarcToken.Api.csproj",
    [string]$FrontendEnvPath = "frontend/.env.local"
)

$ErrorActionPreference = "Stop"

Write-Host "=== MARC Token Platform Local Setup ===" -ForegroundColor Cyan

$dbConn = Read-Host "ConnectionStrings__Default (Postgres)"
$firebaseProjectId = Read-Host "Firebase__ProjectId (Firebase Proje ID'niz)"
if ([string]::IsNullOrWhiteSpace($firebaseProjectId)) { $firebaseProjectId = "my-firebase-project" }

$iyzicoApiKey = Read-Host "Iyzico__ApiKey (sandbox veya production)"
$iyzicoSecret = Read-Host "Iyzico__SecretKey"
$iyzicoBaseUrl = Read-Host "Iyzico__BaseUrl [https://sandbox-api.iyzipay.com]"
if ([string]::IsNullOrWhiteSpace($iyzicoBaseUrl)) { $iyzicoBaseUrl = "https://sandbox-api.iyzipay.com" }
$iyzicoCallback = Read-Host "Iyzico__CallbackUrl"

$frontendUrl = Read-Host "App__FrontendUrl [http://localhost:3000]"
if ([string]::IsNullOrWhiteSpace($frontendUrl)) { $frontendUrl = "http://localhost:3000" }
$tokenCost = Read-Host "App__TokenCostPerIsbn [1]"
if ([string]::IsNullOrWhiteSpace($tokenCost)) { $tokenCost = "1" }
$pythonExec = Read-Host "App__PythonExecutable [python]"
if ([string]::IsNullOrWhiteSpace($pythonExec)) { $pythonExec = "python" }

dotnet user-secrets --project "$ProjectPath" set "ConnectionStrings:Default" "$dbConn"
dotnet user-secrets --project "$ProjectPath" set "Firebase:ProjectId" "$firebaseProjectId"
dotnet user-secrets --project "$ProjectPath" set "Iyzico:ApiKey" "$iyzicoApiKey"
dotnet user-secrets --project "$ProjectPath" set "Iyzico:SecretKey" "$iyzicoSecret"
dotnet user-secrets --project "$ProjectPath" set "Iyzico:BaseUrl" "$iyzicoBaseUrl"
dotnet user-secrets --project "$ProjectPath" set "Iyzico:CallbackUrl" "$iyzicoCallback"
dotnet user-secrets --project "$ProjectPath" set "App:FrontendUrl" "$frontendUrl"
dotnet user-secrets --project "$ProjectPath" set "App:TokenCostPerIsbn" "$tokenCost"
dotnet user-secrets --project "$ProjectPath" set "App:PythonExecutable" "$pythonExec"

Set-Content -Path "$FrontendEnvPath" -Value "NEXT_PUBLIC_API_BASE_URL=http://localhost:8080`nNEXT_PUBLIC_FIREBASE_API_KEY=YOUR_FIREBASE_API_KEY`nNEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=marctoplu.firebaseapp.com`nNEXT_PUBLIC_FIREBASE_PROJECT_ID=marctoplu`nNEXT_PUBLIC_FIREBASE_STORAGE_BUCKET=marctoplu.firebasestorage.app`nNEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=720742633366`nNEXT_PUBLIC_FIREBASE_APP_ID=1:720742633366:web:0f21b2c188157d2fe517b7`n" -Encoding ascii

Write-Host "" 
Write-Host "Kurulum tamamlandi." -ForegroundColor Green
Write-Host "Backend user-secrets kaydedildi." -ForegroundColor Green
Write-Host "Frontend dosyasi olusturuldu: $FrontendEnvPath" -ForegroundColor Green
Write-Host ""
Write-Host "Calistirma komutlari:" -ForegroundColor Yellow
Write-Host "1) dotnet run --project backend/MarcToken.Api/MarcToken.Api.csproj"
Write-Host "2) cd frontend; npm run dev"
