using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentsService.Data;
using PaymentsService.Models;
using Shared.Contracts;

namespace PaymentsService.Services
{
    public class OutboxPublisherService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IPublishEndpoint _publish;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public OutboxPublisherService(IServiceScopeFactory scopeFactory, IPublishEndpoint publish)
        {
            _scopeFactory = scopeFactory;
            _publish = publish;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

                var pending = await db.OutboxMessages
                    .Where(x => x.SentAt == null)
                    .OrderBy(x => x.CreatedAt)
                    .Take(20)
                    .ToListAsync(token);

                foreach (var msg in pending)
                {
                    try
                    {
                        if (msg.Type == nameof(PaymentResultMessage))
                        {
                            var evt = JsonSerializer.Deserialize<PaymentResultMessage>(msg.Payload, _jsonOptions);
                            if (evt != null)
                            {
                                await _publish.Publish(evt, token);
                                msg.SentAt = DateTime.UtcNow;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OutboxPublisherService>>();
                        logger.LogError(ex, "Failed to publish outbox message {Id}", msg.Id);
                    }
                }

                await db.SaveChangesAsync(token);
                await Task.Delay(1000, token);
            }
        }
    }
}
