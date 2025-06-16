using System;
using System.Text.Json;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.Models;
using Shared.Contracts;

namespace PaymentsService.Consumers
{
    public class OrderCreatedConsumer : IConsumer<OrderCreatedMessage>
    {
        private readonly PaymentsDbContext _db;

        public OrderCreatedConsumer(PaymentsDbContext db) => _db = db;

        public async Task Consume(ConsumeContext<OrderCreatedMessage> ctx)
        {
            // используем MessageId как PK — гарантируем idempotence
            if (!ctx.MessageId.HasValue)
                return;

            var inbox = new InboxMessage
            {
                Id = ctx.MessageId!.Value,
                Type = nameof(OrderCreatedMessage),
                Payload = JsonSerializer.Serialize(ctx.Message),
                ReceivedAt = DateTime.UtcNow,
                Processed = false
            };

            try
            {
                _db.InboxMessages.Add(inbox);
                await _db.SaveChangesAsync(ctx.CancellationToken);
            }
            catch (DbUpdateException)
            {
            }
        }
    }
}
