namespace MarcToken.Api.Services;

public sealed class AppOptions
{
    public string FrontendUrl { get; set; } = "http://localhost:3000";
    public int TokenCostPerIsbn { get; set; } = 1;
    public string PythonExecutable { get; set; } = "python";
    public int WorkerPollSeconds { get; set; } = 3;
    public int RunningJobTimeoutSeconds { get; set; } = 120;
    public int MaxParallelJobs { get; set; } = 3;
    public int FileRetentionDays { get; set; } = 7;
    public int GuestRetentionDays { get; set; } = 3;
    public int FreeTrialTokens { get; set; } = 5;
    public int MaxTrialsPerIpPerDay { get; set; } = 5;
    /// <summary>Tek işte kabul edilen en fazla ISBN sayısı (0 = sınırsız). Aşırı büyük işlerin kuyruğu kilitlemesini önler.</summary>
    public int MaxIsbnsPerJob { get; set; } = 20000;
    /// <summary>
    /// Motorun ISBN sonuç önbelleği (uploads/_cache/isbn_cache.sqlite). Kapatılırsa her ISBN dış kaynaklardan sorgulanır.
    /// </summary>
    public bool IsbnCacheEnabled { get; set; } = true;
    /// <summary>
    /// Virgülle ayrılmış admin e-posta adresleri listesi.
    /// Boşsa admin paneli devre dışıdır.
    /// </summary>
    public string AdminEmails { get; set; } = string.Empty;

    public IReadOnlyList<string> GetAdminEmailList() =>
        AdminEmails
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(e => e.ToLowerInvariant())
            .ToList();
}

