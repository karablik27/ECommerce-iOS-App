// OrderService/Controllers/OrdersController.cs
using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using OrdersService.Models;
using Shared.Contracts;

namespace OrdersService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersDbContext _db;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public OrdersController(OrdersDbContext db) => _db = db;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = req.UserId,
                Amount = req.Amount,
                Description = req.Description
            };

            var evt = new OrderCreatedMessage
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Amount = order.Amount
            };

            var outbox = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(OrderCreatedMessage),
                Payload = JsonSerializer.Serialize(evt, _jsonOptions),
                CreatedAt = DateTime.UtcNow
            };

            await using var tx = await _db.Database.BeginTransactionAsync();
            _db.Orders.Add(order);
            _db.OutboxMessages.Add(outbox);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { order.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _db.Orders.ToListAsync());

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var order = await _db.Orders.FindAsync(id);
            return order == null ? NotFound() : Ok(order);
        }
    }

    public class CreateOrderRequest
    {
        public string UserId { get; set; } = default!;
        public decimal Amount { get; set; }
        public string Description { get; set; } = default!;
    }
}
