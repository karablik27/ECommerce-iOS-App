using System;
using System.Text.Json;
using System.Threading.Tasks;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentsService.Data;
using PaymentsService.Models;
using PaymentsService.Services;
using Shared.Contracts;
using Xunit;

namespace Tests;

public class OutboxPublisherServiceTests
{
    [Fact(DisplayName = "Outbox-запись помечается SentAt после публикации")]
    public async Task Marks_Message_As_Sent()
    {
        /* ── ОДНО общее имя in-memory БД ─────────────────────────────── */
        var dbName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        services.AddDbContext<PaymentsDbContext>(opt =>
            opt.UseInMemoryDatabase(dbName));
        services.AddLogging();

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        /* ── добавляем неотправленную запись ─────────────────────────── */
        var evt = new PaymentResultMessage { OrderId = Guid.NewGuid(), IsSuccess = true };

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(PaymentResultMessage),
                Payload = JsonSerializer.Serialize(evt),
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        /* ── in-memory шина MassTransit ──────────────────────────────── */
        var harness = new InMemoryTestHarness();
        await harness.Start();

        try
        {
            var svc = new OutboxPublisherService(scopeFactory, harness.Bus);

            await svc.StartAsync(default);
            await Task.Delay(2_000);            // ждём первую итерацию

            /* ── ASSERT: запись осталась и SentAt != null ─────────────── */
            await using var check = scopeFactory.CreateAsyncScope();
            var db = check.ServiceProvider.GetRequiredService<PaymentsDbContext>();
            var outbox = await db.OutboxMessages.SingleAsync();   // теперь точно найдётся
            Assert.NotNull(outbox.SentAt);

            await svc.StopAsync(default);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
