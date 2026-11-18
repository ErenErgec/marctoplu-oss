import 'package:firebase_core/firebase_core.dart' show FirebaseOptions;
import 'package:flutter/foundation.dart'
    show defaultTargetPlatform, kIsWeb, TargetPlatform;

/// Default [FirebaseOptions] for use with your Firebase apps.
/// Configured for marctoplu project across Android, iOS, and Web.
class DefaultFirebaseOptions {
  static FirebaseOptions get currentPlatform {
    if (kIsWeb) {
      return web;
    }
    switch (defaultTargetPlatform) {
      case TargetPlatform.android:
        return android;
      case TargetPlatform.iOS:
        return ios;
      default:
        throw UnsupportedError(
          'DefaultFirebaseOptions are not configured for this platform.',
        );
    }
  }

  static const FirebaseOptions web = FirebaseOptions(
    apiKey: 'YOUR_FIREBASE_API_KEY',
    appId: '1:720742633366:web:0f21b2c188157d2fe517b7',
    messagingSenderId: '720742633366',
    projectId: 'marctoplu',
    authDomain: 'marctoplu.firebaseapp.com',
    storageBucket: 'marctoplu.firebasestorage.app',
  );

  static const FirebaseOptions android = FirebaseOptions(
    apiKey: 'YOUR_FIREBASE_API_KEY',
    appId: '1:720742633366:android:cc5301ada6a9aad4e517b7',
    messagingSenderId: '720742633366',
    projectId: 'marctoplu',
    storageBucket: 'marctoplu.firebasestorage.app',
  );

  static const FirebaseOptions ios = FirebaseOptions(
    apiKey: 'YOUR_FIREBASE_API_KEY',
    appId: '1:720742633366:ios:559ea0c0c7c03d44e517b7',
    messagingSenderId: '720742633366',
    projectId: 'marctoplu',
    storageBucket: 'marctoplu.firebasestorage.app',
    iosBundleId: 'com.marctoplu.app',
  );
}
