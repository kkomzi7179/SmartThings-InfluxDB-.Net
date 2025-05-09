namespace Api.Services.Hosted;

using Microsoft.Extensions.Hosting;

using Api.Environments;
using Api.Services;

public class SmartThingsDataWorker(ISmartThingsService smartThingsService, ILogger<SmartThingsDataWorker> logger) : BackgroundService {

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		logger.LogInformation("Started.");

		while(!stoppingToken.IsCancellationRequested) {
			if(!smartThingsService.Authorized) {
				logger.LogWarning($"Not authorized. Cannot collect data. Waiting {ProductAppInfo.DelaySmartThingsDataCollect} before next");
				await Task.Delay(ProductAppInfo.DelaySmartThingsDataCollect, stoppingToken);
				continue;
			}

			await smartThingsService.UpdateTargetDataAsync();

			logger.LogDebug($"Waiting {ProductAppInfo.DelaySmartThingsDataCollect} before next collection.");

			await Task.Delay(ProductAppInfo.DelaySmartThingsDataCollect, stoppingToken);
		}

		logger.LogInformation("Stopping.");
	}
}