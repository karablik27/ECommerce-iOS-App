using System;
using System.Threading.Tasks;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrdersService.Consumers;
using OrdersService.Data;
using OrdersService.Models;
using Shared.Contracts;
using Xunit;

namespace Tests;

public class PaymentResultConsumerTests
{
    private static OrdersDbContext Db(string name) =>
        new(new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(name).Options);

    //───────────────────────────────────────────────────────────────
    [Theory(DisplayName = "Статус заказа меняется на FINISHED / CANCELLED")]
    [InlineData(true, "FINISHED")]
    [InlineData(false, "CANCELLED")]
    public async Task Order_Status_Is_Updated(bool isSuccess, string expected)
    {
        var dbName = Guid.NewGuid().ToString();
        var orderId = Guid.NewGuid();

        await using (var dbInit = Db(dbName))
        {
            dbInit.Orders.Add(new Order
            {
                Id = orderId,
                UserId = "u1",
                Amount = 42,
                Description = "t",
                Status = "NEW"
            });
            await dbInit.SaveChangesAsync();
        }

        /* ─── конфигурируем Consumer ДО запуска шины ─── */
        var harness = new InMemoryTestHarness();
        harness.Consumer(() =>
            new PaymentResultConsumer(Db(dbName), NullLogger<PaymentResultConsumer>.Instance));

        await harness.Start();
        try
        {
            await harness.Bus.Publish(new PaymentResultMessage
            {
                OrderId = orderId,
                IsSuccess = isSuccess
            });

            Assert.True(await harness.Consumed.Any<PaymentResultMessage>());

            await using var dbCheck = Db(dbName);
            var order = await dbCheck.Orders.SingleAsync(o => o.Id == orderId);
            Assert.Equal(expected, order.Status);
        }
        finally { await harness.Stop(); }
    }

    //───────────────────────────────────────────────────────────────
    [Fact(DisplayName = "Неизвестный заказ игнорируется")]
    public async Task Unknown_Order_Is_Ignored()
    {
        var dbName = Guid.NewGuid().ToString();

        var harness = new InMemoryTestHarness();
        harness.Consumer(() =>
            new PaymentResultConsumer(Db(dbName), NullLogger<PaymentResultConsumer>.Instance));

        await harness.Start();
        try
        {
            await harness.Bus.Publish(new PaymentResultMessage
            {
                OrderId = Guid.NewGuid(),
                IsSuccess = true
            });

            Assert.True(await harness.Consumed.Any<PaymentResultMessage>());

            await using var db = Db(dbName);
            Assert.Empty(db.Orders);               // в БД ничего не изменилось
        }
        finally { await harness.Stop(); }
    }
}
