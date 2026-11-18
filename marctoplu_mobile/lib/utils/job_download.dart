import 'package:flutter/material.dart';
import 'package:marctoplu_mobile/theme/app_theme.dart';
import 'dart:io';
import 'dart:async';
import 'package:path_provider/path_provider.dart';
import 'package:share_plus/share_plus.dart';
import 'package:marctoplu_mobile/services/api_service.dart';

Future<void> promptDownloadFormatAndShare(BuildContext context, String jobId) async {
  final format = await showModalBottomSheet<String>(
    context: context,
    backgroundColor: context.palette.card,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
    ),
    builder: (ctx) {
      return SafeArea(
        child: Wrap(
          children: [
            Padding(
              padding: const EdgeInsets.all(16.0),
              child: Text(
                'İndirme Formatını Seçin',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: context.palette.textMain),
              ),
            ),
            ListTile(
              leading: Icon(Icons.table_chart_rounded, color: context.palette.success),
              title: const Text('Excel Dosyası (.xlsx)'),
              subtitle: const Text('Tüm ISBN ve MARC detayları'),
              onTap: () => Navigator.pop(ctx, 'xlsx'),
            ),
            ListTile(
              leading: Icon(Icons.local_library_rounded, color: context.palette.accent),
              title: const Text('MARC21 Dosyası (.mrc)'),
              subtitle: const Text('Koha, YORDAM ve kütüphane sistemlerine aktarım'),
              onTap: () => Navigator.pop(ctx, 'mrc'),
            ),
            ListTile(
              leading: Icon(Icons.code_rounded, color: context.palette.accent),
              title: const Text('MARCXML Dosyası (.xml)'),
              subtitle: const Text('XML tabanlı MARC21 kayıtları'),
              onTap: () => Navigator.pop(ctx, 'marcxml'),
            ),
            ListTile(
              leading: Icon(Icons.description_rounded, color: context.palette.textSecondary),
              title: const Text('RIS Dosyası (.ris)'),
              subtitle: const Text('Zotero, EndNote, Mendeley referans formatı'),
              onTap: () => Navigator.pop(ctx, 'ris'),
            ),
          ],
        ),
      );
    },
  );

  if (format != null && context.mounted) {
    await downloadAndShareJob(context, jobId, format: format);
  }
}

/// İndirme biçimi → (dosya uzantısı, MIME türü, görünen ad)
const _formats = <String, (String, String, String)>{
  'xlsx': ('xlsx', 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', 'Excel'),
  'ris': ('ris', 'application/x-research-info-systems', 'RIS'),
  'mrc': ('mrc', 'application/marc', 'MARC21'),
  'marcxml': ('xml', 'application/marcxml+xml', 'MARCXML'),
};

Future<void> downloadAndShareJob(BuildContext context, String jobId, {String format = 'xlsx'}) async {
  final (ext, mime, label) = _formats[format.toLowerCase()] ?? _formats['xlsx']!;
  try {
    if (context.mounted) {
      ScaffoldMessenger.of(context).hideCurrentSnackBar();
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('$label hazırlanıyor ve indiriliyor...'), duration: const Duration(seconds: 2)),
      );
    }
    final bytes = await ApiService().downloadJobResult(jobId, format: format);
    final dir = await getTemporaryDirectory();
    final file = File('${dir.path}/marc_sonuc_$jobId.$ext');
    await file.writeAsBytes(bytes, flush: true);
    
    if (context.mounted) {
      ScaffoldMessenger.of(context).hideCurrentSnackBar();
    }

    Rect? origin;
    if (context.mounted) {
      final renderObj = context.findRenderObject();
      if (renderObj is RenderBox && renderObj.hasSize) {
        origin = renderObj.localToGlobal(Offset.zero) & renderObj.size;
      }
    }

    await Share.shareXFiles(
      [
        XFile(
          file.path,
          mimeType: mime,
          name: 'marc_sonuc_$jobId.$ext',
        ),
      ],
      text: 'MARC Sonuç Dosyası ($label)',
      sharePositionOrigin: origin,
    );
  } catch (e) {
    debugPrint('İndirme hatası ($jobId): $e');
    if (context.mounted) {
      ScaffoldMessenger.of(context).hideCurrentSnackBar();
      final cleanMsg = e.toString().replaceFirst('Exception: ', '');
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('İndirme hatası: $cleanMsg'),
          backgroundColor: Colors.redAccent,
          duration: const Duration(seconds: 4),
        ),
      );
    }
  }
}
