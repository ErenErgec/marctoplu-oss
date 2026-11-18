import 'dart:convert';
import 'dart:io';
import 'package:http/http.dart' as http;
import 'package:firebase_auth/firebase_auth.dart';
import 'package:device_info_plus/device_info_plus.dart';
import 'package:shared_preferences/shared_preferences.dart';

class ApiService {
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'https://api.marctoplu.com',
  );

  static String? _cachedDeviceId;

  static Future<String> getDeviceId() async {
    if (_cachedDeviceId != null && _cachedDeviceId!.isNotEmpty) {
      return _cachedDeviceId!;
    }

    final prefs = await SharedPreferences.getInstance();
    String? storedId = prefs.getString('app_device_id');

    if (storedId != null && storedId.isNotEmpty) {
      _cachedDeviceId = storedId;
      return storedId;
    }

    try {
      final deviceInfo = DeviceInfoPlugin();
      if (Platform.isAndroid) {
        final androidInfo = await deviceInfo.androidInfo;
        storedId = androidInfo.id;
      } else if (Platform.isIOS) {
        final iosInfo = await deviceInfo.iosInfo;
        storedId = iosInfo.identifierForVendor;
      }
    } catch (_) {}

    if (storedId == null || storedId.isEmpty) {
      storedId = 'dev_${DateTime.now().millisecondsSinceEpoch}_${DateTime.now().microsecond}';
    }

    await prefs.setString('app_device_id', storedId);
    _cachedDeviceId = storedId;
    return storedId;
  }

  Future<Map<String, String>> _getHeaders() async {
    final user = FirebaseAuth.instance.currentUser;
    if (user == null) {
      throw Exception('Kullanıcı oturumu bulunamadı.');
    }
    
    final token = await user.getIdToken(true);
    final deviceId = await getDeviceId();

    return {
      'Authorization': 'Bearer $token',
      'Content-Type': 'application/json',
      'Cache-Control': 'no-cache, no-store, must-revalidate',
      'Pragma': 'no-cache',
      'X-Device-Id': deviceId,
    };
  }

  Future<Map<String, dynamic>> getWallet() async {
    final headers = await _getHeaders();
    final timestamp = DateTime.now().millisecondsSinceEpoch;
    final response = await http.get(Uri.parse('$baseUrl/api/me/wallet?_t=$timestamp'), headers: headers);

    if (response.statusCode == 200) {
      return jsonDecode(response.body);
    } else {
      throw Exception('Bakiye bilgisi alınamadı. (Hata kodu: ${response.statusCode})');
    }
  }

  Future<Map<String, dynamic>> createJobFromIsbns(List<String> isbns) async {
    final headers = await _getHeaders();
    final response = await http.post(
      Uri.parse('$baseUrl/api/jobs/from-isbns'),
      headers: headers,
      body: jsonEncode({'isbns': isbns}),
    );

    if (response.statusCode == 200 || response.statusCode == 201) {
      return jsonDecode(response.body);
    } else {
      String errMsg = 'Barkodlar işleme alınamadı.';
      try {
        final errBody = jsonDecode(response.body);
        if (errBody['message'] != null) {
          errMsg = errBody['message'];
        }
      } catch (_) {}
      throw Exception('$errMsg (Hata kodu: ${response.statusCode})');
    }
  }

  Future<Map<String, dynamic>> uploadJobFile({
    String? filePath,
    List<int>? fileBytes,
    required String fileName,
  }) async {
    final user = FirebaseAuth.instance.currentUser;
    if (user == null) {
      throw Exception('Kullanıcı oturumu bulunamadı.');
    }

    final token = await user.getIdToken(true);
    final deviceId = await getDeviceId();

    final uri = Uri.parse('$baseUrl/api/jobs/upload');
    final request = http.MultipartRequest('POST', uri);
    request.headers.addAll({
      'Authorization': 'Bearer $token',
      'X-Device-Id': deviceId,
    });

    if (filePath != null && filePath.isNotEmpty) {
      request.files.add(await http.MultipartFile.fromPath('file', filePath, filename: fileName));
    } else if (fileBytes != null) {
      request.files.add(http.MultipartFile.fromBytes('file', fileBytes, filename: fileName));
    } else {
      throw Exception('Yüklenecek dosya verisi bulunamadı.');
    }

    final streamedResponse = await request.send();
    final response = await http.Response.fromStream(streamedResponse);

    if (response.statusCode == 200 || response.statusCode == 201) {
      return jsonDecode(response.body);
    } else {
      String errMsg = 'Dosya yüklenemedi.';
      try {
        final errBody = jsonDecode(response.body);
        if (errBody['message'] != null) {
          errMsg = errBody['message'];
        }
      } catch (_) {}
      throw Exception('$errMsg (Hata kodu: ${response.statusCode})');
    }
  }

  Future<List<dynamic>> getJobs() async {
    final headers = await _getHeaders();
    final timestamp = DateTime.now().millisecondsSinceEpoch;
    final response = await http.get(Uri.parse('$baseUrl/api/jobs?limit=10&_t=$timestamp'), headers: headers);

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as List<dynamic>;
    } else {
      return [];
    }
  }

  Future<List<dynamic>> getTokenPackages() async {
    final headers = await _getHeaders();
    final response = await http.get(Uri.parse('$baseUrl/api/payments/packages'), headers: headers);

    if (response.statusCode == 200) {
      return jsonDecode(response.body) as List<dynamic>;
    } else {
      return [];
    }
  }

  Future<void> updateFcmToken(String fcmToken) async {
    try {
      final headers = await _getHeaders();
      await http.post(
        Uri.parse('$baseUrl/api/me/profile/fcm-token'),
        headers: headers,
        body: jsonEncode({'fcmToken': fcmToken}),
      );
    } catch (_) {
      // Sessizce başarısız olabilir, kritik bir işleyiş değil
    }
  }

  Future<List<int>> downloadJobResult(String jobId, {String format = 'xlsx'}) async {
    final headers = await _getHeaders();
    final response = await http.get(
      Uri.parse('$baseUrl/api/jobs/$jobId/download?format=$format'),
      headers: headers,
    );

    if (response.statusCode == 200) {
      return response.bodyBytes;
    } else {
      String errMsg = 'Dosya indirilemedi.';
      try {
        final errBody = jsonDecode(utf8.decode(response.bodyBytes));
        if (errBody is Map && errBody['message'] != null && errBody['message'].toString().trim().isNotEmpty) {
          errMsg = errBody['message'].toString().trim();
        }
      } catch (_) {}
      throw Exception('$errMsg (Hata kodu: ${response.statusCode})');
    }
  }

  /// Kullanıcı hesabını ve ilişkili tüm verileri siler.
  /// Apple App Store (Guideline 5.1.1(v)) ve Google Play gereksinimi.
  Future<void> deleteAccount() async {
    final headers = await _getHeaders();
    final response = await http.delete(
      Uri.parse('$baseUrl/api/me/account'),
      headers: headers,
    );

    if (response.statusCode != 200 && response.statusCode != 204) {
      String errMsg = 'Hesap silinemedi.';
      try {
        final errBody = jsonDecode(response.body);
        if (errBody['message'] != null) {
          errMsg = errBody['message'];
        }
      } catch (_) {}
      throw Exception('$errMsg (Hata kodu: ${response.statusCode})');
    }
  }

  /// In-App Purchase (Google Play Billing / App Store) makbuz doğrulaması
  Future<Map<String, dynamic>> verifyIapReceipt({
    required String productId,
    required String purchaseToken,
    String? receiptData,
    String? transactionId,
    String platform = 'android',
  }) async {
    final headers = await _getHeaders();
    final response = await http.post(
      Uri.parse('$baseUrl/api/payments/iap/verify-receipt'),
      headers: headers,
      body: jsonEncode({
        'productId': productId,
        'purchaseToken': purchaseToken,
        'receiptData': receiptData,
        'transactionId': transactionId,
        'platform': platform,
      }),
    );

    final data = jsonDecode(response.body) as Map<String, dynamic>;
    if (response.statusCode == 200) {
      return data;
    } else {
      throw Exception(data['message'] ?? 'Satın alma doğrulanırken bir hata oluştu.');
    }
  }
}
