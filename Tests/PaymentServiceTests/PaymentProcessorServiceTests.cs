using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PaymentsService.Data;
using PaymentsService.Models;
using PaymentsService.Services;
using Shared.Contracts;
using Xunit;

namespace Tests;

public class PaymentProcessorServiceTests
{
    /* помощник: общий InMemory-db для одного теста */
    private static PaymentsDbContext CreateDb(string name) =>
        new(new DbContextOptionsBuilder<PaymentsDbContext>()
                .UseInMemoryDatabase(name).Options);

    /*───────────────────────────────────────────────────────────────────*/
    [Fact(DisplayName = "Успешная оплата списывает деньги и кладёт Outbox")]
    public async Task Successful_Payment_Debits_And_Writes_Outbox()
    {
        var dbName = Guid.NewGuid().ToString();

        /* ─── Account + Inbox ─── */
        await using (var db = CreateDb(dbName))
        {
            db.Accounts.Add(new Account { Id = Guid.NewGuid(), UserId = "u1", Balance = 100m });
            db.InboxMessages.Add(new InboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(OrderCreatedMessage),
                Payload = JsonSerializer.Serialize(
                              new OrderCreatedMessage
                              { OrderId = Guid.NewGuid(), UserId = "u1", Amount = 60m }),
                ReceivedAt = DateTime.UtcNow,
                Processed = false
            });
            await db.SaveChangesAsync();
        }

        /* ─── запускаем сервис ─── */
        var services = new ServiceCollection();
        services.AddDbContext<PaymentsDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddSingleton<PaymentProcessorService>();  // ILogger берётся NullLogger
        services.AddLogging();

        var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<PaymentProcessorService>();

        await svc.StartAsync(default);
        await Task.Delay(1_200);        // ждём первую итерацию
        await svc.StopAsync(default);

        /* ─── проверки ─── */
        await using (var db = CreateDb(dbName))
        {
            var acc = await db.Accounts.SingleAsync(a => a.UserId == "u1");
            Assert.Equal(40m, acc.Balance);                           // списали 60

            var inbox = await db.InboxMessages.SingleAsync();
            Assert.True(inbox.Processed);                             // отмечен обработанным

            var outbox = await db.OutboxMessages.SingleAsync();
            var evt = JsonSerializer.Deserialize<PaymentResultMessage>(outbox.Payload)!;
            Assert.True(evt.IsSuccess);                               // успех
        }
    }

    /*───────────────────────────────────────────────────────────────────*/
    [Fact(DisplayName = "Недостаточно средств ➜ баланс не меняется, IsSuccess=false")]
    public async Task Insufficient_Funds_Writes_Failure()
    {
        var dbName = Guid.NewGuid().ToString();

        await using (var db = CreateDb(dbName))
        {
            db.Accounts.Add(new Account { Id = Guid.NewGuid(), UserId = "u2", Balance = 30m });
            db.InboxMessages.Add(new InboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(OrderCreatedMessage),
                Payload = JsonSerializer.Serialize(
                              new OrderCreatedMessage
                              { OrderId = Guid.NewGuid(), UserId = "u2", Amount = 50m }),
                ReceivedAt = DateTime.UtcNow,
                Processed = false
            });
            await db.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddDbContext<PaymentsDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddSingleton<PaymentProcessorService>();
        services.AddLogging();

        var sp = services.BuildServiceProvider();
        var svc = sp.GetRequiredService<PaymentProcessorService>();

        await svc.StartAsync(default);
        await Task.Delay(1_200);
        await svc.StopAsync(default);

        await using (var db = CreateDb(dbName))
        {
            var acc = await db.Accounts.SingleAsync(a => a.UserId == "u2");
            Assert.Equal(30m, acc.Balance);                           // баланс не изменился

            var outbox = await db.OutboxMessages.SingleAsync();
            var evt = JsonSerializer.Deserialize<PaymentResultMessage>(outbox.Payload)!;
            Assert.False(evt.IsSuccess);                              // отказ
        }
    }
}
