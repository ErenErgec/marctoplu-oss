#!/usr/bin/env bash
set -e

PROJECT_PATH="backend/MarcToken.Api/MarcToken.Api.csproj"
FRONTEND_ENV_PATH="frontend/.env.local"

echo -e "\033[0;36m=== MARC Token Platform Local Setup (Mac/Linux) ===\033[0m"

read -p "ConnectionStrings__Default (Postgres): " dbConn
read -p "Firebase__ProjectId (Firebase Proje ID'niz) [my-firebase-project]: " firebaseProjectId
firebaseProjectId=${firebaseProjectId:-my-firebase-project}

read -p "Iyzico__ApiKey (sandbox veya production): " iyzicoApiKey
read -s -p "Iyzico__SecretKey: " iyzicoSecret
echo ""
read -p "Iyzico__BaseUrl [https://sandbox-api.iyzipay.com]: " iyzicoBaseUrl
iyzicoBaseUrl=${iyzicoBaseUrl:-https://sandbox-api.iyzipay.com}
read -p "Iyzico__CallbackUrl: " iyzicoCallback

read -p "App__FrontendUrl [http://localhost:3000]: " frontendUrl
frontendUrl=${frontendUrl:-http://localhost:3000}
read -p "App__TokenCostPerIsbn [1]: " tokenCost
tokenCost=${tokenCost:-1}
read -p "App__PythonExecutable [python3]: " pythonExec
pythonExec=${pythonExec:-python3}

dotnet user-secrets --project "$PROJECT_PATH" set "ConnectionStrings:Default" "$dbConn"
dotnet user-secrets --project "$PROJECT_PATH" set "Firebase:ProjectId" "$firebaseProjectId"
dotnet user-secrets --project "$PROJECT_PATH" set "Iyzico:ApiKey" "$iyzicoApiKey"
dotnet user-secrets --project "$PROJECT_PATH" set "Iyzico:SecretKey" "$iyzicoSecret"
dotnet user-secrets --project "$PROJECT_PATH" set "Iyzico:BaseUrl" "$iyzicoBaseUrl"
dotnet user-secrets --project "$PROJECT_PATH" set "Iyzico:CallbackUrl" "$iyzicoCallback"
dotnet user-secrets --project "$PROJECT_PATH" set "App:FrontendUrl" "$frontendUrl"
dotnet user-secrets --project "$PROJECT_PATH" set "App:TokenCostPerIsbn" "$tokenCost"
dotnet user-secrets --project "$PROJECT_PATH" set "App:PythonExecutable" "$pythonExec"

echo -e "NEXT_PUBLIC_API_BASE_URL=http://localhost:8080" > "$FRONTEND_ENV_PATH"
echo -e "NEXT_PUBLIC_FIREBASE_API_KEY=\"YOUR_FIREBASE_API_KEY\"" >> "$FRONTEND_ENV_PATH"
echo -e "NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=\"marctoplu.firebaseapp.com\"" >> "$FRONTEND_ENV_PATH"
echo -e "NEXT_PUBLIC_FIREBASE_PROJECT_ID=\"marctoplu\"" >> "$FRONTEND_ENV_PATH"
echo -e "NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET=\"marctoplu.firebasestorage.app\"" >> "$FRONTEND_ENV_PATH"
echo -e "NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=\"720742633366\"" >> "$FRONTEND_ENV_PATH"
echo -e "NEXT_PUBLIC_FIREBASE_APP_ID=\"1:720742633366:web:0f21b2c188157d2fe517b7\"\n" >> "$FRONTEND_ENV_PATH"

echo ""
echo -e "\033[0;32mKurulum tamamlandi.\033[0m"
echo -e "\033[0;32mBackend user-secrets kaydedildi.\033[0m"
echo -e "\033[0;32mFrontend dosyasi olusturuldu: $FRONTEND_ENV_PATH\033[0m"
echo ""
echo -e "\033[0;33mCalistirma komutlari:\033[0m"
echo "1) cd backend/MarcToken.Api && dotnet run"
echo "2) cd frontend && npm install && npm run dev"
echo "Not: Python bagimliliklarini 'pip install -r engine/requirements.txt' ile kurmayi unutmayin."
