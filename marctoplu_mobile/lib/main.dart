import 'package:flutter/material.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:marctoplu_mobile/firebase_options.dart';
import 'package:marctoplu_mobile/theme/app_theme.dart';
import 'package:marctoplu_mobile/screens/auth_gate.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  try {
    if (Firebase.apps.isEmpty) {
      await Firebase.initializeApp(
        options: DefaultFirebaseOptions.currentPlatform,
      );
    }
  } catch (e) {
    debugPrint("Firebase initializeApp handled exception: $e");
  }
  runApp(const MarcTopluMobileApp());
}

class MarcTopluMobileApp extends StatelessWidget {
  const MarcTopluMobileApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'MARCTOPLU',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.lightTheme,
      darkTheme: AppTheme.darkTheme,
      themeMode: ThemeMode.system,
      home: const AuthGate(),
    );
  }
}
