namespace SurveillanceBackend;

public sealed class CaptureStorage
{
    public static readonly TimeSpan RetentionPeriod = TimeSpan.FromHours(72);
    public const long MaximumStorageBytes = 20_000_000_000L;

    private readonly string _captureDirectory;
    private readonly ILogger<CaptureStorage> _logger;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    public CaptureStorage(
        IHostEnvironment environment,
        ILogger<CaptureStorage> logger)
    {
        _captureDirectory = Path.Combine(environment.ContentRootPath, "captures");
        _logger = logger;
        Directory.CreateDirectory(_captureDirectory);
    }

    public async Task<string> SaveAsync(Stream image, CancellationToken cancellationToken)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var fileName = $"motion-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.jpg";
            var temporaryPath = Path.Combine(_captureDirectory, $"{fileName}.tmp");
            var filePath = Path.Combine(_captureDirectory, fileName);

            try
            {
                await using (var output = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 64 * 1024,
                    useAsync: true))
                {
                    await image.CopyToAsync(output, cancellationToken);
                }

                File.Move(temporaryPath, filePath);
            }
            catch
            {
                TryDeleteTemporaryFile(temporaryPath, _logger);
                throw;
            }

            return fileName;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task CleanupAsync(ILogger logger, CancellationToken cancellationToken)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var captures = GetCaptures();
            var expiration = DateTime.UtcNow - RetentionPeriod;

            foreach (var capture in captures.Where(capture => capture.LastWriteTimeUtc < expiration))
            {
                DeleteCapture(capture, logger);
            }

            captures = GetCaptures();
            var totalBytes = captures.Sum(capture => capture.Length);
            foreach (var capture in captures.OrderBy(capture => capture.LastWriteTimeUtc))
            {
                if (totalBytes <= MaximumStorageBytes)
                {
                    break;
                }

                if (DeleteCapture(capture, logger))
                {
                    totalBytes -= capture.Length;
                }
            }
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private FileInfo[] GetCaptures()
    {
        return new DirectoryInfo(_captureDirectory)
            .EnumerateFiles("*.jpg", SearchOption.TopDirectoryOnly)
            .ToArray();
    }

    private static bool DeleteCapture(FileInfo capture, ILogger logger)
    {
        try
        {
            capture.Delete();
            logger.LogInformation("Aufnahme gelöscht: {Capture}", capture.Name);
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Aufnahme konnte nicht gelöscht werden: {Capture}", capture.FullName);
            return false;
        }
    }

    private static void TryDeleteTemporaryFile(string path, ILogger logger)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Temporäre Aufnahme konnte nicht gelöscht werden: {Path}", path);
        }
    }
}

public sealed class CaptureCleanupService(
    CaptureStorage storage,
    ILogger<CaptureCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(CleanupInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCleanupAsync(stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            await storage.CleanupAsync(logger, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Die Bereinigung der Aufnahmen ist fehlgeschlagen.");
        }
    }
}
