import 'package:flutter/material.dart';
import 'package:marctoplu_mobile/theme/app_theme.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'dart:async';
import 'package:marctoplu_mobile/services/api_service.dart';
import 'package:marctoplu_mobile/widgets/link_account_dialog.dart';
import 'package:marctoplu_mobile/screens/auth_gate.dart';

class ProfilePage extends StatefulWidget {
  const ProfilePage({super.key});

  @override
  State<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends State<ProfilePage> {
  bool _isDeletingAccount = false;

  Future<void> _showPrivacyPolicy() async {
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
                    'Kamera Erişimi: Uygulamamız, barkodları taramak için cihazınızın kamerasına erişim gerektirir. '
                    'Kamera yayınınızı kaydetmiyoruz, saklamıyoruz veya iletmiyoruz.\n\n'
                    '2. Üçüncü Taraf Hizmetleri\n'
                    '• Firebase Authentication: Kullanıcı kimlik doğrulaması\n'
                    '• Firebase Cloud Messaging: Uygulama içi bildirimler\n\n'
                    '3. Verilerin Güvenliği\n'
                    'Verilerinizin güvenliği bizim için önemlidir. Kişisel Verilerinizi korumak için ticari olarak kabul edilebilir araçları kullanmaya çabalıyoruz.\n\n'
                    '4. Hesap Silme\n'
                    'Hesabınızı ve kişisel verilerinizi istediğiniz zaman uygulama içinden (Profilim > Hesabımı ve Verilerimi Sil) silebilirsiniz.\n\n'
                    '5. Bize Ulaşın\n'
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
  }

  Future<void> _deleteAccount() async {
    // 1. İlk onay diyaloğu
    final firstConfirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        title: Row(
          children: [
            Icon(Icons.warning_amber_rounded, color: context.palette.danger, size: 28),
            SizedBox(width: 10),
            Text('Hesap Silme'),
          ],
        ),
        content: Text(
          'Hesabınızı silmek istediğinize emin misiniz?\n\n'
          'Bu işlem geri alınamaz. Tüm kişisel verileriniz, cüzdan bakiyeniz ve hesap bilgileriniz kalıcı olarak silinecektir.',
          style: TextStyle(fontSize: 14, color: context.palette.textMuted),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: Text('Vazgeç', style: TextStyle(color: context.palette.textPlaceholder)),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: Text('Devam Et', style: TextStyle(color: context.palette.danger, fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    );

    if (firstConfirm != true || !mounted) return;

    // 2. Son onay diyaloğu
    final finalConfirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        title: Text('Son Onay', style: TextStyle(color: context.palette.danger, fontWeight: FontWeight.bold)),
        content: Text(
          'Bu işlem GERİ ALINAMAZ.\n\n'
          'Hesabınız, tüm kişisel verileriniz ve cüzdan bakiyeniz kalıcı olarak silinecektir.\n\n'
          'Devam etmek istiyor musunuz?',
          style: TextStyle(fontSize: 14, color: context.palette.textMuted),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: Text('Hayır, Vazgeç', style: TextStyle(color: context.palette.textPlaceholder)),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.redAccent,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            ),
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Evet, Hesabımı Sil', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    );

    if (finalConfirm != true || !mounted) return;

    // 3. Silme işlemini gerçekleştir
    setState(() => _isDeletingAccount = true);

    try {
      // Backend'deki verileri sil
      await ApiService().deleteAccount();

      // Firebase Auth hesabını sil
      await FirebaseAuth.instance.currentUser?.delete();

      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: const Text('Hesabınız başarıyla silindi.'),
          backgroundColor: const Color(0xFF10B981),
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      );
      Navigator.of(context, rootNavigator: true).pushAndRemoveUntil(MaterialPageRoute(builder: (context) => const AuthGate()), (route) => false);
    } on FirebaseAuthException catch (e) {
      if (!mounted) return;
      if (e.code == 'requires-recent-login') {
        // Yeniden oturum açma gerekiyor
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: const Text('Güvenlik nedeniyle lütfen çıkış yapıp tekrar giriş yapın ve hesap silme işlemini tekrarlayın.'),
            backgroundColor: AppColors.accentFill,
            behavior: SnackBarBehavior.floating,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            duration: const Duration(seconds: 5),
          ),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Hesap silme hatası: ${e.message}'),
            backgroundColor: Colors.redAccent,
            behavior: SnackBarBehavior.floating,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          ),
        );
      }
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Hata: $e'),
          backgroundColor: Colors.redAccent,
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      );
    } finally {
      if (mounted) setState(() => _isDeletingAccount = false);
    }
  }

  Future<void> _showEditNameDialog(BuildContext context) async {
    final user = FirebaseAuth.instance.currentUser;
    final controller = TextEditingController(text: user?.displayName ?? '');
    String error = '';
    bool isSaving = false;

    await showDialog(
      context: context,
      builder: (dialogCtx) => StatefulBuilder(
        builder: (ctx, setDialogState) => AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
          title: Row(
            children: [
              Icon(Icons.edit_outlined, color: context.palette.accent, size: 24),
              SizedBox(width: 10),
              Text('Ad Soyad Güncelle', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
            ],
          ),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Uygulama içinde ve sol üst karşılama alanında görünecek adınızı ve soyadınızı giriniz.',
                style: TextStyle(fontSize: 13, color: context.palette.textSecondary),
              ),
              const SizedBox(height: 16),
              TextField(
                controller: controller,
                textCapitalization: TextCapitalization.words,
                decoration: InputDecoration(
                  labelText: 'Ad Soyad',
                  prefixIcon: const Icon(Icons.person_outline, size: 20),
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                ),
              ),
              if (error.isNotEmpty) ...[
                const SizedBox(height: 8),
                Text(error, style: TextStyle(color: context.palette.danger, fontSize: 12)),
              ],
            ],
          ),
          actions: [
            TextButton(
              onPressed: isSaving ? null : () => Navigator.of(dialogCtx).pop(),
              child: Text('Vazgeç', style: TextStyle(color: context.palette.textSecondary)),
            ),
            ElevatedButton(
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.accentFill,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
              onPressed: isSaving
                  ? null
                  : () async {
                      final newName = controller.text.trim();
                      if (newName.isEmpty) {
                        setDialogState(() => error = 'Lütfen adınızı ve soyadınızı giriniz.');
                        return;
                      }
                      setDialogState(() {
                        isSaving = true;
                        error = '';
                      });
                      try {
                        await FirebaseAuth.instance.currentUser?.updateDisplayName(newName);
                        await FirebaseAuth.instance.currentUser?.reload();
                        if (mounted) setState(() {});
                        if (dialogCtx.mounted) Navigator.of(dialogCtx).pop();
                        if (context.mounted) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text('Adınız ve soyadınız başarıyla güncellendi.'),
                              backgroundColor: Color(0xFF10B981),
                              behavior: SnackBarBehavior.floating,
                            ),
                          );
                        }
                      } catch (e) {
                        setDialogState(() {
                          isSaving = false;
                          error = 'Hata: $e';
                        });
                      }
                    },
              child: isSaving
                  ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                  : const Text('Kaydet', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final user = FirebaseAuth.instance.currentUser;
    final isAnonymous = user?.isAnonymous ?? false;
    final displayName = user?.displayName?.trim();
    final hasDisplayName = displayName != null && displayName.isNotEmpty;
    final fullName = isAnonymous
        ? 'Misafir Kullanıcı'
        : (hasDisplayName ? displayName : (user?.email?.contains('@') == true ? user!.email!.split('@')[0] : 'Ad Soyad Belirtilmedi'));

    String avatarText = 'M';
    if (!isAnonymous) {
      if (hasDisplayName) {
        final parts = displayName.split(RegExp(r'\s+')).where((p) => p.isNotEmpty).toList();
        if (parts.length >= 2) {
          avatarText = '${parts.first[0]}${parts.last[0]}'.toUpperCase();
        } else if (parts.isNotEmpty) {
          avatarText = parts.first[0].toUpperCase();
        }
      } else if (user?.email != null && user!.email!.isNotEmpty) {
        avatarText = user.email!.substring(0, 1).toUpperCase();
      }
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Profilim')),
      body: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        child: Align(
          alignment: Alignment.topCenter,
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 680),
            child: Padding(
              padding: const EdgeInsets.all(24.0),
              child: Column(
          children: [
            Container(
              width: 96,
              height: 96,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                gradient: const LinearGradient(colors: AppColors.primaryGradient),
                boxShadow: [
                  BoxShadow(
                    color: const Color(0xFFF97316).withValues(alpha: 0.3),
                    blurRadius: 20,
                    offset: const Offset(0, 6),
                  ),
                ],
              ),
              child: Center(
                child: isAnonymous
                    ? Icon(Icons.person_rounded, size: 52, color: context.palette.textMain)
                    : Text(
                        avatarText,
                        style: TextStyle(
                          fontSize: avatarText.length > 1 ? 32 : 38,
                          fontWeight: FontWeight.bold,
                          color: context.palette.textMain,
                        ),
                      ),
              ),
            ),
            const SizedBox(height: 18),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Flexible(
                  child: Text(
                    fullName,
                    textAlign: TextAlign.center,
                    style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: context.palette.textMain),
                  ),
                ),
                if (!isAnonymous) ...[
                  const SizedBox(width: 8),
                  GestureDetector(
                    onTap: () => _showEditNameDialog(context),
                    child: Container(
                      padding: const EdgeInsets.all(5),
                      decoration: BoxDecoration(
                        color: context.palette.surfaceSubtle,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Icon(Icons.edit_outlined, size: 16, color: context.palette.textSecondary),
                    ),
                  ),
                ],
              ],
            ),
            if (!isAnonymous && (user?.email != null)) ...[
              const SizedBox(height: 4),
              Text(
                user!.email!,
                style: TextStyle(fontSize: 14, color: context.palette.textSecondary),
              ),
            ],
            const SizedBox(height: 6),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
              decoration: BoxDecoration(
                color: user?.isAnonymous == true
                    ? context.palette.accent.withValues(alpha: 0.15)
                    : context.palette.success.withValues(alpha: 0.15),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Text(
                user?.isAnonymous == true ? 'Misafir Oturumu (5 Ücretsiz Hak)' : 'Firebase Doğrulanmış Kullanıcı',
                style: TextStyle(
                  color: user?.isAnonymous == true ? context.palette.accent : context.palette.success,
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
            const SizedBox(height: 36),
            Align(
              alignment: Alignment.centerLeft,
              child: Text(
                'Hesap Bilgileri',
                style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: context.palette.textSecondary),
              ),
            ),
            const SizedBox(height: 12),
            Container(
              decoration: BoxDecoration(
                color: context.palette.card,
                borderRadius: BorderRadius.circular(20),
                border: Border.all(color: context.palette.border),
              ),
              child: Column(
                children: [
                  if (!isAnonymous) ...[
                    ListTile(
                      leading: Icon(Icons.badge_outlined, color: context.palette.accent),
                      title: Text('Ad Soyad', style: TextStyle(color: context.palette.textMain, fontWeight: FontWeight.w600)),
                      subtitle: Text(
                        hasDisplayName ? displayName : 'Belirtilmedi (Eklemek için dokunun)',
                        style: TextStyle(
                          color: hasDisplayName ? context.palette.textPlaceholder : context.palette.accent,
                          fontSize: 12,
                        ),
                      ),
                      trailing: Icon(Icons.edit_outlined, color: context.palette.textPlaceholder, size: 20),
                      onTap: () => _showEditNameDialog(context),
                    ),
                    Divider(height: 1, color: context.palette.border),
                  ],
                  ListTile(
                    leading: Icon(Icons.shield_outlined, color: context.palette.accent),
                    title: Text('Oturum Güvenliği', style: TextStyle(color: context.palette.textMain, fontWeight: FontWeight.w600)),
                    subtitle: Text('JWT Bearer & Firebase Auth Altyapısı', style: TextStyle(color: context.palette.textPlaceholder, fontSize: 12)),
                  ),
                  Divider(height: 1, color: context.palette.border),
                  ListTile(
                    leading: Icon(Icons.privacy_tip_outlined, color: context.palette.accent),
                    title: Text('Gizlilik Politikası', style: TextStyle(color: context.palette.textMain, fontWeight: FontWeight.w600)),
                    subtitle: Text('Verilerinizin nasıl korunduğunu öğrenin', style: TextStyle(color: context.palette.textPlaceholder, fontSize: 12)),
                    trailing: Icon(Icons.chevron_right_rounded, color: context.palette.textPlaceholder),
                    onTap: _showPrivacyPolicy,
                  ),
                  if (user?.isAnonymous == true) ...[
                    Divider(height: 1, color: context.palette.border),
                    ListTile(
                      leading: Icon(Icons.link_rounded, color: context.palette.accent),
                      title: Text('Hesabı Kalıcı Yap', style: TextStyle(color: context.palette.textMain, fontWeight: FontWeight.bold)),
                      subtitle: Text('E-posta ve şifre ekleyerek haklarınızı koruyun', style: TextStyle(color: context.palette.accent, fontSize: 12)),
                      trailing: Icon(Icons.chevron_right_rounded, color: context.palette.accent),
                      onTap: () => showLinkAccountDialog(context, onSuccess: () => setState(() {})),
                    ),
                  ],
                ],
              ),
            ),
            const Spacer(),
            // Hesap Silme Butonu
            SizedBox(
              width: double.infinity,
              height: 52,
              child: OutlinedButton.icon(
                icon: _isDeletingAccount
                    ? SizedBox(width: 18, height: 18, child: CircularProgressIndicator(color: context.palette.danger, strokeWidth: 2))
                    : Icon(Icons.delete_forever_rounded, color: context.palette.danger),
                label: Text('Hesabımı ve Verilerimi Sil', style: TextStyle(color: context.palette.danger, fontWeight: FontWeight.w600, fontSize: 13)),
                style: OutlinedButton.styleFrom(
                  side: BorderSide(color: context.palette.danger.withValues(alpha: 0.4)),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                ),
                onPressed: _isDeletingAccount ? null : _deleteAccount,
              ),
            ),
            const SizedBox(height: 12),
            // Çıkış Yap Butonu
            SizedBox(
              width: double.infinity,
              height: 52,
              child: OutlinedButton.icon(
                icon: Icon(Icons.logout_rounded, color: context.palette.danger),
                label: Text('Çıkış Yap', style: TextStyle(color: context.palette.danger, fontWeight: FontWeight.bold)),
                style: OutlinedButton.styleFrom(
                  side: BorderSide(color: context.palette.danger),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                ),
                onPressed: () async {
                  await FirebaseAuth.instance.signOut();
                  if (context.mounted) {
                    Navigator.of(context, rootNavigator: true).pushAndRemoveUntil(MaterialPageRoute(builder: (context) => const AuthGate()), (route) => false);
                  }
                },
              ),
            ),
            const SizedBox(height: 24),
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
    );
  }
}
