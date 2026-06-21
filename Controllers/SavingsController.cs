using JiSaveSacco.API.Data;
using JiSaveSacco.API.DTOs.Savings;
using JiSaveSacco.API.Models;
using JiSaveSacco.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JiSaveSacco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SavingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IdentityService _identity;

        public SavingsController(AppDbContext context, IdentityService identity)
        {
            _context = context;
            _identity = identity;
        }

        // =========================
        // ADD TRANSACTION (ADMIN / STAFF)
        // =========================
        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        public async Task<IActionResult> AddTransaction(CreateSavingsDto dto)
        {
            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == dto.MemberId);

            if (member == null)
                return NotFound("Member not found");

            var lastBalance = await _context.SavingsTransactions
                .Where(s => s.MemberId == dto.MemberId)
                .OrderByDescending(s => s.SavingId)
                .Select(s => s.BalanceAfter)
                .FirstOrDefaultAsync();

            decimal newBalance = lastBalance;

            if (dto.TransactionType.ToLower() == "deposit")
            {
                newBalance += dto.Amount;
            }
            else if (dto.TransactionType.ToLower() == "withdrawal")
            {
                if (dto.Amount > lastBalance)
                    return BadRequest("Insufficient funds");

                newBalance -= dto.Amount;
            }
            else
            {
                return BadRequest("Invalid transaction type");
            }

            var transaction = new SavingsTransaction
            {
                MemberId = dto.MemberId,
                Amount = dto.Amount,
                TransactionType = dto.TransactionType,
                BalanceAfter = newBalance,
                TransactionDate = DateTime.UtcNow
            };

            _context.SavingsTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new SavingsResponseDto
            {
                SavingId = transaction.SavingId,
                Amount = transaction.Amount,
                TransactionType = transaction.TransactionType,
                BalanceAfter = transaction.BalanceAfter,
                TransactionDate = transaction.TransactionDate
            });
        }

        // =========================
        // MY SAVINGS STATEMENT (SECURE)
        // =========================
        [Authorize(Roles = "Member")]
        [HttpGet("my-statement")]
        public async Task<IActionResult> GetMyStatement()
        {
            var memberId = _identity.GetMemberId();

            if (memberId == null)
                return Unauthorized("Member not linked to account");

            var transactions = await _context.SavingsTransactions
                .Where(s => s.MemberId == memberId)
                .OrderByDescending(s => s.TransactionDate)
                .Select(s => new SavingsResponseDto
                {
                    SavingId = s.SavingId,
                    Amount = s.Amount,
                    TransactionType = s.TransactionType,
                    BalanceAfter = s.BalanceAfter,
                    TransactionDate = s.TransactionDate
                })
                .ToListAsync();

            return Ok(transactions);
        }
    }
}