using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Consumers;
using PaymentsService.Data;
using Shared.Contracts;
using Xunit;

namespace Tests;

public class OrderCreatedConsumerTests
{
    //─────────────────────────────────────────────────────────────
    [Fact(DisplayName = "Inbox-запись создаётся при первом сообщении")]
    public async Task Adds_Inbox_Record_On_First_Message()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new PaymentsDbContext(options);

        var harness = new InMemoryTestHarness();
        harness.Consumer(() => new OrderCreatedConsumer(db));

        await harness.Start();
        try
        {
            var msgId = Guid.NewGuid();

            await harness.Bus.Publish(
                new OrderCreatedMessage { OrderId = Guid.NewGuid(), UserId = "u1", Amount = 1_000 },
                ctx => ctx.MessageId = msgId);

            Assert.True(await harness.Consumed.Any<OrderCreatedMessage>());

            Assert.Equal(1, await db.InboxMessages.CountAsync());
            var rec = await db.InboxMessages.SingleAsync();
            Assert.Equal(msgId, rec.Id);
            Assert.False(rec.Processed);
        }
        finally
        {
            await harness.Stop();
        }
    }

    //─────────────────────────────────────────────────────────────
    [Fact(DisplayName = "Дубликат с тем же MessageId игнорируется")]
    public async Task Duplicate_Message_Is_Ignored()
    {
        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new PaymentsDbContext(options);

        var harness = new InMemoryTestHarness();
        harness.Consumer(() => new OrderCreatedConsumer(db));

        await harness.Start();
        try
        {
            var msgId = Guid.NewGuid();

            // публикуем одно и то же MessageId дважды
            for (int i = 0; i < 2; i++)
            {
                await harness.Bus.Publish(
                    new OrderCreatedMessage { OrderId = Guid.NewGuid(), UserId = "u1", Amount = 500 },
                    ctx => ctx.MessageId = msgId);
            }

            // консьюмер получил хотя бы одно из сообщений
            Assert.True(await harness.Consumed.Any<OrderCreatedMessage>());

            // в Inbox по-прежнему одна запись — дубликат отброшен
            Assert.Equal(1, await db.InboxMessages.CountAsync());
        }
        finally
        {
            await harness.Stop();
        }
    }
}
