// OrderService/Consumers/PaymentResultConsumer.cs
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using OrdersService.Models;
using Shared.Contracts;

namespace OrdersService.Consumers
{
    public class PaymentResultConsumer : IConsumer<PaymentResultMessage>
    {
        private readonly OrdersDbContext _db;
        private readonly ILogger<PaymentResultConsumer> _logger;

        public PaymentResultConsumer(OrdersDbContext db, ILogger<PaymentResultConsumer> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentResultMessage> ctx)
        {
            var msg = ctx.Message;
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == msg.OrderId);
            if (order == null)
            {
                _logger.LogWarning("Order not found: {OrderId}", msg.OrderId);
                return;
            }

            order.Status = msg.IsSuccess ? "FINISHED" : "CANCELLED";
            await _db.SaveChangesAsync();
            _logger.LogInformation("Order {OrderId} set to {Status}", order.Id, order.Status);
        }
    }
}
