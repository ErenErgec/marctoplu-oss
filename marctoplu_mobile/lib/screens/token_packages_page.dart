import 'package:flutter/material.dart';
import 'package:marctoplu_mobile/theme/app_theme.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'dart:io';
import 'dart:async';
import 'package:in_app_purchase/in_app_purchase.dart';
import 'package:marctoplu_mobile/services/api_service.dart';
import 'package:marctoplu_mobile/widgets/link_account_dialog.dart';

class TokenPackagesPage extends StatefulWidget {
  const TokenPackagesPage({super.key});

  @override
  State<TokenPackagesPage> createState() => _TokenPackagesPageState();
}

class _TokenPackagesPageState extends State<TokenPackagesPage> {
  final InAppPurchase _iap = InAppPurchase.instance;
  late StreamSubscription<List<PurchaseDetails>> _subscription;

  static const Set<String> _productIds = {
    'marc_tokens_100',
    'marc_tokens_500',
    'marc_tokens_2000',
  };

  Map<String, ProductDetails> _products = {};
  bool _isStoreAvailable = false;
  bool _isLoading = true;
  bool _isProcessingPurchase = false;

  final List<Map<String, dynamic>> _packages = [
    {
      'id': 'marc_tokens_100',
      'name': '100 Token Paket',
      'tokens': 100,
      'defaultPrice': '199.90 ₺',
      'popular': false,
    },
    {
      'id': 'marc_tokens_500',
      'name': '500 Token Paket',
      'tokens': 500,
      'defaultPrice': '799.90 ₺',
      'popular': true,
    },
    {
      'id': 'marc_tokens_2000',
      'name': '2000 Token Paket',
      'tokens': 2000,
      'defaultPrice': '2.499.90 ₺',
      'popular': false,
    },
  ];

