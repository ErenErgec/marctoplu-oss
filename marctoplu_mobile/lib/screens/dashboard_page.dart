import 'package:flutter/material.dart';
import 'package:marctoplu_mobile/theme/app_theme.dart';
import 'package:firebase_auth/firebase_auth.dart';
import 'dart:async';
import 'package:marctoplu_mobile/services/api_service.dart';
import 'package:marctoplu_mobile/services/notification_service.dart';
import 'package:marctoplu_mobile/screens/scanner_page.dart';
import 'package:marctoplu_mobile/screens/excel_upload_sheet.dart';
import 'package:marctoplu_mobile/screens/jobs_history_page.dart';
import 'package:marctoplu_mobile/screens/token_packages_page.dart';
import 'package:marctoplu_mobile/screens/profile_page.dart';
import 'package:marctoplu_mobile/utils/job_download.dart';
import 'package:marctoplu_mobile/widgets/link_account_dialog.dart';

class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key});

  @override
  State<DashboardPage> createState() => DashboardPageState();
}

class DashboardPageState extends State<DashboardPage> {
  final ApiService _api = ApiService();
  int? _balance;
  List<dynamic> _recentJobs = [];
  bool _isLoading = true;

  String _selectedFilter = 'Tümü';
  String _error = '';
  Timer? _pollingTimer;

  @override
  void initState() {
    super.initState();
    loadData();
    NotificationService().initialize();
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    super.dispose();
  }

  void _checkAndStartPolling() {
    final hasActive = _recentJobs.any((j) {
      final s = (j['status'] ?? '').toString().toLowerCase();
      return s == 'queued' || s == 'running' || s == 'cancel_requested';
    });

    if (hasActive) {
      if (_pollingTimer == null || !_pollingTimer!.isActive) {
        _pollingTimer = Timer.periodic(const Duration(seconds: 3), (_) {
          _silentPollData();
        });
      }
    } else {
      _pollingTimer?.cancel();
      _pollingTimer = null;
    }
  }

  Future<void> _silentPollData() async {
    try {
      final wallet = await _api.getWallet();
      final jobs = await _api.getJobs();
      if (mounted) {
        setState(() {
          _balance = wallet['balance'] as int?;
          _recentJobs = jobs;
        });
        _checkAndStartPolling();
      }
    } catch (_) {}
  }

