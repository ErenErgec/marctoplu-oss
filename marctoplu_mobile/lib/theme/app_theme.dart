import 'package:flutter/material.dart';

/// Web paneliyle (frontend/app/styles/base.css) aynı renk tokenları.
/// Ekranlar renklere `context.palette.x` ile erişir; sabit Color(0x…) kullanmayın.
@immutable
class AppPalette extends ThemeExtension<AppPalette> {
  const AppPalette({
    required this.background,
    required this.card,
    required this.surfaceSubtle,
    required this.border,
    required this.borderStrong,
    required this.textMain,
    required this.textMuted,
    required this.textSecondary,
    required this.textPlaceholder,
    required this.accent,
    required this.accentDeep,
    required this.accentSurface,
    required this.accentBorder,
    required this.success,
    required this.successBg,
    required this.warning,
    required this.warningBg,
    required this.danger,
    required this.dangerBg,
    required this.dangerBorder,
    required this.appleButton,
    required this.onAppleButton,
  });

  /// Sayfa zemini (web: --bg-dark)
  final Color background;

  /// Kart / sheet / input zemini (web: --bg-card)
  final Color card;
  final Color surfaceSubtle;
  final Color border;
  final Color borderStrong;

  final Color textMain;
  final Color textMuted;
  final Color textSecondary;
  final Color textPlaceholder;

  /// Metin, ikon ve kenar olarak marka rengi. Koyu temada okunabilirlik için açılır.
  /// Beyaz metinli dolgular için [AppColors.accentFill] / [AppColors.primaryGradient] kullanın.
  final Color accent;
  final Color accentDeep;
  final Color accentSurface;
  final Color accentBorder;

  final Color success;
  final Color successBg;
  final Color warning;
  final Color warningBg;
  final Color danger;
  final Color dangerBg;
  final Color dangerBorder;

  /// "Apple ile Devam Et": Apple yönergesi gereği açık zeminde siyah, koyu zeminde beyaz
  final Color appleButton;
  final Color onAppleButton;

  static const light = AppPalette(
    background: Color(0xFFF8FAFC),
    card: Color(0xFFFFFFFF),
    surfaceSubtle: Color(0xFFF1F5F9),
    border: Color(0xFFE2E8F0),
    borderStrong: Color(0xFFCBD5E1),
    textMain: Color(0xFF0F172A),
    textMuted: Color(0xFF334155),
    textSecondary: Color(0xFF64748B),
    textPlaceholder: Color(0xFF94A3B8),
    accent: Color(0xFFC2410C),
    accentDeep: Color(0xFF9A3412),
    accentSurface: Color(0xFFFFF7ED),
    accentBorder: Color(0xFFFED7AA),
    success: Color(0xFF047857),
    successBg: Color(0xFFECFDF5),
    warning: Color(0xFFB45309),
    warningBg: Color(0xFFFFFBEB),
    danger: Color(0xFFB91C1C),
    dangerBg: Color(0xFFFEF2F2),
    dangerBorder: Color(0xFFFECACA),
    appleButton: Color(0xFF000000),
    onAppleButton: Color(0xFFFFFFFF),
  );

  static const dark = AppPalette(
    background: Color(0xFF0B1120),
    card: Color(0xFF131C2E),
    surfaceSubtle: Color(0xFF1E293B),
    border: Color(0xFF243049),
    borderStrong: Color(0xFF334155),
    textMain: Color(0xFFF1F5F9),
    textMuted: Color(0xFFCBD5E1),
    textSecondary: Color(0xFF94A3B8),
    textPlaceholder: Color(0xFF64748B),
    accent: Color(0xFFFB923C),
    accentDeep: Color(0xFFFED7AA),
    accentSurface: Color(0x1AEA580C),
    accentBorder: Color(0x59FB923C),
    success: Color(0xFF34D399),
    successBg: Color(0x1F10B981),
    warning: Color(0xFFFBBF24),
    warningBg: Color(0x1FF59E0B),
    danger: Color(0xFFF87171),
    dangerBg: Color(0x1FEF4444),
    dangerBorder: Color(0x52F87171),
    appleButton: Color(0xFFFFFFFF),
    onAppleButton: Color(0xFF000000),
  );

