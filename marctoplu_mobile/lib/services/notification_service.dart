import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:marctoplu_mobile/services/api_service.dart';

@pragma('vm:entry-point')
Future<void> _firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  // Arka planda gelen mesajı işle
  debugPrint("Arka planda mesaj alındı: ${message.messageId}");
}

class NotificationService {
  static final NotificationService _instance = NotificationService._internal();

  factory NotificationService() => _instance;

  NotificationService._internal();

  final FirebaseMessaging _messaging = FirebaseMessaging.instance;
  bool _initialized = false;

  Future<void> initialize() async {
    if (_initialized) {
      // Zaten ilklendirildiyse sadece token senkronizasyonu yap
      await syncToken();
      return;
    }
    _initialized = true;

    try {
      // Arka plan dinleyicisi
      FirebaseMessaging.onBackgroundMessage(_firebaseMessagingBackgroundHandler);

      // İzin iste (iOS ve Android 13+)
      NotificationSettings settings = await _messaging.requestPermission(
        alert: true,
        badge: true,
        sound: true,
      );

      if (settings.authorizationStatus == AuthorizationStatus.authorized) {
        debugPrint('Kullanıcı bildirim izni verdi.');
      } else {
        debugPrint('Kullanıcı bildirim izni vermedi.');
      }

      // Token'ı al ve sunucuya gönder
      await syncToken();

      // Token değiştiğinde de sunucuyu güncelle
      _messaging.onTokenRefresh.listen((newToken) {
        debugPrint("FCM Token yenilendi: $newToken");
        ApiService().updateFcmToken(newToken);
      });

      // Ön planda (uygulama açıkken) mesaj gelirse
      FirebaseMessaging.onMessage.listen((RemoteMessage message) {
        debugPrint('Ön planda bildirim geldi: ${message.notification?.title} - ${message.notification?.body}');
      });
    } catch (e) {
      debugPrint("NotificationService initialize hatası: $e");
    }
  }

  Future<void> syncToken() async {
    try {
      String? token = await _messaging.getToken();
      if (token != null && token.isNotEmpty) {
        debugPrint("FCM Token senkronize ediliyor: $token");
        await ApiService().updateFcmToken(token);
      }
    } catch (e) {
      debugPrint("FCM Token senkronizasyon hatası: $e");
    }
  }
}
