import 'package:flutter/material.dart';
import 'package:marctoplu_mobile/theme/app_theme.dart';
import 'package:file_picker/file_picker.dart';
import 'package:marctoplu_mobile/services/api_service.dart';

class ExcelUploadSheet extends StatefulWidget {
  const ExcelUploadSheet({super.key});

  static Future<bool?> show(BuildContext context) {
    return showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) => const ExcelUploadSheet(),
    );
  }

  @override
  State<ExcelUploadSheet> createState() => _ExcelUploadSheetState();
}

class _ExcelUploadSheetState extends State<ExcelUploadSheet> {
  PlatformFile? _selectedFile;
  bool _isUploading = false;
  String? _errorMessage;
  final ApiService _api = ApiService();

  Future<void> _pickFile() async {
    setState(() {
      _errorMessage = null;
    });

    try {
      final result = await FilePicker.pickFiles(
        type: FileType.custom,
        allowedExtensions: ['xlsx'],
        withData: true,
      );

      if (result != null && result.files.isNotEmpty) {
        final file = result.files.single;
        if (!file.name.toLowerCase().endsWith('.xlsx')) {
          setState(() {
            _errorMessage = 'Lütfen sadece .xlsx uzantılı bir Excel dosyası seçin.';
          });
          return;
        }

        setState(() {
          _selectedFile = file;
        });
      }
    } catch (e) {
      setState(() {
        _errorMessage = 'Dosya seçilirken hata oluştu: ${e.toString()}';
      });
    }
  }

