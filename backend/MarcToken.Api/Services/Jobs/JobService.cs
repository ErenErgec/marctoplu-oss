using ClosedXML.Excel;
using MarcToken.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MarcToken.Api.Services;

public sealed class JobService(
    AppDbContext db,
    TokenService tokenService,
    PythonJobRunner pythonRunner,
    JobNotificationSender notificationSender,
    IOptions<AppOptions> options,
    IWebHostEnvironment env,
    ILogger<JobService> logger)
{
    private readonly AppOptions _options = options.Value;

    public async Task<Job> CreateJobAsync(Guid userId, IFormFile file, CancellationToken ct = default)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("Uploaded file is empty.");
        }

        JobFileInspector.ValidateUploadFile(file);
        // Dosya ayrıştırılmadan önce: bakiyesi olmayan hesaplar sunucuya ağır Excel ayrıştırması yaptıramasın
        await EnsureHasBalanceAsync(userId, ct);

        var uploadDir = Path.Combine(env.ContentRootPath, "uploads", userId.ToString("N"));
        Directory.CreateDirectory(uploadDir);

        var jobId = Guid.NewGuid();
        var inputPath = Path.Combine(uploadDir, $"{jobId}_input.xlsx");
        await using (var stream = File.Create(inputPath))
        {
            await file.CopyToAsync(stream, ct);
        }

        int totalIsbns;
        try
        {
            JobFileInspector.EnsureSafeXlsx(inputPath);
            totalIsbns = JobFileInspector.CountIsbns(inputPath);
            if (totalIsbns <= 0)
            {
                throw new InvalidOperationException("No valid ISBN found in excel file.");
            }
            EnsureWithinIsbnLimit(totalIsbns);
        }
        catch
        {
            TryDeleteFile(inputPath);
            throw;
        }

        return await CreateJobCoreAsync(userId, jobId, inputPath, totalIsbns, ct);
    }

    public async Task<Job> CreateJobFromIsbnsAsync(Guid userId, List<string> isbns, CancellationToken ct = default)
    {
        var uniqueIsbns = isbns
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (uniqueIsbns.Count == 0)
        {
            throw new InvalidOperationException("Gecerli bir ISBN bulunamadi.");
        }

        EnsureWithinIsbnLimit(uniqueIsbns.Count);
        await EnsureHasBalanceAsync(userId, ct);

        var uploadDir = Path.Combine(env.ContentRootPath, "uploads", userId.ToString("N"));
        Directory.CreateDirectory(uploadDir);

        var jobId = Guid.NewGuid();
        var inputPath = Path.Combine(uploadDir, $"{jobId}_input.xlsx");

        using (var wb = new XLWorkbook())
        {
            var ws = wb.Worksheets.Add("ISBNS");
            ws.Cell(1, 1).Value = "ISBN";
            for (int i = 0; i < uniqueIsbns.Count; i++)
            {
                var cell = ws.Cell(i + 2, 1);
                cell.Value = uniqueIsbns[i];
            }
            wb.SaveAs(inputPath);
        }

        return await CreateJobCoreAsync(userId, jobId, inputPath, uniqueIsbns.Count, ct);
    }

    private async Task<Job> CreateJobCoreAsync(Guid userId, Guid jobId, string inputPath, int totalIsbns, CancellationToken ct)
    {
        var estimatedTokens = totalIsbns * Math.Max(1, _options.TokenCostPerIsbn);
        var job = new Job
        {
            Id = jobId,
            UserId = userId,
            Status = "queued",
            TotalIsbns = totalIsbns,
            ProcessedIsbns = 0,
            EstimatedTokens = estimatedTokens,
            ConsumedTokens = 0,
            RefundedTokens = 0,
            InputFilePath = inputPath,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        // İş kaydı ve token düşümü tek transaction'da: bakiye yetmezse iş hiç görünmez, böylece worker
        // ödemesi alınmamış bir işi kuyruktan alamaz.
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            db.Jobs.Add(job);
            await db.SaveChangesAsync(ct);
            await tokenService.ConsumeAsync(userId, jobId, estimatedTokens, "job_consume_upfront", ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            db.ChangeTracker.Clear();
            TryDeleteFile(inputPath);
            throw;
        }
        return job;
    }

    private async Task EnsureHasBalanceAsync(Guid userId, CancellationToken ct)
    {
        if (await tokenService.PeekBalanceAsync(userId, ct) < Math.Max(1, _options.TokenCostPerIsbn))
        {
            throw new InvalidOperationException("Yetersiz token bakiyesi. Lütfen token satın alın.");
        }
    }

    private void EnsureWithinIsbnLimit(int count)
    {
        if (_options.MaxIsbnsPerJob > 0 && count > _options.MaxIsbnsPerJob)
        {
            throw new InvalidOperationException(
                $"Tek seferde en fazla {_options.MaxIsbnsPerJob} ISBN işlenebilir. Lütfen listenizi bölerek yükleyin.");
        }
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Geçici iş dosyası silinemedi: {Path}", path);
        }
    }

    /// <summary>
    /// İşi satır kilidiyle (SELECT … FOR UPDATE) güncel haliyle okur ve <paramref name="action"/>'ı aynı transaction'da
    /// çalıştırıp kaydeder. Aynı iş üzerindeki eşzamanlı durum geçişleri (paralel iptal istekleri, iptal ↔ tamamlanma,
    /// zaman aşımı kurtarma) sıraya girer ve her biri öncekinin sonucunu görür; böylece bir iş iki kez iade edilemez.
    /// Kilitli satır worker'ın FOR UPDATE SKIP LOCKED sorgusunda atlanır.
    /// </summary>
    private async Task<T> WithLockedJobAsync<T>(Guid jobId, Func<Job?, Task<T>> action, CancellationToken ct)
    {
        // Bağlamda daha önce yüklenmiş (bayat) bir kopya varsa EF onu döndürür; kilitli okuma güncel satırı görmeli.
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var job = await db.Jobs
            .FromSqlInterpolated($"select * from jobs where id = {jobId} for update")
            .FirstOrDefaultAsync(ct);
        var result = await action(job);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return result;
    }

    public async Task<List<Job>> ListJobsAsync(Guid userId, int limit, CancellationToken ct = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 100);
        return await db.Jobs
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(safeLimit)
            .ToListAsync(ct);
    }

    public async Task<Job?> GetJobAsync(Guid userId, Guid jobId, CancellationToken ct = default)
        => await db.Jobs.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == jobId, ct);

    public async Task<bool> DeleteJobAsync(Guid userId, Guid jobId, CancellationToken ct = default)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == jobId, ct);
        if (job is null) return false;

        var status = job.Status.ToLowerInvariant();
        if (status is "queued" or "running")
            throw new InvalidOperationException("Aktif bir iş silinemez. Önce iptal edin.");

        db.Jobs.Remove(job);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<JobDownloadResult?> GetJobDownloadAsync(Guid userId, Guid jobId, string format = "xlsx", CancellationToken ct = default)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == jobId, ct);
        if (job is null)
        {
            return null;
        }

        if (!string.Equals(job.Status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("İndirme sadece tamamlanan işlemler için kullanılabilir.");
        }

        string? resolvedXlsxPath = null;
        if (!string.IsNullOrWhiteSpace(job.OutputFilePath) && File.Exists(job.OutputFilePath))
        {
            resolvedXlsxPath = job.OutputFilePath;
        }
        else
        {
            // Check possible alternate locations (e.g. storage vs uploads, userId folder vs flat)
            var candidatePaths = new[]
            {
                Path.Combine(env.ContentRootPath, "uploads", userId.ToString("N"), $"{job.Id}_output.xlsx"),
                Path.Combine(env.ContentRootPath, "storage", userId.ToString("N"), $"{job.Id}_output.xlsx"),
                Path.Combine(env.ContentRootPath, "uploads", $"{job.Id}_output.xlsx"),
                Path.Combine(env.ContentRootPath, "storage", $"{job.Id}_output.xlsx"),
            };
            resolvedXlsxPath = candidatePaths.FirstOrDefault(File.Exists);
        }

        if (resolvedXlsxPath is null)
        {
            throw new InvalidOperationException("Sonuç dosyası sunucuda bulunamadı. Sunucu güncellemesi veya saklama süresi dolduğu için silinmiş olabilir.");
        }

        // MARC21 çıktıları motor tarafından Excel'in yanına yazılır (.mrc ISO 2709, .xml MARCXML)
        var marcFormat = format?.ToLowerInvariant() switch
        {
            "mrc" => (Extension: ".mrc", ContentType: "application/marc"),
            "marcxml" => (Extension: ".xml", ContentType: "application/marcxml+xml; charset=utf-8"),
            _ => ((string Extension, string ContentType)?)null,
        };
        if (marcFormat is { } marc)
        {
            var marcPath = Path.ChangeExtension(resolvedXlsxPath, marc.Extension);
            if (!File.Exists(marcPath))
            {
                throw new InvalidOperationException(
                    "Bu iş MARC çıktısı özelliğinden önce işlendiği için MARC dosyası yok. ISBN listesini yeniden yükleyerek MARC21 dosyası alabilirsiniz.");
            }

            // Kayıt sayısı her iki biçimde aynıdır; .mrc'deki kayıt sonlandırıcılarından sayılır
            var foundCount = JobFileInspector.CountMarcRecords(Path.ChangeExtension(resolvedXlsxPath, ".mrc"));
            return new JobDownloadResult(
                job,
                marcPath,
                marc.ContentType,
                $"{job.Id}_output{marc.Extension}",
                foundCount,
                Math.Max(0, job.TotalIsbns - foundCount)
            );
        }

        var isRis = string.Equals(format, "ris", StringComparison.OrdinalIgnoreCase);
        if (isRis)
        {
            var risPath = Path.ChangeExtension(resolvedXlsxPath, ".ris");
            int foundCount;
            int notFoundCount;

            if (!File.Exists(risPath))
            {
                (foundCount, notFoundCount) = JobFileInspector.GenerateRisFromExcel(resolvedXlsxPath, risPath);
            }
            else
            {
                (foundCount, notFoundCount) = JobFileInspector.CountRisRecords(risPath, job.TotalIsbns);
            }

            return new JobDownloadResult(
                job,
                risPath,
                "application/x-research-info-systems; charset=utf-8",
                $"{job.Id}_output.ris",
                foundCount,
                notFoundCount
            );
        }

        var foundInJob = job.ProcessedIsbns;
        var notFoundInJob = Math.Max(0, job.TotalIsbns - foundInJob);
        return new JobDownloadResult(
            job,
            resolvedXlsxPath,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{job.Id}_output.xlsx",
            foundInJob,
            notFoundInJob
        );
    }

    public Task<Job?> CancelJobAsync(Guid userId, Guid jobId, CancellationToken ct = default)
        => WithLockedJobAsync(jobId, async job =>
        {
            if (job is null || job.UserId != userId)
            {
                return null;
            }

            var status = job.Status.ToLowerInvariant();
            if (status == "queued")
            {
                var refund = Math.Max(0, job.EstimatedTokens - job.ConsumedTokens - job.RefundedTokens);
                if (refund > 0)
                {
                    await tokenService.CreditAsync(job.UserId, null, job.Id, refund, "job_refund_cancel_queued", ct);
                    job.RefundedTokens += refund;
                }

                job.Status = "cancelled";
                job.ErrorMessage = "Kullanici tarafindan iptal edildi.";
                job.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (status == "running")
            {
                // İade, motor durduktan sonra ProcessJobAsync'te bulunan kayıt sayısına göre yapılır
                var cancelFlagPath = pythonRunner.GetCancelFlagPath(job.Id);
                await File.WriteAllTextAsync(cancelFlagPath, "cancel", ct);
                job.ErrorMessage = "Iptal istegi alindi.";
                job.UpdatedAt = DateTimeOffset.UtcNow;
            }

            return job;
        }, ct);

    /// <summary>
    /// Sıradaki queued job'ı DB'den alır ve status'ünü 'running' olarak işaretler.
    /// Başka bir worker aynı job'ı almasın diye FOR UPDATE SKIP LOCKED kullanılır.
    /// </summary>
    /// <returns>Alınan job; kuyrukta iş yoksa null</returns>
    public async Task<Job?> TryPickNextQueuedJobAsync(CancellationToken ct = default)
    {
        await RecoverStaleRunningJobsAsync(ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var job = await db.Jobs
            .FromSqlRaw("select * from jobs where status = 'queued' order by created_at asc limit 1 for update skip locked")
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (job is null)
        {
            await tx.RollbackAsync(ct);
            return null;
        }

        job.Status = "running";
        job.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return job;
    }

    /// <summary>
    /// Daha önce 'running' olarak işaretlenmiş bir job'ı çalıştırır.
    /// Bu metod ayrı bir DI scope'ta çağrılmalıdır.
    /// </summary>
    public async Task RunPickedJobAsync(Job job, CancellationToken ct = default)
    {
        await ProcessJobAsync(job, ct);
    }

    private async Task RecoverStaleRunningJobsAsync(CancellationToken ct)
    {
        var timeoutSeconds = Math.Max(30, _options.RunningJobTimeoutSeconds);
        var staleThreshold = DateTimeOffset.UtcNow.AddSeconds(-timeoutSeconds);

        var staleJobIds = await db.Jobs
            .AsNoTracking()
            .Where(x => x.Status == "running" && x.UpdatedAt < staleThreshold)
            .Select(x => x.Id)
            .ToListAsync(ct);

        foreach (var staleJobId in staleJobIds)
        {
            await WithLockedJobAsync(staleJobId, async staleJob =>
            {
                // Kilit beklenirken iş bitmiş veya ilerleme yazmış olabilir
                if (staleJob is null || staleJob.Status != "running" || staleJob.UpdatedAt >= staleThreshold)
                {
                    return false;
                }

                // Çıktı teslim edilmedi: canlı ilerlemede gösterilen tüketim dahil tamamı iade edilir
                staleJob.ConsumedTokens = 0;
                var refund = Math.Max(0, staleJob.EstimatedTokens - staleJob.RefundedTokens);
                if (refund > 0)
                {
                    await tokenService.CreditAsync(staleJob.UserId, null, staleJob.Id, refund, "job_refund_stale_running", ct);
                    staleJob.RefundedTokens += refund;
                }

                staleJob.Status = "failed";
                staleJob.ErrorMessage = "Islem zaman asimina ugradi ve otomatik sonlandirildi.";
                staleJob.UpdatedAt = DateTimeOffset.UtcNow;
                return true;
            }, ct);
        }
    }

    private async Task ProcessJobAsync(Job job, CancellationToken ct)
    {
        try
        {
            var outputPath = Path.Combine(Path.GetDirectoryName(job.InputFilePath) ?? env.ContentRootPath, $"{job.Id}_output.xlsx");
            var result = await pythonRunner.RunAsync(
                job.Id, job.InputFilePath, outputPath,
                (count, found) => UpdateRunningProgressAsync(job.Id, count, found, ct),
                ct);

            // Ücret yalnızca bulunan ISBN'ler için ve yalnızca kullanıcıya çıktı teslim edildiyse alınır.
            // Bulunamayan/hatalı ISBN'ler ile çıktısız biten (iptal, hata) işlerin tokeni iade edilir.
            var costPerIsbn = Math.Max(1, _options.TokenCostPerIsbn);
            var processed = Math.Max(0, result.ProcessedCount);
            var hasOutput = !string.IsNullOrWhiteSpace(result.OutputPath) && File.Exists(result.OutputPath);
            var billable = hasOutput ? Math.Clamp(result.FoundCount, 0, processed) : 0;
            var consumedTokens = billable * costPerIsbn;
            var finalized = await WithLockedJobAsync(job.Id, async tracked =>
            {
                if (tracked is null || tracked.Status is "completed" or "failed" or "cancelled")
                {
                    logger.LogInformation("Job already finalized. JobId: {JobId}, Status: {Status}", job.Id, tracked?.Status);
                    return null;
                }

                var refundTotal = Math.Max(0, tracked.EstimatedTokens - consumedTokens);
                var additionalRefund = Math.Max(0, refundTotal - tracked.RefundedTokens);
                if (additionalRefund > 0)
                {
                    var refundReason = result.Canceled ? "job_refund_cancel_running" : "job_refund";
                    await tokenService.CreditAsync(tracked.UserId, null, tracked.Id, additionalRefund, refundReason, ct);
                }

                tracked.ProcessedIsbns = processed;
                tracked.ConsumedTokens = consumedTokens;
                tracked.RefundedTokens += additionalRefund;
                tracked.OutputFilePath = string.IsNullOrWhiteSpace(result.OutputPath) ? null : result.OutputPath;
                tracked.Status = result.Canceled ? "cancelled" : (result.Success ? "completed" : "failed");
                tracked.ErrorMessage = result.Canceled ? "Kullanici tarafindan iptal edildi." : (result.Success ? null : result.Error);
                tracked.UpdatedAt = DateTimeOffset.UtcNow;
                return tracked;
            }, ct);

            if (finalized is not null)
            {
                await notificationSender.TrySendAsync(finalized.UserId, finalized.Status, finalized.ProcessedIsbns);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job processing failed. JobId: {JobId}", job.Id);
            var failed = await WithLockedJobAsync(job.Id, async tracked =>
            {
                if (tracked is null || tracked.Status is "completed" or "failed" or "cancelled")
                {
                    return null;
                }

                // Çıktı teslim edilmedi: tamamı iade edilir
                tracked.ConsumedTokens = 0;
                var refund = Math.Max(0, tracked.EstimatedTokens - tracked.RefundedTokens);
                if (refund > 0)
                {
                    await tokenService.CreditAsync(tracked.UserId, null, tracked.Id, refund, "job_refund_failed", CancellationToken.None);
                    tracked.RefundedTokens += refund;
                }

                tracked.Status = "failed";
                tracked.ErrorMessage = ex.Message;
                tracked.UpdatedAt = DateTimeOffset.UtcNow;
                return tracked;
            }, CancellationToken.None);

            if (failed is not null)
            {
                await notificationSender.TrySendAsync(failed.UserId, "failed", 0);
            }
        }
    }

    private async Task UpdateRunningProgressAsync(Guid jobId, int processedCount, int foundCount, CancellationToken ct)
    {
        var tracked = await db.Jobs.FirstOrDefaultAsync(x => x.Id == jobId, ct);
        if (tracked is null)
        {
            return;
        }

        if (processedCount <= tracked.ProcessedIsbns)
        {
            return;
        }

        tracked.ProcessedIsbns = Math.Min(processedCount, tracked.TotalIsbns);
        // Canlı gösterim: şu ana kadar bulunanlar (kesin tüketim iş bitince hesaplanır)
        tracked.ConsumedTokens = Math.Min(foundCount, tracked.ProcessedIsbns) * Math.Max(1, _options.TokenCostPerIsbn);
        tracked.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

public sealed record JobDownloadResult(
    Job Job,
    string FilePath,
    string ContentType,
    string DownloadFileName,
    int FoundCount,
    int NotFoundCount
);
