using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrdersService.Data;
using OrdersService.Models;
using Shared.Contracts;

namespace OrdersService.Services
{
    public class OutboxPublisherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxPublisherService> _logger;
        private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        public OutboxPublisherService(IServiceScopeFactory scopeFactory,
                                      ILogger<OutboxPublisherService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                var bus = scope.ServiceProvider.GetRequiredService<IBus>();       // singleton

                var pending = await db.OutboxMessages
                                      .Where(x => x.SentAt == null)
                                      .OrderBy(x => x.CreatedAt)
                                      .Take(20)
                                      .ToListAsync(token);

                foreach (var msg in pending)
                {
                    try
                    {
                        if (msg.Type == nameof(OrderCreatedMessage) &&
                            JsonSerializer.Deserialize<OrderCreatedMessage>(msg.Payload, _json) is { } evt)
                        {
                            await bus.Publish(evt, token);    // публикуем
                            msg.SentAt = DateTime.UtcNow;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish outbox message {Id}", msg.Id);
                    }
                }

                await db.SaveChangesAsync(token);
                await Task.Delay(2_000, token);
            }
        }
    }
}
