
public class MotionEmailBatchService : BackgroundService
{
    private readonly CameraAlertService _alertService;
    private readonly ILogger<MotionEmailBatchService> _logger;

    private readonly List<MotionCapture> _captures = new();
    private readonly object _lock = new();
    private readonly SemaphoreSlim _signal = new(0);

    private static readonly TimeSpan Cooldown =
        TimeSpan.FromSeconds(30);

    public MotionEmailBatchService(
        CameraAlertService alertService,
        ILogger<MotionEmailBatchService> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    // Add image to the current batch
    public void AddCapture(MotionCapture capture)
    {
        lock (_lock)
        {
            bool firstCapture = _captures.Count == 0;

            _captures.Add(capture);

            if (firstCapture)
            {
                _signal.Release();
            }
        }
    }

    // Process collected images in the background
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait for the first image
                await _signal.WaitAsync(stoppingToken);

                // Collect images for 30 seconds
                await Task.Delay(Cooldown, stoppingToken);

                List<MotionCapture> batch;

                lock (_lock)
                {
                    batch = new List<MotionCapture>(_captures);
                    _captures.Clear();
                }

                if (batch.Count == 0)
                    continue;

                try
                {
                    await _alertService.SendMotionBatchAsync(
                        batch,
                        stoppingToken);

                    _logger.LogInformation(
                        "Alarm-E-Mail mit {Count} Bildern versendet.",
                        batch.Count);
                }
                catch (Exception ex) when (
                    ex is not OperationCanceledException)
                {
                    _logger.LogError(
                        ex,
                        "E-Mail-Versand für {Count} Bilder fehlgeschlagen.",
                        batch.Count);
                }
            }
        }
        catch (OperationCanceledException) when (
            stoppingToken.IsCancellationRequested)
        {
            // Stop background processing
        }
    }
}
