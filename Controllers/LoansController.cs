using JiSaveSacco.API.Data;
using JiSaveSacco.API.DTOs.Loans;
using JiSaveSacco.API.Models;
using JiSaveSacco.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JiSaveSacco.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoansController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditService _audit;
        private readonly NotificationService _notify;
        private readonly IdentityService _identity;

        public LoansController(
            AppDbContext context,
            AuditService audit,
            NotificationService notify,
            IdentityService identity)
        {
            _context = context;
            _audit = audit;
            _notify = notify;
            _identity = identity;
        }

        // =========================
        // GET PENDING LOANS (ADMIN)
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingLoans()
        {
            var loans = await _context.Loans
                .Where(l => l.Status == "Pending")
                .OrderByDescending(l => l.ApplicationDate)
                .ToListAsync();

            return Ok(loans);
        }

        // =========================
        // APPLY FOR LOAN (MEMBER)
        // =========================
        [Authorize(Roles = "Member")]
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyLoan(CreateLoanDto dto)
        {
            var memberId = _identity.GetMemberId();

            if (memberId is null)
                return Unauthorized("Member identity missing");

            var loan = new Loan
            {
                MemberId = memberId.Value,
                LoanAmount = dto.LoanAmount,
                InterestRate = dto.InterestRate,
                OutstandingBalance = dto.LoanAmount + (dto.LoanAmount * dto.InterestRate / 100),
                Status = "Pending",
                ApplicationDate = DateTime.UtcNow
            };

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            await _audit.Log(
                _identity.GetUserId(),
                "Applied for loan",
                "Loans",
                loan.LoanId
            );

            return Ok(new
            {
                message = "Loan application submitted",
                loanId = loan.LoanId,
                status = loan.Status
            });
        }

        // =========================
        // APPROVE LOAN (ADMIN)
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPut("approve/{loanId}")]
        public async Task<IActionResult> ApproveLoan(int loanId)
        {
            var loan = await _context.Loans.FindAsync(loanId);

            if (loan is null)
                return NotFound("Loan not found");

            if (loan.Status != "Pending")
                return BadRequest("Loan already processed");

            loan.Status = "Approved";
            loan.ApprovalDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _audit.Log(
                _identity.GetUserId(),
                "Approved loan",
                "Loans",
                loan.LoanId
            );

            await _notify.Send(
                loan.MemberId,
                "Loan Approved",
                "Your loan has been approved successfully."
            );

            return Ok("Loan approved");
        }

        // =========================
        // REJECT LOAN (ADMIN)
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPut("reject/{loanId}")]
        public async Task<IActionResult> RejectLoan(int loanId)
        {
            var loan = await _context.Loans.FindAsync(loanId);

            if (loan is null)
                return NotFound("Loan not found");

            if (loan.Status != "Pending")
                return BadRequest("Loan already processed");

            loan.Status = "Rejected";

            await _context.SaveChangesAsync();

            await _audit.Log(
                _identity.GetUserId(),
                "Rejected loan",
                "Loans",
                loan.LoanId
            );

            await _notify.Send(
                loan.MemberId,
                "Loan Rejected",
                "Your loan application was not approved."
            );

            return Ok("Loan rejected");
        }

        // =========================
        // LOAN REPAYMENT (MEMBER) — FIXED SAFETY LOGIC
        // =========================
        [Authorize(Roles = "Member")]
        [HttpPost("repay")]
        public async Task<IActionResult> RepayLoan(LoanRepaymentDto dto)
        {
            var memberId = _identity.GetMemberId();

            if (memberId is null)
                return Unauthorized("Member identity missing");

            var loan = await _context.Loans
                .FirstOrDefaultAsync(l =>
                    l.LoanId == dto.LoanId &&
                    l.MemberId == memberId);

            if (loan is null)
                return NotFound("Loan not found");

            if (loan.Status != "Approved")
                return BadRequest("Loan is not active");

            if (dto.AmountPaid <= 0)
                return BadRequest("Payment must be greater than zero");

            if (dto.AmountPaid > loan.OutstandingBalance)
                return BadRequest("Payment exceeds outstanding balance");

            // safe deduction
            loan.OutstandingBalance -= dto.AmountPaid;

            var repayment = new LoanRepayment
            {
                LoanId = loan.LoanId,
                AmountPaid = dto.AmountPaid,
                RemainingBalance = loan.OutstandingBalance,
                PaymentDate = DateTime.UtcNow
            };

            _context.LoanRepayments.Add(repayment);

            // auto close loan
            if (loan.OutstandingBalance == 0)
            {
                loan.Status = "Paid";
            }

            await _context.SaveChangesAsync();

            await _audit.Log(
                _identity.GetUserId(),
                "Loan repayment",
                "LoanRepayments",
                repayment.RepaymentId
            );

            await _notify.Send(
                loan.MemberId,
                "Payment Received",
                $"You paid {dto.AmountPaid}. Remaining balance: {loan.OutstandingBalance}"
            );

            return Ok(new
            {
                message = "Repayment successful",
                remaining = loan.OutstandingBalance
            });
        }

        // =========================
        // GET MY LOANS (MEMBER)
        // =========================
        [Authorize(Roles = "Member")]
        [HttpGet("my-loans")]
        public async Task<IActionResult> GetMyLoans()
        {
            var memberId = _identity.GetMemberId();

            if (memberId is null)
                return Unauthorized("Member identity missing");

            var loans = await _context.Loans
                .Where(l => l.MemberId == memberId)
                .OrderByDescending(l => l.ApplicationDate)
                .ToListAsync();

            return Ok(loans);
        }
    }
}