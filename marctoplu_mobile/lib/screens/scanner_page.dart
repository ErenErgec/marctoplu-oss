import 'package:flutter/material.dart';
import 'package:marctoplu_mobile/theme/app_theme.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:marctoplu_mobile/services/api_service.dart';
import 'package:marctoplu_mobile/screens/excel_upload_sheet.dart';

class ScannerPage extends StatefulWidget {
  const ScannerPage({super.key});

  @override
  State<ScannerPage> createState() => _ScannerPageState();
}

class _ScannerPageState extends State<ScannerPage> {
  final MobileScannerController _scannerController = MobileScannerController();
  final Set<String> _scannedIsbns = {};
  bool _isProcessing = false;
  final ApiService _api = ApiService();

  void _onDetect(BarcodeCapture capture) {
    final List<Barcode> barcodes = capture.barcodes;
    for (final barcode in barcodes) {
      if (barcode.rawValue != null) {
        final rawValue = barcode.rawValue!.trim();
        if (rawValue.length > 5 && !_scannedIsbns.contains(rawValue)) {
          setState(() {
            _scannedIsbns.add(rawValue);
          });
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              backgroundColor: AppColors.accentFill,
              behavior: SnackBarBehavior.floating,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              content: Row(
                children: [
                  Icon(Icons.check_circle_outline, color: context.palette.textMain, size: 20),
                  const SizedBox(width: 8),
                  Text('ISBN Eklendi: $rawValue', style: TextStyle(color: context.palette.textMain, fontWeight: FontWeight.bold)),
                ],
              ),
              duration: const Duration(milliseconds: 600),
            ),
          );
        }
      }
    }
  }

  Future<void> _submitIsbns() async {
    if (_scannedIsbns.isEmpty) return;
    
    setState(() {
      _isProcessing = true;
    });

    try {
      final isbnsList = _scannedIsbns.toList();
      await _api.createJobFromIsbns(isbnsList);
      
      if (!mounted) return;
      
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Row(
            children: [
              Icon(Icons.stars, color: context.palette.textMain),
              SizedBox(width: 8),
              Text('İşlem başlatıldı! MARC verileri hazırlanıyor.'),
            ],
          ),
          backgroundColor: const Color(0xFF10B981),
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      );
      
      Navigator.of(context).pop(true);
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Hata: ${e.toString()}'),
          backgroundColor: Colors.redAccent,
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        ),
      );
    } finally {
      if (mounted) {
        setState(() {
          _isProcessing = false;
        });
      }
    }
  }

  @override
  void dispose() {
    _scannerController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: context.palette.background,
      appBar: AppBar(
        backgroundColor: context.palette.background,
        elevation: 0,
        title: Text('ISBN Barkod Okuyucu', style: TextStyle(color: context.palette.textMain, fontWeight: FontWeight.bold)),
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.palette.textMain, size: 20),
          onPressed: () => Navigator.of(context).pop(),
        ),
        actions: [
          IconButton(
            tooltip: 'Excel ile ISBN Yükle',
            icon: Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: context.palette.card,
                shape: BoxShape.circle,
                border: Border.all(color: context.palette.border),
              ),
              child: Icon(
                Icons.upload_file_rounded,
                color: context.palette.success,
                size: 20,
              ),
            ),
            onPressed: () async {
              final uploaded = await ExcelUploadSheet.show(context);
              if (uploaded == true && context.mounted) {
                Navigator.of(context).pop(true);
              }
            },
          ),
          const SizedBox(width: 4),
          ValueListenableBuilder<MobileScannerState>(
            valueListenable: _scannerController,
            builder: (context, state, child) {
              final isTorchOn = state.torchState == TorchState.on;
              return IconButton(
                icon: Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: isTorchOn ? context.palette.accent.withValues(alpha: 0.2) : context.palette.card,
                    shape: BoxShape.circle,
                    border: Border.all(color: isTorchOn ? context.palette.accent : context.palette.border),
                  ),
                  child: Icon(
                    isTorchOn ? Icons.flash_on_rounded : Icons.flash_off_rounded,
                    color: isTorchOn ? context.palette.accent : context.palette.textPlaceholder,
                    size: 20,
                  ),
                ),
                onPressed: () => _scannerController.toggleTorch(),
              );
            },
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: Column(
        children: [
          // Camera Viewfinder Area
          Expanded(
            flex: 5,
            child: Stack(
              children: [
                MobileScanner(
                  controller: _scannerController,
                  onDetect: _onDetect,
                  errorBuilder: (context, error) {
                    return Container(
                      color: context.palette.background,
                      child: Center(
                        child: Padding(
                          padding: const EdgeInsets.all(32),
                          child: Column(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(Icons.no_photography_outlined, size: 64, color: context.palette.textPlaceholder),
                              const SizedBox(height: 20),
                              Text(
                                'Kamera Erişimi Gerekli',
                                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: context.palette.textMain),
                              ),
                              const SizedBox(height: 10),
                              Text(
                                'ISBN barkodlarını tarayabilmek için kamera izni vermeniz gerekmektedir. '
                                'Lütfen cihaz ayarlarından kamera erişimini etkinleştirin.',
                                textAlign: TextAlign.center,
                                style: TextStyle(fontSize: 14, color: context.palette.textSecondary),
                              ),
                              const SizedBox(height: 24),
                              ElevatedButton.icon(
                                icon: const Icon(Icons.settings_rounded, size: 18),
                                label: const Text('Ayarları Aç', style: TextStyle(fontWeight: FontWeight.bold)),
                                style: ElevatedButton.styleFrom(
                                  backgroundColor: AppColors.accentFill,
                                  foregroundColor: context.palette.textMain,
                                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                                  padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
                                ),
                                onPressed: () {
                                  // Platform-specific settings open
                                  _scannerController.stop();
                                },
                              ),
                            ],
                          ),
                        ),
                      ),
                    );
                  },
                ),
                // Scanning Overlay Box
                Center(
                  child: Container(
                    width: 280,
                    height: 180,
                    decoration: BoxDecoration(
                      border: Border.all(color: context.palette.accent, width: 2),
                      borderRadius: BorderRadius.circular(20),
                      boxShadow: [
                        BoxShadow(
                          color: const Color(0xFFF97316).withValues(alpha: 0.2),
                          blurRadius: 16,
                          spreadRadius: 2,
                        ),
                      ],
                    ),
                  ),
                ),
                // Floating Scan Counter Pill
                Positioned(
                  top: 16,
                  left: 0,
                  right: 0,
                  child: Center(
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                      decoration: BoxDecoration(
                        color: context.palette.card.withValues(alpha: 0.92),
                        borderRadius: BorderRadius.circular(20),
                        border: Border.all(color: context.palette.accent.withValues(alpha: 0.5)),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.qr_code_2_rounded, color: context.palette.accent, size: 18),
                          const SizedBox(width: 8),
                          Text(
                            'Taranan: ${_scannedIsbns.length} Kitap',
                            style: TextStyle(fontWeight: FontWeight.bold, color: context.palette.textMain, fontSize: 13),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),

          // Bottom Scanned Items Sheet (Fintech Cart Style)
          Expanded(
            flex: 4,
            child: Container(
              padding: const EdgeInsets.all(20.0),
              decoration: BoxDecoration(
                color: context.palette.card,
                borderRadius: BorderRadius.only(
                  topLeft: Radius.circular(28),
                  topRight: Radius.circular(28),
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Center(
                    child: Container(
                      width: 40,
                      height: 4,
                      decoration: BoxDecoration(
                        color: context.palette.border,
                        borderRadius: BorderRadius.circular(10),
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        'Taranan ISBN Listesi',
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: context.palette.textMain),
                      ),
                      if (_scannedIsbns.isNotEmpty)
                        TextButton(
                          onPressed: () => setState(() => _scannedIsbns.clear()),
                          child: Text('Temizle', style: TextStyle(color: context.palette.danger, fontSize: 12)),
                        ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Expanded(
                    child: _scannedIsbns.isEmpty
                        ? Center(
                            child: Text(
                              'Barkodları kameranın karşısına getirin.',
                              style: TextStyle(color: context.palette.textSecondary, fontSize: 13),
                            ),
                          )
                        : ListView.builder(
                            itemCount: _scannedIsbns.length,
                            itemBuilder: (context, index) {
                              final isbn = _scannedIsbns.elementAt(index);
                              return Container(
                                margin: const EdgeInsets.only(bottom: 8),
                                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                                decoration: BoxDecoration(
                                  color: context.palette.background,
                                  borderRadius: BorderRadius.circular(14),
                                  border: Border.all(color: context.palette.border),
                                ),
                                child: Row(
                                  children: [
                                    Icon(Icons.bookmark_rounded, color: context.palette.accent, size: 18),
                                    const SizedBox(width: 12),
                                    Expanded(
                                      child: Text(
                                        isbn,
                                        style: TextStyle(fontWeight: FontWeight.bold, color: context.palette.textMain, fontSize: 14),
                                      ),
                                    ),
                                    IconButton(
                                      icon: Icon(Icons.close_rounded, color: context.palette.danger, size: 18),
                                      onPressed: () {
                                        setState(() {
                                          _scannedIsbns.remove(isbn);
                                        });
                                      },
                                    ),
                                  ],
                                ),
                              );
                            },
                          ),
                  ),
                  const SizedBox(height: 12),
                  Container(
                    width: double.infinity,
                    height: 52,
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(16),
                      gradient: _scannedIsbns.isEmpty || _isProcessing
                          ? null
                          : const LinearGradient(
                              colors: AppColors.primaryGradient,
                            ),
                      color: _scannedIsbns.isEmpty || _isProcessing ? context.palette.border : null,
                    ),
                    child: ElevatedButton(
                      onPressed: (_scannedIsbns.isEmpty || _isProcessing) ? null : _submitIsbns,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.transparent,
                        shadowColor: Colors.transparent,
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                      ),
                      child: _isProcessing
                          ? SizedBox(
                              width: 22,
                              height: 22,
                              child: CircularProgressIndicator(color: context.palette.textMain, strokeWidth: 2),
                            )
                          : Text(
                              'Sisteme Gönder (${_scannedIsbns.length} Kitap)',
                              style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: context.palette.textMain),
                            ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