  Future<void> _uploadFile() async {
    if (_selectedFile == null) return;

    setState(() {
      _isUploading = true;
      _errorMessage = null;
    });

    try {
      final res = await _api.uploadJobFile(
        filePath: _selectedFile!.path,
        fileBytes: _selectedFile!.bytes,
        fileName: _selectedFile!.name,
      );

      if (!mounted) return;

      final estimated = res['estimatedTokens'];
      final message = estimated != null
          ? 'Excel yüklendi, sorgu başladı. En fazla $estimated token ayrıldı; bulunamayanlar iade edilecek.'
          : 'Excel başarıyla yüklendi! MARC sorgusu başlatıldı.';

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Row(
            children: [
              const Icon(Icons.check_circle_rounded, color: Colors.white, size: 20),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  message,
                  style: const TextStyle(fontWeight: FontWeight.w600, color: Colors.white),
                ),
              ),
            ],
          ),
          backgroundColor: const Color(0xFF10B981),
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          duration: const Duration(seconds: 4),
        ),
      );

      Navigator.of(context).pop(true);
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.toString().replaceFirst('Exception: ', '');
      });
    } finally {
      if (mounted) {
        setState(() {
          _isUploading = false;
        });
      }
    }
  }

  String _formatFileSize(int bytes) {
    if (bytes < 1024) return '$bytes B';
    if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(1)} KB';
    return '${(bytes / (1024 * 1024)).toStringAsFixed(2)} MB';
  }

  @override
  Widget build(BuildContext context) {
    final bottomInset = MediaQuery.of(context).viewInsets.bottom;

    return Container(
      padding: EdgeInsets.fromLTRB(24, 16, 24, 24 + bottomInset),
      decoration: BoxDecoration(
        color: context.palette.card,
        borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
      ),
      child: SafeArea(
        top: false,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Drag handle
            Center(
              child: Container(
                width: 40,
                height: 4,
                margin: const EdgeInsets.only(bottom: 20),
                decoration: BoxDecoration(
                  color: context.palette.border,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),

            // Header Row
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: context.palette.success.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(14),
                  ),
                  child: Icon(
                    Icons.table_view_rounded,
                    color: context.palette.success,
                    size: 24,
                  ),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Excel ile ISBN Yükle',
                        style: TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: context.palette.textMain,
                          letterSpacing: -0.3,
                        ),
                      ),
                      SizedBox(height: 2),
                      Text(
                        'Toplu kataloglama için .xlsx dosyası',
                        style: TextStyle(
                          fontSize: 13,
                          color: context.palette.textSecondary,
                        ),
                      ),
                    ],
                  ),
                ),
                IconButton(
                  icon: Icon(Icons.close_rounded, color: context.palette.textPlaceholder),
                  onPressed: _isUploading ? null : () => Navigator.of(context).pop(),
                ),
              ],
            ),

            const SizedBox(height: 20),

            // Info Box
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: context.palette.background,
                borderRadius: BorderRadius.circular(14),
                border: Border.all(color: context.palette.border),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(
                    Icons.info_outline_rounded,
                    color: context.palette.accent,
                    size: 18,
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      'Excel dosyanızın herhangi bir sütununda ISBN numaralarının bulunması yeterlidir. Sistem otomatik tespit edecektir.',
                      style: TextStyle(
                        fontSize: 12,
                        color: context.palette.textMuted,
                        height: 1.4,
                      ),
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 18),

            // Picker Card
            GestureDetector(
              onTap: _isUploading ? null : _pickFile,
              child: Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(vertical: 24, horizontal: 16),
                decoration: BoxDecoration(
                  color: _selectedFile == null
                      ? context.palette.background
                      : context.palette.successBg,
                  borderRadius: BorderRadius.circular(18),
                  border: Border.all(
                    color: _selectedFile == null
                        ? context.palette.borderStrong
                        : context.palette.success,
                    width: _selectedFile == null ? 1.5 : 2,
                  ),
                ),
                child: _selectedFile == null
                    ? Column(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(14),
                            decoration: BoxDecoration(
                              color: context.palette.accent.withValues(alpha: 0.1),
                              shape: BoxShape.circle,
                            ),
                            child: Icon(
                              Icons.file_upload_outlined,
                              color: context.palette.accent,
                              size: 32,
                            ),
                          ),
                          const SizedBox(height: 12),
                          Text(
                            'Excel Dosyası Seçin (.xlsx)',
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.bold,
                              color: context.palette.textMain,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            'Cihazınızdaki dosyalara göz atmak için tıklayın',
                            style: TextStyle(
                              fontSize: 12,
                              color: context.palette.textPlaceholder,
                            ),
                          ),
                        ],
                      )
                    : Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: context.palette.success.withValues(alpha: 0.15),
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: Icon(
                              Icons.table_chart_rounded,
                              color: context.palette.success,
                              size: 28,
                            ),
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  _selectedFile!.name,
                                  style: TextStyle(
                                    fontSize: 14,
                                    fontWeight: FontWeight.bold,
                                    color: context.palette.textMain,
                                  ),
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                ),
                                const SizedBox(height: 3),
                                Text(
                                  _formatFileSize(_selectedFile!.size),
                                  style: TextStyle(
                                    fontSize: 12,
                                    color: context.palette.success,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          TextButton(
                            onPressed: _isUploading ? null : _pickFile,
                            style: TextButton.styleFrom(
                              foregroundColor: context.palette.accent,
                              textStyle: const TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            child: const Text('Değiştir'),
                          ),
                        ],
                      ),
              ),
            ),

            if (_errorMessage != null) ...[
              const SizedBox(height: 14),
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: context.palette.dangerBg,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: context.palette.dangerBorder),
                ),
                child: Row(
                  children: [
                    Icon(Icons.error_outline_rounded, color: context.palette.danger, size: 20),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        _errorMessage!,
                        style: TextStyle(
                          fontSize: 12,
                          color: context.palette.danger,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ],

            const SizedBox(height: 24),

            // Submit Button
            SizedBox(
              width: double.infinity,
              height: 52,
              child: ElevatedButton(
                onPressed: (_selectedFile == null || _isUploading) ? null : _uploadFile,
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.accentFillDeep,
                  foregroundColor: Colors.white,
                  disabledBackgroundColor: context.palette.border,
                  disabledForegroundColor: context.palette.textPlaceholder,
                  elevation: 0,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(16),
                  ),
                ),
                child: _isUploading
                    ? const Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          SizedBox(
                            width: 20,
                            height: 20,
                            child: CircularProgressIndicator(
                              color: Colors.white,
                              strokeWidth: 2.2,
                            ),
                          ),
                          SizedBox(width: 12),
                          Text(
                            'Yükleniyor ve Analiz Ediliyor...',
                            style: TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.bold,
                              color: Colors.white,
                            ),
                          ),
                        ],
                      )
                    : const Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(Icons.cloud_upload_rounded, size: 20),
                          SizedBox(width: 8),
                          Text(
                            'Yüklemeyi Başlat ve MARC Sorgula',
                            style: TextStyle(
                              fontSize: 15,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
