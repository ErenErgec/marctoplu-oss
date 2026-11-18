namespace MarcToken.Api.Services;

/// <summary>
/// "Iap" config bölümü: mağaza satın alma doğrulaması için kimlik bilgileri.
/// Bir platformun anahtarları eksikse o platformdaki satın almalar reddedilir (sıkı mod).
/// </summary>
public sealed class IapOptions
{
    public string GooglePlayPackageName { get; set; } = "com.marctoplu.app";

    /// <summary>Play Console'da "finansal verileri görüntüleme" yetkisi olan service account JSON dosyasının yolu.</summary>
    public string? GoogleServiceAccountPath { get; set; }

    public string AppleBundleId { get; set; } = "com.marctoplu.app";
    public string? AppleIssuerId { get; set; }
    public string? AppleKeyId { get; set; }

    /// <summary>App Store Connect'ten indirilen In-App Purchase anahtarının (.p8) yolu.</summary>
    public string? ApplePrivateKeyPath { get; set; }

    /// <summary>
    /// Production'da bulunamayan işlemler sandbox'ta da aranır. App Review ve TestFlight satın almaları
    /// sandbox'ta gerçekleştiği için açık kalmalıdır; kapatılırsa inceleme sırasında satın alma başarısız olur.
    /// </summary>
    public bool AppleAllowSandbox { get; set; } = true;

    public bool IsGooglePlayConfigured =>
        !string.IsNullOrWhiteSpace(GooglePlayPackageName)
        && !string.IsNullOrWhiteSpace(GoogleServiceAccountPath)
        && File.Exists(GoogleServiceAccountPath);

    public bool IsAppStoreConfigured =>
        !string.IsNullOrWhiteSpace(AppleBundleId)
        && !string.IsNullOrWhiteSpace(AppleIssuerId)
        && !string.IsNullOrWhiteSpace(AppleKeyId)
        && !string.IsNullOrWhiteSpace(ApplePrivateKeyPath)
        && File.Exists(ApplePrivateKeyPath);
}
