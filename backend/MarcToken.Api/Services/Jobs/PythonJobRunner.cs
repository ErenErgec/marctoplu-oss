using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MarcToken.Api.Services;

/// <summary>
/// python/job_runner.py sürecini başlatır; stdout'tan JSON satırlarını okur
/// ({"event":"progress","processedCount":n} ve son {"success":...} payload'ı),
/// cancel flag dosyası belirdiğinde süreci öldürür.
/// </summary>
public sealed class PythonJobRunner(IOptions<AppOptions> options, IWebHostEnvironment env)
{
    private readonly AppOptions _options = options.Value;

    /// <summary>uploads/ altındaki kalıcı motor verisi (ISBN önbelleği); FileCleanupService bu klasörü silmez.</summary>
    public const string CacheDirectoryName = "_cache";

    public string GetCancelFlagPath(Guid jobId)
    {
        var cancelDir = Path.Combine(env.ContentRootPath, "uploads", "_cancel_flags");
        Directory.CreateDirectory(cancelDir);
        return Path.Combine(cancelDir, $"{jobId:N}.flag");
    }

    public async Task<JobRunResult> RunAsync(Guid jobId, string inputPath, string outputPath, Func<int, int, Task> onProgress, CancellationToken ct)
    {
        var scriptPath = Path.Combine(env.ContentRootPath, "python", "job_runner.py");
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException("Python runner script not found.", scriptPath);
        }

        var cancelFlagPath = GetCancelFlagPath(jobId);
        if (File.Exists(cancelFlagPath))
        {
            File.Delete(cancelFlagPath);
        }

        var arguments = $"-u \"{scriptPath}\" --input \"{inputPath}\" --output \"{outputPath}\" --cancel-flag \"{cancelFlagPath}\"";
        if (_options.IsbnCacheEnabled)
        {
            // uploads/ kalıcı volume'da; _cache klasörünü FileCleanupService atlar
            var cachePath = Path.Combine(env.ContentRootPath, "uploads", CacheDirectoryName, "isbn_cache.sqlite");
            arguments += $" --cache-path \"{cachePath}\"";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _options.PythonExecutable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = env.ContentRootPath,
        };
        startInfo.Environment["PYTHONUNBUFFERED"] = "1";

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var stderrLines = new List<string>();
        string? finalPayloadLine = null;
        var processedFromProgress = 0;
        var foundFromProgress = 0;

        var stdoutReaderTask = Task.Run(async () =>
        {
            while (true)
            {
                var line = await process.StandardOutput.ReadLineAsync(ct);
                if (line is null)
                {
                    break;
                }

                var trimmed = line.Trim();
                if (!trimmed.StartsWith('{') || !trimmed.EndsWith('}'))
                {
                    continue;
                }

                try
                {
                    using var doc = JsonDocument.Parse(trimmed);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("event", out var eventNode)
                        && string.Equals(eventNode.GetString(), "progress", StringComparison.OrdinalIgnoreCase)
                        && root.TryGetProperty("processedCount", out var processedNode)
                        && processedNode.TryGetInt32(out var progressed))
                    {
                        processedFromProgress = Math.Max(progressed, processedFromProgress);
                        if (root.TryGetProperty("foundCount", out var foundNode) && foundNode.TryGetInt32(out var found))
                        {
                            foundFromProgress = Math.Max(found, foundFromProgress);
                        }
                    }

                    if (root.TryGetProperty("success", out _))
                    {
                        finalPayloadLine = trimmed;
                    }
                }
                catch
                {
                    // Ignore non-json payload lines from python dependencies.
                }
            }
        }, ct);

        var stderrReaderTask = Task.Run(async () =>
        {
            while (true)
            {
                var line = await process.StandardError.ReadLineAsync(ct);
                if (line is null)
                {
                    break;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    stderrLines.Add(line.Trim());
                }
            }
        }, ct);

        var cancelRequested = false;
        var lastSavedProgress = -1;
        while (!process.HasExited)
        {
            if (processedFromProgress > lastSavedProgress)
            {
                lastSavedProgress = processedFromProgress;
                await onProgress(processedFromProgress, foundFromProgress);
            }

            if (File.Exists(cancelFlagPath))
            {
                cancelRequested = true;
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Process already ended.
                }
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }

        await process.WaitForExitAsync(ct);
        await Task.WhenAll(stdoutReaderTask, stderrReaderTask);

        if (processedFromProgress > lastSavedProgress)
        {
            await onProgress(processedFromProgress, foundFromProgress);
        }

        var stderr = string.Join(Environment.NewLine, stderrLines);
        var jsonLine = finalPayloadLine;

        if (string.IsNullOrWhiteSpace(jsonLine))
        {
            if (cancelRequested)
            {
                return new JobRunResult(false, Math.Max(0, processedFromProgress), string.Empty, "Iptal edildi.", true, Math.Max(0, foundFromProgress));
            }

            throw new InvalidOperationException($"Python runner returned invalid output. stderr: {stderr}");
        }

        var payload = JsonSerializer.Deserialize<JobRunResult>(jsonLine, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("Could not parse python result payload.");

        if (cancelRequested)
        {
            payload = payload with
            {
                Success = false,
                Canceled = true,
                Error = "Iptal edildi.",
                ProcessedCount = Math.Max(payload.ProcessedCount, processedFromProgress),
                FoundCount = Math.Max(payload.FoundCount, foundFromProgress),
            };
        }
        else if (process.ExitCode != 0 && payload.Success)
        {
            payload = payload with { Success = false, Error = string.IsNullOrWhiteSpace(stderr) ? "Python process failed." : stderr };
        }

        if (File.Exists(cancelFlagPath))
        {
            File.Delete(cancelFlagPath);
        }

        return payload;
    }
}

/// <param name="FoundCount">Bulunan ISBN sayısı; ücretlendirme bunun üzerinden yapılır.</param>
public sealed record JobRunResult(bool Success, int ProcessedCount, string OutputPath, string? Error, bool Canceled = false, int FoundCount = 0);
