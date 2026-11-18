# Mimari ve akışlar

```
 Web (Next.js) ─┐                       ┌─ PostgreSQL (EF Core)
                ├─ HTTPS/JWT ─► API ────┤
 Mobil (Flutter)┘   (Caddy)   (.NET 8)  └─ uploads/ (girdi/çıktı dosyaları)
                                 │
                   JobWorkerService ─► python job_runner.py ─► engine/marc_toplu_sorgula.py ─► LoC / TO-KAT / OpenLibrary / Google Books
                                 │
                            FCM push (Firebase Admin)
```
Prod: `docker-compose.prod.yml` içinde caddy, postgres, backend (python3 + /engine dahil) ve frontend (standalone) servisleri var. `DOMAIN` → frontend, `api.DOMAIN` → backend.

## İş (Job) yaşam döngüsü
1. `POST /api/jobs/upload` veya `from-isbns`: `.xlsx` kaydedilir, benzersiz ISBN sayılır (`JobFileInspector.CountIsbns`). `estimatedTokens = ISBN × TokenCostPerIsbn` **peşin düşülür** (`job_consume_upfront`). Bakiye yetersizse iş silinir ve hata döner.
2. Durum `queued` olur. `JobWorkerService` işi `FOR UPDATE SKIP LOCKED` ile alır ve `running` yapar. En fazla `MaxParallelJobs` iş paralel çalışır.
3. `PythonJobRunner`, `job_runner.py` sürecini başlatır (`App:IsbnCacheEnabled` ise `--cache-path uploads/_cache/isbn_cache.sqlite` ile; önbellekte taze kaydı olan ISBN dış kaynaklara sorulmaz, bkz. engine/marc_toplu_sorgula.py). stdout'taki progress satırlarıyla `ProcessedIsbns` ve `ConsumedTokens` (= şu ana kadar **bulunan** × cost) canlı güncellenir. İstemciler 2 sn'de bir sorgular.
4. Bitişte **yalnızca bulunan ISBN'ler** ücretlendirilir: çıktı dosyası varsa `consumed = foundCount × cost`, yoksa (iptal öncesi çıktı yazılmadı, hata) `consumed = 0`. `estimated − consumed` iade edilir (`job_refund` / `job_refund_cancel_running`). Durum `completed`, `failed` veya `cancelled` olur. FCM bildirimi gönderilir.
5. İptal: `queued` iş hemen iade edilir. `running` işte cancel flag dosyası yazılır; runner süreci öldürür ve 4. adım uygulanır.
6. `RunningJobTimeoutSeconds` boyunca güncellenmeyen `running` işler, bir sonraki kuyruk taramasında `failed` yapılır ve tam iade edilir (`ConsumedTokens` sıfırlanır). İşleme sırasında beklenmeyen hata da (`job_refund_failed`) tam iade eder.
7. İndirme: `format=xlsx` motorun çıktısını, `format=ris` `.ris` dosyasını (yoksa Excel'den üretilir), `format=mrc` / `format=marcxml` motorun yazdığı `.mrc` / `.xml` MARC21 dosyalarını döndürür. MARC dosyası yoksa (özellikten önce işlenmiş iş) 400 ve açıklayıcı mesaj döner. Dosyalar `FileRetentionDays` sonra silinir.

## Token ekonomisi
- Bakiye `Wallet` tablosunda, her hareket `TokenLedger` tablosunda tutulur. Tüm değişiklikler `TokenService` üzerinden yapılır.
- Ücret kuralı: 1 **bulunan** kayıt = `TokenCostPerIsbn` token. Bulunamayan (`not_found`) ve sorgulanamayan (`error`) ISBN'ler ile çıktısız biten işler ücretlendirilmez. Önbellekten gelen bulunmuş kayıt normal ücretlidir. Web giriş/kayıt sayfaları ve `/kullanim-kosullari` bu kuralı anlatır; değiştirirseniz orayı da güncelleyin.
- Kaynaklar: mobil cihaz denemesi (`FreeTrialTokens`, cihaz ve IP/gün limitli, `web_` önekli cihaz kimlikleri hariç), IAP, admin tanımlaması (iyzico askıda).

## Ödeme akışları
- **Mobil IAP:** Store satın alması → `POST /api/payments/iap/verify-receipt {platform, productId, purchaseToken, transactionId}`. Misafirler reddedilir, token miktarı `ProductTokens` sözlüğünden (controller'da sabit) gelir.
  - Doğrulama: Android'de Google Play Developer API (`purchaseState == 0`), iOS'ta App Store Server API (bundleId + productId eşleşmeli, `revocationDate` olmamalı; production'da yoksa sandbox'ta aranır).
  - Kredi: `TokenService.CreditOnceAsync` ile `iap:android:<purchaseToken>` / `iap:ios:<transactionId>` reason'ı altında **bir kez** yüklenir. Aynı reason için `pg_advisory_xact_lock` kullanıldığından paralel istekler çift kredi alamaz.
  - Fiyat (yalnızca admin istatistiği için, ledger satırının `metadata` jsonb alanında): iOS'ta App Store işlem bilgisindeki `price` (×1000), `currency`, `storefront`; Android'de doğrulamadan sonra Orders API (`GET applications/{pkg}/orders/{orderId}`) ile `total`, `tax`, `developerRevenueInBuyerCurrency` (hepsi ×1000 tam sayı: `priceMilli`, `taxMilli`, `proceedsMilli`) ve `regionCode`. Her iki platformda `environment` yazılır (iOS `Production`/`Sandbox`; Android `Production`/`Test`/`Promo`/`Rewarded`). Admin paneli **Satın Almalar** sekmesi (`GET /api/admin/purchases`) her satın almayı kullanıcı, paket, tutar, durum (geçerli/iade), ortam ve sipariş/işlem numarasıyla listeler. Admin istatistiği yalnızca `Production` kayıtları para birimine göre toplar (brüt: vergi ve mağaza komisyonu düşülmemiş). Orders API çağrısı en iyi çaba ile yapılır (5 sn zaman aşımı); başarısız olursa kredi yine yüklenir, yalnızca fiyat boş kalır. Service account'un Play Console'da "finansal verileri görüntüleme" izni olmalı. Bu özellikten önceki satın almaların fiyatı yok.
  - Yanıtlar: geçersiz → 400, anahtar yok / mağaza erişilemez → 503 (tekrar denenebilir), daha önce işlenmiş → 200 `creditedTokens: 0`.
  - Mobil istemci satın almayı (`completePurchase`, Android'de tüketim) **yalnızca 200 yanıtından sonra** tamamlar. Başarısızsa satın alma tüketilmez; Android'de paketler ekranı her açıldığında `restorePurchases` ile yeniden doğrulanır. Google, 3 gün içinde onaylanmayan satın almaları otomatik iade eder.
- **İadeler (webhook):** Google RTDN (Pub/Sub push → `POST /api/payments/iap/webhooks/google`, `voidedPurchaseNotification`) ve App Store Server Notifications V2 (`POST /api/payments/iap/webhooks/apple`, `REFUND` / `REFUND_REVERSED`). Uç noktalar anonimdir; bildirimin imzası doğrulanmaz, yalnızca tetikleyicidir. `IapRefundService` iadeyi mağaza API'sinden doğrular: Android'de Voided Purchases API (son 30 gün), iOS'ta işlemdeki `revocationDate`. Ardından `TokenService.DebitOnceAsync` ile `iap_refund:<platform>:<ref>` reason'ı altında satın alınan token miktarını **bir kez** düşer. Bakiye eksiye inebilir; eksi bakiyede iş başlatılamaz. İade geri alınırsa `iap_refund_reversed:<platform>:<ref>` ile tokenler geri yüklenir. Bildirim iade dediği halde mağaza API'si henüz göstermiyorsa ya da mağazaya ulaşılamıyorsa 503 dönülür ve mağaza tekrar dener. Admin istatistiği ve Satın Almalar sekmesi iade edilenleri gelirden düşer. Kurulum: DEPLOYMENT.md §9.5.
- **Web iyzico (askıda, ileride gerekirse entegre edilecek):** `checkout/init` ile `Payment` kaydı ve iyzico checkout URL'si oluşur. iyzico `callback` → `FinalizeAndCreditCheckoutAsync` → kredi yüklenir → düz metin yanıt döner (frontend'e yönlendirme yok). Web arayüzü şu an kullanıcıyı mobile yönlendiriyor.

## Destek talepleri
Web'deki "Destek & Sorun Bildir" formu `POST /api/support/tickets` ile `support_tickets` tablosuna yazar (kullanıcı başına en fazla 10 açık talep). Admin panelindeki **Destek** sekmesi talepleri listeler, çözüldü olarak işaretler ve telafi tokeni formunu doldurur. Talepler e-posta ve mesaj içerdiği için hesap silme ve misafir temizliğinde silinir.

## Kötüye kullanım önlemleri
- **İş durum geçişleri kilitli:** İptal, iş sonu, hata ve zaman aşımı kurtarma `JobService.WithLockedJobAsync` içinde `SELECT … FOR UPDATE` ile yapılır. Paralel iptal istekleri ya da iptal ile tamamlanmanın çakışması aynı işi iki kez iade edemez. Yeni bir durum geçişi eklerken bu yardımcıyı kullanın.
- **İş oluşturma tek transaction:** İş kaydı ve `job_consume_upfront` düşümü birlikte commit edilir; ödemesi alınmamış iş kuyrukta görünmez.
- **Ön kontroller:** Excel ayrıştırılmadan önce bakiye ≥ 1 ücret aranır (`TokenService.PeekBalanceAsync`; cüzdan oluşturmaz, deneme hakkını bozmaz). .xlsx açılmış boyutu 200 MB ve 2000 dosyayla sınırlıdır. İş başına en fazla `App:MaxIsbnsPerJob` ISBN kabul edilir.
- **IAP:** Her mağaza işlemi bir kez kredilendirilir. İadeler (Google'ın 3 gün içinde tamamlanmayan satın almaları otomatik iadesi dahil, `voidedReason` 8) webhook ile düşülür ve bakiye eksiye inebilir.
- **Bilinen sınırlar:** Hesap silinince cüzdan ve ledger bağlantısı silindiği için eksi bakiye yeni hesaba taşınmaz. Silinmiş hesabın iadesi yalnızca loglanır. Ücretsiz deneme, istemcinin gönderdiği `X-Device-Id` ve IP/gün sınırıyla korunur; cihaz kimliği taklit edilebilir, kayıp en fazla `FreeTrialTokens` × `MaxTrialsPerIpPerDay` / IP / gün olur.

## Rate limiting
`Infrastructure/RateLimitPolicies.cs`, kullanıcıya (yoksa IP'ye) göre sabit pencere uygular: iş oluşturma 10/dk, IAP doğrulama 30/dk, mağaza iade bildirimi 60/dk (IP başına), destek talebi 5/10 dk. Aşılırsa 429 + `Retry-After` + Türkçe `message` döner.

## Kimlik
Firebase ID token JWT olarak doğrulanır (issuer/audience = `Firebase:ProjectId`). `UserContext.GetUserIdOrThrow` Firebase UID'yi MD5 ile deterministik bir `Guid`'e çevirir. Tüm tablolar bu Guid'i kullanır. Anonim kullanıcılar `GuestCleanupService` tarafından `GuestRetentionDays` sonra Firebase'den ve veritabanından silinir.

## Bilinen sorunlar / teknik borç
Refactor sırasında fark edildi, davranış değiştirilmedi:
- App Store yanıtındaki `signedTransactionInfo` JWS imzası Apple kök sertifikasına karşı doğrulanmıyor. Yanıt, kimliği doğrulanmış TLS isteğiyle doğrudan Apple'dan alındığı için kabul edilebilir.
- `AppleAllowSandbox=true` iken TestFlight kullanıcıları sandbox'ta ücretsiz satın alma yapıp gerçek token alabilir. App Review için gerekli; TestFlight grubunu dar tutun.
- `Migrations/20260908130000_AddEmailToUserProfile.cs` dosyasının `.Designer.cs` eşi yok, bu yüzden EF onu görmüyor. Sütun `Program.cs`'teki `ALTER ... IF NOT EXISTS` ile eklenir. `20260925..._AddSupportTickets` snapshot'ı bu sütunla hizaladı (yine `IF NOT EXISTS` ile).
- Mobilde `TokenWalletPage` hiçbir yerden açılmıyor (ölü kod).
- `job_runner.py` motoru göreli yolla (`../../../engine`) buluyor. Klasör yapısı değişirse kırılır.
- Frontend lint hatasız geçer; yalnızca `<img>` kullanımı için `@next/next/no-img-element` uyarıları var.
- Backend ve frontend için otomatik test yok; motorun birim testleri `engine/tests/` altında, mobilde yalnızca smoke test var.
