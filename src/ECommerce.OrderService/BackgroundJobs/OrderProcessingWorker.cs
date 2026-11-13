using ECommerce.Application.Interfaces.Repositories;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Domain.Enums;

namespace ECommerce.OrderService.BackgroundJobs;

public sealed class OrderProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderProcessingWorker> _logger;

    public OrderProcessingWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<OrderProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = Math.Max(15, _configuration.GetValue("Orders:AutoProcessIntervalSeconds", 60));
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ProcessConfirmedOrdersAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is shutting down.
        }
    }

    private async Task ProcessConfirmedOrdersAsync(CancellationToken ct)
    {
        var afterMinutes = _configuration.GetValue("Orders:AutoProcessAfterMinutes", 2);

        if (afterMinutes <= 0)
        {
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            var pending = await orders.GetByStatusOlderThanAsync(
                OrderStatus.Confirmed,
                DateTime.UtcNow.AddMinutes(-afterMinutes),
                limit: 20,
                ct);

            foreach (var order in pending)
            {
                try
                {
                    await orderService.UpdateStatusAsync(order.Id, OrderStatus.Processing, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Could not auto-process order {OrderId}.", order.Id);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Order processing sweep failed.");
        }
    }
}
