using Microsoft.Extensions.Options;

namespace MarcToken.Api.Services;

/// <summary>
/// Arka planda sıradaki job'ları işleyen hosted servis.
/// MaxParallelJobs ayarıyla eş zamanlı olarak birden fazla iş çalıştırabilir.
/// </summary>
public sealed class JobWorkerService(
    IServiceScopeFactory scopeFactory,
    IOptions<AppOptions> options,
    ILogger<JobWorkerService> logger) : BackgroundService
{
    private readonly AppOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delaySeconds = Math.Max(1, _options.WorkerPollSeconds);
        var maxParallel = Math.Max(1, _options.MaxParallelJobs);
        using var semaphore = new SemaphoreSlim(maxParallel, maxParallel);

        logger.LogInformation(
            "Job worker başlatıldı. MaxParallelJobs={Max}, PollSeconds={Poll}",
            maxParallel, delaySeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Kapasite müsaitse hemen bir sonraki job'ı al
                if (semaphore.CurrentCount > 0)
                {
                    var picked = await TryPickAndRunJobAsync(semaphore, stoppingToken);
                    if (!picked)
                    {
                        // Kuyrukta iş yok — biraz bekle
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                    }
                    // Kuyrukta iş varsa ve kapasite varsa anında tekrar dön (bekleme yapma)
                }
                else
                {
                    // Tüm slotlar dolu — kısa bekle, sonra tekrar kontrol et
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Job worker döngüsünde beklenmeyen hata.");
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
        }

        logger.LogInformation("Job worker durduruluyor — aktif işler tamamlanıyor...");
    }

    /// <summary>
    /// Sıradaki queued job'ı alır ve arka planda çalıştırır.
    /// </summary>
    /// <returns>true: job alındı ve başlatıldı; false: sırada iş yok</returns>
    private async Task<bool> TryPickAndRunJobAsync(SemaphoreSlim semaphore, CancellationToken stoppingToken)
    {
        // Slot rezerve et
        await semaphore.WaitAsync(stoppingToken);

        bool jobStarted = false;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var jobService = scope.ServiceProvider.GetRequiredService<JobService>();
            var picked = await jobService.TryPickNextQueuedJobAsync(stoppingToken);

            if (picked is null)
            {
                return false;
            }

            jobStarted = true;
            var jobId = picked.Id;

            // Job'ı arka planda çalıştır — tamamlandığında slot serbest bırakılır
            _ = Task.Run(async () =>
            {
                try
                {
                    using var jobScope = scopeFactory.CreateScope();
                    var js = jobScope.ServiceProvider.GetRequiredService<JobService>();
                    await js.RunPickedJobAsync(picked, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Arka plan job işleme hatası. JobId={JobId}", jobId);
                }
                finally
                {
                    semaphore.Release();
                }
            }, stoppingToken);

            return true;
        }
        finally
        {
            if (!jobStarted)
            {
                semaphore.Release();
            }
        }
    }
}
