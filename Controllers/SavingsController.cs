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
        private readonly AuditService _audit;
        private readonly NotificationService _notify;

        public SavingsController(
            AppDbContext context,
            IdentityService identity,
            AuditService audit,
            NotificationService notify)
        {
            _context = context;
            _identity = identity;
            _audit = audit;
            _notify = notify;
        }

        // =========================================================
        // ADMIN / STAFF: MANUAL TRANSACTION ENTRY (LEDGER CONTROL)
        // =========================================================
        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        public async Task<IActionResult> AddTransaction(CreateSavingsDto dto)
        {
            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == dto.MemberId);

            if (member is null)
                return NotFound("Member not found");

            var lastBalance = await _context.SavingsTransactions
                .Where(s => s.MemberId == dto.MemberId)
                .OrderByDescending(s => s.SavingId)
                .Select(s => (decimal?)s.BalanceAfter)
                .FirstOrDefaultAsync() ?? 0m;

            var type = dto.TransactionType.Trim().ToLower();

            decimal newBalance;

            if (type == "deposit")
                newBalance = lastBalance + dto.Amount;

            else if (type == "withdrawal")
            {
                if (dto.Amount > lastBalance)
                    return BadRequest("Insufficient funds");

                newBalance = lastBalance - dto.Amount;
            }
            else
            {
                return BadRequest("Invalid transaction type");
            }

            var tx = new SavingsTransaction
            {
                MemberId = dto.MemberId,
                Amount = dto.Amount,
                TransactionType = type,
                BalanceAfter = newBalance,
                TransactionDate = DateTime.UtcNow
            };

            _context.SavingsTransactions.Add(tx);
            await _context.SaveChangesAsync();

            await _audit.Log(
                _identity.GetUserId(),
                $"ADMIN {type.ToUpper()}",
                "SavingsTransactions",
                tx.SavingId
            );

            await _notify.Send(
                dto.MemberId,
                "Savings Update",
                $"A {type} of {dto.Amount} was recorded. Balance: {newBalance}"
            );

            return Ok(new SavingsResponseDto
            {
                SavingId = tx.SavingId,
                Amount = tx.Amount,
                TransactionType = tx.TransactionType,
                BalanceAfter = tx.BalanceAfter,
                TransactionDate = tx.TransactionDate
            });
        }

        // =========================================================
        // MEMBER: SELF DEPOSIT (SECURE - NO MEMBER ID INPUT)
        // =========================================================
        [Authorize(Roles = "Member")]
        [HttpPost("deposit")]
        public async Task<IActionResult> MemberDeposit(MemberDepositDto dto)
        {
            var memberId = _identity.GetMemberId();

            if (memberId is null)
                return Unauthorized("Member identity missing");

            var lastBalance = await _context.SavingsTransactions
                .Where(s => s.MemberId == memberId)
                .OrderByDescending(s => s.SavingId)
                .Select(s => (decimal?)s.BalanceAfter)
                .FirstOrDefaultAsync() ?? 0m;

            var newBalance = lastBalance + dto.Amount;

            var tx = new SavingsTransaction
            {
                MemberId = memberId.Value,
                Amount = dto.Amount,
                TransactionType = "deposit",
                BalanceAfter = newBalance,
                TransactionDate = DateTime.UtcNow
            };

            _context.SavingsTransactions.Add(tx);
            await _context.SaveChangesAsync();

            await _audit.Log(
                _identity.GetUserId(),
                "MEMBER DEPOSIT",
                "SavingsTransactions",
                tx.SavingId
            );

            await _notify.Send(
                memberId.Value,
                "Deposit Successful",
                $"Deposit of {dto.Amount} received. Balance: {newBalance}"
            );

            return Ok(new
            {
                message = "Deposit successful",
                balance = newBalance
            });
        }

        // =========================================================
        // MEMBER: STATEMENT
        // =========================================================
        [Authorize(Roles = "Member")]
        [HttpGet("my-statement")]
        public async Task<IActionResult> GetMyStatement()
        {
            var memberId = _identity.GetMemberId();

            if (memberId is null)
                return Unauthorized("Member identity missing");

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


        [Authorize(Roles = "Member")]
        [HttpPost("withdraw")]
        public async Task<IActionResult> MemberWithdraw([FromBody] MemberWithdrawDto dto)
        {
            var memberId = _identity.GetMemberId();

            if (memberId is null)
                return Unauthorized("Member identity missing");

            var lastBalance = await _context.SavingsTransactions
                .Where(s => s.MemberId == memberId)
                .OrderByDescending(s => s.SavingId)
                .Select(s => (decimal?)s.BalanceAfter)
                .FirstOrDefaultAsync() ?? 0m;

            if (dto.Amount <= 0)
                return BadRequest("Amount must be greater than zero");

            if (dto.Amount > lastBalance)
                return BadRequest("Insufficient funds");

            var newBalance = lastBalance - dto.Amount;

            var tx = new SavingsTransaction
            {
                MemberId = memberId.Value,
                Amount = dto.Amount,
                TransactionType = "withdrawal",
                BalanceAfter = newBalance,
                TransactionDate = DateTime.UtcNow
            };

            _context.SavingsTransactions.Add(tx);
            await _context.SaveChangesAsync();

            await _audit.Log(
                _identity.GetUserId(),
                "MEMBER WITHDRAWAL",
                "SavingsTransactions",
                tx.SavingId
            );

            await _notify.Send(
                memberId.Value,
                "Withdrawal Successful",
                $"Withdrawal of {dto.Amount} processed. Balance: {newBalance}"
            );

            return Ok(new
            {
                message = "Withdrawal successful",
                balance = newBalance
            });
        }

        // =========================================================
        // MEMBER: CURRENT BALANCE
        // =========================================================
        [Authorize(Roles = "Member")]
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var memberId = _identity.GetMemberId();

            if (memberId is null)
                return Unauthorized("Member identity missing");

            var balance = await _context.SavingsTransactions
                .Where(s => s.MemberId == memberId)
                .OrderByDescending(s => s.SavingId)
                .Select(s => (decimal?)s.BalanceAfter)
                .FirstOrDefaultAsync() ?? 0m;

            return Ok(new { balance });
        }
    }
}