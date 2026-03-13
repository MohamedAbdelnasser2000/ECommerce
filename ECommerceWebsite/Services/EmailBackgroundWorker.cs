using Microsoft.Extensions.Hosting;

namespace ECommerceWebsite.Services;

public class EmailBackgroundWorker : BackgroundService
{
    private readonly ILogger<EmailBackgroundWorker> _logger;
    private readonly IEmailBackgroundQueue _queue;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public EmailBackgroundWorker(
        ILogger<EmailBackgroundWorker> logger,
        IEmailBackgroundQueue queue,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _queue = queue;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email background worker started");
        await foreach (var item in _queue.DequeueAsync(stoppingToken))
        {
            var attempt = 0;
            var maxAttempts = 5;
            var delayMs = 1000;

            while (!stoppingToken.IsCancellationRequested)
            {
                attempt++;
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    var sent = await emailService.SendEmailAsync(item.ToEmail, item.Subject, item.HtmlContent, item.TextContent);
                    if (sent)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending email to {Email} on attempt {Attempt}", item.ToEmail, attempt);
                }

                if (attempt >= maxAttempts)
                {
                    _logger.LogWarning("Giving up sending email to {Email} after {Attempts} attempts", item.ToEmail, attempt);
                    break;
                }

                try
                {
                    await Task.Delay(delayMs, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                delayMs = Math.Min(delayMs * 2, 30000);
            }
        }
        _logger.LogInformation("Email background worker stopping");
    }
}


