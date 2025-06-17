// PaymentsService/Controllers/AccountsController.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentsService.Data;
using PaymentsService.Models;

namespace PaymentsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly PaymentsDbContext _db;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(PaymentsDbContext db, ILogger<AccountsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // POST /api/Accounts/{userId}
        [HttpPost("{userId}")]
        public async Task<IActionResult> Create(string userId)
        {
            // Проверка: передан ли userId
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest(new { error = "UserId не должен быть пустым" });

            try
            {
                // Проверка: существует ли уже аккаунт с таким userId
                if (await _db.Accounts.AnyAsync(a => a.UserId == userId))
                    return Conflict(new { error = "Аккаунт уже существует" });

                // Создание нового аккаунта
                var acc = new Account
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Balance = 0m
                };

                _db.Accounts.Add(acc);
                await _db.SaveChangesAsync();

                // Возвращаем 201 Created с телом нового аккаунта
                return CreatedAtAction(nameof(GetBalance), new { userId }, acc);
            }
            catch (DbUpdateException dbEx)
            {
                // Обработка ошибки БД (например, при нарушении ограничений)
                var root = dbEx.GetBaseException();
                _logger.LogError(dbEx, "Ошибка базы данных при создании аккаунта для userId={UserId}: {Root}", userId, root.Message);
                return StatusCode(500, new { error = "Ошибка при обновлении базы данных", detail = root.Message });
            }
            catch (Exception ex)
            {
                // Обработка любой другой неожиданной ошибки
                var root = ex.GetBaseException();
                _logger.LogError(ex, "Неожиданная ошибка при создании аккаунта для userId={UserId}: {Root}", userId, root.Message);
                return StatusCode(500, new { error = "Неожиданная ошибка", detail = root.Message });
            }
        }

        // POST /api/Accounts/{userId}/deposit/{amount}
        [HttpPost("{userId}/deposit/{amount:decimal}")]
        public async Task<IActionResult> Deposit(string userId, decimal amount)
        {
            // Проверка: сумма должна быть положительной
            if (amount <= 0)
                return BadRequest(new { error = "Сумма должна быть больше 0" });

            try
            {
                // Поиск аккаунта
                var acc = await _db.Accounts.SingleOrDefaultAsync(a => a.UserId == userId);
                if (acc == null)
                    return NotFound(new { error = "Аккаунт не найден" });

                // Пополнение баланса
                acc.Balance += amount;
                await _db.SaveChangesAsync();

                return Ok(new { acc.UserId, acc.Balance });
            }
            catch (DbUpdateException dbEx)
            {
                var root = dbEx.GetBaseException();
                _logger.LogError(dbEx, "Ошибка БД при пополнении аккаунта userId={UserId}, сумма={Amount}: {Root}", userId, amount, root.Message);
                return StatusCode(500, new { error = "Ошибка при обновлении базы данных", detail = root.Message });
            }
            catch (Exception ex)
            {
                var root = ex.GetBaseException();
                _logger.LogError(ex, "Неожиданная ошибка при пополнении аккаунта userId={UserId}, сумма={Amount}: {Root}", userId, amount, root.Message);
                return StatusCode(500, new { error = "Неожиданная ошибка", detail = root.Message });
            }
        }

        // GET /api/Accounts/{userId}/balance
        [HttpGet("{userId}/balance")]
        public async Task<IActionResult> GetBalance(string userId)
        {
            try
            {
                // Поиск аккаунта по userId
                var acc = await _db.Accounts.SingleOrDefaultAsync(a => a.UserId == userId);
                if (acc == null)
                    return NotFound(new { error = "Аккаунт не найден" });

                return Ok(new { acc.UserId, acc.Balance });
            }
            catch (DbUpdateException dbEx)
            {
                var root = dbEx.GetBaseException();
                _logger.LogError(dbEx, "Ошибка БД при получении баланса для userId={UserId}: {Root}", userId, root.Message);
                return StatusCode(500, new { error = "Ошибка при запросе к базе данных", detail = root.Message });
            }
            catch (Exception ex)
            {
                var root = ex.GetBaseException();
                _logger.LogError(ex, "Неожиданная ошибка при получении баланса для userId={UserId}: {Root}", userId, root.Message);
                return StatusCode(500, new { error = "Неожиданная ошибка", detail = root.Message });
            }
        }
    }
}
