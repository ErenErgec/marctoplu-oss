import 'package:flutter/material.dart';
import 'package:marctoplu_mobile/theme/app_theme.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'dart:io';
import 'package:google_sign_in/google_sign_in.dart';
import 'package:marctoplu_mobile/utils/google_sign_in.dart';

void showLinkAccountDialog(BuildContext context, {VoidCallback? onSuccess}) {
  final nameController = TextEditingController();
  final emailController = TextEditingController();
  final passwordController = TextEditingController();
  bool isSubmitting = false;
  String errorMsg = '';

  showDialog(
    context: context,
    builder: (dialogCtx) => StatefulBuilder(
      builder: (context, setDialogState) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        title: Row(
          children: [
            Icon(Icons.shield_outlined, color: context.palette.accent),
            SizedBox(width: 8),
            Text('Hesabı Kalıcı Yap', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
          ],
        ),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Mevcut deneme haklarınız ve geçmiş sorgularınız hesabınıza kalıcı olarak aktarılacaktır.',
                style: TextStyle(fontSize: 13, color: context.palette.textSecondary),
              ),
              const SizedBox(height: 16),
              if (Platform.isIOS) ...[
                SizedBox(
                  width: double.infinity,
                  height: 48,
                  child: ElevatedButton(
                    onPressed: isSubmitting
                        ? null
                        : () async {
                            setDialogState(() {
                              isSubmitting = true;
                              errorMsg = '';
                            });
                            try {
                              final appleProvider = AppleAuthProvider();
                              appleProvider.addScope('email');
                              appleProvider.addScope('name');
                              await FirebaseAuth.instance.currentUser!.linkWithProvider(appleProvider);
                              if (context.mounted) {
                                Navigator.of(dialogCtx).pop();
                                ScaffoldMessenger.of(context).showSnackBar(
                                  const SnackBar(
                                    content: Text('Tebrikler! Apple hesabınız bağlandı ve bakiyeniz korundu.'),
                                    backgroundColor: Color(0xFF10B981),
                                  ),
                                );
                                onSuccess?.call();
                              }
                            } on FirebaseAuthException catch (e) {
                              setDialogState(() {
                                isSubmitting = false;
                                if (e.code == 'credential-already-in-use') {
                                  errorMsg = 'Bu Apple hesabı zaten başka bir hesaba bağlı.';
                                } else {
                                  errorMsg = e.message ?? 'Apple hesabı bağlanırken hata oluştu.';
                                }
                              });
                            } catch (e) {
                              setDialogState(() {
                                isSubmitting = false;
                                errorMsg = 'Hata: $e';
                              });
                            }
                          },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: context.palette.appleButton,
                      foregroundColor: context.palette.onAppleButton,
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      elevation: 0,
                    ),
                    child: const Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.apple, size: 22),
                        SizedBox(width: 10),
                        Text(
                          'Apple ile Hesabı Bağla',
                          style: TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 10),
              ],
              // Google ile Hesabı Bağla Butonu
              SizedBox(
                width: double.infinity,
                height: 48,
                child: OutlinedButton(
                  onPressed: isSubmitting
                      ? null
                      : () async {
                          setDialogState(() {
                            isSubmitting = true;
                            errorMsg = '';
                          });
                          try {
                            final googleSignIn = getGoogleSignIn();
                            final GoogleSignInAccount? googleUser = await googleSignIn.signIn();
                            if (googleUser == null) {
                              setDialogState(() => isSubmitting = false);
                              return;
                            }
                            final GoogleSignInAuthentication googleAuth = await googleUser.authentication;
                            final OAuthCredential credential = GoogleAuthProvider.credential(
                              accessToken: googleAuth.accessToken,
                              idToken: googleAuth.idToken,
                            );
                            await FirebaseAuth.instance.currentUser!.linkWithCredential(credential);
                            if (context.mounted) {
                              Navigator.of(dialogCtx).pop();
                              ScaffoldMessenger.of(context).showSnackBar(
                                const SnackBar(
                                  content: Text('Tebrikler! Google hesabınız bağlandı ve bakiyeniz korundu.'),
                                  backgroundColor: Color(0xFF10B981),
                                ),
                              );
                              onSuccess?.call();
                            }
                          } on FirebaseAuthException catch (e) {
                            setDialogState(() {
                              isSubmitting = false;
                              if (e.code == 'credential-already-in-use') {
                                errorMsg = 'Bu Google hesabı zaten başka bir hesaba bağlı.';
                              } else {
                                errorMsg = e.message ?? 'Google hesabı bağlanırken hata oluştu.';
                              }
                            });
                          } catch (e) {
                            setDialogState(() {
                              isSubmitting = false;
                              errorMsg = 'Hata: $e';
                            });
                          }
                        },
                  style: OutlinedButton.styleFrom(
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    side: BorderSide(color: context.palette.border),
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Image.asset('assets/google_logo.png', width: 20, height: 20),
                      const SizedBox(width: 10),
                      Text(
                        'Google ile Hesabı Bağla',
                        style: TextStyle(color: context.palette.textMain, fontWeight: FontWeight.w600, fontSize: 14),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 14),
              Row(
                children: [
                  Expanded(child: Divider(color: context.palette.border)),
                  Padding(
                    padding: EdgeInsets.symmetric(horizontal: 10),
                    child: Text('veya e-posta belirleyin', style: TextStyle(fontSize: 11, color: context.palette.textSecondary)),
                  ),
                  Expanded(child: Divider(color: context.palette.border)),
                ],
              ),
              const SizedBox(height: 14),
              TextField(
                controller: nameController,
                textCapitalization: TextCapitalization.words,
                decoration: InputDecoration(
                  labelText: 'Ad Soyad',
                  prefixIcon: const Icon(Icons.person_outline, size: 20),
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: emailController,
                keyboardType: TextInputType.emailAddress,
                decoration: InputDecoration(
                  labelText: 'E-posta Adresi',
                  prefixIcon: const Icon(Icons.email_outlined, size: 20),
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: passwordController,
                obscureText: true,
                decoration: InputDecoration(
                  labelText: 'Şifre',
                  prefixIcon: const Icon(Icons.lock_outline, size: 20),
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                ),
              ),
              if (errorMsg.isNotEmpty) ...[
                const SizedBox(height: 10),
                Text(errorMsg, style: TextStyle(color: context.palette.danger, fontSize: 12)),
              ],
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: isSubmitting ? null : () => Navigator.of(dialogCtx).pop(),
            child: Text('İptal', style: TextStyle(color: context.palette.textSecondary)),
          ),
          ElevatedButton(
            onPressed: isSubmitting
                ? null
                : () async {
                    final email = emailController.text.trim();
                    final password = passwordController.text;
                    if (email.isEmpty || password.length < 6) {
                      setDialogState(() => errorMsg = 'Geçerli bir e-posta ve en az 6 haneli şifre girin.');
                      return;
                    }
                    setDialogState(() {
                      isSubmitting = true;
                      errorMsg = '';
                    });
                    try {
                      final cred = EmailAuthProvider.credential(email: email, password: password);
                      await FirebaseAuth.instance.currentUser!.linkWithCredential(cred);
                      if (nameController.text.trim().isNotEmpty) {
                        await FirebaseAuth.instance.currentUser!.updateDisplayName(nameController.text.trim());
                        await FirebaseAuth.instance.currentUser!.reload();
                      }
                      if (context.mounted) {
                        Navigator.of(dialogCtx).pop();
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(
                            content: Text('Tebrikler! Hesabınız kalıcı hale getirildi. Bakiyeniz korundu.'),
                            backgroundColor: Color(0xFF10B981),
                          ),
                        );
                        onSuccess?.call();
                      }
                    } on FirebaseAuthException catch (e) {
                      setDialogState(() {
                        isSubmitting = false;
                        if (e.code == 'email-already-in-use') {
                          errorMsg = 'Bu e-posta adresi zaten kullanımda.';
                        } else if (e.code == 'weak-password') {
                          errorMsg = 'Şifre çok zayıf. En az 6 karakter olmalı.';
                        } else {
                          errorMsg = e.message ?? 'Bağlama hatası oluştu.';
                        }
                      });
                    } catch (e) {
                      setDialogState(() {
                        isSubmitting = false;
                        errorMsg = 'Beklenmeyen hata: $e';
                      });
                    }
                  },
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.accentFill,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            ),
            child: isSubmitting
                ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                : const Text('Kaydet ve Bağla', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    ),
  );
}
