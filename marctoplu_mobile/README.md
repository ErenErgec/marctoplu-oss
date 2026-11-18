# MARCTOPLU Mobil (Flutter)

iOS ve Android için MARCTOPLU istemcisi: kamera ile ISBN tarama, Excel ile toplu iş, MARC önizleme, IAP ile token satın alma, FCM bildirimleri.

```bash
flutter pub get
flutter run --dart-define=API_BASE_URL=http://localhost:8080
```

- `API_BASE_URL` verilmezse uygulama resmi sunucuya (`https://api.marctoplu.com`) bağlanır.
- Android emülatöründe bilgisayarınızdaki backend'e `http://10.0.2.2:8080`, gerçek cihazda bilgisayarınızın yerel ağ IP'si ile ulaşılır.

Release derlemeleri için [../docs/DEPLOYMENT.md](../docs/DEPLOYMENT.md) §7'ye bakın.

## Kendi kurulumunuz için

Depodaki Firebase yapılandırması resmi MARCTOPLU projesine aittir ve public kopyada API anahtarları `YOUR_FIREBASE_API_KEY` ile maskelenmiştir. Kendi backend'inizle çalışmak için:

1. **Firebase:** Kendi projenizi bağlayın. [FlutterFire CLI](https://firebase.google.com/docs/flutter/setup) ile:
   ```bash
   dart pub global activate flutterfire_cli
   flutterfire configure --project=<firebase-proje-id>
   ```
   Bu komut `lib/firebase_options.dart`, `android/app/google-services.json` ve `ios/Runner/GoogleService-Info.plist` dosyalarını yeniden üretir. Backend'in `Firebase:ProjectId` değeri aynı proje olmalıdır.
2. **Uygulama kimliği:** Mağazada yayınlayacaksanız `com.marctoplu.app` paket adını/bundle ID'sini kendinizinkiyle değiştirin (`android/app/build.gradle.kts` → `namespace`/`applicationId`, Kotlin paket klasörü, Xcode'da `PRODUCT_BUNDLE_IDENTIFIER`). Aynı değeri backend'e `IAP_GOOGLE_PLAY_PACKAGE_NAME` / `IAP_APPLE_BUNDLE_ID` olarak verin.
3. **Uygulama içi ürünler:** Mağaza ürün kimlikleri (`marc_tokens_100`, `marc_tokens_500`, `marc_tokens_2000`) hem `lib/screens/token_packages_page.dart` hem de backend'deki `InAppPurchasesController.ProductTokens` sözlüğünde tanımlıdır. Play Console ve App Store Connect'te aynı kimliklerle tüketilebilir ürün oluşturun ya da iki yeri birlikte güncelleyin.
4. **Ad ve logo:** "MARCTOPLU" adı ve logosu AGPL kapsamı dışındadır; yayınlayacağınız uygulamada kendi ad ve simgelerinizi kullanın ([../TRADEMARK.md](../TRADEMARK.md)).
