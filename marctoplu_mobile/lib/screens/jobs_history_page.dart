import 'package:flutter/material.dart';
import 'package:marctoplu_mobile/theme/app_theme.dart';
import 'dart:async';
import 'package:marctoplu_mobile/services/api_service.dart';
import 'package:marctoplu_mobile/utils/job_download.dart';

class JobsHistoryPage extends StatefulWidget {
  const JobsHistoryPage({super.key});

  @override
  State<JobsHistoryPage> createState() => JobsHistoryPageState();
}

class JobsHistoryPageState extends State<JobsHistoryPage> {
  final ApiService _api = ApiService();
  List<dynamic> _jobs = [];
  bool _isLoading = true;
  Timer? _pollingTimer;

  @override
  void initState() {
    super.initState();
    loadJobs();
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    super.dispose();
  }

  void _checkAndStartPolling() {
    final hasActive = _jobs.any((j) {
      final s = (j['status'] ?? '').toString().toLowerCase();
      return s == 'queued' || s == 'running' || s == 'cancel_requested';
    });

    if (hasActive) {
      if (_pollingTimer == null || !_pollingTimer!.isActive) {
        _pollingTimer = Timer.periodic(const Duration(seconds: 3), (_) {
          _silentPollJobs();
        });
      }
    } else {
      _pollingTimer?.cancel();
      _pollingTimer = null;
    }
  }

  Future<void> _silentPollJobs() async {
    try {
      final jobs = await _api.getJobs();
      if (mounted) {
        setState(() {
          _jobs = jobs;
        });
        _checkAndStartPolling();
      }
    } catch (_) {}
  }

  Future<void> loadJobs() async {
    setState(() => _isLoading = true);
    final jobs = await _api.getJobs();
    if (mounted) {
      setState(() {
        _jobs = jobs;
        _isLoading = false;
      });
      _checkAndStartPolling();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('İşlem Geçmişi'),
        actions: [
          IconButton(
            icon: Icon(Icons.sync_rounded, color: context.palette.accent),
            onPressed: loadJobs,
          ),
        ],
      ),
      body: _isLoading
          ? Center(child: CircularProgressIndicator(color: context.palette.accent))
          : RefreshIndicator(
              onRefresh: loadJobs,
              color: context.palette.accent,
              backgroundColor: context.palette.card,
              child: Align(
                alignment: Alignment.topCenter,
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 760),
                  child: ListView.builder(
                padding: const EdgeInsets.all(20),
                itemCount: _jobs.length,
                itemBuilder: (context, index) {
                  final job = _jobs[index];
                  final status = job['status'] ?? 'queued';
                  final totalIsbns = job['totalIsbns'] ?? 0;
                  final isProcessing = status == 'queued' || status == 'running';
                  final consumedTokens = (job['consumedTokens'] as int?) ?? 0;
                  // Yalnızca bulunan kayıtlar ücretlendirilir; canlıda şu ana kadar bulunanlar gösterilir
                  final consumed = consumedTokens;
                  final consumedText = consumed > 0
                      ? '-$consumed MARC'
                      : (isProcessing ? 'İşleniyor...' : '0 MARC');

                  Color color = context.palette.warning;
                  if (status == 'completed') color = context.palette.success;
                  if (status == 'failed') color = context.palette.danger;

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
                            CircleAvatar(
                              backgroundColor: color.withValues(alpha: 0.15),
                              child: Icon(Icons.bookmark_outline_rounded, color: color, size: 20),
                            ),
                            const SizedBox(width: 14),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text('$totalIsbns Adet ISBN Taraması', style: TextStyle(fontWeight: FontWeight.bold, color: context.palette.textMain, fontSize: 14)),
                                  const SizedBox(height: 4),
                                  Text('Durum: $status', style: TextStyle(color: color, fontSize: 12, fontWeight: FontWeight.w600)),
                                ],
                              ),
                            ),
                            Text(
                              consumedText,
                              style: TextStyle(
                                fontWeight: FontWeight.bold,
                                color: consumed > 0 ? context.palette.textMain : context.palette.textPlaceholder,
                                fontSize: 14,
                              ),
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
            ),
          ),
        ),
    );
  }
}
