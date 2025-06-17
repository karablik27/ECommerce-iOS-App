// OrderService/Controllers/OrdersController.cs
using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<OrdersController> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public OrdersController(OrdersDbContext db, ILogger<OrdersController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.UserId))
                return BadRequest("UserId не может быть пустым.");

            if (req.Amount <= 0)
                return BadRequest("Amount должен быть больше 0.");

            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании заказа.");
                return StatusCode(500, "Внутренняя ошибка сервера при создании заказа.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var orders = await _db.Orders.ToListAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка заказов.");
                return StatusCode(500, "Внутренняя ошибка сервера при получении заказов.");
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var order = await _db.Orders.FindAsync(id);
                return order == null ? NotFound("Заказ не найден.") : Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении заказа по ID.");
                return StatusCode(500, "Внутренняя ошибка сервера при поиске заказа.");
            }
        }
    }

    public class CreateOrderRequest
    {
        public string UserId { get; set; } = default!;
        public decimal Amount { get; set; }
        public string Description { get; set; } = default!;
    }
}
