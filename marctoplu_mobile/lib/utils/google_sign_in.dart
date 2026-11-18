import 'dart:io';
import 'package:google_sign_in/google_sign_in.dart';

GoogleSignIn getGoogleSignIn() {
  return GoogleSignIn(
    clientId: Platform.isIOS
        ? '720742633366-7cdtnb51blkvur4685kcgri17pv4g7q1.apps.googleusercontent.com'
        : null,
    serverClientId: '720742633366-568v9v4dgrht2shgbkhgsqludbhl6du4.apps.googleusercontent.com',
  );
}
