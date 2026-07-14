using JiSaveSacco.API.Data;
using JiSaveSacco.API.Models;
using JiSaveSacco.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JiSaveSacco.API.DTOs;          
using JiSaveSacco.API.DTOs.Savings;  

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
        // MEMBER: GET MY PERSONAL SAVINGS TRANSACTION HISTORY TRAIL
        // =========================================================
        [Authorize(Roles = "Member")]
        [HttpGet("my-transactions")]
        public async Task<IActionResult> GetMyTransactions()
        {
            var memberId = _identity.GetMemberId();
            if (memberId is null)
                return Unauthorized("Member identity context missing.");

            // Pull transactions belonging to this member, sorted newest first
            var transactions = await _context.SavingsTransactions
                .Where(t => t.MemberId == memberId)
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new
                {
                   
                    TransactionDate = t.TransactionDate,
                    TransactionType = t.TransactionType,
                    Amount = t.Amount,
                    BalanceAfter = t.BalanceAfter
                })
                .ToListAsync();

            return Ok(transactions);
        }

        // =========================================================
        // ADMIN / STAFF: GET ALL MEMBER SAVINGS WITH DETAILS
        // =========================================================
        [Authorize(Roles = "Admin,Staff")]
        [HttpGet("admin-overview")]
        public async Task<IActionResult> GetAdminSavingsOverview()
        {
            var overview = await _context.Members
                .Select(m => new
                {
                    m.MemberId,
                    m.MemberNo,
                    FullName = m.FirstName + " " + m.LastName,
                    m.Phone,
                    m.Email,
                    m.Status,
                    TotalSaved = _context.SavingsTransactions
                        .Where(s => s.MemberId == m.MemberId)
                        .OrderByDescending(s => s.SavingId)
                        .Select(s => s.BalanceAfter)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(overview);
        }

        // =========================================================
        // MEMBER: SELF DEPOSIT (SECURE)
        // =========================================================
        [Authorize(Roles = "Member")]
        [HttpPost("deposit")]
        public async Task<IActionResult> MemberDeposit([FromBody] MemberDepositDto dto)
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

            await _audit.Log(_identity.GetUserId(), "MEMBER DEPOSIT", "SavingsTransactions", tx.SavingId);
            await _notify.Send(
                    memberId.Value,
                    "Deposit Successful",
                    $"Deposit of Ksh {dto.Amount:N2} received. Balance: Ksh {newBalance:N2}"
                );

            return Ok(new { message = "Deposit successful", balance = newBalance });
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

        // =========================================================
        // MEMBER: WITHDRAW
        // =========================================================
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

            await _audit.Log(_identity.GetUserId(), "MEMBER WITHDRAWAL", "SavingsTransactions", tx.SavingId);
            await _notify.Send(
                    memberId.Value,
                    "Withdrawal Successful",
                    $"Withdrawal of Ksh {dto.Amount:N2} processed. Balance: Ksh {newBalance:N2}"
                );

            return Ok(new { message = "Withdrawal successful", balance = newBalance });
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