  @override
  AppPalette copyWith() => this;

  @override
  AppPalette lerp(ThemeExtension<AppPalette>? other, double t) {
    if (other is! AppPalette) return this;
    Color l(Color a, Color b) => Color.lerp(a, b, t)!;
    return AppPalette(
      background: l(background, other.background),
      card: l(card, other.card),
      surfaceSubtle: l(surfaceSubtle, other.surfaceSubtle),
      border: l(border, other.border),
      borderStrong: l(borderStrong, other.borderStrong),
      textMain: l(textMain, other.textMain),
      textMuted: l(textMuted, other.textMuted),
      textSecondary: l(textSecondary, other.textSecondary),
      textPlaceholder: l(textPlaceholder, other.textPlaceholder),
      accent: l(accent, other.accent),
      accentDeep: l(accentDeep, other.accentDeep),
      accentSurface: l(accentSurface, other.accentSurface),
      accentBorder: l(accentBorder, other.accentBorder),
      success: l(success, other.success),
      successBg: l(successBg, other.successBg),
      warning: l(warning, other.warning),
      warningBg: l(warningBg, other.warningBg),
      danger: l(danger, other.danger),
      dangerBg: l(dangerBg, other.dangerBg),
      dangerBorder: l(dangerBorder, other.dangerBorder),
      appleButton: l(appleButton, other.appleButton),
      onAppleButton: l(onAppleButton, other.onAppleButton),
    );
  }
}

extension AppPaletteContext on BuildContext {
  AppPalette get palette => Theme.of(this).extension<AppPalette>() ?? AppPalette.light;
}

/// İki temada da aynı kalan marka renkleri (beyaz metinli dolgular, gradyanlar).
class AppColors {
  /// Beyaz metinli buton/rozet dolgusu (web: --primary-gradient'ın açık ucu)
  static const accentFill = Color(0xFFEA580C);
  static const accentFillDeep = Color(0xFFC2410C);

  /// Web: --primary-gradient
  static const primaryGradient = [Color(0xFFEA580C), Color(0xFFC2410C)];

  /// Web: cüzdan kartı
  static const walletGradient = [Color(0xFF78350F), Color(0xFF9A3412), Color(0xFFC2410C)];
}

class AppTheme {
  static ThemeData get lightTheme => _build(Brightness.light, AppPalette.light);
  static ThemeData get darkTheme => _build(Brightness.dark, AppPalette.dark);

  static ThemeData _build(Brightness brightness, AppPalette p) {
    final base = brightness == Brightness.dark ? ThemeData.dark() : ThemeData.light();
    return base.copyWith(
      scaffoldBackgroundColor: p.background,
      primaryColor: AppColors.accentFill,
      cardColor: p.card,
      canvasColor: p.card,
      dialogTheme: DialogThemeData(backgroundColor: p.card),
      bottomSheetTheme: BottomSheetThemeData(backgroundColor: p.card, modalBackgroundColor: p.card),
      colorScheme: ColorScheme(
        brightness: brightness,
        primary: p.accent,
        onPrimary: Colors.white,
        secondary: AppColors.accentFill,
        onSecondary: Colors.white,
        error: p.danger,
        onError: Colors.white,
        surface: p.card,
        onSurface: p.textMain,
      ),
      appBarTheme: AppBarTheme(
        backgroundColor: p.background,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        centerTitle: false,
        iconTheme: IconThemeData(color: p.textMain),
        titleTextStyle: TextStyle(fontSize: 20, fontWeight: FontWeight.w700, color: p.textMain),
      ),
      dividerTheme: DividerThemeData(color: p.border, thickness: 1),
      inputDecorationTheme: InputDecorationTheme(
        hintStyle: TextStyle(color: p.textPlaceholder),
        labelStyle: TextStyle(color: p.textSecondary),
      ),
      snackBarTheme: const SnackBarThemeData(behavior: SnackBarBehavior.floating),
      progressIndicatorTheme: ProgressIndicatorThemeData(color: p.accent),
      extensions: [p],
    );
  }
}
