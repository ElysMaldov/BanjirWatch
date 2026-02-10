namespace BanjirWatch.Services;

/// <summary>
/// Background service that periodically fetches flood data from APIs
/// </summary>
public class FloodDataBackgroundService : BackgroundService
{
    private readonly ILogger<FloodDataBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _fetchInterval = TimeSpan.FromMinutes(30); // Fetch every 30 minutes

    public FloodDataBackgroundService(
        ILogger<FloodDataBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Flood Data Background Service starting...");

        // Initial delay to allow app to fully start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Fetching flood data at: {Time}", DateTime.UtcNow);

                using var scope = _serviceProvider.CreateScope();
                var weatherService = scope.ServiceProvider.GetRequiredService<WeatherService>();

                await weatherService.FetchAndStoreFloodDataAsync();

                _logger.LogInformation("Flood data fetch completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in flood data background service");
            }

            try
            {
                await Task.Delay(_fetchInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Flood Data Background Service stopped");
    }
}
