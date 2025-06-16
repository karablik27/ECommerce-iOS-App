using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PaymentsService.Controllers;
using PaymentsService.Data;
using PaymentsService.Models;
using Xunit;

namespace Tests;

public class AccountsControllerTests
{
    private static PaymentsDbContext CreateInMemoryDb()
    {
        var opts = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PaymentsDbContext(opts);
    }

    [Fact(DisplayName = "Create создаёт счёт и возвращает 201")]
    public async Task Create_Returns_Created_Account()
    {
        await using var db = CreateInMemoryDb();
        var ctrl = new AccountsController(db, NullLogger<AccountsController>.Instance);

        var result = await ctrl.Create("u1");

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var acc = Assert.IsType<Account>(created.Value);

        Assert.Equal("u1", acc.UserId);
        Assert.Equal(0m, acc.Balance);

        Assert.Equal(1, await db.Accounts.CountAsync());
    }

    [Fact(DisplayName = "Deposit увеличивает баланс и отдаёт 200")]
    public async Task Deposit_Increases_Balance()
    {
        await using var db = CreateInMemoryDb();
        db.Accounts.Add(new Account
        {
            Id = Guid.NewGuid(),
            UserId = "u1",
            Balance = 100m
        });
        await db.SaveChangesAsync();

        var ctrl = new AccountsController(db, NullLogger<AccountsController>.Instance);

        var result = await ctrl.Deposit("u1", 50m);

        Assert.IsType<OkObjectResult>(result);

        var acc = await db.Accounts.SingleAsync(a => a.UserId == "u1");
        Assert.Equal(150m, acc.Balance);
    }


}
