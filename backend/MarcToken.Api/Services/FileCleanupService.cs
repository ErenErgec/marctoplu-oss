using Microsoft.Extensions.Options;
using MarcToken.Api.Data;

namespace MarcToken.Api.Services;

public sealed class FileCleanupService(
    IOptions<AppOptions> options,
    IWebHostEnvironment env,
    ILogger<FileCleanupService> logger) : BackgroundService
{
    private readonly AppOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Günde bir kez çalıştır.
        var delay = TimeSpan.FromHours(24);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CleanUpOldFiles();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Dosya temizleme işlemi sırasında hata oluştu.");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }

    private void CleanUpOldFiles()
    {
        var retentionDays = _options.FileRetentionDays;
        if (retentionDays <= 0)
        {
            // Eğer negatif veya 0 verilmişse dosyaları hiç silme
            return;
        }

        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var uploadsDir = Path.Combine(env.ContentRootPath, "uploads");

        if (!Directory.Exists(uploadsDir))
        {
            return;
        }

        // ISBN önbelleği gibi kalıcı motor verisi saklama süresine tabi değildir
        var cacheDir = Path.Combine(uploadsDir, PythonJobRunner.CacheDirectoryName) + Path.DirectorySeparatorChar;
        var files = Directory.GetFiles(uploadsDir, "*.*", SearchOption.AllDirectories)
            .Where(f => !f.StartsWith(cacheDir, StringComparison.Ordinal));
        var deletedCount = 0;

        foreach (var file in files)
        {
            try
            {
                // .flag dosyaları genelde çalışan job iptalleri için kısa ömürlüdür, onlara da aynı kural geçerli.
                var fi = new FileInfo(file);
                if (fi.LastWriteTimeUtc < cutoffDate)
                {
                    fi.Delete();
                    deletedCount++;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Dosya silinirken hata: {File}", file);
            }
        }

        if (deletedCount > 0)
        {
            logger.LogInformation("Dosya temizleme servisi çalıştı: {RetentionDays} günden eski {Count} dosya silindi.", retentionDays, deletedCount);
        }
    }
}
