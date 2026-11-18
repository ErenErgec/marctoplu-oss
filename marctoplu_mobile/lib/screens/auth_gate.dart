import 'package:flutter/material.dart';
import 'package:marctoplu_mobile/theme/app_theme.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'package:marctoplu_mobile/screens/login_page.dart';
import 'package:marctoplu_mobile/screens/dashboard_page.dart';

class AuthGate extends StatelessWidget {
  const AuthGate({super.key});

  @override
  Widget build(BuildContext context) {
    return StreamBuilder<User?>(
      stream: FirebaseAuth.instance.authStateChanges(),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return Scaffold(
            body: Center(child: CircularProgressIndicator(color: context.palette.accent)),
          );
        }
        
        final user = snapshot.data;
        if (user == null) {
          return const LoginPage();
        }
        
        return const DashboardPage();
      },
    );
  }
}
