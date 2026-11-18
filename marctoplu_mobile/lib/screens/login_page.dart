import 'package:flutter/material.dart';
import 'package:marctoplu_mobile/theme/app_theme.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'dart:io';
import 'dart:async';
import 'package:google_sign_in/google_sign_in.dart';
import 'package:marctoplu_mobile/utils/google_sign_in.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _nameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _isLoading = false;
  bool _obscurePassword = true;
  bool _isRegister = false;
  bool _showEmailForm = false;
  String _error = '';

  @override
  void dispose() {
    _nameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _login() async {
    if (_emailController.text.trim().isEmpty || _passwordController.text.isEmpty) {
      setState(() => _error = 'Lütfen e-posta ve şifrenizi girin.');
      return;
    }

    setState(() {
      _isLoading = true;
      _error = '';
    });
    try {
      await FirebaseAuth.instance.signInWithEmailAndPassword(
        email: _emailController.text.trim(),
        password: _passwordController.text,
      );
    } catch (e) {
      setState(() => _error = 'Giriş başarısız: E-posta veya şifre hatalı.');
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _register() async {
    final name = _nameController.text.trim();
    final email = _emailController.text.trim();
    final password = _passwordController.text;
    if (name.isEmpty) {
      setState(() => _error = 'Lütfen adınızı ve soyadınızı girin.');
      return;
    }
    if (email.isEmpty || password.isEmpty) {
      setState(() => _error = 'Lütfen e-posta ve şifrenizi girin.');
      return;
    }
    if (password.length < 6) {
      setState(() => _error = 'Şifre en az 6 karakter olmalıdır.');
      return;
    }

    setState(() {
      _isLoading = true;
      _error = '';
    });
    try {
      final cred = await FirebaseAuth.instance.createUserWithEmailAndPassword(
        email: email,
        password: password,
      );
      await cred.user?.updateDisplayName(name);
      await cred.user?.reload();
    } on FirebaseAuthException catch (e) {
      if (e.code == 'email-already-in-use') {
        setState(() => _error = 'Bu e-posta adresi zaten kullanımda.');
      } else if (e.code == 'weak-password') {
        setState(() => _error = 'Şifre çok zayıf. En az 6 karakter olmalı.');
      } else {
        setState(() => _error = e.message ?? 'Kayıt işlemi başarısız.');
      }
    } catch (e) {
      setState(() => _error = 'Kayıt sırasında hata oluştu: $e');
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _googleLogin() async {
    setState(() {
      _isLoading = true;
      _error = '';
    });
    try {
      final googleSignIn = getGoogleSignIn();
      final GoogleSignInAccount? googleUser = await googleSignIn.signIn();
      if (googleUser == null) {
        if (mounted) setState(() => _isLoading = false);
        return;
      }

      final GoogleSignInAuthentication googleAuth = await googleUser.authentication;
      final OAuthCredential credential = GoogleAuthProvider.credential(
        accessToken: googleAuth.accessToken,
        idToken: googleAuth.idToken,
      );

      final cred = await FirebaseAuth.instance.signInWithCredential(credential);
      if (googleUser.displayName != null && (cred.user?.displayName == null || cred.user!.displayName!.isEmpty)) {
        await cred.user?.updateDisplayName(googleUser.displayName);
        await cred.user?.reload();
      }
    } on FirebaseAuthException catch (e) {
      if (mounted) {
        setState(() {
          _error = e.message ?? 'Google ile giriş başarısız oldu.';
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = 'Google ile giriş hatası: $e';
        });
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _appleLogin() async {
    setState(() {
      _isLoading = true;
      _error = '';
    });
    try {
      final appleProvider = AppleAuthProvider();
      appleProvider.addScope('email');
      appleProvider.addScope('name');

      final userCredential = await FirebaseAuth.instance.signInWithProvider(appleProvider);
      final appleUser = userCredential.user;
      if (appleUser != null && (appleUser.displayName == null || appleUser.displayName!.isEmpty)) {
        final profileName = userCredential.additionalUserInfo?.profile?['name'];
        if (profileName != null && profileName is Map) {
          final fullName = [profileName['firstName'], profileName['lastName']]
              .where((s) => s != null && s.toString().trim().isNotEmpty)
              .join(' ');
          if (fullName.isNotEmpty) {
            await appleUser.updateDisplayName(fullName);
            await appleUser.reload();
          }
        }
      }
    } on FirebaseAuthException catch (e) {
      if (mounted) {
        setState(() {
          _error = e.message ?? 'Apple ile giriş başarısız oldu.';
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _error = 'Apple ile giriş hatası: $e';
        });
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _guestLogin() async {
    setState(() {
      _isLoading = true;
      _error = '';
    });
    try {
      await FirebaseAuth.instance.signInAnonymously();
    } catch (e) {
      setState(() => _error = 'Misafir girişi başarısız: $e');
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Stack(
        children: [
          // Background ambient gradient glow
          Positioned(
            top: -100,
            right: -80,
            child: Container(
              width: 300,
              height: 300,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                boxShadow: [
                  BoxShadow(
                    color: const Color(0xFFF59E0B).withValues(alpha: 0.18),
                    blurRadius: 100,
                    spreadRadius: 50,
                  ),
                ],
              ),
            ),
          ),
          Positioned(
            bottom: -80,
            left: -60,
            child: Container(
              width: 250,
              height: 250,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                boxShadow: [
                  BoxShadow(
                    color: const Color(0xFFF97316).withValues(alpha: 0.15),
                    blurRadius: 90,
                    spreadRadius: 40,
                  ),
                ],
              ),
            ),
          ),
          SafeArea(
            child: CustomScrollView(
              slivers: [
                SliverFillRemaining(
                  hasScrollBody: false,
                  child: Align(
                    alignment: Alignment.topCenter,
                    child: ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 480),
                      child: Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 28.0, vertical: 32.0),
                        child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const SizedBox(height: 24),
                        // Brand Header Logo
                        Center(
                          child: Container(
                            decoration: BoxDecoration(
                              color: context.palette.card,
                              borderRadius: BorderRadius.circular(28),
                              boxShadow: [
                                BoxShadow(
                                  color: Colors.black.withValues(alpha: 0.1),
                                  blurRadius: 20,
                                  offset: const Offset(0, 10),
                                ),
                              ],
                            ),
                            child: ClipRRect(
                              borderRadius: BorderRadius.circular(28),
                              child: Image.asset(
                                'assets/logo.png',
                                height: 120,
                              ),
                            ),
                          ),
                        ),
                        const Spacer(),

                        Text(
                          _showEmailForm ? (_isRegister ? 'Hesap Oluşturun' : 'E-posta ile Giriş') : 'Hoş Geldiniz',
                          style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: context.palette.textMain, letterSpacing: -0.5),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          _showEmailForm 
                              ? (_isRegister ? 'Yeni bir hesap açarak sorgulamaya başlayın.' : 'Hesap bilgilerinizi giriniz.')
                              : 'Hesabınıza erişmek için yönteminizi seçin.',
                          style: TextStyle(fontSize: 14, color: context.palette.textPlaceholder),
                        ),
                        const SizedBox(height: 24),

                        if (!_showEmailForm) ...[
                          // Apple ile Giriş Yap Butonu (iOS Apple Review Guideline 4.8)
                          if (Platform.isIOS) ...[
                            Container(
                              width: double.infinity,
                              height: 52,
                              decoration: BoxDecoration(
                                color: context.palette.appleButton,
                                borderRadius: BorderRadius.circular(18),
                                boxShadow: [
                                  BoxShadow(
                                    color: Colors.black.withValues(alpha: 0.08),
                                    blurRadius: 10,
                                    offset: const Offset(0, 4),
                                  ),
                                ],
                              ),
                              child: ElevatedButton(
                                onPressed: _isLoading ? null : _appleLogin,
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: context.palette.appleButton,
                                  foregroundColor: context.palette.onAppleButton,
                                  elevation: 0,
                                  shadowColor: Colors.transparent,
                                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
                                ),
                                child: const Row(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Icon(Icons.apple, size: 24),
                                    SizedBox(width: 10),
                                    Text(
                                      'Apple ile Devam Et',
                                      style: TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                            const SizedBox(height: 14),
                          ],

                          // Google ile Giriş Yap Butonu
                          Container(
                            width: double.infinity,
                            height: 52,
                            decoration: BoxDecoration(
                              color: context.palette.card,
                              borderRadius: BorderRadius.circular(18),
                              border: Border.all(color: context.palette.border, width: 1.2),
                              boxShadow: [
                                BoxShadow(
                                  color: Colors.black.withValues(alpha: 0.04),
                                  blurRadius: 10,
                                  offset: const Offset(0, 4),
                                ),
                              ],
                            ),
                            child: ElevatedButton(
                              onPressed: _isLoading ? null : _googleLogin,
                              style: ElevatedButton.styleFrom(
                                backgroundColor: context.palette.card,
                                foregroundColor: context.palette.textMain,
                                elevation: 0,
                                shadowColor: Colors.transparent,
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
                              ),
                              child: Row(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  Image.asset(
                                    'assets/google_logo.png',
                                    height: 22,
                                    width: 22,
                                  ),
                                  const SizedBox(width: 12),
                                  Text(
                                    'Google ile Devam Et',
                                    style: TextStyle(
                                      fontSize: 15,
                                      fontWeight: FontWeight.w600,
                                      color: context.palette.textMain,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                          const SizedBox(height: 14),

                          // E-posta ile Devam Et Butonu
                          Container(
                            width: double.infinity,
                            height: 52,
                            decoration: BoxDecoration(
                              color: context.palette.card,
                              borderRadius: BorderRadius.circular(18),
                              border: Border.all(color: context.palette.border, width: 1.2),
                              boxShadow: [
                                BoxShadow(
                                  color: Colors.black.withValues(alpha: 0.04),
                                  blurRadius: 10,
                                  offset: const Offset(0, 4),
                                ),
                              ],
                            ),
                            child: ElevatedButton(
                              onPressed: () => setState(() => _showEmailForm = true),
                              style: ElevatedButton.styleFrom(
                                backgroundColor: context.palette.card,
                                foregroundColor: context.palette.textMain,
                                elevation: 0,
                                shadowColor: Colors.transparent,
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
                              ),
                              child: Row(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  Icon(Icons.email_outlined, color: context.palette.textMain, size: 22),
                                  SizedBox(width: 12),
                                  Text(
                                    'E-posta ile Devam Et',
                                    style: TextStyle(
                                      fontSize: 15,
                                      fontWeight: FontWeight.w600,
                                      color: context.palette.textMain,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                          const SizedBox(height: 18),

                          // Divider with "veya"
                          Row(
                            children: [
                              Expanded(child: Divider(color: context.palette.border)),
                              Padding(
                                padding: EdgeInsets.symmetric(horizontal: 14),
                                child: Text(
                                  'veya',
                                  style: TextStyle(color: context.palette.textSecondary, fontSize: 13, fontWeight: FontWeight.w500),
                                ),
                              ),
                              Expanded(child: Divider(color: context.palette.border)),
                            ],
                          ),
                          const SizedBox(height: 18),

                          // Misafir Girişi Butonu
                          Container(
                            width: double.infinity,
                            height: 52,
                            decoration: BoxDecoration(
                              color: context.palette.card,
                              borderRadius: BorderRadius.circular(18),
                              border: Border.all(color: context.palette.accent.withValues(alpha: 0.4), width: 1.5),
                              boxShadow: [
                                BoxShadow(
                                  color: const Color(0xFFF97316).withValues(alpha: 0.08),
                                  blurRadius: 10,
                                  offset: const Offset(0, 4),
                                ),
                              ],
                            ),
                            child: OutlinedButton.icon(
                              onPressed: _isLoading ? null : _guestLogin,
                              style: OutlinedButton.styleFrom(
                                side: BorderSide.none,
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
                              ),
                              icon: Icon(Icons.person_outline_rounded, color: context.palette.accent, size: 20),
                              label: Text(
                                'Misafir Girişi (5 Test Hakkı)',
                                style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: context.palette.textMain),
                              ),
                            ),
                          ),
                          const SizedBox(height: 8),
                          Center(
                            child: Text(
                              'Misafir oturumu ve verileri 3 gün sonra otomatik temizlenir.',
                              style: TextStyle(fontSize: 11, color: context.palette.textSecondary),
                            ),
                          ),
                        ] else ...[
                          // Geri Dön Butonu
                          Align(
                            alignment: Alignment.centerLeft,
                            child: TextButton.icon(
                              onPressed: () => setState(() {
                                _showEmailForm = false;
                                _error = '';
                              }),
                              icon: Icon(Icons.arrow_back_rounded, color: context.palette.textSecondary, size: 18),
                              label: Text('Geri Dön', style: TextStyle(color: context.palette.textSecondary)),
                              style: TextButton.styleFrom(padding: EdgeInsets.zero, alignment: Alignment.centerLeft),
                            ),
                          ),
                          const SizedBox(height: 16),

                          if (_isRegister) ...[
                            // Name Input
                            Container(
                              decoration: BoxDecoration(
                                color: context.palette.card,
                                borderRadius: BorderRadius.circular(18),
                                border: Border.all(color: context.palette.border),
                              ),
                              child: TextField(
                                controller: _nameController,
                                textCapitalization: TextCapitalization.words,
                                style: TextStyle(color: context.palette.textMain, fontSize: 15),
                                decoration: InputDecoration(
                                  prefixIcon: Icon(Icons.person_outline_rounded, color: context.palette.accent, size: 20),
                                  hintText: 'Ad Soyad',
                                  hintStyle: TextStyle(color: context.palette.textSecondary, fontSize: 14),
                                  border: InputBorder.none,
                                  contentPadding: EdgeInsets.all(18),
                                ),
                              ),
                            ),
                            const SizedBox(height: 16),
                          ],

                          // Email Input
                          Container(
                            decoration: BoxDecoration(
                              color: context.palette.card,
                              borderRadius: BorderRadius.circular(18),
                              border: Border.all(color: context.palette.border),
                            ),
                            child: TextField(
                              controller: _emailController,
                              keyboardType: TextInputType.emailAddress,
                              style: TextStyle(color: context.palette.textMain, fontSize: 15),
                              decoration: InputDecoration(
                                prefixIcon: Icon(Icons.alternate_email_rounded, color: context.palette.accent, size: 20),
                                hintText: 'E-posta Adresi',
                                hintStyle: TextStyle(color: context.palette.textSecondary, fontSize: 14),
                                border: InputBorder.none,
                                contentPadding: EdgeInsets.all(18),
                              ),
                            ),
                          ),
                          const SizedBox(height: 16),

                          // Password Input
                          Container(
                            decoration: BoxDecoration(
                              color: context.palette.card,
                              borderRadius: BorderRadius.circular(18),
                              border: Border.all(color: context.palette.border),
                            ),
                            child: TextField(
                              controller: _passwordController,
                              obscureText: _obscurePassword,
                              style: TextStyle(color: context.palette.textMain, fontSize: 15),
                              decoration: InputDecoration(
                                prefixIcon: Icon(Icons.lock_outline_rounded, color: context.palette.accent, size: 20),
                                suffixIcon: IconButton(
                                  icon: Icon(
                                    _obscurePassword ? Icons.visibility_off_outlined : Icons.visibility_outlined,
                                    color: context.palette.textSecondary,
                                    size: 20,
                                  ),
                                  onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
                                ),
                                hintText: 'Şifre',
                                hintStyle: TextStyle(color: context.palette.textSecondary, fontSize: 14),
                                border: InputBorder.none,
                                contentPadding: const EdgeInsets.all(18),
                              ),
                            ),
                          ),

                          if (_error.isNotEmpty) ...[
                            const SizedBox(height: 16),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                              decoration: BoxDecoration(
                                color: context.palette.danger.withValues(alpha: 0.12),
                                borderRadius: BorderRadius.circular(14),
                                border: Border.all(color: context.palette.danger.withValues(alpha: 0.3)),
                              ),
                              child: Row(
                                children: [
                                  Icon(Icons.error_outline_rounded, color: context.palette.danger, size: 18),
                                  const SizedBox(width: 10),
                                  Expanded(
                                    child: Text(
                                      _error,
                                      style: TextStyle(color: context.palette.danger, fontSize: 13, fontWeight: FontWeight.w500),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],

                          const SizedBox(height: 28),

                          // CTA Button
                          Container(
                            width: double.infinity,
                            height: 56,
                            decoration: BoxDecoration(
                              borderRadius: BorderRadius.circular(18),
                              gradient: const LinearGradient(
                                colors: AppColors.primaryGradient,
                              ),
                              boxShadow: [
                                BoxShadow(
                                  color: const Color(0xFFF97316).withValues(alpha: 0.3),
                                  blurRadius: 20,
                                  offset: const Offset(0, 8),
                                ),
                              ],
                            ),
                            child: ElevatedButton(
                              onPressed: _isLoading ? null : (_isRegister ? _register : _login),
                              style: ElevatedButton.styleFrom(
                                backgroundColor: Colors.transparent,
                                shadowColor: Colors.transparent,
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(18)),
                              ),
                              child: _isLoading
                                  ? SizedBox(
                                      width: 24,
                                      height: 24,
                                      child: CircularProgressIndicator(color: context.palette.textMain, strokeWidth: 2.5),
                                    )
                                  : Text(
                                      _isRegister ? 'Kayıt Ol' : 'Giriş Yap',
                                      style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: context.palette.textMain),
                                    ),
                            ),
                          ),
                          const SizedBox(height: 12),

                          // Toggle Register / Login
                          Center(
                            child: TextButton(
                              onPressed: () => setState(() {
                                _isRegister = !_isRegister;
                                _error = '';
                              }),
                              child: Text(
                                _isRegister ? 'Zaten hesabınız var mı? Giriş Yapın' : 'Hesabınız yok mu? Kayıt Olun',
                                style: TextStyle(
                                  fontSize: 13,
                                  fontWeight: FontWeight.w600,
                                  color: context.palette.accent,
                                ),
                              ),
                            ),
                          ),
                        ],
                        
                        const Spacer(),
                        
                        const SizedBox(height: 16),
                        Center(
                          child: TextButton(
                            onPressed: () {
                        showModalBottomSheet(
                          context: context,
                          isScrollControlled: true,
                          backgroundColor: context.palette.card,
                          shape: const RoundedRectangleBorder(
                            borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
                          ),
                          builder: (context) => DraggableScrollableSheet(
                            initialChildSize: 0.85,
                            maxChildSize: 0.95,
                            minChildSize: 0.5,
                            expand: false,
                            builder: (context, scrollController) => Padding(
                              padding: const EdgeInsets.all(20),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Center(
                                    child: Container(
                                      width: 40, height: 4,
                                      decoration: BoxDecoration(
                                        color: context.palette.border,
                                        borderRadius: BorderRadius.circular(10),
                                      ),
                                    ),
                                  ),
                                  const SizedBox(height: 16),
                                  Text('Gizlilik Politikası', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: context.palette.textMain)),
                                  const SizedBox(height: 16),
                                  Expanded(
                                    child: SingleChildScrollView(
                                      controller: scrollController,
                                      child: Text(
                                        'MARCTOPLU Mobil Uygulaması Gizlilik Politikası\n\n'
                                        'Son Güncelleme: 26 Ağustos 2026\n\n'
                                        'MARCTOPLU ("biz", "bizim", "bize") MARCTOPLU mobil uygulamasını ("Hizmet") işletmektedir.\n\n'
                                        '1. Bilgi Toplama ve Kullanım\n'
                                        'Size Hizmetimizi sağlamak ve geliştirmek için çeşitli amaçlarla farklı türde bilgiler topluyoruz.\n\n'
                                        'Toplanan Veri Türleri:\n'
                                        '• E-posta adresi\n'
                                        '• Ad ve soyad\n'
                                        '• Kullanım Verileri\n\n'
                                        'Kamera Erişimi: Uygulamamız, barkodları taramak için kamera erişimi gerektirir. '
                                        'Kamera yayınınızı kaydetmiyoruz, saklamıyoruz veya iletmiyoruz.\n\n'
                                        '2. Hesap Silme\n'
                                        'Hesabınızı ve kişisel verilerinizi istediğiniz zaman uygulama içinden silebilirsiniz.\n\n'
                                        '3. Bize Ulaşın\n'
                                        'E-posta: info@marctoplu.com',
                                        style: TextStyle(fontSize: 14, color: context.palette.textMuted, height: 1.6),
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        );
                      },
                      child: Text(
                        'Gizlilik Politikası ve Kullanım Koşulları',
                        style: TextStyle(color: context.palette.textPlaceholder, fontSize: 12, decoration: TextDecoration.underline),
                      ),
                    ),
                  ),
                  const SizedBox(height: 12),
                  Center(
                    child: Text(
                      '© 2026 MARCTOPLU. Tüm hakları saklıdır.',
                      style: TextStyle(color: context.palette.textPlaceholder, fontSize: 10),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    ],
  ),
),
],
),
);
  }
}
