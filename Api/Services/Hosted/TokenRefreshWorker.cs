namespace Api.Services.Hosted;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Api.Environments;

public class TokenRefreshWorker(ISmartThingsService smartThingsService, ILogger<TokenRefreshWorker> logger) : BackgroundService {

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		logger.LogInformation("Started.");

		while(!stoppingToken.IsCancellationRequested) {
			await smartThingsService.RefreshTokenAsync();

			logger.LogDebug($"Waiting {ProductAppInfo.DelaySmartThingsTokenRefresh} before next check.");

			await Task.Delay(ProductAppInfo.DelaySmartThingsTokenRefresh, stoppingToken);
		}

		logger.LogInformation("Stopping.");
	}
}