  Future<void> loadData() async {
    setState(() {
      _isLoading = true;
      _error = '';
    });
    try {
      final wallet = await _api.getWallet();
      final jobs = await _api.getJobs();
      setState(() {
        _balance = wallet['balance'] as int?;
        _recentJobs = jobs;
      });
      _checkAndStartPolling();
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  List<dynamic> get _filteredJobs {
    if (_selectedFilter == 'Tamamlanan') {
      return _recentJobs.where((j) => j['status'] == 'completed').toList();
    } else if (_selectedFilter == 'İşleniyor') {
      return _recentJobs.where((j) => j['status'] == 'queued' || j['status'] == 'running').toList();
    }
    return _recentJobs;
  }

  @override
  Widget build(BuildContext context) {
    final user = FirebaseAuth.instance.currentUser;
    final isAnonymous = user?.isAnonymous ?? false;
    final displayName = user?.displayName?.trim();
    final hasDisplayName = displayName != null && displayName.isNotEmpty;
    final userEmail = isAnonymous ? 'Misafir Kullanıcı' : (user?.email ?? 'Kullanıcı');
    final userName = isAnonymous
        ? 'Misafir'
        : (hasDisplayName ? displayName : (userEmail.contains('@') ? userEmail.split('@')[0] : userEmail));

    String avatarText = 'M';
    if (!isAnonymous) {
      if (hasDisplayName) {
        final parts = displayName.split(RegExp(r'\s+')).where((p) => p.isNotEmpty).toList();
        if (parts.length >= 2) {
          avatarText = '${parts.first[0]}${parts.last[0]}'.toUpperCase();
        } else if (parts.isNotEmpty) {
          avatarText = parts.first[0].toUpperCase();
        }
      } else if (userName.isNotEmpty) {
        avatarText = userName.substring(0, 1).toUpperCase();
      }
    }

    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: loadData,
          color: context.palette.accent,
          backgroundColor: context.palette.card,
          child: Align(
            alignment: Alignment.topCenter,
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 760),
              child: SingleChildScrollView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.symmetric(horizontal: 20.0, vertical: 16.0),
                child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Top Custom Header Bar
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    GestureDetector(
                      onTap: () async {
                        await Navigator.of(context).push(
                          MaterialPageRoute(builder: (context) => const ProfilePage()),
                        );
                        if (mounted) setState(() {});
                      },
                      child: Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(2),
                            decoration: const BoxDecoration(
                              shape: BoxShape.circle,
                              gradient: LinearGradient(
                                colors: AppColors.primaryGradient,
                              ),
                            ),
                            child: CircleAvatar(
                              radius: 22,
                              backgroundColor: context.palette.card,
                              child: Text(
                                avatarText,
                                style: TextStyle(
                                  fontWeight: FontWeight.bold,
                                  color: context.palette.accent,
                                  fontSize: avatarText.length > 1 ? 14 : 18,
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(width: 12),
                          Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Text(
                                    'Hoş Geldiniz',
                                    style: TextStyle(fontSize: 12, color: context.palette.textPlaceholder, fontWeight: FontWeight.w500),
                                  ),
                                  const SizedBox(width: 6),
                                  Container(
                                    width: 6,
                                    height: 6,
                                    decoration: const BoxDecoration(color: Color(0xFF10B981), shape: BoxShape.circle),
                                  ),
                                ],
                              ),
                              Text(
                                userName,
                                style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: context.palette.textMain, letterSpacing: -0.3),
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                    Row(
                      children: [

                        _buildHeaderIconButton(
                          icon: Icons.sync_rounded,
                          onTap: loadData,
                        ),
                      ],
                    ),
                  ],
                ),
                if (isAnonymous) ...[
                  const SizedBox(height: 16),
                  _buildGuestBanner(context),
                ],
                const SizedBox(height: 22),

                // Craft Fintech Credit Card (Token Wallet Card)
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(22),
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(26),
                    gradient: const LinearGradient(
                      colors: AppColors.walletGradient,
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                    ),
                    boxShadow: [
                      BoxShadow(
                        color: const Color(0xFFB45309).withValues(alpha: 0.35),
                        blurRadius: 24,
                        offset: const Offset(0, 10),
                      ),
                    ],
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [

                      Text(
                        'TOPLAM KULLANILABİLİR BAKİYE',
                        style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: Colors.white.withValues(alpha: 0.75), letterSpacing: 1),
                      ),
                      const SizedBox(height: 6),
                      if (_isLoading)
                        const SizedBox(
                          height: 38,
                          child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2.5),
                        )
                      else if (_error.isNotEmpty)
                        Text(_error, style: const TextStyle(color: Colors.amberAccent, fontSize: 13))
                      else
                        Row(
                          crossAxisAlignment: CrossAxisAlignment.baseline,
                          textBaseline: TextBaseline.alphabetic,
                          children: [
                            Text(
                              '${_balance ?? 0}',
                              style: const TextStyle(fontSize: 36, fontWeight: FontWeight.w800, color: Colors.white, letterSpacing: -0.5),
                            ),
                            const SizedBox(width: 8),
                            Text(
                              'MARC Token',
                              style: TextStyle(fontSize: 14, fontWeight: FontWeight.w600, color: Colors.white.withValues(alpha: 0.8)),
                            ),
                          ],
                        ),
                      const SizedBox(height: 22),

                      // Card Actions Bar
                      Row(
                        children: [
                          Expanded(
                            child: ElevatedButton.icon(
                              icon: const Icon(Icons.qr_code_scanner_rounded, size: 18),
                              label: const Text('Barkod Tara', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
                              style: ElevatedButton.styleFrom(
                                backgroundColor: Colors.white,
                                foregroundColor: const Color(0xFF9A3412),
                                padding: const EdgeInsets.symmetric(vertical: 12),
                                elevation: 0,
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                              ),
                              onPressed: () {
                                Navigator.of(context).push(
                                  MaterialPageRoute(builder: (context) => const ScannerPage()),
                                ).then((_) => loadData());
                              },
                            ),
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: ElevatedButton.icon(
                              icon: const Icon(Icons.table_view_rounded, size: 18),
                              label: const Text('Excel Yükle', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
                              style: ElevatedButton.styleFrom(
                                backgroundColor: Colors.white.withValues(alpha: 0.22),
                                foregroundColor: Colors.white,
                                padding: const EdgeInsets.symmetric(vertical: 12),
                                elevation: 0,
                                shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(14),
                                  side: BorderSide(color: Colors.white.withValues(alpha: 0.3)),
                                ),
                              ),
                              onPressed: () async {
                                final uploaded = await ExcelUploadSheet.show(context);
                                if (uploaded == true) {
                                  loadData();
                                }
                              },
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 10),
                      SizedBox(
                        width: double.infinity,
                        child: TextButton.icon(
                          icon: const Icon(Icons.add_circle_outline_rounded, size: 16, color: Colors.white),
                          label: const Text('Token / Paket Satın Al', style: TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.w600)),
                          style: TextButton.styleFrom(
                            backgroundColor: Colors.black.withValues(alpha: 0.16),
                            padding: const EdgeInsets.symmetric(vertical: 9),
                            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                          ),
                          onPressed: () {
                            Navigator.of(context).push(
                              MaterialPageRoute(builder: (context) => const TokenPackagesPage()),
                            );
                          },
                        ),
                      ),
                    ],
                  ),
                ),

                const SizedBox(height: 24),

                // Transactions Header with Filter Pills
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'Son ISBN Aktivitesi',
                      style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: context.palette.textMain, letterSpacing: -0.3),
                    ),
                    TextButton(
                      onPressed: () {
                        Navigator.of(context).push(
                          MaterialPageRoute(builder: (context) => const JobsHistoryPage()),
                        );
                      },
                      child: Text('Tümü', style: TextStyle(color: context.palette.accent, fontSize: 13, fontWeight: FontWeight.w600)),
                    ),
                  ],
                ),
                const SizedBox(height: 8),

                // Filter Pills
                Row(
                  children: ['Tümü', 'Tamamlanan', 'İşleniyor'].map((filter) {
                    final isSelected = _selectedFilter == filter;
                    return Padding(
                      padding: const EdgeInsets.only(right: 8.0),
                      child: ChoiceChip(
                        label: Text(filter, style: TextStyle(fontSize: 12, fontWeight: isSelected ? FontWeight.bold : FontWeight.normal)),
                        selected: isSelected,
                        selectedColor: context.palette.accent.withValues(alpha: 0.2),
                        backgroundColor: context.palette.card,
                        side: BorderSide(color: isSelected ? context.palette.accent : context.palette.border),
                        labelStyle: TextStyle(color: isSelected ? context.palette.accent : context.palette.textPlaceholder),
                        onSelected: (selected) {
                          if (selected) setState(() => _selectedFilter = filter);
                        },
                      ),
                    );
                  }).toList(),
                ),

                const SizedBox(height: 14),

                // Transaction Feed
                if (_filteredJobs.isEmpty)
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(28),
                    decoration: BoxDecoration(
                      color: context.palette.card,
                      borderRadius: BorderRadius.circular(22),
                      border: Border.all(color: context.palette.border),
                    ),
                    child: Column(
                      children: [
                        Icon(Icons.search_off_rounded, color: context.palette.textSecondary, size: 36),
                        SizedBox(height: 10),
                        Text('Bu filtreye uygun ISBN işlemi bulunamadı.', style: TextStyle(color: context.palette.textPlaceholder, fontSize: 13)),
                      ],
                    ),
                  )
                else
                  ListView.builder(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    itemCount: _filteredJobs.length > 5 ? 5 : _filteredJobs.length,
                    itemBuilder: (context, index) {
                      final job = _filteredJobs[index];
                      final status = job['status'] ?? 'queued';
                      final totalIsbns = job['totalIsbns'] ?? 0;
                      final isProcessing = status == 'queued' || status == 'running';
                      final consumedTokens = (job['consumedTokens'] as int?) ?? 0;
                      // Yalnızca bulunan kayıtlar ücretlendirilir; canlıda şu ana kadar bulunanlar gösterilir
                      final consumed = consumedTokens;
                      final consumedText = consumed > 0
                          ? '-$consumed MARC'
                          : (isProcessing ? 'İşleniyor...' : '0 MARC');

                      Color statusColor = context.palette.warning;
                      String statusText = 'İşleniyor';
                      if (status == 'completed') {
                        statusColor = context.palette.success;
                        statusText = 'Tamamlandı';
                      } else if (status == 'failed') {
                        statusColor = context.palette.danger;
                        statusText = 'Başarısız';
                      }

                      return Container(
                        margin: const EdgeInsets.only(bottom: 12),
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: context.palette.card,
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(color: context.palette.border),
                        ),
                        child: Column(
                          children: [
                            Row(
                              children: [
                                Container(
                                  padding: const EdgeInsets.all(12),
                                  decoration: BoxDecoration(
                                    color: statusColor.withValues(alpha: 0.12),
                                    borderRadius: BorderRadius.circular(16),
                                  ),
                                  child: Icon(Icons.bookmark_border_rounded, color: statusColor, size: 22),
                                ),
                                const SizedBox(width: 14),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        '$totalIsbns Kitap ISBN Taraması',
                                        style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14, color: context.palette.textMain),
                                      ),
                                      const SizedBox(height: 4),
                                      Row(
                                        children: [
                                          Container(
                                            width: 7,
                                            height: 7,
                                            decoration: BoxDecoration(color: statusColor, shape: BoxShape.circle),
                                          ),
                                          const SizedBox(width: 6),
                                          Text(statusText, style: TextStyle(color: statusColor, fontSize: 12, fontWeight: FontWeight.w600)),
                                        ],
                                      ),
                                    ],
                                  ),
                                ),
                                Column(
                                  crossAxisAlignment: CrossAxisAlignment.end,
                                  children: [
                                    Text(
                                      consumedText,
                                      style: TextStyle(
                                        fontWeight: FontWeight.bold,
                                        fontSize: 14,
                                        color: consumed > 0 ? context.palette.textMain : context.palette.textPlaceholder,
                                      ),
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      isProcessing ? 'Şu ana kadar' : 'Token Harcandı',
                                      style: TextStyle(color: context.palette.textSecondary, fontSize: 11),
                                    ),
                                  ],
                                ),
                              ],
                            ),
                            if (status == 'completed' && job['id'] != null) ...[
                              const SizedBox(height: 12),
                              SizedBox(
                                width: double.infinity,
                                child: OutlinedButton.icon(
                                  icon: const Icon(Icons.download_rounded, size: 16),
                                  label: const Text('Sonucu İndir', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
                                  style: OutlinedButton.styleFrom(
                                    foregroundColor: context.palette.success,
                                    side: BorderSide(color: context.palette.success.withValues(alpha: 0.5)),
                                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                                    padding: const EdgeInsets.symmetric(vertical: 8),
                                  ),
                                  onPressed: () => promptDownloadFormatAndShare(context, job['id']),
                                ),
                              ),
                            ],
                          ],
                        ),
                      );
                    },
                  ),
              ],
            ),
          ),
        ),
      ),
    ),
  ),
);
  }

  Widget _buildHeaderIconButton({required IconData icon, required VoidCallback onTap}) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(16),
      child: Container(
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: context.palette.card,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: context.palette.border),
        ),
        child: Icon(icon, color: context.palette.textMain, size: 20),
      ),
    );
  }

  Widget _buildGuestBanner(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: context.palette.accentSurface,
        borderRadius: BorderRadius.circular(18),
        border: Border.all(color: context.palette.accent.withValues(alpha: 0.3)),
      ),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: context.palette.accent.withValues(alpha: 0.15),
              shape: BoxShape.circle,
            ),
            child: Icon(Icons.flash_on_rounded, color: context.palette.accent, size: 20),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Misafir Oturumu (3 Günlük Geçici)',
                  style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold, color: context.palette.textMain),
                ),
                SizedBox(height: 2),
                Text(
                  '3 gün içinde silinir. Bakiyenizi kaybetmemek için kalıcı yapın.',
                  style: TextStyle(fontSize: 11, color: context.palette.textSecondary),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          ElevatedButton(
            onPressed: () => showLinkAccountDialog(context, onSuccess: () => setState(() {})),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.accentFill,
              foregroundColor: Colors.white,
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              elevation: 0,
            ),
            child: const Text('Kalıcı Yap', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    );
  }
}
