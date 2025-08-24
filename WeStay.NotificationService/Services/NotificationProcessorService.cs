using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeStay.NotificationService.Services.Interfaces;

namespace WeStay.NotificationService.Services
{
    public class NotificationProcessorService : BackgroundService
    {
        private readonly ILogger<NotificationProcessorService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(1); // Process every minute

        public NotificationProcessorService(
            ILogger<NotificationProcessorService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Processor Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationServices>();
                        await notificationService.ProcessPendingNotificationsAsync();
                    }

                    _logger.LogInformation("Processed pending notifications. Waiting for next interval...");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred processing pending notifications");
                }

                await Task.Delay(_processingInterval, stoppingToken);
            }

            _logger.LogInformation("Notification Processor Service is stopping.");
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Processor Service is stopping.");
            await base.StopAsync(stoppingToken);
        }
    }
}