  @override
  void initState() {
    super.initState();
    final purchaseUpdated = _iap.purchaseStream;
    _subscription = purchaseUpdated.listen(
      _onPurchaseUpdate,
      onDone: () => _subscription.cancel(),
      onError: (error) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text('Satın alma dinleme hatası: $error'),
              backgroundColor: Colors.redAccent,
            ),
          );
        }
      },
    );
    _initStore();
  }

  @override
  void dispose() {
    _subscription.cancel();
    super.dispose();
  }

  Future<void> _initStore() async {
    try {
      final available = await _iap.isAvailable();
      if (!available) {
        if (mounted) {
          setState(() {
            _isStoreAvailable = false;
            _isLoading = false;
          });
        }
        return;
      }

      final response = await _iap.queryProductDetails(_productIds);
      if (mounted) {
        setState(() {
          _isStoreAvailable = true;
          _isLoading = false;
          _products = {for (var p in response.productDetails) p.id: p};
        });
      }

      // Android: daha önce doğrulanamadığı için tüketilmemiş satın almaları purchaseStream'e
      // "restored" olarak yeniden getirir; _onPurchaseUpdate onları tekrar doğrular.
      // iOS'ta tamamlanmamış işlemler zaten kendiliğinden yeniden gelir.
      if (Platform.isAndroid) {
        try {
          await _iap.restorePurchases();
        } catch (e) {
          debugPrint('Bekleyen satın almalar sorgulanamadı: $e');
        }
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _isStoreAvailable = false;
          _isLoading = false;
        });
      }
    }
  }

  Future<void> _onPurchaseUpdate(List<PurchaseDetails> purchaseDetailsList) async {
    for (final purchaseDetails in purchaseDetailsList) {
      if (purchaseDetails.status == PurchaseStatus.pending) {
        if (mounted) {
          setState(() => _isProcessingPurchase = true);
        }
      } else {
        if (purchaseDetails.status == PurchaseStatus.error) {
          if (mounted) {
            setState(() => _isProcessingPurchase = false);
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text('Satın alma hatası: ${purchaseDetails.error?.message ?? "İşlem iptal edildi veya başarısız oldu."}'),
                backgroundColor: Colors.redAccent,
              ),
            );
          }
        }

        var shouldComplete = purchaseDetails.status != PurchaseStatus.purchased &&
            purchaseDetails.status != PurchaseStatus.restored;
        if (!shouldComplete) {
          shouldComplete = await _handleSuccessfulPurchase(purchaseDetails);
        }

        // Satın alma yalnızca sunucu token'ları yükledikten sonra tamamlanır (Android'de tüketilir).
        // Doğrulama başarısızsa tamamlanmaz; ekran bir sonraki açılışta tekrar dener.
        if (shouldComplete && purchaseDetails.pendingCompletePurchase) {
          await _iap.completePurchase(purchaseDetails);
        }
      }
    }
  }

  /// Satın almayı sunucuda doğrular. Token'lar yüklendiyse (veya daha önce yüklenmişse) true döner.
  Future<bool> _handleSuccessfulPurchase(PurchaseDetails purchaseDetails) async {
    try {
      final purchaseToken = purchaseDetails.verificationData.serverVerificationData;
      final productId = purchaseDetails.productID;

      final res = await ApiService().verifyIapReceipt(
        productId: productId,
        purchaseToken: purchaseToken,
        receiptData: purchaseDetails.verificationData.localVerificationData,
        transactionId: purchaseDetails.purchaseID,
        platform: Platform.isIOS ? 'ios' : 'android',
      );

      if (mounted) {
        setState(() => _isProcessingPurchase = false);
        showDialog(
          context: context,
          builder: (ctx) => AlertDialog(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
            title: Row(
              children: [
                Icon(Icons.check_circle_rounded, color: context.palette.success, size: 28),
                SizedBox(width: 10),
                Text('Satın Alma Başarılı', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
              ],
            ),
            content: Text(
              res['message'] ?? 'Token bakiyeniz cüzdanınıza yüklendi. Hemen ISBN sorgulamaya başlayabilirsiniz.',
              style: TextStyle(fontSize: 14, color: context.palette.textMuted),
            ),
            actions: [
              ElevatedButton(
                onPressed: () {
                  Navigator.of(ctx).pop();
                  Navigator.of(context).pop();
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF10B981),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                ),
                child: const Text('Tamam', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
              ),
            ],
          ),
        );
      }
      return true;
    } catch (e) {
      if (mounted) {
        setState(() => _isProcessingPurchase = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Doğrulama hatası: $e'),
            backgroundColor: Colors.redAccent,
          ),
        );
      }
      return false;
    }
  }

  Future<void> _buyPackage(Map<String, dynamic> pkg) async {
    final isAnonymous = FirebaseAuth.instance.currentUser?.isAnonymous ?? false;
    if (isAnonymous) {
      showDialog(
        context: context,
        builder: (ctx) => AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
          title: Row(
            children: [
              Icon(Icons.shield_outlined, color: context.palette.accent),
              SizedBox(width: 8),
              Text('Ödeme İçin Hesap Gerekli', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            ],
          ),
          content: Text(
            'Satın aldığınız token\'ların cihaz sıfırlandığında veya silindiğinde kaybolmaması için satın alma öncesinde hesabınızı bir e-posta ile bağlamanız gerekmektedir.',
            style: TextStyle(fontSize: 13, color: context.palette.textSecondary, height: 1.4),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(ctx).pop(),
              child: Text('Vazgeç', style: TextStyle(color: context.palette.textSecondary)),
            ),
            ElevatedButton(
              onPressed: () {
                Navigator.of(ctx).pop();
                showLinkAccountDialog(context);
              },
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.accentFill,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
              ),
              child: const Text('Hesabı Bağla'),
            ),
          ],
        ),
      );
      return;
    }

    final productId = pkg['id'] as String;
    final productDetails = _products[productId];

    if (productDetails != null) {
      setState(() => _isProcessingPurchase = true);
      final purchaseParam = PurchaseParam(productDetails: productDetails);
      await _iap.buyConsumable(purchaseParam: purchaseParam);
    } else {
      showDialog(
        context: context,
        builder: (ctx) => AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
          title: Row(
            children: [
              Icon(Icons.store_mall_directory_outlined, color: context.palette.accent),
              const SizedBox(width: 8),
              Text(
                Platform.isIOS ? 'Apple App Store' : 'Google Play Billing',
                style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
              ),
            ],
          ),
          content: Text(
            '${pkg['name']} için ${Platform.isIOS ? "Apple App Store (StoreKit)" : "Google Play Billing"} entegrasyonu hazırdır.\n\n'
            'Ürün Kimliği: $productId\n'
            'Uygulama ${Platform.isIOS ? "App Store" : "Play Store"}\'a yüklendiğinde ve ürünler onaylandığında ödeme penceresi doğrudan açılacaktır.',
            style: TextStyle(fontSize: 13, color: context.palette.textSecondary, height: 1.5),
          ),
          actions: [
            ElevatedButton(
              onPressed: () => Navigator.of(ctx).pop(),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.accentFill,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
              ),
              child: const Text('Anladım', style: TextStyle(color: Colors.white)),
            ),
          ],
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return Scaffold(
        appBar: AppBar(
          title: const Text('Token Paketleri'),
          centerTitle: true,
        ),
        body: Center(
          child: CircularProgressIndicator(color: context.palette.accent),
        ),
      );
    }

    final isAnonymous = FirebaseAuth.instance.currentUser?.isAnonymous ?? false;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Token Paketleri'),
        centerTitle: true,
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 16),
            child: Icon(
              _isStoreAvailable ? Icons.verified_outlined : Icons.storefront_outlined,
              color: _isStoreAvailable ? context.palette.success : context.palette.textPlaceholder,
              size: 20,
            ),
          ),
        ],
      ),
      body: Stack(
        children: [
          Align(
            alignment: Alignment.topCenter,
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 760),
              child: ListView(
            padding: const EdgeInsets.all(20),
            children: [
              if (isAnonymous)
                Container(
                  margin: const EdgeInsets.only(bottom: 20),
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: context.palette.warningBg,
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(color: context.palette.warning),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Icon(Icons.warning_amber_rounded, color: context.palette.warning),
                          SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              'Misafir Oturumundasınız',
                              style: TextStyle(fontWeight: FontWeight.bold, color: context.palette.warning),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 6),
                      Text(
                        'Satın alacağınız token\'ların kaybolmaması ve cihaz değişiminde korunması için ödeme yapmadan önce lütfen hesabınızı bağlayın.',
                        style: TextStyle(fontSize: 12, color: context.palette.warning, height: 1.4),
                      ),
                      const SizedBox(height: 12),
                      SizedBox(
                        width: double.infinity,
                        child: ElevatedButton.icon(
                          onPressed: () => showLinkAccountDialog(context),
                          icon: const Icon(Icons.link, size: 16),
                          label: const Text('Hesabı Kalıcı Yap (Giriş Yap / Bağla)'),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.accentFill,
                            foregroundColor: Colors.white,
                            elevation: 0,
                            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                            padding: const EdgeInsets.symmetric(vertical: 12),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ..._packages.map((pkg) {
                final isPopular = pkg['popular'] as bool;
                final productId = pkg['id'] as String;
                final productDetails = _products[productId];
                final displayPrice = productDetails?.price ?? pkg['defaultPrice'] as String;

                return Stack(
                  clipBehavior: Clip.none,
                  children: [
                    Container(
                      margin: const EdgeInsets.only(bottom: 16),
                      padding: const EdgeInsets.all(22),
                      decoration: BoxDecoration(
                        color: context.palette.card,
                        borderRadius: BorderRadius.circular(24),
                        border: Border.all(
                          color: isPopular ? context.palette.accent : context.palette.border,
                          width: isPopular ? 2 : 1,
                        ),
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(pkg['name'] as String, style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: context.palette.textMain)),
                              const SizedBox(height: 4),
                              Text('${pkg['tokens']} Bulunan Kayıt', style: TextStyle(color: context.palette.textPlaceholder, fontSize: 13)),
                            ],
                          ),
                          ElevatedButton(
                            onPressed: _isProcessingPurchase ? null : () => _buyPackage(pkg),
                            style: ElevatedButton.styleFrom(
                              backgroundColor: isPopular ? AppColors.accentFill : const Color(0xFFF59E0B),
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                              padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 12),
                            ),
                            child: Text(
                              displayPrice,
                              style: TextStyle(fontWeight: FontWeight.bold, color: context.palette.textMain, fontSize: 13),
                            ),
                          ),
                        ],
                      ),
                    ),
                    if (isPopular)
                      Positioned(
                        top: -10,
                        left: 24,
                        child: Container(
                          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                          decoration: BoxDecoration(
                            color: AppColors.accentFill,
                            borderRadius: BorderRadius.circular(12),
                          ),
                          child: const Text('EN ÇOK TERCİH EDİLEN', style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Colors.white)),
                        ),
                      ),
                  ],
                );
              }),
              const SizedBox(height: 16),
              Center(
                child: TextButton.icon(
                  onPressed: _isProcessingPurchase
                      ? null
                      : () async {
                          setState(() => _isProcessingPurchase = true);
                          try {
                            await _iap.restorePurchases();
                            if (context.mounted) {
                              ScaffoldMessenger.of(context).showSnackBar(
                                const SnackBar(
                                  content: Text('Satın alma geçmişiniz kontrol edildi.'),
                                  backgroundColor: Color(0xFF10B981),
                                ),
                              );
                            }
                          } catch (e) {
                            if (context.mounted) {
                              ScaffoldMessenger.of(context).showSnackBar(
                                SnackBar(
                                  content: Text('Geri yükleme hatası: $e'),
                                  backgroundColor: Colors.redAccent,
                                ),
                              );
                            }
                          } finally {
                            if (mounted) setState(() => _isProcessingPurchase = false);
                          }
                        },
                  icon: Icon(Icons.restore_rounded, size: 18, color: context.palette.textSecondary),
                  label: Text(
                    'Satın Almaları Geri Yükle',
                    style: TextStyle(
                      color: context.palette.textSecondary,
                      fontSize: 13,
                      fontWeight: FontWeight.w500,
                      decoration: TextDecoration.underline,
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 12),
            ],
          ),
        ),
      ),
          if (_isProcessingPurchase)
            Container(
              color: Colors.black.withValues(alpha: 0.4),
              child: Center(
                child: Card(
                  color: context.palette.card,
                  child: Padding(
                    padding: EdgeInsets.all(24.0),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        CircularProgressIndicator(color: context.palette.accent),
                        SizedBox(height: 16),
                        Text('Satın alma işleniyor...', style: TextStyle(fontWeight: FontWeight.bold)),
                      ],
                    ),
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}
