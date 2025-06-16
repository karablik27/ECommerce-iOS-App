using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.Models;
using Microsoft.Extensions.Logging;

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
            try
            {
                if (await _db.Accounts.AnyAsync(a => a.UserId == userId))
                    return Conflict(new { error = "Account already exists" });

                var acc = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 0m };
                _db.Accounts.Add(acc);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetBalance), new { userId }, acc);
            }
            catch (DbUpdateException dbEx)
            {
                // Корневая причина — из InnerException
                var root = dbEx.GetBaseException();
                _logger.LogError(dbEx, "DB error in Create(userId={UserId}): {Root}", userId, root.Message);
                return StatusCode(500, new
                {
                    error = "Database update error",
                    detail = root.Message,
                    stack = root.StackTrace
                });
            }
            catch (Exception ex)
            {
                var root = ex.GetBaseException();
                _logger.LogError(ex, "Unexpected error in Create(userId={UserId}): {Root}", userId, root.Message);
                return StatusCode(500, new
                {
                    error = "Unexpected error",
                    detail = root.Message,
                    stack = root.StackTrace
                });
            }
        }

        // POST /api/Accounts/{userId}/deposit/{amount}
        [HttpPost("{userId}/deposit/{amount:decimal}")]
        public async Task<IActionResult> Deposit(string userId, decimal amount)
        {
            try
            {
                if (amount <= 0)
                    return BadRequest(new { error = "Amount must be greater than 0" });

                var acc = await _db.Accounts.SingleOrDefaultAsync(a => a.UserId == userId);
                if (acc == null)
                    return NotFound(new { error = "Account not found" });

                acc.Balance += amount;
                await _db.SaveChangesAsync();

                return Ok(new { acc.UserId, acc.Balance });
            }
            catch (DbUpdateException dbEx)
            {
                var root = dbEx.GetBaseException();
                _logger.LogError(dbEx, "DB error in Deposit(userId={UserId},amount={Amount}): {Root}", userId, amount, root.Message);
                return StatusCode(500, new
                {
                    error = "Database update error",
                    detail = root.Message,
                    stack = root.StackTrace
                });
            }
            catch (Exception ex)
            {
                var root = ex.GetBaseException();
                _logger.LogError(ex, "Unexpected error in Deposit(userId={UserId},amount={Amount}): {Root}", userId, amount, root.Message);
                return StatusCode(500, new
                {
                    error = "Unexpected error",
                    detail = root.Message,
                    stack = root.StackTrace
                });
            }
        }


        // GET /api/Accounts/{userId}/balance
        [HttpGet("{userId}/balance")]
        public async Task<IActionResult> GetBalance(string userId)
        {
            try
            {
                var acc = await _db.Accounts.SingleOrDefaultAsync(a => a.UserId == userId);
                if (acc == null)
                    return NotFound(new { error = "Account not found" });

                return Ok(new { acc.UserId, acc.Balance });
            }
            catch (DbUpdateException dbEx)
            {
                var root = dbEx.GetBaseException();
                _logger.LogError(dbEx, "DB error in GetBalance(userId={UserId}): {Root}", userId, root.Message);
                return StatusCode(500, new
                {
                    error = "Database query error",
                    detail = root.Message,
                    stack = root.StackTrace
                });
            }
            catch (Exception ex)
            {
                var root = ex.GetBaseException();
                _logger.LogError(ex, "Unexpected error in GetBalance(userId={UserId}): {Root}", userId, root.Message);
                return StatusCode(500, new
                {
                    error = "Unexpected error",
                    detail = root.Message,
                    stack = root.StackTrace
                });
            }
        }
    }
}
