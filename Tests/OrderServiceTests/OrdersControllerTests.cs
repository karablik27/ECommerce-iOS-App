// Tests/OrderServiceTests/OrdersControllerTests.cs
using System;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;                         // ← SQLite
using Microsoft.EntityFrameworkCore;
using OrdersService.Controllers;
using OrdersService.Data;
using OrdersService.Models;
using Xunit;

namespace Tests;

public class OrdersControllerTests
{
    /* helper: in-memory SQLite с открытым соединением  */
    private static OrdersDbContext SqliteDb(out SqliteConnection conn)
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();                           // база живёт, пока открыт connection

        var opt = new DbContextOptionsBuilder<OrdersDbContext>()
                  .UseSqlite(conn)
                  .Options;

        var db = new OrdersDbContext(opt);
        db.Database.EnsureCreated();           // создаём таблицы
        return db;
    }

    /* helper для «обычного» InMemory (для простых select'ов) */
    private static OrdersDbContext MemDb(string name) =>
        new(new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(name).Options);

    //───────────────────────────────────────────────────────────────
    [Fact(DisplayName = "Create: добавляет Order и Outbox, отдаёт 200")]
    public async Task Create_Adds_Order_And_Outbox()
    {
        using var db = SqliteDb(out var conn);
        var ctl = new OrdersController(db);

        var req = new CreateOrderRequest
        {
            UserId = "u1",
            Amount = 123m,
            Description = "test-desc"
        };

        // Act
        var res = await ctl.Create(req);

        // 200 OK + Guid Id
        var ok = Assert.IsType<OkObjectResult>(res);
        var id = (Guid)ok.Value!.GetType().GetProperty("Id")!.GetValue(ok.Value)!;
        Assert.NotEqual(Guid.Empty, id);

        // БД: по 1 записи в Orders и Outbox
        Assert.Equal(1, await db.Orders.CountAsync());
        Assert.Equal(1, await db.OutboxMessages.CountAsync());

        var order = await db.Orders.SingleAsync();
        Assert.Equal("u1", order.UserId);
        Assert.Equal(123m, order.Amount);
        Assert.Equal("test-desc", order.Description);
        Assert.Equal("NEW", order.Status);

        conn.Close();          // закрыть соединение → база удалится
    }

    //───────────────────────────────────────────────────────────────
    [Fact(DisplayName = "GetAll: возвращает список заказов")]
    public async Task GetAll_Returns_List()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var seed = MemDb(dbName))
        {
            seed.Orders.Add(new Order
            {
                Id = Guid.NewGuid(),
                UserId = "u1",
                Amount = 10,
                Description = "seed"
            });
            await seed.SaveChangesAsync();
        }

        await using var db = MemDb(dbName);
        var ctl = new OrdersController(db);
        var res = await ctl.GetAll();
        var ok = Assert.IsType<OkObjectResult>(res);
        var list = Assert.IsAssignableFrom<IEnumerable>(ok.Value);
        Assert.Single(list);
    }

    //───────────────────────────────────────────────────────────────
    [Fact(DisplayName = "GetById: 404, если заказа нет")]
    public async Task GetById_Returns_404_When_NotFound()
    {
        await using var db = MemDb(Guid.NewGuid().ToString());
        var ctl = new OrdersController(db);

        var res = await ctl.GetById(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(res);
    }
}
