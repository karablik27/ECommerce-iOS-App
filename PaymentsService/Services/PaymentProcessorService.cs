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
    public class PaymentProcessorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PaymentProcessorService> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public PaymentProcessorService(IServiceScopeFactory scopeFactory, ILogger<PaymentProcessorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

                // забираем непроцессированные задачи (inbox)
                var inboxes = await db.InboxMessages
                    .Where(x => !x.Processed && x.Type == nameof(OrderCreatedMessage))
                    .OrderBy(x => x.ReceivedAt)
                    .Take(20)
                    .ToListAsync(token);

                foreach (var msg in inboxes)
                {
                    try
                    {
                        var order = JsonSerializer.Deserialize<OrderCreatedMessage>(msg.Payload, _jsonOptions);
                        if (order == null)
                        {
                            msg.Processed = true;
                            continue;
                        }

                        // забираем счёт и сразу сохраняем исходную строку RowVersion
                        var account = await db.Accounts
                            .SingleOrDefaultAsync(a => a.UserId == order.UserId, token);

                        bool success = account != null && account.Balance >= order.Amount;
                        if (success)
                        {
                            account!.Balance -= order.Amount;
                        }

                        // готовим исходящее событие
                        var resultEvt = new PaymentResultMessage
                        {
                            OrderId = order.OrderId,
                            IsSuccess = success
                        };
                        db.OutboxMessages.Add(new OutboxMessage
                        {
                            Id = Guid.NewGuid(),
                            Type = nameof(PaymentResultMessage),
                            Payload = JsonSerializer.Serialize(resultEvt),
                            CreatedAt = DateTime.UtcNow
                        });

                        // помечаем inbox как обработанный
                        msg.Processed = true;

                        // сохраняем все вместе — сюда EF подставит WHERE ... AND RowVersion = @old
                        await db.SaveChangesAsync(token);

                        _logger.LogInformation(
                            "Processed Order {OrderId}: Success={Success}",
                            order.OrderId, success);
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        // версия строки не совпала — кто-то другой уже списал деньги, 
                        // считаем, что транзакция успешно выполнена ранее
                        msg.Processed = true;
                        await db.SaveChangesAsync(token);
                        _logger.LogWarning(ex, "Concurrency conflict on account update, skipping duplicate");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing inbox message {Id}", msg.Id);
                    }
                }

                await Task.Delay(1000, token);
            }
        }
    }
}
