import 'package:flutter/material.dart';
import 'package:marctoplu_mobile/theme/app_theme.dart';
import 'dart:async';
import 'package:marctoplu_mobile/services/api_service.dart';
import 'package:marctoplu_mobile/screens/token_packages_page.dart';
import 'package:marctoplu_mobile/utils/job_download.dart';

class TokenWalletPage extends StatefulWidget {
  const TokenWalletPage({super.key});

  @override
  State<TokenWalletPage> createState() => _TokenWalletPageState();
}

class _TokenWalletPageState extends State<TokenWalletPage> {
  final ApiService _api = ApiService();
  int? _balance;
  List<dynamic> _recentJobs = [];
  bool _isLoading = true;
  String _error = '';

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() {
      _isLoading = true;
      _error = '';
    });
    try {
      final wallet = await _api.getWallet();
      final jobs = await _api.getJobs();
      if (mounted) {
        setState(() {
          _balance = wallet['balance'] as int?;
          _recentJobs = jobs;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() => _error = e.toString());
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Cüzdanım')),
      body: RefreshIndicator(
        onRefresh: _loadData,
        color: context.palette.accent,
        child: Align(
          alignment: Alignment.topCenter,
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 720),
            child: SingleChildScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.all(20),
              child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Wallet Card
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(26),
                  gradient: const LinearGradient(
                    colors: AppColors.walletGradient,
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                  ),
                  boxShadow: [
                    BoxShadow(
                      color: const Color(0xFF9A3412).withValues(alpha: 0.35),
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
                    const SizedBox(height: 12),
                    if (_isLoading)
                      SizedBox(height: 38, child: CircularProgressIndicator(color: Colors.white))
                    else if (_error.isNotEmpty)
                      Text(_error, style: const TextStyle(color: Colors.amberAccent, fontSize: 13))
                    else
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.baseline,
                        textBaseline: TextBaseline.alphabetic,
                        children: [
                          Text(
                            '${_balance ?? 0}',
                            style: const TextStyle(fontSize: 42, fontWeight: FontWeight.w800, color: Colors.white, letterSpacing: -0.5),
                          ),
                          const SizedBox(width: 8),
                          Text(
                            'MARC Token',
                            style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: Colors.white.withValues(alpha: 0.75)),
                          ),
                        ],
                      ),
                    const SizedBox(height: 24),
                    SizedBox(
                      width: double.infinity,
                      child: ElevatedButton.icon(
                        icon: const Icon(Icons.add_shopping_cart_rounded, size: 18),
                        label: const Text('Token Satın Al', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: Colors.white,
                          foregroundColor: const Color(0xFF9A3412),
                          padding: const EdgeInsets.symmetric(vertical: 14),
                          elevation: 0,
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                        ),
                        onPressed: () {
                          Navigator.of(context).push(
                            MaterialPageRoute(builder: (context) => const TokenPackagesPage()),
                          ).then((_) => _loadData());
                        },
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 32),
              Text(
                'Son İşlemler',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: context.palette.textMain),
              ),
              const SizedBox(height: 16),
              if (_isLoading)
                Center(child: Padding(padding: EdgeInsets.all(20), child: CircularProgressIndicator(color: context.palette.accent)))
              else if (_recentJobs.isEmpty)
                Center(
                  child: Padding(
                    padding: EdgeInsets.all(40),
                    child: Text('Henüz işlem bulunmuyor.', style: TextStyle(color: context.palette.textPlaceholder)),
                  ),
                )
              else
                ..._recentJobs.take(5).map((job) {
                  final status = job['status'] ?? 'queued';
                  final isProcessing = status == 'queued' || status == 'running';
                  final consumedTokens = (job['consumedTokens'] as int?) ?? 0;
                  final processedIsbns = (job['processedIsbns'] as int?) ?? 0;
                  final consumed = consumedTokens > 0
                      ? consumedTokens
                      : (isProcessing && processedIsbns > 0 ? processedIsbns : 0);
                  final consumedText = consumed > 0
                      ? '-$consumed MARC'
                      : (isProcessing ? 'İşleniyor...' : '0 MARC');
                  final totalIsbns = job['totalIsbns'] ?? 0;
                  
                  return ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: CircleAvatar(
                      backgroundColor: context.palette.card,
                      child: Icon(Icons.receipt_rounded, color: context.palette.accent, size: 20),
                    ),
                    title: Text('$totalIsbns ISBN Tarandı', style: TextStyle(color: context.palette.textMain, fontWeight: FontWeight.w600)),
                    subtitle: Text('Durum: $status', style: TextStyle(color: context.palette.textPlaceholder, fontSize: 12)),
                    trailing: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          consumedText,
                          style: TextStyle(
                            color: consumed > 0 ? context.palette.textMain : context.palette.textPlaceholder,
                            fontWeight: FontWeight.bold,
                            fontSize: 14,
                          ),
                        ),
                        if (status == 'completed' && job['id'] != null) ...[
                          const SizedBox(width: 8),
                          IconButton(
                            icon: Icon(Icons.download_rounded, color: context.palette.success, size: 20),
                            onPressed: () => promptDownloadFormatAndShare(context, job['id']),
                            tooltip: 'Sonucu İndir',
                          ),
                        ],
                      ],
                    ),
                  );
                }),
            ],
          ),
        ),
      ),
    ),
  ),
);
  }
}
